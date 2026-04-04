// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Server._FreakyStation.ERP;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.SSDIndicator;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPSystemStage2Tests
{
    private const string ConsentBlockedErpInteraction = "ERPzad";
    private const string HumanPrototype = "MobHuman";
    private const string NonErpInteraction = "RPFIVE";
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
    public async Task BuildStateContainsEnabledAndDisabledEntries()
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
            var state = server.System<ERPSystem>().BuildState(user, target, ERPPanelMode.Target);
            Assert.That(state.Interactions, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TryPerformInteractionRejectsDisabledEntry()
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
            var erpSystem = server.System<ERPSystem>();
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, ConsentBlockedErpInteraction), Is.False);
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, NonErpInteraction), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BuildStateAndActionsBlockDeadAndSsdTargets()
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
            var erpSystem = server.System<ERPSystem>();
            Assert.That(EnabledInteractionIds(erpSystem.BuildState(user, target, ERPPanelMode.Target)),
                Contains.Item(NonErpInteraction));
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(target, MobState.Dead);

            var erpSystem = server.System<ERPSystem>();
            Assert.That(erpSystem.BuildState(user, target, ERPPanelMode.Target).Interactions, Is.Empty);
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, NonErpInteraction), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var mobStateSystem = server.System<MobStateSystem>();
            mobStateSystem.ChangeMobState(target, MobState.Alive);

            var indicator = server.EntMan.EnsureComponent<SSDIndicatorComponent>(target);
            indicator.IsSSD = true;

            var mindContainer = server.EntMan.EnsureComponent<MindContainerComponent>(target);
            mindContainer.LastMindStored = mindSystem.CreateMind(null).Owner;

            var erpSystem = server.System<ERPSystem>();
            Assert.That(erpSystem.BuildState(user, target, ERPPanelMode.Target).Interactions, Is.Empty);
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, NonErpInteraction), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var mindContainer = server.EntMan.GetComponent<MindContainerComponent>(target);
            mindContainer.LastMindStored = null;

            var erpSystem = server.System<ERPSystem>();
            Assert.That(EnabledInteractionIds(erpSystem.BuildState(user, target, ERPPanelMode.Target)),
                Contains.Item(NonErpInteraction));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TryPerformInteractionExposesCooldownForNonArousalInteraction()
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
        });

        await server.WaitAssertion(() =>
        {
            var erpSystem = server.System<ERPSystem>();
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, NonErpInteraction), Is.True);
            Assert.That(erpSystem.TryPerformInteraction(user, target, ERPPanelMode.Target, NonErpInteraction), Is.False);
            Assert.That(erpSystem.BuildState(user, target, ERPPanelMode.Target).CooldownEndTime, Is.GreaterThan(TimeSpan.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BuildStateSelfModeUsesMatchingSelfCatalog()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid male = default;
        EntityUid female = default;
        EntityUid unsexed = default;

        await server.WaitAssertion(() =>
        {
            male = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            female = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            unsexed = server.EntMan.Spawn(HumanPrototype, map.MapCoords);

            MakeErpCapable(server, male, ERPConsent.Enabled);
            MakeErpCapable(server, female, ERPConsent.Enabled);
            MakeErpCapable(server, unsexed, ERPConsent.Enabled);

            SetSex(server, male, Content.Shared.Humanoid.Sex.Male);
            SetSex(server, female, Content.Shared.Humanoid.Sex.Female);
            SetSex(server, unsexed, Content.Shared.Humanoid.Sex.Unsexed);
        });

        await server.WaitAssertion(() =>
        {
            var erpSystem = server.System<ERPSystem>();

            var maleState = erpSystem.BuildState(male, male, ERPPanelMode.Self);
            var femaleState = erpSystem.BuildState(female, female, ERPPanelMode.Self);
            var unsexedState = erpSystem.BuildState(unsexed, unsexed, ERPPanelMode.Self);

            Assert.That(EnabledInteractionIds(maleState).Select(x => x.Id).Order().ToArray(),
                Is.EqualTo(MaleSelfInteractions.Order().ToArray()));
            Assert.That(EnabledInteractionIds(femaleState).Select(x => x.Id).Order().ToArray(),
                Is.EqualTo(FemaleSelfInteractions.Order().ToArray()));
            Assert.That(unsexedState.Interactions, Is.Empty);
            Assert.That(maleState.Interactions.All(i => !FemaleSelfInteractions.Contains(i.InteractionId.Id)), Is.True);
            Assert.That(femaleState.Interactions.All(i => !MaleSelfInteractions.Contains(i.InteractionId.Id)), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    private static void MakeErpCapable(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, ERPConsent consent)
    {
        var erp = server.EntMan.EnsureComponent<ERPComponent>(uid);
        erp.Consent = consent;
    }

    private static void SetSex(
        RobustIntegrationTest.ServerIntegrationInstance server,
        EntityUid uid,
        Content.Shared.Humanoid.Sex sex)
    {
        var appearance = server.EntMan.GetComponent<Content.Shared.Humanoid.HumanoidAppearanceComponent>(uid);
        appearance.Sex = sex;
    }

    private static List<ProtoId<ERPPrototype>> EnabledInteractionIds(ERPInteractionEuiState state)
    {
        var result = new List<ProtoId<ERPPrototype>>();

        foreach (var interaction in state.Interactions)
        {
            if (!interaction.Enabled)
                continue;

            result.Add(interaction.InteractionId);
        }

        return result;
    }
}
