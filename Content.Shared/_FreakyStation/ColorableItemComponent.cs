// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._FreakyStation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ColorableItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color CurrentColor = Color.White;
}
