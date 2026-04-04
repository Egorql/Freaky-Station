// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._FreakyStation.ERP;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPExposureSystemTests
{
    private const string HumanPrototype = "MobHuman";
    private const string JumpsuitPrototype = "ClothingUniformJumpsuitColorGrey";
    private const string OuterClothingPrototype = "ClothingOuterWinterCoat";
    private const string BreastCoverPrototype = "LPPBraBlack";
    private const string UnderwearPrototype = "LPPBeeShorts";
    private const string MonkeyTemplate = "monkey";

    [Test]
    public async Task ExposureStateTracksHumanCoveringSlots()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid uid = default;

        await server.WaitAssertion(() =>
        {
            uid = server.EntMan.Spawn(HumanPrototype, map.MapCoords);

            var exposure = server.System<ERPExposureSystem>();
            var initialState = exposure.GetExposureState(uid);

            Assert.Multiple(() =>
            {
                Assert.That(initialState.HasJumpsuit, Is.False);
                Assert.That(initialState.HasOuterClothing, Is.False);
                Assert.That(initialState.HasBreastCover, Is.False);
                Assert.That(initialState.HasUnderwear, Is.False);
                Assert.That(initialState.HasRelevantCoverage, Is.False);
            });

            Equip(server, uid, "jumpsuit", JumpsuitPrototype);
            Equip(server, uid, "outerClothing", OuterClothingPrototype);
            Equip(server, uid, "breast", BreastCoverPrototype);
            Equip(server, uid, "underwear", UnderwearPrototype);

            var state = exposure.GetExposureState(uid);

            Assert.Multiple(() =>
            {
                Assert.That(state.HasJumpsuit, Is.True);
                Assert.That(state.HasOuterClothing, Is.True);
                Assert.That(state.HasBreastCover, Is.True);
                Assert.That(state.HasUnderwear, Is.True);
                Assert.That(state.HasRelevantCoverage, Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ExposureStateIgnoresMissingOptionalSlotsInTemplate()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid uid = default;

        await server.WaitAssertion(() =>
        {
            uid = server.EntMan.Spawn(HumanPrototype, map.MapCoords);
            SetInventoryTemplate(server, uid, MonkeyTemplate);

            var exposure = server.System<ERPExposureSystem>();
            var initialState = exposure.GetExposureState(uid);

            Assert.Multiple(() =>
            {
                Assert.That(initialState.HasJumpsuit, Is.False);
                Assert.That(initialState.HasOuterClothing, Is.False);
                Assert.That(initialState.HasBreastCover, Is.False);
                Assert.That(initialState.HasUnderwear, Is.False);
                Assert.That(initialState.HasRelevantCoverage, Is.False);
            });

            Equip(server, uid, "jumpsuit", JumpsuitPrototype);

            var state = exposure.GetExposureState(uid);

            Assert.Multiple(() =>
            {
                Assert.That(state.HasJumpsuit, Is.True);
                Assert.That(state.HasOuterClothing, Is.False);
                Assert.That(state.HasBreastCover, Is.False);
                Assert.That(state.HasUnderwear, Is.False);
                Assert.That(state.HasRelevantCoverage, Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    private static void Equip(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, string slot, string prototype)
    {
        var inventory = server.System<InventorySystem>();
        var clothing = server.EntMan.SpawnEntity(prototype, server.EntMan.GetComponent<TransformComponent>(uid).Coordinates);
        Assert.That(inventory.TryEquip(uid, clothing, slot, force: true), Is.True);
    }

    private static void SetInventoryTemplate(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid uid, string templateId)
    {
        var inventory = server.EntMan.GetComponent<InventoryComponent>(uid);
        server.System<InventorySystem>().SetTemplateId((uid, inventory), templateId);
    }
}
