// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._FreakyStation.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ClothingStainComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BloodLevel;

    [DataField, AutoNetworkedField]
    public string? BloodColorHex;

    [DataField, AutoNetworkedField]
    public int BioLevel;

    [DataField, AutoNetworkedField]
    public string? BioColorHex;

    [DataField]
    public int MaxLevel = 3;

    [DataField]
    public string? ItemState;

    [DataField]
    public string? EquippedState;
}
