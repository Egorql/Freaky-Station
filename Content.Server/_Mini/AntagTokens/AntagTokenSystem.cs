// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Mini.AntagTokens.Components;
using Content.Shared._Mini.AntagTokens;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Server.Player;

namespace Content.Server._Mini.AntagTokens;

public sealed class AntagTokenSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private readonly Dictionary<NetUserId, PlayerTokenState> _states = new();
    private readonly Dictionary<EntityUid, ReservedGhostRuleState> _reservedGhostRules = new();
    private readonly Dictionary<NetUserId, int?> _sponsorLevelOverrides = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagTokenOpenRequestEvent>(OnOpenRequest);
        SubscribeNetworkEvent<AntagTokenPurchaseRequestEvent>(OnPurchaseRequest);
        SubscribeNetworkEvent<AntagTokenClearRequestEvent>(OnClearRequest);

        SubscribeLocalEvent<AntagSelectionComponent, AntagSelectionExcludeSessionEvent>(OnExcludeReservedSession);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnJoinedLobby);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRoundstartJobsAssigned, after: new[] { typeof(AntagSelectionSystem) });
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GameRuleComponent, GameRuleEndedEvent>(OnReservedGhostRuleEnded);
        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, ComponentStartup>(OnAntagSpawnerStartup);
        SubscribeLocalEvent<ReservedGhostRoleComponent, TakeGhostRoleEvent>(OnReservedGhostTakeRole, before: new[] { typeof(GhostRoleSystem) });
        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnReservedGhostSpawnerUsed);

        _userDb.AddOnLoadPlayer(LoadPlayerData);
        _userDb.AddOnPlayerDisconnect(OnPlayerDisconnect);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        SaveAll();
    }

    public bool AddBalance(NetUserId userId, int amount, out int grantedAmount, out string? note)
    {
        grantedAmount = 0;
        note = null;

        if (amount <= 0)
            return false;

        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);

        var cap = GetMonthlyCap(userId);
        var available = cap.HasValue ? Math.Max(0, cap.Value - state.MonthlyEarned) : amount;
        grantedAmount = Math.Min(amount, available);

        if (grantedAmount > 0)
        {
            state.Balance += grantedAmount;
            if (cap.HasValue)
                state.MonthlyEarned += grantedAmount;
        }

        if (grantedAmount < amount)
            note = "Достигнут месячный лимит токенов для вашего уровня поддержки.";

        PersistState(userId, state);
        SendState(userId);
        return grantedAmount > 0 || note != null;
    }

    public bool AddBalance(NetUserId userId, int amount)
    {
        return AddBalance(userId, amount, out _, out _);
    }

    public bool SetBalance(NetUserId userId, int amount)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        state.Balance = Math.Max(0, amount);
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public bool SetMonthlyEarned(NetUserId userId, int amount)
    {
        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);
        state.MonthlyEarned = Math.Max(0, amount);
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public void SetSponsorLevelOverride(NetUserId userId, int? sponsorLevel)
    {
        if (sponsorLevel is <= 0)
            _sponsorLevelOverrides.Remove(userId);
        else
            _sponsorLevelOverrides[userId] = sponsorLevel;

        SendState(userId);
    }

    public int GetEffectiveSponsorLevel(NetUserId userId)
    {
        if (_sponsorLevelOverrides.TryGetValue(userId, out var overrideLevel) &&
            overrideLevel is > 0)
        {
            return overrideLevel.Value;
        }

        return EntitySystem.Get<SponsorSystem>().Sponsors
            .FirstOrDefault(s => s.Uid == userId.UserId.ToString()).Level;
    }

    public bool TryOpenForSession(ICommonSession session)
    {
        if (EnsureStateExists(session.UserId) == null)
            return false;

        SendState(session.UserId);
        return true;
    }

    public bool TryGetDebugState(NetUserId userId, [NotNullWhen(true)] out PlayerTokenState? state)
    {
        state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);
        return true;
    }

    public bool TryPurchaseForSession(ICommonSession session, string roleId, out string? error)
    {
        error = null;

        if (!AntagTokenCatalog.TryGetRole(roleId, out var role))
        {
            error = "Такой роли нет в магазине.";
            return false;
        }

        var state = EnsureStateExists(session.UserId);
        if (state == null)
        {
            error = "Профиль токенов ещё не загружен.";
            return false;
        }

        if (role.Mode == AntagPurchaseMode.Unavailable)
        {
            error = "Эта роль пока не настроена на сервере.";
            return false;
        }

        if (state.Balance < role.Cost)
        {
            error = "Недостаточно токенов.";
            return false;
        }

        if (role.Mode == AntagPurchaseMode.LobbyDeposit)
        {
            if (state.PendingDepositRoleId == role.Id)
            {
                error = "Эта роль уже выбрана и ожидает раунда.";
                return false;
            }

            if (state.PendingDepositRoleId != null)
            {
                error = "Сначала снимите текущий депозит роли.";
                return false;
            }

            if (IsRoleSaturated(role.Id, session.UserId))
            {
                error = "Лимит заявок на эту роль уже занят.";
                return false;
            }

            state.Balance -= role.Cost;
            state.PendingDepositRoleId = role.Id;
            PersistState(session.UserId, state);
            SendState(session.UserId);
            return true;
        }


        if (role.GameRuleId == null || !_gameTicker.StartGameRule(role.GameRuleId, out var ruleEntity))
        {
            error = "Не удалось запустить событие для этой роли.";
            return false;
        }

        state.Balance -= role.Cost;
        PersistState(session.UserId, state);
        _reservedGhostRules[ruleEntity] = new ReservedGhostRuleState(session.UserId, role.Id);
        MarkReservedGhostSpawners(ruleEntity, session.UserId);
        SendState(session.UserId);
        return true;
    }

    public bool ClearDeposit(NetUserId userId, out string? error)
    {
        error = null;
        var state = EnsureStateExists(userId);
        if (state == null)
        {
            error = "Профиль токенов ещё не загружен.";
            return false;
        }

        if (state.PendingDepositRoleId == null)
        {
            error = "Сейчас нет активного депозита.";
            return false;
        }

        if (!AntagTokenCatalog.TryGetRole(state.PendingDepositRoleId, out var role))
        {
            state.PendingDepositRoleId = null;
            PersistState(userId, state);
            SendState(userId);
            return true;
        }

        state.Balance += role.Cost;
        state.PendingDepositRoleId = null;
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    private async Task LoadPlayerData(ICommonSession player, CancellationToken cancel)
    {
        var tokenEntries = await _db.GetPlayerAntagTokens(player.UserId.UserId, cancel);
        var selection = await _db.GetPlayerAntagTokenSelection(player.UserId.UserId, cancel);

        var state = new PlayerTokenState();
        foreach (var token in tokenEntries)
        {
            switch (token.TokenId)
            {
                case AntagTokenCatalog.BalanceEntryId:
                    state.Balance = Math.Max(0, token.Amount);
                    break;
                case AntagTokenCatalog.MonthlyEarnedEntryId:
                    state.MonthlyEarned = Math.Max(0, token.Amount);
                    break;
                case AntagTokenCatalog.MonthlyYearEntryId:
                    state.MonthlyYear = token.Amount;
                    break;
                case AntagTokenCatalog.MonthlyMonthEntryId:
                    state.MonthlyMonth = token.Amount;
                    break;
            }
        }

        if (selection?.TokenId == AntagTokenCatalog.DepositSelectionTokenId &&
            selection.AntagId is { Length: > 0 } roleId &&
            AntagTokenCatalog.TryGetRole(roleId, out var role) &&
            role.Mode == AntagPurchaseMode.LobbyDeposit)
        {
            state.PendingDepositRoleId = roleId;
        }

        NormalizeMonthlyState(state, DateTime.UtcNow);
        _states[player.UserId] = state;
    }

    private void OnPlayerDisconnect(ICommonSession player)
    {
        if (_states.TryGetValue(player.UserId, out var state))
            PersistState(player.UserId, state);

        _states.Remove(player.UserId);
    }

    private void OnJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        SendState(ev.PlayerSession.UserId);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent _)
    {
        _reservedGhostRules.Clear();
        SaveAll();
    }

    private void OnRoundstartJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        foreach (var session in ev.Players)
        {
            if (!TryGetPendingLobbyRole(session.UserId, out var role))
                continue;

            if (IsReservedRoleBlockedByCurrentJob(session))
            {
                ShowPopup(session, "Текущая должность из Command/Security блокирует токен-роль. Резерв сохранён до подходящего раунда.");
                SendState(session.UserId);
                continue;
            }

            if (!TryAssignReservedRoundstartRole(session, role, out var error))
            {
                ShowPopup(session, error ?? "Не удалось выдать зарезервированную роль. Резерв сохранён.");
                SendState(session.UserId);
                continue;
            }

            var state = EnsureStateExists(session.UserId);
            if (state == null)
                continue;

            state.PendingDepositRoleId = null;
            PersistState(session.UserId, state);
            SendState(session.UserId);
            ShowPopup(session, $"Зарезервированная роль \"{GetRoleName(role)}\" выдана.");
        }
    }

    private void OnAntagSpawnerStartup(Entity<GhostRoleAntagSpawnerComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Rule is not { } rule ||
            !_reservedGhostRules.TryGetValue(rule, out var reservedState))
        {
            return;
        }

        var reserved = EnsureComp<ReservedGhostRoleComponent>(ent);
        reserved.ReservedUserId = reservedState.UserId;
    }

    private void OnReservedGhostRuleEnded(Entity<GameRuleComponent> ent, ref GameRuleEndedEvent args)
    {
        if (!_reservedGhostRules.Remove(ent, out var reservedState))
            return;

        if (!AntagTokenCatalog.TryGetRole(reservedState.RoleId, out var role))
            return;

        var state = EnsureStateExists(reservedState.UserId);
        if (state == null)
            return;

        state.Balance += role.Cost;
        PersistState(reservedState.UserId, state);
        SendState(reservedState.UserId);

        if (_playerManager.TryGetSessionById(reservedState.UserId, out var session))
            ShowPopup(session, $"Событие для роли \"{GetRoleName(role)}\" не состоялось. Токены возвращены.");
    }

    private void OnOpenRequest(AntagTokenOpenRequestEvent _, EntitySessionEventArgs args)
    {
        SendState(args.SenderSession.UserId);
    }

    private void OnPurchaseRequest(AntagTokenPurchaseRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!TryPurchaseForSession(args.SenderSession, ev.RoleId, out var error))
        {
            ShowPopup(args.SenderSession, error ?? "Покупка недоступна.");
            SendState(args.SenderSession.UserId);
            return;
        }

        if (!AntagTokenCatalog.TryGetRole(ev.RoleId, out var role))
            return;

        var message = role.Mode == AntagPurchaseMode.GhostRule
            ? "Событие запущено. Только вы сможете занять эту гост-роль."
            : "Роль задепана на следующий подходящий раунд.";

        ShowPopup(args.SenderSession, message);
        SendState(args.SenderSession.UserId);
    }

    private void OnClearRequest(AntagTokenClearRequestEvent _, EntitySessionEventArgs args)
    {
        if (!ClearDeposit(args.SenderSession.UserId, out var error))
        {
            ShowPopup(args.SenderSession, error ?? "Не удалось снять депозит.");
            return;
        }

        ShowPopup(args.SenderSession, "Депозит роли снят, токены возвращены.");
        SendState(args.SenderSession.UserId);
    }

    private void OnExcludeReservedSession(Entity<AntagSelectionComponent> _, ref AntagSelectionExcludeSessionEvent args)
    {
        args.Excluded = HasPendingLobbyDeposit(args.Session.UserId);
    }

    private void OnReservedGhostTakeRole(Entity<ReservedGhostRoleComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.Player.UserId == ent.Comp.ReservedUserId)
            return;

        ShowPopup(args.Player, "Эта гост-роль зарезервирована другим игроком.");
        args.TookRole = true;
    }


    private void OnReservedGhostSpawnerUsed(Entity<GhostRoleAntagSpawnerComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        if (ent.Comp.Rule is not { } rule)
            return;

        _reservedGhostRules.Remove(rule);
        RemCompDeferred<ReservedGhostRoleComponent>(ent);
    }

    private void MarkReservedGhostSpawners(EntityUid ruleEntity, NetUserId reservedUserId)
    {
        var query = EntityQueryEnumerator<GhostRoleAntagSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Rule != ruleEntity)
                continue;

            var reserved = EnsureComp<ReservedGhostRoleComponent>(uid);
            reserved.ReservedUserId = reservedUserId;
        }
    }

    private bool TryGetPendingLobbyRole(NetUserId userId, [NotNullWhen(true)] out AntagRoleDefinition? role)
    {
        role = null;

        if (!_states.TryGetValue(userId, out var state) ||
            state.PendingDepositRoleId == null ||
            !AntagTokenCatalog.TryGetRole(state.PendingDepositRoleId, out var selectedRole) ||
            selectedRole.Mode != AntagPurchaseMode.LobbyDeposit ||
            selectedRole.AntagId == null ||
            selectedRole.GameRuleId == null)
        {
            return false;
        }

        role = selectedRole;
        return true;
    }

    private static bool MatchesDefinition(string antagId, AntagSelectionDefinition definition)
    {
        return definition.PrefRoles.Contains(antagId) || definition.FallbackRoles.Contains(antagId);
    }

    private bool HasPendingLobbyDeposit(NetUserId userId)
    {
        return TryGetPendingLobbyRole(userId, out _);
    }

    private bool TryAssignReservedRoundstartRole(ICommonSession session, AntagRoleDefinition role, out string? error)
    {
        error = null;

        if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            error = "Игрок сейчас не в валидной сессии для выдачи токен-роли.";
            return false;
        }

        if (session.AttachedEntity is not { Valid: true })
        {
            error = "У игрока ещё нет валидной сущности для выдачи токен-роли.";
            return false;
        }

        if (_mind.TryGetMind(session, out var mindId, out _) && _role.MindIsAntagonist(mindId))
        {
            error = "Игрок уже получил антагонистическую роль другим путём. Резерв сохранён.";
            return false;
        }

        var ruleEntity = _gameTicker.AddGameRule(role.GameRuleId!);
        if (!TryComp<AntagSelectionComponent>(ruleEntity, out var selection))
        {
            error = "У токен-правила нет AntagSelectionComponent.";
            return false;
        }

        if (!TryFindMatchingDefinition(selection, role.AntagId!, out var definition))
        {
            error = "В токен-правиле не найден подходящий antag definition.";
            return false;
        }

        var matchedDefinition = definition
            ?? throw new InvalidOperationException("Matched antag definition unexpectedly null.");
        _antagSelection.MakeAntag((ruleEntity, selection), session, matchedDefinition);
        return true;
    }

    private static bool TryFindMatchingDefinition(
        AntagSelectionComponent selection,
        string antagId,
        [NotNullWhen(true)] out AntagSelectionDefinition? definition)
    {
        foreach (var def in selection.Definitions)
        {
            if (!MatchesDefinition(antagId, def))
                continue;

            definition = def;
            return true;
        }

        definition = null;
        return false;
    }

    private bool IsReservedRoleBlockedByCurrentJob(ICommonSession session)
    {
        if (!_mind.TryGetMind(session, out var mindId, out _) ||
            !_jobs.MindTryGetJobId(mindId, out var jobId) ||
            jobId == null)
        return false;

        if (!_jobs.TryGetAllDepartments(jobId.Value, out var departments))
            return false;

        return departments.Any(d => d.ID is "Command" or "Security");
    }

    private void SendState(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session) ||
            !_states.TryGetValue(userId, out var state))
        {
            return;
        }

        NormalizeMonthlyState(state, DateTime.UtcNow);

        var roles = new List<AntagTokenRoleEntry>(AntagTokenCatalog.Roles.Count);
        foreach (var role in AntagTokenCatalog.Roles.Values)
        {
            var purchased = state.PendingDepositRoleId == role.Id;
            var saturated = role.Mode == AntagPurchaseMode.LobbyDeposit && !purchased && IsRoleSaturated(role.Id, userId);
            var available = role.Mode != AntagPurchaseMode.Unavailable;
            var canAfford = state.Balance >= role.Cost;

            string? statusLocKey = null;
            if (purchased)
                statusLocKey = "antag-store-status-deposited";
            else if (!available)
                statusLocKey = role.UnavailableReasonLocKey ?? "antag-store-status-unavailable";
            else if (saturated)
                statusLocKey = "antag-store-status-saturated";
            else if (!canAfford)
                statusLocKey = "antag-store-status-not-enough";
            else if (state.PendingDepositRoleId != null && role.Mode == AntagPurchaseMode.LobbyDeposit)
                statusLocKey = "antag-store-status-has-other-deposit";

            roles.Add(new AntagTokenRoleEntry(
                role.Id,
                role.Cost,
                role.Mode,
                purchased,
                canAfford,
                saturated,
                available,
                statusLocKey));
        }

        var payload = new AntagTokenState(
            state.Balance,
            state.MonthlyEarned,
            GetMonthlyCap(userId),
            state.PendingDepositRoleId,
            roles);

        RaiseNetworkEvent(new AntagTokenStateEvent(payload), session);
    }

    private void PersistState(NetUserId userId, PlayerTokenState state)
    {
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.BalanceEntryId, state.Balance);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyEarnedEntryId, state.MonthlyEarned);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyYearEntryId, state.MonthlyYear);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyMonthEntryId, state.MonthlyMonth);

        if (state.PendingDepositRoleId == null)
        {
            _ = _db.ClearPlayerAntagTokenSelection(userId.UserId);
        }
        else
        {
            _ = _db.SetPlayerAntagTokenSelection(userId.UserId, AntagTokenCatalog.DepositSelectionTokenId, state.PendingDepositRoleId);
        }
    }

    private void SaveAll()
    {
        foreach (var (userId, state) in _states)
        {
            PersistState(userId, state);
        }
    }

    private PlayerTokenState? EnsureStateExists(NetUserId userId)
    {
        if (_states.TryGetValue(userId, out var state))
            return state;

        if (!_playerManager.TryGetSessionById(userId, out _))
            return null;

        state = new PlayerTokenState();
        NormalizeMonthlyState(state, DateTime.UtcNow);
        _states[userId] = state;
        return state;
    }

    private void NormalizeMonthlyState(PlayerTokenState state, DateTime nowUtc)
    {
        if (state.MonthlyYear == nowUtc.Year && state.MonthlyMonth == nowUtc.Month)
            return;

        state.MonthlyYear = nowUtc.Year;
        state.MonthlyMonth = nowUtc.Month;
        state.MonthlyEarned = 0;
    }

    private int? GetMonthlyCap(NetUserId userId)
    {
        var sponsorLevel = GetEffectiveSponsorLevel(userId);

        return sponsorLevel <= 0 ? null : AntagTokenCatalog.GetSponsorMonthlyCap(sponsorLevel);
    }

    private bool IsRoleSaturated(string roleId, NetUserId exceptUserId)
    {
        var connectedPlayers = _playerManager.Sessions.Count(s => s.Status is not (SessionStatus.Disconnected or SessionStatus.Zombie));
        var maxDeposits = Math.Max(1, connectedPlayers / 10);
        var currentDeposits = _states
            .Where(kv => kv.Key != exceptUserId)
            .Count(kv => kv.Value.PendingDepositRoleId == roleId);

        return currentDeposits >= maxDeposits;
    }

    private static string GetRoleName(AntagRoleDefinition role)
    {
        return role.Id switch
        {
            AntagTokenCatalog.ThiefRole => "Вор",
            AntagTokenCatalog.AgentRole => "Агент",
            AntagTokenCatalog.NinjaRole => "Ниндзя",
            AntagTokenCatalog.DragonRole => "Космический дракон",
            AntagTokenCatalog.AbductorRole => "Абдуктор",
            AntagTokenCatalog.InitialInfectedRole => "Нулевой заражённый",
            AntagTokenCatalog.RevenantRole => "Ревенант",
            AntagTokenCatalog.YaoRole => "Яо",
            AntagTokenCatalog.HeadRevRole => "Глава революции",
            AntagTokenCatalog.CosmicCultRole => "Космический культист",
            AntagTokenCatalog.DevilRole => "Дьявол",
            AntagTokenCatalog.BlobRole => "Блоб",
            AntagTokenCatalog.WizardRole => "Маг",
            AntagTokenCatalog.SlaughterDemonRole => "Демон резни",
            AntagTokenCatalog.ChangelingRole => "Генокрад",
            AntagTokenCatalog.HereticRole => "Еретик",
            AntagTokenCatalog.ShadowlingRole => "Шедоулинг",
            _ => role.Id
        };
    }

    private void ShowPopup(ICommonSession session, string message)
    {
        if (session.AttachedEntity is { Valid: true } uid)
            _popup.PopupEntity(message, uid, uid);
    }

    private readonly record struct ReservedGhostRuleState(NetUserId UserId, string RoleId);

    public sealed class PlayerTokenState
    {
        public int Balance { get; set; }
        public int MonthlyEarned { get; set; }
        public int MonthlyYear { get; set; }
        public int MonthlyMonth { get; set; }
        public string? PendingDepositRoleId { get; set; }
    }
}
