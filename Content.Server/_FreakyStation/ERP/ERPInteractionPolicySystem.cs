// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.SSDIndicator;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._FreakyStation.ERP
{
    public sealed class ERPInteractionPolicySystem : EntitySystem
    {
        private const float InteractionRange = 2f;

        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly ERPExposureSystem _exposure = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        public bool CanOpenPanel(EntityUid user)
        {
            return TryComp<ERPComponent>(user, out var comp)
                   && comp.Consent == ERPConsent.Enabled
                   && !IsErpBodyUnavailable(user)
                   && _actionBlocker.CanInteract(user, user);
        }

        public bool CanKeepPanelOpen(EntityUid user, EntityUid target, ERPPanelMode mode)
        {
            if (TerminatingOrDeleted(user) || TerminatingOrDeleted(target))
                return false;

            if (!CanOpenPanel(user))
                return false;

            if (mode == ERPPanelMode.Self)
                return user == target;

            if (user == target || IsErpBodyUnavailable(target))
                return false;

            if (!TryComp<ERPComponent>(target, out var targetComp) || targetComp.Consent != ERPConsent.Enabled)
                return false;

            if (!_actionBlocker.CanInteract(user, target))
                return false;

            if (!_transform.InRange(user, target, InteractionRange))
                return false;

            return TryComp<ERPComponent>(target, out _)
                   && TryComp<HumanoidAppearanceComponent>(user, out _)
                   && TryComp<HumanoidAppearanceComponent>(target, out _);
        }

        public List<ERPInteractionEntryState> GetInteractionEntries(EntityUid user, EntityUid target, ERPPanelMode mode)
        {
            var result = new List<ERPInteractionEntryState>();

            if (!TryComp<ERPComponent>(user, out _)
                || !TryComp<ERPComponent>(target, out _)
                || !TryComp<HumanoidAppearanceComponent>(user, out var userHumanoid)
                || !TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid)
                || !CanKeepPanelOpen(user, target, mode))
            {
                return result;
            }

            var userHasClothing = _exposure.HasRelevantCoverage(user);
            var targetHasClothing = _exposure.HasRelevantCoverage(target);

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<ERPPrototype>())
            {
                if (mode == ERPPanelMode.Self)
                {
                    if (!prototype.UseSelf)
                        continue;
                }
                else if (prototype.UseSelf)
                {
                    continue;
                }

                if (!IsSexCompatible(prototype.UserSex, userHumanoid.Sex))
                    continue;

                if (!IsSexCompatible(prototype.TargetSex, targetHumanoid.Sex))
                    continue;

                var deniedReason = GetDeniedReason(
                    prototype,
                    userHasClothing,
                    targetHasClothing);

                result.Add(new ERPInteractionEntryState
                {
                    InteractionId = prototype.ID,
                    Enabled = deniedReason == ERPInteractionDeniedReason.None,
                    DeniedReason = deniedReason,
                });
            }

            return result;
        }

        public List<ProtoId<ERPPrototype>> GetAllowedInteractions(EntityUid user, EntityUid target, ERPPanelMode mode)
        {
            var result = new List<ProtoId<ERPPrototype>>();

            foreach (var interaction in GetInteractionEntries(user, target, mode))
            {
                if (!interaction.Enabled)
                    continue;

                result.Add(interaction.InteractionId);
            }

            return result;
        }

        public bool IsErpBodyUnavailable(EntityUid uid)
        {
            if (_mobState.IsDead(uid))
                return true;

            if (!TryComp<SSDIndicatorComponent>(uid, out var indicator) || !indicator.IsSSD)
                return false;

            if (!TryComp<MindContainerComponent>(uid, out var mindContainer))
                return false;

            return mindContainer.Mind != null || mindContainer.LastMindStored != null;
        }

        private static ERPInteractionDeniedReason GetDeniedReason(
            ERPPrototype prototype,
            bool userHasClothing,
            bool targetHasClothing)
        {
            var deniedReason = ERPInteractionDeniedReason.None;

            if (prototype.UserWithoutCloth && userHasClothing)
                deniedReason |= ERPInteractionDeniedReason.NeedUserUncovered;

            if (prototype.TargetWithoutCloth && targetHasClothing)
                deniedReason |= ERPInteractionDeniedReason.NeedTargetUncovered;

            return deniedReason;
        }

        private static bool IsSexCompatible(Sex requiredSex, Sex actualSex)
        {
            return requiredSex == Sex.Unsexed || requiredSex == actualSex;
        }
    }
}
