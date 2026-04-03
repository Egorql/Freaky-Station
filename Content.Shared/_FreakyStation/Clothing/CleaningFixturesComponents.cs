// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FreakyStation.Clothing;

[RegisterComponent]
public sealed partial class ShowerComponent : Component
{
    [DataField]
    public bool Enabled;

    [DataField]
    public TimeSpan CleanInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextCleanTime;
}

[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    [DataField]
    public bool Running;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan EndTime;
}

[Serializable, NetSerializable]
public enum CleaningFixtureVisuals : byte
{
    Active
}
