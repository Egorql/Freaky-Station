// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Server.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency.UI;
using Content.Server.Administration.Notes;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Goobstation.Server.ServerCurrency.UI
{
    public sealed class CurrencyEui : BaseEui
    {
        private static readonly HashSet<NetUserId> OpenPlayers = new();

        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;
        [Dependency] private readonly IAdminNotesManager _notesMan = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IChatManager _chat = default!;

        private bool _hasRouletteResult;
        private int _lastRouletteBet;
        private int _lastRoulettePayout;
        private float _lastRouletteMultiplier;
        private int _lastRouletteSpinId;
        private bool _spinPending;
        private int _pendingBet;
        private int _pendingSpinId;
        private RouletteMode _pendingMode = RouletteMode.X2;

        private const int MinRouletteBet = 10;
        private const float NormalSpinResolveDelaySeconds = 2.8f;
        private const float FastSpinResolveDelaySeconds = 0.65f;

        public CurrencyEui()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void Opened()
        {
            OpenPlayers.Add(Player.UserId);
            StateDirty();
        }

        public override void Closed()
        {
            OpenPlayers.Remove(Player.UserId);
        }

        public static bool IsOpenFor(ICommonSession player)
        {
            return OpenPlayers.Contains(player.UserId);
        }

        public override EuiStateBase GetNewState()
        {
            return new CurrencyEuiState(
                _hasRouletteResult,
                _lastRouletteBet,
                _lastRoulettePayout,
                _lastRouletteMultiplier,
                _lastRouletteSpinId);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            if (msg is CurrencyEuiMsg.Buy buy)
            {
                BuyToken(buy.TokenId, Player);
                StateDirty();
                return;
            }

            if (msg is CurrencyEuiMsg.SpinRoulette spin)
            {
                QueueSpin(spin.Bet, spin.SpinId, spin.Mode, spin.FastSpin);
            }
        }

        private void QueueSpin(int bet, int spinId, RouletteMode mode, bool fastSpin)
        {
            if (_spinPending)
                return;

            if (bet < MinRouletteBet)
            {
                SetResult(spinId, bet, 0, 0f);
                StateDirty();
                return;
            }

            _spinPending = true;
            _pendingBet = bet;
            _pendingSpinId = spinId;
            _pendingMode = mode;

            var delay = fastSpin ? FastSpinResolveDelaySeconds : NormalSpinResolveDelaySeconds;

            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                ResolveSpin(Player.UserId, Player.Name);
                StateDirty();
            });
        }

        private void ResolveSpin(NetUserId userId, string playerName)
        {
            if (!_spinPending)
                return;

            _spinPending = false;
            var bet = _pendingBet;
            var spinId = _pendingSpinId;
            var mode = _pendingMode;
            _pendingBet = 0;
            _pendingSpinId = 0;
            _pendingMode = RouletteMode.X2;

            var balance = _currencyMan.GetBalance(userId);
            if (balance < bet)
            {
                SetResult(spinId, bet, 0, 0f);
                return;
            }

            _currencyMan.RemoveCurrency(userId, bet);

            var (multiplier, payout) = ResolveModeResult(mode, bet);
            var jackpot = Math.Abs(multiplier - 10f) < 0.001f;

            if (payout > 0)
                _currencyMan.AddCurrency(userId, payout);

            RouletteStats.RecordSpin(bet, payout);
            SetResult(spinId, bet, payout, multiplier);

            if (payout <= 0 && _currencyMan.GetBalance(userId) == 0)
            {
                var bustedMessage = Loc.GetString("gs-roulette-busted-notify", ("player", playerName));
                _chat.DispatchServerAnnouncement(bustedMessage, Color.OrangeRed);
            }

            if (jackpot)
            {
                var pot = RouletteStats.GetAllTimeDepositedCoins();
                var message = Loc.GetString("gs-roulette-jackpot-notify",
                    ("player", playerName),
                    ("amount", _currencyMan.Stringify(payout)),
                    ("pot", _currencyMan.Stringify(pot)));
                _chat.ChatMessageToAll(
                    Content.Shared.Chat.ChatChannel.OOC,
                    message,
                    message,
                    EntityUid.Invalid,
                    false,
                    true,
                    colorOverride: Color.FromHex("#fff0ff", Color.Honeydew));
            }
        }

        private void SetResult(int spinId, int bet, int payout, float multiplier)
        {
            if (payout > 0)
            {
                _hasRouletteResult = true;
                _lastRouletteBet = bet;
                _lastRoulettePayout = payout;
                _lastRouletteMultiplier = multiplier;
                _lastRouletteSpinId = spinId;
                return;
            }

            _hasRouletteResult = true;
            _lastRouletteBet = bet;
            _lastRoulettePayout = 0;
            _lastRouletteMultiplier = multiplier;
            _lastRouletteSpinId = spinId;
        }

        private (float Multiplier, int Payout) ResolveModeResult(RouletteMode mode, int bet)
        {
            return mode switch
            {
                RouletteMode.X2 => RollFixedMode(0.50f, 2f, bet),
                RouletteMode.X5 => RollFixedMode(0.20f, 5f, bet),
                RouletteMode.X10 => RollFixedMode(0.10f, 10f, bet),
                _ => RollFixedMode(0.50f, 2f, bet)
            };
        }

        private (float Multiplier, int Payout) RollFixedMode(float chance, float multiplier, int bet)
        {
            if (!_random.Prob(chance))
                return (0f, 0);

            return (multiplier, (int) MathF.Floor(bet * multiplier));
        }

        private async void BuyToken(ProtoId<TokenListingPrototype> tokenId, ICommonSession playerName)
        {
            var balance = _currencyMan.GetBalance(Player.UserId);

            if (!_protoMan.TryIndex<TokenListingPrototype>(tokenId, out var token))
                return;

            if (balance < token.Price)
                return;

            await _notesMan.AddAdminRemark(Player, Player.UserId, 0,
                Loc.GetString(token.AdminNote), 0, false, null);
            _currencyMan.RemoveCurrency(Player.UserId, token.Price);
        }
    }
}
