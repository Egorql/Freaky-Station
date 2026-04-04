// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;

namespace Content.Shared._FreakyStation.ERP;

[RegisterComponent]
public sealed partial class ERPComponent : Component
{
    [DataField]
    public ERPConsent Consent = ERPConsent.Disabled;

    [DataField]
    public bool NonCon;

    [DataField]
    public float Arousal;

    [DataField]
    public float TargetArousal;

    [DataField]
    public TimeSpan CooldownUntil;

    [DataField]
    public TimeSpan LastInteractionAt;
}
