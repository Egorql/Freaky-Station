// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPPrototypeContentTests
{
    [Test]
    public async Task SelfInteractionsExistForMaleAndFemaleOnly()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var selfInteractions = server.ResolveDependency<IPrototypeManager>()
                .EnumeratePrototypes<ERPPrototype>()
                .Where(p => p.UseSelf)
                .OrderBy(p => p.ID)
                .ToArray();

            Assert.That(selfInteractions, Has.Length.EqualTo(11));
            Assert.That(selfInteractions.All(p => p.ArousalDelta > 0), Is.True);
            Assert.That(selfInteractions.All(p => p.UserWithoutCloth), Is.True);
            Assert.That(selfInteractions.All(p => !p.TargetWithoutCloth), Is.True);

            var male = selfInteractions.Where(p => p.UserSex == Sex.Male).ToArray();
            var female = selfInteractions.Where(p => p.UserSex == Sex.Female).ToArray();

            Assert.That(male, Has.Length.EqualTo(5));
            Assert.That(female, Has.Length.EqualTo(6));
            Assert.That(selfInteractions.All(p => p.UserSex == p.TargetSex), Is.True);
            Assert.That(selfInteractions.All(p => p.UserSex != Sex.Unsexed), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ArousalDrivenInteractionsHaveExpectedSemantics()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var prototypes = server.ResolveDependency<IPrototypeManager>()
                .EnumeratePrototypes<ERPPrototype>()
                .ToArray();

            Assert.That(prototypes, Is.Not.Empty);

            var arousalDriven = prototypes.Where(p => p.ArousalDelta > 0).ToArray();
            var nonErp = new[] { "RPFIVE", "RPPAT", "ERPkiss1", "ERPHug" }
                .Select(id => server.ResolveDependency<IPrototypeManager>().Index<ERPPrototype>(id))
                .ToArray();

            Assert.That(arousalDriven, Is.Not.Empty);
            Assert.That(nonErp, Is.Not.Empty);
            Assert.That(arousalDriven.All(p => p.ArousalDelta > 0), Is.True,
                "ERP interactions should have positive arousal progression.");
            Assert.That(nonErp.All(p => p.ArousalDelta == 0), Is.True,
                "Non-ERP interactions should not contribute to arousal progression.");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RebalancedArousalDeltasMatchExpectedTiers()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            var expectedErpInteractions = new Dictionary<string, int>
            {
                ["ERPzad"] = 15,
                ["ERPkiss2"] = 15,
                ["ERPSelfMasturbateMale"] = 15,
                ["ERPSelfStrokeHeadMale"] = 20,
                ["ERPSelfJerkOffHardMale"] = 25,
                ["ERPSelfAnalTeaseMale"] = 15,
                ["ERPSelfAnalFingerMale"] = 20,
                ["ERPSelfRubClitFemale"] = 15,
                ["ERPSelfRubPussyFemale"] = 20,
                ["ERPSelfFingerFemale"] = 25,
                ["ERPSelfAnalTeaseFemale"] = 15,
                ["ERPSelfAnalFingerFemale"] = 20,
                ["ERPSelfFondleBreastsFemale"] = 15,
                ["ERPMasturbirovat"] = 15,
                ["ERPMasturbirovatt"] = 15,
                ["ERPanal"] = 20,
                ["ERPvaginal"] = 20,
                ["ERPminetM"] = 20,
                ["ERPkuninakanuni"] = 20,
                ["ERPkuninakanunim"] = 20,
                ["ERPsoso"] = 20,
                ["ERPTouchChest"] = 15,
                ["ERPTouchChestM"] = 15,
                ["ERPLickNipples"] = 15,
                ["ERPGrind"] = 15,
                ["ERPSitOnLap"] = 15,
                ["ERPRide"] = 25,
                ["ERPDoggy"] = 25,
                ["ERP69Female"] = 25,
                ["ERP69Male"] = 25,
                ["ERPCumInside"] = 40,
                ["ERPCumMouth"] = 35,
                ["ERPCumFace"] = 30,
                ["ERPCumChest"] = 30,
                ["ERPTitjob"] = 20,
                ["ERPFootjob"] = 15,
                ["ERPMissionary"] = 25,
                ["ERPStanding"] = 35,
            };

            foreach (var (id, expectedDelta) in expectedErpInteractions)
            {
                var prototype = prototypeManager.Index<ERPPrototype>(id);
                Assert.Multiple(() =>
                {
                    Assert.That(prototype.ArousalDelta, Is.EqualTo(expectedDelta), $"{id} arousalDelta drifted.");
                });
            }

            var expectedNonErp = new Dictionary<string, int>
            {
                ["RPFIVE"] = 0,
                ["RPPAT"] = 0,
                ["ERPkiss1"] = 0,
                ["ERPHug"] = 0,
            };

            foreach (var (id, expectedDelta) in expectedNonErp)
            {
                var prototype = prototypeManager.Index<ERPPrototype>(id);
                Assert.Multiple(() =>
                {
                    Assert.That(prototype.ArousalDelta, Is.EqualTo(expectedDelta), $"{id} arousalDelta should stay zero.");
                });
            }
        });

        await pair.CleanReturnAsync();
    }
}
