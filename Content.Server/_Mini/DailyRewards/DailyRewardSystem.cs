// SPDX-FileCopyrightText: 2026 Casha
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Mini.AntagTokens;
using Content.Shared._Mini.AntagTokens;
using Content.Shared._Mini.DailyRewards;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Mini.DailyRewards;

public sealed class DailyRewardSystem : EntitySystem
{
    private const string StreakRewardIconPath = "/Textures/_Mini/DailyRewards/streak.png";

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AntagTokenSystem _antagTokens = default!;
    private readonly Dictionary<NetUserId, SessionRewardState> _states = new();
    private readonly DailyRewardComponent _defaultComponent = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<DailyRewardOpenRequestEvent>(OnOpenRequest);
        SubscribeNetworkEvent<DailyRewardClaimRequestEvent>(OnClaimRequest);

        _userDb.AddOnLoadPlayer(LoadPlayerData);
        _userDb.AddOnPlayerDisconnect(OnPlayerDisconnect);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        SaveAll();
    }

    public bool TryGetDebugState(NetUserId userId, [NotNullWhen(true)] out DailyRewardProgress? progress)
    {
        var state = EnsureStateExists(userId);
        if (state != null)
        {
            progress = state.Progress;
            EnsureCurrentDay(progress, DateTime.UtcNow);
            return true;
        }

        progress = null;
        return false;
    }

    public bool SetTodayActiveTime(NetUserId userId, TimeSpan activeTime)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        EnsureCurrentDay(state.Progress, DateTime.UtcNow);
        state.Progress.PendingActiveTime = activeTime < TimeSpan.Zero ? TimeSpan.Zero : activeTime;
        _ = _db.UpsertDailyRewardProgress(state.Progress);
        return true;
    }

    public bool SetStreak(NetUserId userId, int streak)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        var component = TryGetComponentFor(userId) ?? _defaultComponent;
        state.Progress.CurrentStreak = Math.Clamp(streak, 0, component.MaxStreak);
        _ = _db.UpsertDailyRewardProgress(state.Progress);
        return true;
    }

    public bool SetLastClaimTime(NetUserId userId, DateTime? lastClaimTimeUtc)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        state.Progress.LastClaimTime = lastClaimTimeUtc;
        _ = _db.UpsertDailyRewardProgress(state.Progress);
        return true;
    }

    public bool MakeReadyToClaim(NetUserId userId, DailyRewardComponent? component = null)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        component ??= TryGetComponentFor(userId) ?? _defaultComponent;

        EnsureCurrentDay(state.Progress, DateTime.UtcNow);
        state.Progress.PendingActiveTime = component.MinimumActiveTime;
        state.Progress.LastClaimTime = DateTime.UtcNow - component.ClaimCooldown - TimeSpan.FromMinutes(1);
        _ = _db.UpsertDailyRewardProgress(state.Progress);
        return true;
    }

    public bool ResetProgress(NetUserId userId)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        state.Progress.CurrentStreak = 0;
        state.Progress.LastClaimTime = null;
        state.Progress.PendingActiveDate = DateTime.UtcNow.Date;
        state.Progress.PendingActiveTime = TimeSpan.Zero;
        state.ActiveSince = null;
        state.ActiveStartedAtUtc = null;
        _ = _db.UpsertDailyRewardProgress(state.Progress);
        return true;
    }

    public bool TryOpenForSession(ICommonSession session)
    {
        EnsureStateExists(session.UserId);
        SendState(session);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var session in _playerManager.Sessions)
        {
            if (!_states.ContainsKey(session.UserId))
                continue;

            SendState(session);
        }
    }

    private async Task LoadPlayerData(ICommonSession player, CancellationToken cancel)
    {
        var progress = await _db.GetDailyRewardProgress(player.UserId.UserId, cancel);
        _states[player.UserId] = new SessionRewardState(progress ?? new DailyRewardProgress
        {
            PlayerId = player.UserId.UserId,
            CurrentStreak = 0,
            PendingActiveDate = DateTime.UtcNow.Date,
            PendingActiveTime = TimeSpan.Zero,
        });
    }

    private void OnPlayerDisconnect(ICommonSession player)
    {
        FlushActiveSegment(player);

        if (_states.TryGetValue(player.UserId, out var state))
            _ = _db.UpsertDailyRewardProgress(state.Progress);

        _states.Remove(player.UserId);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<DailyRewardComponent>(ev.Mob);
        StartTracking(ev.Player);
        SendState(ev.Player);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        StartTracking(ev.Player);

        if (ev.Entity is not { Valid: true } uid)
            return;

        EnsureComp<DailyRewardComponent>(uid);
        SendState(ev.Player);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        FlushActiveSegment(ev.Player);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        FlushActiveSegment(ev.PlayerSession);
        StartTracking(ev.PlayerSession);

        if (ev.PlayerSession.AttachedEntity is { Valid: true } uid)
            EnsureComp<DailyRewardComponent>(uid);

        SendState(ev.PlayerSession);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent _)
    {
        SaveAll();
    }

    private void OnOpenRequest(DailyRewardOpenRequestEvent _, EntitySessionEventArgs args)
    {
        TryOpenForSession(args.SenderSession);
    }

    private void OnClaimRequest(DailyRewardClaimRequestEvent _, EntitySessionEventArgs args)
    {
        ClaimReward(args.SenderSession);
    }

    private void StartTracking(ICommonSession session)
    {
        if (!_states.TryGetValue(session.UserId, out var state))
            return;

        EnsureCurrentDay(state.Progress, DateTime.UtcNow);

        if (state.ActiveSince != null || session.Status != SessionStatus.InGame || session.AttachedEntity == null)
            return;

        state.ActiveSince = _timing.CurTime;
        state.ActiveStartedAtUtc = DateTime.UtcNow;
    }

    private void FlushActiveSegment(ICommonSession session)
    {
        if (!_states.TryGetValue(session.UserId, out var state) || state.ActiveSince == null)
            return;

        var nowUtc = DateTime.UtcNow;
        var startedAtUtc = state.ActiveStartedAtUtc ?? nowUtc;
        AccumulateActiveTime(state.Progress, startedAtUtc, nowUtc);
        state.ActiveSince = null;
        state.ActiveStartedAtUtc = null;
    }

    private void SaveAll()
    {
        foreach (var session in _playerManager.Sessions)
        {
            FlushActiveSegment(session);
        }

        foreach (var state in _states.Values)
        {
            _ = _db.UpsertDailyRewardProgress(state.Progress);
        }
    }

    private void ClaimReward(ICommonSession session)
    {
        if (!_states.TryGetValue(session.UserId, out var state))
            return;

        FlushActiveSegment(session);
        var component = GetConfigFor(session);

        var now = DateTime.UtcNow;
        if (!CanClaim(state.Progress, component, now, out var nextDay))
        {
            if (session.AttachedEntity is { Valid: true } uid)
                _popup.PopupEntity("Ежедневная награда пока недоступна.", uid, uid);

            SendState(session);
            return;
        }

        if (state.Progress.LastClaimTime != null &&
            now - state.Progress.LastClaimTime.Value > component.ExpirationWindow)
        {
            state.Progress.CurrentStreak = 0;
        }

        var reward = GetRewardPreview(component, nextDay);
        if (reward.TokenAmount > 0)
        {
            _antagTokens.AddBalance(session.UserId, reward.TokenAmount, out var grantedAmount, out var note);

            if (session.AttachedEntity is { Valid: true } uid)
            {
                var message = grantedAmount > 0
                    ? $"Получено токенов: {grantedAmount}."
                    : "Токены по этой награде не начислены.";

                if (!string.IsNullOrWhiteSpace(note))
                    message = $"{message} {note}";

                _popup.PopupEntity(
                    message,
                    uid,
                    uid);
            }
        }
        else
        {
            if (session.AttachedEntity is { Valid: true } uid)
                _popup.PopupEntity($"Ежедневная награда за день {nextDay} получена.", uid, uid);
        }

        state.Progress.CurrentStreak = nextDay;
        state.Progress.LastClaimTime = now;
        state.Progress.PendingActiveTime = TimeSpan.Zero;

        _ = _db.UpsertDailyRewardProgress(state.Progress);

        StartTracking(session);
        SendState(session);
    }

    private bool CanClaim(DailyRewardProgress progress, DailyRewardComponent component, DateTime now, out int nextDay)
    {
        EnsureCurrentDay(progress, now);

        var currentStreak = progress.CurrentStreak;
        if (progress.LastClaimTime != null && now - progress.LastClaimTime.Value > component.ExpirationWindow)
            currentStreak = 0;

        nextDay = Math.Clamp(currentStreak + 1, 1, component.MaxStreak);

        if (progress.LastClaimTime != null && now - progress.LastClaimTime.Value < component.ClaimCooldown)
            return false;

        return progress.PendingActiveTime >= component.MinimumActiveTime;
    }

    private void SendState(ICommonSession session)
    {
        if (!_states.TryGetValue(session.UserId, out var state))
            return;

        var component = GetConfigFor(session);
        var now = DateTime.UtcNow;
        EnsureCurrentDay(state.Progress, now);

        var pending = state.Progress.PendingActiveTime;
        if (state.ActiveSince != null)
            pending = GetCurrentDayActiveTime(state.Progress, state.ActiveStartedAtUtc, now);

        var lastClaim = state.Progress.LastClaimTime;
        var visibleStreak = state.Progress.CurrentStreak;
        if (lastClaim != null && now - lastClaim.Value > component.ExpirationWindow)
            visibleStreak = 0;

        var nextDay = Math.Clamp(visibleStreak + 1, 1, component.MaxStreak);
        var timeUntilExpiration = lastClaim == null
            ? component.ExpirationWindow
            : MaxZero(component.ExpirationWindow - (now - lastClaim.Value));
        var timeUntilNextClaim = lastClaim == null
            ? TimeSpan.Zero
            : MaxZero(component.ClaimCooldown - (now - lastClaim.Value));
        var canClaim = pending >= component.MinimumActiveTime && timeUntilNextClaim == TimeSpan.Zero;

        var rewards = new List<DailyRewardEntry>(component.MaxStreak);
        for (var day = 1; day <= component.MaxStreak; day++)
        {
            var reward = GetRewardPreview(component, day);
            rewards.Add(new DailyRewardEntry(
                day,
                reward.DisplayName,
                reward.TokenAmount > 0,
                reward.IconPath,
                day <= visibleStreak,
                day == nextDay));
        }

        RaiseNetworkEvent(new DailyRewardStateEvent(new DailyRewardUpdateMessage(
            visibleStreak,
            nextDay,
            canClaim,
            state.ActiveSince != null,
            lastClaim != null,
            timeUntilExpiration,
            timeUntilNextClaim,
            pending,
            component.MinimumActiveTime,
            rewards)), session);
    }

    private static RewardDefinition GetRewardPreview(DailyRewardComponent component, int day)
    {
        var tokenAmount = GetRewardAmount(component, day);
        var displayName = tokenAmount > 0
            ? $"+{tokenAmount} ток."
            : "Прогресс стрика";

        return new RewardDefinition(displayName, tokenAmount, StreakRewardIconPath);
    }

    private static int GetRewardAmount(DailyRewardComponent component, int day)
    {
        var amount = 0;

        if (component.BaseRewardEveryDays > 0 && day % component.BaseRewardEveryDays == 0)
            amount += component.BaseRewardAmount;

        if (component.BonusTokenRewards.TryGetValue(day, out var bonus))
            amount += bonus;

        return amount;
    }

    private static TimeSpan MaxZero(TimeSpan span)
    {
        return span < TimeSpan.Zero ? TimeSpan.Zero : span;
    }

    private DailyRewardComponent? TryGetComponentFor(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session) ||
            session.AttachedEntity is not { Valid: true } uid)
        {
            return null;
        }

        return EnsureComp<DailyRewardComponent>(uid);
    }

    private DailyRewardComponent GetConfigFor(ICommonSession session)
    {
        if (session.AttachedEntity is { Valid: true } uid)
            return EnsureComp<DailyRewardComponent>(uid);

        return _defaultComponent;
    }

    private SessionRewardState? EnsureStateExists(NetUserId userId)
    {
        if (_states.TryGetValue(userId, out var existing))
            return existing;

        if (!_playerManager.TryGetSessionById(userId, out var session))
            return null;

        var state = new SessionRewardState(new DailyRewardProgress
        {
            PlayerId = session.UserId.UserId,
            CurrentStreak = 0,
            PendingActiveDate = DateTime.UtcNow.Date,
            PendingActiveTime = TimeSpan.Zero,
        });

        _states[userId] = state;
        return state;
    }

    private static void EnsureCurrentDay(DailyRewardProgress progress, DateTime nowUtc)
    {
        var today = nowUtc.Date;
        if (progress.PendingActiveDate?.Date == today)
            return;

        progress.PendingActiveDate = today;
        progress.PendingActiveTime = TimeSpan.Zero;
    }

    private static void AccumulateActiveTime(DailyRewardProgress progress, DateTime startedAtUtc, DateTime endedAtUtc)
    {
        if (endedAtUtc <= startedAtUtc)
            return;

        var current = startedAtUtc;
        while (current < endedAtUtc)
        {
            var dayStart = current.Date;
            var nextDay = dayStart.AddDays(1);
            var segmentEnd = endedAtUtc < nextDay ? endedAtUtc : nextDay;

            EnsureCurrentDay(progress, current);
            progress.PendingActiveTime += segmentEnd - current;

            current = segmentEnd;
        }

        EnsureCurrentDay(progress, endedAtUtc);
    }

    private static TimeSpan GetCurrentDayActiveTime(DailyRewardProgress progress, DateTime? activeStartedAtUtc, DateTime nowUtc)
    {
        EnsureCurrentDay(progress, nowUtc);

        if (activeStartedAtUtc == null || activeStartedAtUtc >= nowUtc)
            return progress.PendingActiveTime;

        var todayStart = nowUtc.Date;
        var effectiveStart = activeStartedAtUtc.Value < todayStart ? todayStart : activeStartedAtUtc.Value;
        return progress.PendingActiveTime + (nowUtc - effectiveStart);
    }

    private sealed class SessionRewardState(DailyRewardProgress progress)
    {
        public DailyRewardProgress Progress { get; } = progress;
        public TimeSpan? ActiveSince { get; set; }
        public DateTime? ActiveStartedAtUtc { get; set; }
    }

    private readonly record struct RewardDefinition(string? DisplayName, int TokenAmount, string IconPath);
}
