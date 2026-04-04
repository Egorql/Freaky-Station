using Content.Server.Examine;
using Content.Shared.Atmos.Rotting;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Preferences;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._FreakyStation;

[TestFixture]
public sealed class ERPPresentationTests
{
    private const string HumanPrototype = "MobHuman";

    [Test]
    public async Task ExamineUsesSharedConsentAndNonConFormatting()
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
            server.EntMan.EnsureComponent<ERPComponent>(user);
            server.EntMan.EnsureComponent<ERPComponent>(target);

            var erp = server.EntMan.GetComponent<ERPComponent>(target);
            var examine = server.System<ExamineSystem>();

            erp.Consent = ERPConsent.Disabled;
            erp.NonCon = false;
            var disabledMarkup = examine.GetExamineText(target, user).ToMarkup();
            Assert.That(disabledMarkup,
                Does.Contain(Loc.GetString("erp-examine-consent",
                    ("consent", ERPFormatting.FormatConsentMarkup(ERPConsent.Disabled)))));
            Assert.That(disabledMarkup,
                Does.Contain(Loc.GetString("erp-examine-non-con",
                    ("nonCon", ERPFormatting.FormatNonConMarkup(false)))));
            Assert.That(disabledMarkup, Does.Contain(ERPFormatting.FormatConsentMarkup(ERPConsent.Disabled)));
            Assert.That(disabledMarkup, Does.Contain(ERPFormatting.FormatNonConMarkup(false)));

            erp.Consent = ERPConsent.Enabled;
            erp.NonCon = true;
            var enabledMarkup = examine.GetExamineText(target, user).ToMarkup();
            Assert.That(enabledMarkup, Does.Contain(ERPFormatting.FormatConsentMarkup(ERPConsent.Enabled)));
            Assert.That(enabledMarkup, Does.Contain(ERPFormatting.FormatNonConMarkup(true)));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ExaminePlacesErpStatusAboveMindAndRottingFooters()
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

            var erp = server.EntMan.EnsureComponent<ERPComponent>(target);
            erp.Consent = ERPConsent.Enabled;
            erp.NonCon = false;

            var mindSystem = server.System<SharedMindSystem>();
            var mindContainer = server.EntMan.GetComponent<MindContainerComponent>(target);
            mindSystem.SetShowExamineInfo((target, mindContainer), true);

            server.EntMan.EnsureComponent<PerishableComponent>(target);
            server.EntMan.EnsureComponent<RottingComponent>(target);

            var examine = server.System<ExamineSystem>();
            var markup = examine.GetExamineText(target, user).ToMarkup();

            var erpLine = Loc.GetString("erp-examine-consent",
                ("consent", ERPFormatting.FormatConsentMarkup(ERPConsent.Enabled)));
            var nonConLine = Loc.GetString("erp-examine-non-con",
                ("nonCon", ERPFormatting.FormatNonConMarkup(false)));
            var catatonicLine = $"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", target))}[/color]";
            var rottingLine = Loc.GetString("rotting-rotting", ("target", Identity.Entity(target, server.EntMan)));

            Assert.That(markup, Does.Contain(erpLine));
            Assert.That(markup, Does.Contain(nonConLine));
            Assert.That(markup, Does.Contain(catatonicLine));
            Assert.That(markup, Does.Contain(rottingLine));

            Assert.That(markup.IndexOf(erpLine, System.StringComparison.Ordinal), Is.LessThan(markup.IndexOf(catatonicLine, System.StringComparison.Ordinal)));
            Assert.That(markup.IndexOf(nonConLine, System.StringComparison.Ordinal), Is.LessThan(markup.IndexOf(catatonicLine, System.StringComparison.Ordinal)));
            Assert.That(markup.IndexOf(erpLine, System.StringComparison.Ordinal), Is.LessThan(markup.IndexOf(rottingLine, System.StringComparison.Ordinal)));
            Assert.That(markup.IndexOf(nonConLine, System.StringComparison.Ordinal), Is.LessThan(markup.IndexOf(rottingLine, System.StringComparison.Ordinal)));
        });

        await pair.CleanReturnAsync();
    }
}
