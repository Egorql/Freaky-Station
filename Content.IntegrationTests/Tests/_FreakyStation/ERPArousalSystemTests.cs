// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._FreakyStation.ERP;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Reflection;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPArousalSystemTests
{
    private const string HumanPrototype = "MobHuman";
    private const string NonErpInteraction = "RPFIVE";

    [Test]
    public async Task TryApplyInteractionAddsSharedCooldown()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;
        EntityUid target = default;
        ERPPrototype interaction = null!;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            target = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);
            MakeErpCapable(server, target, ERPConsent.Enabled);
            interaction = server.ResolveDependency<IPrototypeManager>().Index<ERPPrototype>(NonErpInteraction);
        });

        await server.WaitAssertion(() =>
        {
            var arousalSystem = server.System<ERPArousalSystem>();
            Assert.That(arousalSystem.TryApplyInteraction(user, target, interaction), Is.True);
            Assert.That(arousalSystem.TryApplyInteraction(user, target, interaction), Is.False);

            var userErp = server.EntMan.GetComponent<ERPComponent>(user);
            var targetErp = server.EntMan.GetComponent<ERPComponent>(target);
            Assert.That(arousalSystem.GetVisibleCooldownEndTime(userErp, targetErp), Is.GreaterThan(TimeSpan.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task UpdateAllHandlesMissingOptionalAudio()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);

            var appearance = server.EntMan.GetComponent<HumanoidAppearanceComponent>(user);
            appearance.Sex = Sex.Male;

            var erp = server.EntMan.GetComponent<ERPComponent>(user);
            erp.Arousal = 1f;
            erp.TargetArousal = 1f;
            erp.CooldownUntil = TimeSpan.Zero;

            server.System<ERPArousalSystem>().UpdateAll(1f);

            Assert.That(erp.Arousal, Is.EqualTo(0f).Within(0.001f));
            Assert.That(erp.TargetArousal, Is.EqualTo(0f).Within(0.001f));
            Assert.That(erp.CooldownUntil, Is.GreaterThan(TimeSpan.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task UpdateAllHandlesFemaleOrgasmEffects()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);

            var appearance = server.EntMan.GetComponent<HumanoidAppearanceComponent>(user);
            appearance.Sex = Sex.Female;

            var erp = server.EntMan.GetComponent<ERPComponent>(user);
            erp.Arousal = 1f;
            erp.TargetArousal = 1f;
            erp.CooldownUntil = TimeSpan.Zero;

            server.System<ERPArousalSystem>().UpdateAll(1f);

            Assert.That(erp.Arousal, Is.EqualTo(0f).Within(0.001f));
            Assert.That(erp.TargetArousal, Is.EqualTo(0f).Within(0.001f));
            Assert.That(erp.CooldownUntil, Is.GreaterThan(TimeSpan.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MissingOptionalEffectDoesNotThrow()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid user = default;

        await server.WaitAssertion(() =>
        {
            user = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            MakeErpCapable(server, user, ERPConsent.Enabled);

            var system = server.System<ERPArousalSystem>();
            var method = typeof(ERPArousalSystem).GetMethod("TrySpawnOptionalEffect",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            var result = method!.Invoke(system, new object[]
            {
                "DefinitelyMissingErpEffect",
                server.EntMan.GetComponent<TransformComponent>(user).Coordinates,
                null,
            });

            Assert.That(result, Is.EqualTo(false));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EffectSquirtPrototypeIsIndexed()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            Assert.That(prototypes.TryIndex<EntityPrototype>("EffectSquirt", out _), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    private static void MakeErpCapable(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, ERPConsent consent)
    {
        var erp = server.EntMan.EnsureComponent<ERPComponent>(uid);
        erp.Consent = consent;
    }
}
