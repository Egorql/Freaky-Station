// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;
using Robust.Shared.Localization;

namespace Content.Shared._FreakyStation.ERP;

public static class ERPFormatting
{
    public static string GetConsentLocalizationKey(ERPConsent consent)
    {
        return consent switch
        {
            ERPConsent.Enabled => "humanoid-profile-editor-erp-consent-enabled",
            _ => "humanoid-profile-editor-erp-consent-disabled",
        };
    }

    public static string GetConsentColor(ERPConsent consent)
    {
        return consent switch
        {
            ERPConsent.Enabled => "green",
            _ => "red",
        };
    }

    public static string GetNonConLocalizationKey(bool enabled)
    {
        return enabled
            ? "erp-non-con-on"
            : "erp-non-con-off";
    }

    public static string GetNonConColor(bool enabled)
    {
        return enabled
            ? "green"
            : "red";
    }

    public static string FormatConsentMarkup(ERPConsent consent)
    {
        return FormatMarkup(Loc.GetString(GetConsentLocalizationKey(consent)), GetConsentColor(consent));
    }

    public static string FormatNonConMarkup(bool enabled)
    {
        return FormatMarkup(Loc.GetString(GetNonConLocalizationKey(enabled)), GetNonConColor(enabled));
    }

    public static string FormatMarkup(string text, string color)
    {
        return $"[color={color}]{text}[/color]";
    }
}
