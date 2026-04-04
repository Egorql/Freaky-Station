using Content.Shared._FreakyStation.ERP;
using Content.Shared.Preferences;
using NUnit.Framework;

namespace Content.Tests.Shared._FreakyStation.ERP;

[TestFixture]
public sealed class ERPFormattingTests
{
    [TestCase(ERPConsent.Disabled, "humanoid-profile-editor-erp-consent-disabled", "red")]
    [TestCase(ERPConsent.Enabled, "humanoid-profile-editor-erp-consent-enabled", "green")]
    public void ConsentPresentationMappingIsStable(ERPConsent consent, string expectedKey, string expectedColor)
    {
        Assert.That(ERPFormatting.GetConsentLocalizationKey(consent), Is.EqualTo(expectedKey));
        Assert.That(ERPFormatting.GetConsentColor(consent), Is.EqualTo(expectedColor));
    }

    [TestCase(false, "erp-non-con-off", "red")]
    [TestCase(true, "erp-non-con-on", "green")]
    public void NonConPresentationMappingIsStable(bool enabled, string expectedKey, string expectedColor)
    {
        Assert.That(ERPFormatting.GetNonConLocalizationKey(enabled), Is.EqualTo(expectedKey));
        Assert.That(ERPFormatting.GetNonConColor(enabled), Is.EqualTo(expectedColor));
    }
}
