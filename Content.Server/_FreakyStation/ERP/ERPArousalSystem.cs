// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server._FreakyStation.Clothing;
using Content.Shared.Alert;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Vapor;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._FreakyStation.ERP
{
    public sealed class ERPArousalSystem : EntitySystem
    {
        private static readonly ResPath MaleOrgasmSound = new("/Audio/ERP/male_orgasm.ogg");
        private static readonly ResPath FemaleOrgasmSound = new("/Audio/ERP/female_orgasm.ogg");
        private static readonly Color SquirtEffectColor = Color.White;

        private static readonly TimeSpan ArousalDecayDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan InteractionCooldown = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan OrgasmCooldown = TimeSpan.FromMinutes(1);
        private const float ArousalDecayPerSecond = 0.06f;

        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly ClothingStainSystem _clothingStains = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly HashSet<string> _missingOptionalEffects = new();
        private readonly HashSet<ResPath> _missingOptionalSounds = new();

        public bool TryApplyInteraction(EntityUid user, EntityUid target, ERPPrototype interaction)
        {
            if (!TryComp<ERPComponent>(user, out var userComp) || !TryComp<ERPComponent>(target, out var targetComp))
                return false;

            var currentTime = _gameTiming.CurTime;
            if (currentTime < userComp.CooldownUntil || currentTime < targetComp.CooldownUntil)
                return false;

            PlayInteractionEffects(user, target, interaction);
            ApplyInteractionCooldown(userComp, targetComp, currentTime);

            if (interaction.ArousalDelta > 0)
            {
                ApplyArousal(user, userComp, interaction.ArousalDelta);
                ApplyArousal(target, targetComp, interaction.ArousalDelta);
            }

            return true;
        }

        public TimeSpan GetVisibleCooldownEndTime(ERPComponent first, ERPComponent second)
        {
            return first.CooldownUntil > second.CooldownUntil
                ? first.CooldownUntil
                : second.CooldownUntil;
        }

        public void UpdateAll(float frameTime, Action<EntityUid, EntityUid?>? dirtyRelevantEuis = null)
        {
            var currentTime = _gameTiming.CurTime;
            var query = EntityQueryEnumerator<ERPComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var shouldDirty = UpdateArousal(uid, comp, frameTime, currentTime);
                UpdateArousalAlert(uid, comp);

                if (shouldDirty)
                    dirtyRelevantEuis?.Invoke(uid, null);
            }
        }

        private static void ApplyInteractionCooldown(ERPComponent userComp, ERPComponent targetComp, TimeSpan currentTime)
        {
            var nextCooldown = currentTime + InteractionCooldown;

            if (userComp.CooldownUntil < nextCooldown)
                userComp.CooldownUntil = nextCooldown;

            if (targetComp.CooldownUntil < nextCooldown)
                targetComp.CooldownUntil = nextCooldown;
        }

        private void ApplyArousal(EntityUid uid, ERPComponent comp, int percent)
        {
            var variation = Math.Max(1, percent / 2);
            var added = (percent + _random.Next(-variation, variation + 1)) / 100f;
            if (added <= 0f)
                return;

            comp.TargetArousal += added;
            comp.LastInteractionAt = _gameTiming.CurTime;
            TrySpawnOptionalEffect("EffectHearts", Transform(uid).Coordinates);
        }

        private bool UpdateArousal(EntityUid uid, ERPComponent comp, float frameTime, TimeSpan currentTime)
        {
            comp.Arousal += (comp.TargetArousal - comp.Arousal) * frameTime;

            if (currentTime - comp.LastInteractionAt > ArousalDecayDelay && comp.TargetArousal > 0f)
                comp.TargetArousal -= ArousalDecayPerSecond * frameTime;

            comp.Arousal = MathHelper.Clamp(comp.Arousal, 0f, 1f);
            comp.TargetArousal = MathHelper.Clamp(comp.TargetArousal, 0f, 1f);

            if (comp.Arousal < 1f || currentTime < comp.CooldownUntil)
                return false;

            HandleOrgasm(uid, comp, currentTime);
            return true;
        }

        private void HandleOrgasm(EntityUid uid, ERPComponent comp, TimeSpan currentTime)
        {
            comp.Arousal = 0f;
            comp.TargetArousal = 0f;
            comp.CooldownUntil = currentTime + OrgasmCooldown;

            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
                return;

            if (humanoid.Sex == Sex.Male)
            {
                Spawn("PuddleSperma", Transform(uid).Coordinates);
                TryPlayOptionalSound(MaleOrgasmSound, uid);
                _clothingStains.ApplyStainsToEquippedClothing(uid, bio: true);
            }
            else if (humanoid.Sex == Sex.Female)
            {
                TrySpawnOptionalEffect("EffectSquirt", Transform(uid).Coordinates, SquirtEffectColor);
                TryPlayOptionalSound(FemaleOrgasmSound, uid);
            }
        }

        private bool TrySpawnOptionalEffect(string prototypeId, EntityCoordinates coordinates, Color? color = null)
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(prototypeId, out _))
            {
                if (_missingOptionalEffects.Add(prototypeId))
                    Log.Warning($"ERP optional effect is missing and will be skipped: {prototypeId}");

                return false;
            }

            var effect = Spawn(prototypeId, coordinates);

            if (color != null && TryComp<AppearanceComponent>(effect, out var appearance))
            {
                _appearance.SetData(effect, VaporVisuals.Color, color.Value.WithAlpha(1f), appearance);
                _appearance.SetData(effect, VaporVisuals.State, true, appearance);
            }

            return true;
        }

        private void TryPlayOptionalSound(ResPath soundPath, EntityUid uid)
        {
            if (!_resources.ContentFileExists(soundPath))
            {
                if (_missingOptionalSounds.Add(soundPath))
                    Log.Warning($"ERP optional sound is missing and will be skipped: {soundPath}");

                return;
            }

            _audio.PlayPvs(soundPath.ToString(), uid);
        }

        private void UpdateArousalAlert(EntityUid uid, ERPComponent comp)
        {
            if (comp.Arousal < 0.1f)
            {
                _alerts.ClearAlert(uid, "Arousal");
                return;
            }

            var severity = (short) Math.Clamp(Math.Floor(comp.Arousal * 10) + 1, 1, 10);
            _alerts.ShowAlert(uid, "Arousal", severity);
        }

        private void PlayInteractionEffects(EntityUid user, EntityUid target, ERPPrototype interaction)
        {
            if (interaction.Emotes.Count > 0)
            {
                var emote = _random.Pick(interaction.Emotes);
                emote = emote.Replace("%user", Identity.Name(user, EntityManager));
                emote = emote.Replace("%target", user == target ? "себя" : Identity.Name(target, EntityManager, user));
                _chat.TrySendInGameICMessage(user, emote, InGameICChatType.Emote, ChatTransmitRange.Normal, checkRadioPrefix: false);
            }

            if (interaction.Sounds.Count > 0)
                _audio.PlayPvs(_random.Pick(interaction.Sounds), user);
        }
    }
}
