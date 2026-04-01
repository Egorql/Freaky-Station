// SPDX-FileCopyrightText: 2025 ChatGPT
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Goobstation.Server.ServerCurrency
{
    /// <summary>
    /// Currency manager with DB-backed persistence.
    /// </summary>
    public sealed class ServerCurrencyManager : ICommonCurrencyManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerDbManager _db = default!;

        private readonly Dictionary<NetUserId, int> _balances = new();
        private readonly Dictionary<NetUserId, int> _pendingDelta = new();
        private readonly Dictionary<NetUserId, int> _pendingSet = new();
        private readonly HashSet<NetUserId> _loaded = new();
        private readonly HashSet<NetUserId> _loading = new();
        private readonly object _sync = new();

        private const string CurrencyTracker = "GoobCoinsBalance";

        public event Action? ClientBalanceChange;
        public event Action<PlayerBalanceChangeEvent>? BalanceChange;

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            foreach (var session in _playerManager.Sessions)
            {
                if (session.Status is SessionStatus.Connected or SessionStatus.InGame)
                    _ = EnsureLoadedAsync(session.UserId);
            }
        }

        public void Shutdown()
        {
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;

            List<(NetUserId, int)> pendingSaves;
            lock (_sync)
            {
                pendingSaves = _balances.Select(pair => (pair.Key, pair.Value)).ToList();
            }

            foreach (var (userId, balance) in pendingSaves)
            {
                _ = SaveBalanceAsync(userId, balance);
            }
        }

        public bool CanAfford(NetUserId? userId, int amount, out int balance)
        {
            balance = GetBalance(userId);
            return balance >= amount && balance - amount >= 0;
        }

        public string Stringify(int amount) => amount == 1
            ? $"{amount} {Loc.GetString("server-currency-name-singular")}"
            : $"{amount} {Loc.GetString("server-currency-name-plural")}";

        public int AddCurrency(NetUserId userId, int amount)
        {
            return ModifyBalance(userId, amount);
        }

        public int RemoveCurrency(NetUserId userId, int amount)
        {
            return ModifyBalance(userId, -amount);
        }

        public (int, int) TransferCurrency(NetUserId sourceUserId, NetUserId targetUserId, int amount)
        {
            var src = ModifyBalance(sourceUserId, -amount);
            var dst = ModifyBalance(targetUserId, amount);
            return (src, dst);
        }

        public int SetBalance(NetUserId userId, int amount)
        {
            amount = Math.Max(0, amount);
            int oldBalance;
            bool needLoad = false;

            lock (_sync)
            {
                oldBalance = _balances.GetValueOrDefault(userId, 0);
                _balances[userId] = amount;

                if (_loaded.Contains(userId))
                {
                    _pendingSet.Remove(userId);
                    _pendingDelta.Remove(userId);
                }
                else
                {
                    _pendingSet[userId] = amount;

                    if (!_loading.Contains(userId))
                    {
                        _loading.Add(userId);
                        needLoad = true;
                    }
                }
            }

            if (needLoad)
                _ = EnsureLoadedAsync(userId);
            else
                _ = SaveBalanceAsync(userId, amount);

            RaiseBalanceChanged(userId, amount, oldBalance);
            return oldBalance;
        }

        public int GetBalance(NetUserId? userId = null)
        {
            if (userId == null)
                return 0;

            lock (_sync)
            {
                return _balances.GetValueOrDefault(userId.Value, 0);
            }
        }

        private int ModifyBalance(NetUserId userId, int delta)
        {
            int oldBalance;
            int newBalance;
            bool needLoad = false;
            bool shouldSave;

            lock (_sync)
            {
                oldBalance = _balances.GetValueOrDefault(userId, 0);
                newBalance = Math.Max(0, oldBalance + delta);
                _balances[userId] = newBalance;

                if (_loaded.Contains(userId))
                {
                    shouldSave = true;
                }
                else
                {
                    shouldSave = false;
                    if (_pendingSet.TryGetValue(userId, out var pendingSet))
                        _pendingSet[userId] = Math.Max(0, pendingSet + delta);
                    else
                        _pendingDelta[userId] = _pendingDelta.GetValueOrDefault(userId, 0) + delta;

                    if (!_loading.Contains(userId))
                    {
                        _loading.Add(userId);
                        needLoad = true;
                    }
                }
            }

            if (needLoad)
                _ = EnsureLoadedAsync(userId);

            if (shouldSave)
                _ = SaveBalanceAsync(userId, newBalance);

            RaiseBalanceChanged(userId, newBalance, oldBalance);
            return newBalance;
        }

        private async Task EnsureLoadedAsync(NetUserId userId)
        {
            int loadedBalance = 0;
            try
            {
                var trackers = await _db.GetPlayTimes(userId.UserId, CancellationToken.None);
                var tracker = trackers.Find(t => t.Tracker == CurrencyTracker);
                if (tracker != null)
                    loadedBalance = Math.Max(0, (int) Math.Round(tracker.TimeSpent.TotalMinutes));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load currency for {userId}: {e}");
            }

            int finalBalance;
            bool shouldSave = false;

            lock (_sync)
            {
                _loading.Remove(userId);

                if (_loaded.Contains(userId))
                    return;

                if (_pendingSet.TryGetValue(userId, out var pendingSet))
                {
                    finalBalance = Math.Max(0, pendingSet);
                    _pendingSet.Remove(userId);
                    _pendingDelta.Remove(userId);
                    shouldSave = true;
                }
                else
                {
                    var delta = _pendingDelta.GetValueOrDefault(userId, 0);
                    finalBalance = Math.Max(0, loadedBalance + delta);
                    _pendingDelta.Remove(userId);
                }

                _balances[userId] = finalBalance;
                _loaded.Add(userId);
            }

            if (shouldSave)
                _ = SaveBalanceAsync(userId, finalBalance);
        }

        private async Task SaveBalanceAsync(NetUserId userId, int balance)
        {
            try
            {
                await _db.UpdatePlayTimes(new List<PlayTimeUpdate>
                {
                    new(userId, CurrencyTracker, TimeSpan.FromMinutes(Math.Max(0, balance)))
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to save currency for {userId}: {e}");
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus is SessionStatus.Connected or SessionStatus.InGame)
            {
                _ = EnsureLoadedAsync(e.Session.UserId);
                return;
            }

            if (e.NewStatus != SessionStatus.Disconnected)
                return;

            int balance;
            lock (_sync)
            {
                balance = _balances.GetValueOrDefault(e.Session.UserId, 0);
                _balances.Remove(e.Session.UserId);
                _pendingDelta.Remove(e.Session.UserId);
                _pendingSet.Remove(e.Session.UserId);
                _loaded.Remove(e.Session.UserId);
                _loading.Remove(e.Session.UserId);
            }

            _ = SaveBalanceAsync(e.Session.UserId, balance);
        }

        private void RaiseBalanceChanged(NetUserId userId, int newBalance, int oldBalance)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
            {
                BalanceChange?.Invoke(new PlayerBalanceChangeEvent(session, userId, newBalance, oldBalance));
            }
        }
    }
}
