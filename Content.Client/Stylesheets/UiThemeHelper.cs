// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;

namespace Content.Client.Stylesheets;

public static class UiThemeHelper
{
    public static Color GetTonedAccent(IConfigurationManager cfg)
    {
        var accent = new Color(
            (byte) cfg.GetCVar(CCVars.InterfaceAccentRed),
            (byte) cfg.GetCVar(CCVars.InterfaceAccentGreen),
            (byte) cfg.GetCVar(CCVars.InterfaceAccentBlue));

        var accentGray = (byte) (accent.R * 0.299f + accent.G * 0.587f + accent.B * 0.114f);
        return Color.InterpolateBetween(accent, new Color(accentGray, accentGray, accentGray), 0.35f);
    }

    public static Color AccentTint(IConfigurationManager cfg, string baseHex, float mix)
    {
        var original = Color.FromHex(baseHex);
        return Color.InterpolateBetween(original, GetTonedAccent(cfg), mix).WithAlpha(original.A);
    }
}
