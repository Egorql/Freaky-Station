using Content.Server.Examine;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Preferences;
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
}
