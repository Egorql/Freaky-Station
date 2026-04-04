// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Server._FreakyStation.ERP;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.SSDIndicator;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPInteractionPolicySystemTests
{
    private const string HumanPrototype = "MobHuman";
    private const string ClothingRequiredInteraction = "ERPLickNipples";
    private const string FemaleUserInteraction = "ERPkuninakanuni";
    private const string FemaleTargetInteraction = "ERPTouchChest";
    private const string MonkeyTemplate = "monkey";
    private const string NonErpInteraction = "RPFIVE";
    private const string TestJumpsuit = "ClothingUniformJumpsuitColorGrey";
    private static readonly string[] MaleSelfInteractions =
    {
        "ERPSelfMasturbateMale",
        "ERPSelfStrokeHeadMale",
        "ERPSelfJerkOffHardMale",
        "ERPSelfAnalTeaseMale",
        "ERPSelfAnalFingerMale",
    };

    private static readonly string[] FemaleSelfInteractions =
    {
        "ERPSelfRubClitFemale",
        "ERPSelfRubPussyFemale",
        "ERPSelfFingerFemale",
        "ERPSelfAnalTeaseFemale",
        "ERPSelfAnalFingerFemale",
        "ERPSelfFondleBreastsFemale",
    };

    [Test]
    public async Task PolicyBlocksDeadAndPlayerSsdBodies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var mindSystem = server.System<SharedMindSystem>();

        EntityUid user = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.True);

            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);
            Assert.That(entries.Any(e => e.Enabled && e.InteractionId == NonErpInteraction), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(target, MobState.Dead);

            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.IsErpBodyUnavailable(target), Is.True);
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.False);
            Assert.That(policy.GetInteractionEntries(user, target, ERPPanelMode.Target), Is.Empty);
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(target, MobState.Alive);

            var indicator = server.EntMan.EnsureComponent<SSDIndicatorComponent>(target);
            indicator.IsSSD = true;

            var mindContainer = server.EntMan.EnsureComponent<MindContainerComponent>(target);
            mindContainer.LastMindStored = mindSystem.CreateMind(null).Owner;

            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.IsErpBodyUnavailable(target), Is.True);
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.False);
            Assert.That(policy.GetInteractionEntries(user, target, ERPPanelMode.Target), Is.Empty);
        });

        await server.WaitAssertion(() =>
        {
            var mindContainer = server.EntMan.GetComponent<MindContainerComponent>(target);
            mindContainer.LastMindStored = null;

            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.IsErpBodyUnavailable(target), Is.False);
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.True);

            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);
            Assert.That(entries.Any(e => e.Enabled && e.InteractionId == NonErpInteraction), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(user, MobState.Dead);

            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.IsErpBodyUnavailable(user), Is.True);
            Assert.That(policy.CanKeepPanelOpen(user, user, ERPPanelMode.Self), Is.False);
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(user, MobState.Alive);

            var indicator = server.EntMan.EnsureComponent<SSDIndicatorComponent>(user);
            indicator.IsSSD = true;

            var mindContainer = server.EntMan.EnsureComponent<MindContainerComponent>(user);
            mindContainer.LastMindStored = mindSystem.CreateMind(null).Owner;

            var policy = server.System<ERPInteractionPolicySystem>();
            Assert.That(policy.IsErpBodyUnavailable(user), Is.True);
            Assert.That(policy.CanKeepPanelOpen(user, user, ERPPanelMode.Self), Is.False);
            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DisabledTargetBlocksPanelAndAllInteractions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Disabled);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();
            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);

            Assert.That(policy.CanKeepPanelOpen(user, target, ERPPanelMode.Target), Is.False);
            Assert.That(entries, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EntriesExplainClothingRestrictionsAndHideWrongTargetSex()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);

            SetSex(server, user, Sex.Male);
            SetSex(server, target, Sex.Male);
            EquipJumpsuit(server, user);
            EquipJumpsuit(server, target);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();
            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);

            AssertEntry(entries, ClothingRequiredInteraction, false, ERPInteractionDeniedReason.NeedTargetUncovered);
            AssertEntry(entries, "ERPanal", false,
                ERPInteractionDeniedReason.NeedUserUncovered | ERPInteractionDeniedReason.NeedTargetUncovered);
            Assert.That(entries.Any(e => e.InteractionId == FemaleTargetInteraction), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EntriesHideWrongUserSexWithoutBeingMaskedByClothing()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);

            SetSex(server, user, Sex.Male);
            SetSex(server, target, Sex.Unsexed);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();
            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);

            Assert.That(entries.Any(e => e.InteractionId == FemaleUserInteraction), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EntriesIgnoreMissingOptionalTemplateSlotsForCoverage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);
            SetInventoryTemplate(server, target, MonkeyTemplate);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();
            var entries = policy.GetInteractionEntries(user, target, ERPPanelMode.Target);

            AssertEntry(entries, ClothingRequiredInteraction, true, ERPInteractionDeniedReason.None);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SelfModeOnlyReturnsMatchingSexInteractions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid male = default;
        EntityUid female = default;
        EntityUid unsexed = default;
        EntityUid target = default;

        await server.WaitAssertion(() =>
        {
            male = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            female = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            unsexed = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);

            MakeErpCapable(server, male, ERPConsent.Enabled);
            MakeErpCapable(server, female, ERPConsent.Enabled);
            MakeErpCapable(server, unsexed, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);

            SetSex(server, male, Sex.Male);
            SetSex(server, female, Sex.Female);
            SetSex(server, unsexed, Sex.Unsexed);
            SetSex(server, target, Sex.Female);
        });

        await server.WaitAssertion(() =>
        {
            var policy = server.System<ERPInteractionPolicySystem>();

            var maleEntries = policy.GetInteractionEntries(male, male, ERPPanelMode.Self);
            var femaleEntries = policy.GetInteractionEntries(female, female, ERPPanelMode.Self);
            var unsexedEntries = policy.GetInteractionEntries(unsexed, unsexed, ERPPanelMode.Self);
            var targetEntries = policy.GetInteractionEntries(male, target, ERPPanelMode.Target);

            Assert.That(maleEntries.Select(e => e.InteractionId.Id).Order().ToArray(),
                Is.EqualTo(MaleSelfInteractions.Order().ToArray()));
            Assert.That(femaleEntries.Select(e => e.InteractionId.Id).Order().ToArray(),
                Is.EqualTo(FemaleSelfInteractions.Order().ToArray()));
            Assert.That(unsexedEntries, Is.Empty);
            Assert.That(targetEntries.All(e => !MaleSelfInteractions.Contains(e.InteractionId.Id)), Is.True);
            Assert.That(targetEntries.All(e => !FemaleSelfInteractions.Contains(e.InteractionId.Id)), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    private static void MakeErpCapable(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, ERPConsent consent)
    {
        var erp = server.EntMan.EnsureComponent<ERPComponent>(uid);
        erp.Consent = consent;
    }

    private static void SetSex(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, Sex sex)
    {
        var appearance = server.EntMan.GetComponent<HumanoidAppearanceComponent>(uid);
        appearance.Sex = sex;
    }

    private static void EquipJumpsuit(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid)
    {
        var inventory = server.System<InventorySystem>();
        var clothing = server.EntMan.SpawnEntity(TestJumpsuit, server.EntMan.GetComponent<TransformComponent>(uid).Coordinates);
        Assert.That(inventory.TryEquip(uid, clothing, "jumpsuit", force: true), Is.True);
    }

    private static void SetInventoryTemplate(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, string templateId)
    {
        var inventory = server.EntMan.GetComponent<InventoryComponent>(uid);
        server.System<InventorySystem>().SetTemplateId((uid, inventory), templateId);
    }

    private static void AssertEntry(
        IReadOnlyCollection<ERPInteractionEntryState> entries,
        string interactionId,
        bool enabled,
        ERPInteractionDeniedReason deniedReason)
    {
        var entry = entries.Single(e => e.InteractionId == interactionId);
        Assert.That(entry.Enabled, Is.EqualTo(enabled));
        Assert.That(entry.DeniedReason, Is.EqualTo(deniedReason));
    }
}
