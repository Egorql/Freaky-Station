// SPDX-FileCopyrightText: 2025 ChatGPT
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.ServerCurrency;
using Content.Corvax.Interfaces.Shared;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Content.Goobstation.Server.ServerCurrency
{
    /// <summary>
    /// Connects the currency manager to round-end reward logic and client balance updates.
    /// </summary>
    public sealed class ServerCurrencySystem : EntitySystem
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        private ISharedSponsorsManager? _sponsors;

        private int _goobcoinsPerPlayer = 10;
        private int _goobcoinsNonAntagMultiplier = 1;
        private int _goobcoinsServerMultiplier = 1;
        private int _goobcoinsMinPlayers = 5;
        private bool _goobcoinsUseLowPopMultiplier = true;
        private double _goobcoinsLowPopMultiplierStrength = 1.0;
        private bool _goobcoinsUseShortRoundPenalty = true;
        private int _goobcoinsShortRoundPenaltyTargetMinutes = 90;

        public override void Initialize()
        {
            base.Initialize();
            _currencyMan.BalanceChange += OnBalanceChange;
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
            SubscribeNetworkEvent<PlayerBalanceRequestEvent>(OnBalanceRequest);
            IoCManager.Instance!.TryResolveType(out _sponsors);
            Subs.CVar(_cfg, GoobCVars.GoobcoinsPerPlayer, value => _goobcoinsPerPlayer = Math.Max(0, value), true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinNonAntagMultiplier, value => _goobcoinsNonAntagMultiplier = Math.Max(0, value), true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinServerMultiplier, value => _goobcoinsServerMultiplier = Math.Max(0, value), true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinMinPlayers, value => _goobcoinsMinPlayers = Math.Max(0, value), true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinUseLowpopMultiplier, value => _goobcoinsUseLowPopMultiplier = value, true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinLowpopMultiplierStrength, value => _goobcoinsLowPopMultiplierStrength = Math.Max(0, value), true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinUseShortRoundPenalty, value => _goobcoinsUseShortRoundPenalty = value, true);
            Subs.CVar(_cfg, GoobCVars.GoobcoinShortRoundPenaltyTargetMinutes, value => _goobcoinsShortRoundPenaltyTargetMinutes = Math.Max(1, value), true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _currencyMan.BalanceChange -= OnBalanceChange;
        }

        private void OnRoundEndText(RoundEndTextAppendEvent ev)
        {
            if (_players.PlayerCount < _goobcoinsMinPlayers)
                return;

            var maxPlayers = Math.Max(1, _players.MaxPlayers);
            var lowPopMultiplier = 1.0 - (_players.PlayerCount / (double) maxPlayers);

            var query = EntityQueryEnumerator<MindContainerComponent>();
            while (query.MoveNext(out var uid, out var mindContainer))
            {
                var isBorg = HasComp<BorgChassisComponent>(uid);
                if (!(HasComp<HumanoidAppearanceComponent>(uid)
                    || HasComp<BorgBrainComponent>(uid)
                    || isBorg))
                    continue;

                if (!mindContainer.HasMind || mindContainer.Mind is not { } mindId)
                    continue;

                var mind = Comp<MindComponent>(mindId);
                if (!mind.OriginalOwnerUserId.HasValue
                    || _mind.IsCharacterDeadIc(mind) && !isBorg
                    || !_players.TryGetSessionById(mind.UserId, out var session))
                    continue;

                var money = _goobcoinsPerPlayer;

                money += _jobs.GetJobGoobcoins(session);
                if (!_jobs.CanBeAntag(session))
                    money *= _goobcoinsNonAntagMultiplier;

                if (_goobcoinsUseLowPopMultiplier)
                    money += (int) Math.Round(money * lowPopMultiplier * _goobcoinsLowPopMultiplierStrength);

                if (_goobcoinsServerMultiplier != 1)
                    money *= _goobcoinsServerMultiplier;

                if (_sponsors != null && _sponsors.TryGetServerPrototypes(session.UserId, out _))
                    money *= 2;

                if (_goobcoinsUseShortRoundPenalty)
                {
                    var roundMinutesActual = _gameTicker.RoundDuration().TotalMinutes;
                    money = (int) (money * Math.Min(1, roundMinutesActual / _goobcoinsShortRoundPenaltyTargetMinutes));
                }

                _currencyMan.AddCurrency(mind.OriginalOwnerUserId.Value, money);
            }
        }

        private void OnBalanceRequest(PlayerBalanceRequestEvent ev, EntitySessionEventArgs eventArgs)
        {
            var senderSession = eventArgs.SenderSession;
            var balance = _currencyMan.GetBalance(senderSession.UserId);
            RaiseNetworkEvent(new PlayerBalanceUpdateEvent(balance, balance), senderSession);
        }

        private void OnBalanceChange(PlayerBalanceChangeEvent ev)
        {
            RaiseNetworkEvent(new PlayerBalanceUpdateEvent(ev.NewBalance, ev.OldBalance), ev.UserSes);

            if (!ev.UserSes.AttachedEntity.HasValue)
                return;

            var userEnt = ev.UserSes.AttachedEntity.Value;
            if (ev.NewBalance > ev.OldBalance)
            {
                _popupSystem.PopupEntity("+" + _currencyMan.Stringify(ev.NewBalance - ev.OldBalance), userEnt, userEnt, PopupType.Medium);
            }
            else if (ev.NewBalance < ev.OldBalance)
            {
                _popupSystem.PopupEntity("-" + _currencyMan.Stringify(ev.OldBalance - ev.NewBalance), userEnt, userEnt, PopupType.MediumCaution);
            }
        }
    }
}
