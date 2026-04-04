using Content.Shared.Preferences;
using NUnit.Framework;

namespace Content.Tests.Shared._FreakyStation.ERP;

[TestFixture]
public sealed class ERPConsentTest
{
    [Test]
    public void MemberwiseEqualsTracksErpAndNonCon()
    {
        var baseProfile = new HumanoidCharacterProfile();
        var enabledProfile = baseProfile.WithERPConsent(ERPConsent.Enabled);
        var nonConProfile = baseProfile.WithNonCon(true);

        Assert.That(baseProfile.MemberwiseEquals(enabledProfile), Is.False);
        Assert.That(baseProfile.MemberwiseEquals(nonConProfile), Is.False);
        Assert.That(enabledProfile.GetHashCode(), Is.Not.EqualTo(baseProfile.GetHashCode()));
        Assert.That(nonConProfile.GetHashCode(), Is.Not.EqualTo(baseProfile.GetHashCode()));
    }
}
