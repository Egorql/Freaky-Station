// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Server.EUI;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FreakyStation.ERP
{
    public sealed class ERPSystem : EntitySystem
    {
        private static readonly SpriteSpecifier VerbIcon =
            new SpriteSpecifier.Texture(new("/Textures/_FreakStation/Casha/ERPicon/erp.svg.192dpi.png"));

        [Dependency] private readonly ERPArousalSystem _arousal = default!;
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] private readonly ERPExposureSystem _exposure = default!;
        [Dependency] private readonly ERPInteractionPolicySystem _policy = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly HashSet<ERPEUI> _openEuis = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ERPComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        }

        private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
        {
            if (ev.Profile is not HumanoidCharacterProfile profile)
                return;

            var component = EnsureComp<ERPComponent>(ev.Mob);
            component.Consent = profile.ERPConsent;
            component.NonCon = profile.NonCon;
            component.Arousal = 0f;
            component.TargetArousal = 0f;
            component.CooldownUntil = TimeSpan.Zero;
            component.LastInteractionAt = TimeSpan.Zero;

            DirtyRelevantEuis(ev.Mob);
        }

        private void AddVerbs(GetVerbsEvent<Verb> args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            if (!TryComp<ERPComponent>(args.User, out _))
                return;

            if (!_policy.CanOpenPanel(args.User))
                return;

            if (args.User == args.Target)
            {
                if (_policy.GetInteractionEntries(args.User, args.User, ERPPanelMode.Self).Count == 0)
                    return;

                args.Verbs.Add(new Verb
                {
                    Priority = -1,
                    Text = "ERP",
                    Icon = VerbIcon,
                    Act = () => OpenPanel(actor.PlayerSession, args.User, ERPPanelMode.Self),
                    Impact = LogImpact.Low,
                });
                return;
            }

            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!TryComp<ERPComponent>(args.Target, out _))
                return;

            if (!TryComp<HumanoidAppearanceComponent>(args.User, out _)
                || !TryComp<HumanoidAppearanceComponent>(args.Target, out _))
                return;

            var hasEnabledTargetInteractions = false;
            foreach (var interaction in _policy.GetInteractionEntries(args.User, args.Target, ERPPanelMode.Target))
            {
                if (!interaction.Enabled)
                    continue;

                hasEnabledTargetInteractions = true;
                break;
            }

            if (!hasEnabledTargetInteractions)
                return;

            args.Verbs.Add(new Verb
            {
                Priority = -1,
                Text = "ERP",
                Icon = VerbIcon,
                Act = () =>
                {
                    if (!CanKeepPanelOpen(args.User, args.Target, ERPPanelMode.Target))
                        return;

                    OpenPanel(actor.PlayerSession, args.Target, ERPPanelMode.Target);
                },
                Impact = LogImpact.Low,
            });
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _arousal.UpdateAll(frameTime, DirtyRelevantEuis);
        }

        public bool CanKeepPanelOpen(EntityUid user, EntityUid target, ERPPanelMode mode)
        {
            return _policy.CanKeepPanelOpen(user, target, mode);
        }

        public ERPInteractionEuiState BuildState(EntityUid user, EntityUid target, ERPPanelMode mode)
        {
            var state = new ERPInteractionEuiState
            {
                Mode = mode,
                Target = GetNetEntity(target),
            };

            if (!_policy.CanKeepPanelOpen(user, target, mode))
                return state;

            if (!TryComp<ERPComponent>(user, out var userComp)
                || !TryComp<ERPComponent>(target, out var targetComp)
                || !TryComp<HumanoidAppearanceComponent>(user, out var userHumanoid)
                || !TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid))
            {
                return state;
            }

            state.UserSex = userHumanoid.Sex;
            state.TargetSex = targetHumanoid.Sex;
            state.UserHasClothing = _exposure.HasRelevantCoverage(user);
            state.TargetHasClothing = _exposure.HasRelevantCoverage(target);
            state.UserConsent = userComp.Consent;
            state.TargetConsent = targetComp.Consent;
            state.UserNonCon = userComp.NonCon;
            state.TargetNonCon = targetComp.NonCon;
            state.UserArousal = userComp.Arousal;
            state.TargetArousal = targetComp.Arousal;
            state.CooldownEndTime = _arousal.GetVisibleCooldownEndTime(userComp, targetComp);
            state.Interactions = _policy.GetInteractionEntries(user, target, mode);

            return state;
        }

        public bool TryPerformInteraction(EntityUid user, EntityUid target, ERPPanelMode mode, ProtoId<ERPPrototype> interactionId)
        {
            if (!_policy.CanKeepPanelOpen(user, target, mode))
                return false;

            if (!_prototypeManager.TryIndex(interactionId, out var interaction))
                return false;

            var interactionEnabled = false;
            foreach (var entry in _policy.GetInteractionEntries(user, target, mode))
            {
                if (entry.InteractionId != interactionId)
                    continue;

                interactionEnabled = entry.Enabled;
                break;
            }

            if (!interactionEnabled)
                return false;

            if (!_arousal.TryApplyInteraction(user, target, interaction))
                return false;

            DirtyRelevantEuis(user, target);
            return true;
        }

        public void RegisterEui(ERPEUI eui)
        {
            _openEuis.Add(eui);
        }

        public void UnregisterEui(ERPEUI eui)
        {
            _openEuis.Remove(eui);
        }

        private void OpenPanel(ICommonSession player, EntityUid target, ERPPanelMode mode)
        {
            _eui.OpenEui(new ERPEUI(this, GetNetEntity(target), mode), player);
        }

        private void OnExamined(EntityUid uid, ERPComponent component, ref ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.PushMarkup(
                Loc.GetString("erp-examine-consent", ("consent", ERPFormatting.FormatConsentMarkup(component.Consent))),
                ExaminePriorities.FeatureStatus);
            args.PushMarkup(
                Loc.GetString("erp-examine-non-con", ("nonCon", ERPFormatting.FormatNonConMarkup(component.NonCon))),
                ExaminePriorities.FeatureStatus);
        }

        private void DirtyRelevantEuis(EntityUid first, EntityUid? second = null)
        {
            var openEuis = new List<ERPEUI>(_openEuis);

            foreach (var eui in openEuis)
            {
                if (eui.IsShutDown)
                {
                    _openEuis.Remove(eui);
                    continue;
                }

                if (eui.TracksEntity(first) || second != null && eui.TracksEntity(second.Value))
                    eui.StateDirty();
            }
        }
    }
}
