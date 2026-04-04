// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._FreakyStation;

[RegisterComponent]
public sealed partial class ItemColorizerComponent : Component
{
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier ColorizeSound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");
}

[Serializable, NetSerializable]
public sealed class ItemColorizerBoundUserInterfaceState(string? itemName, Color currentColor) : BoundUserInterfaceState
{
    public readonly string? ItemName = itemName;
    public readonly Color CurrentColor = currentColor;
}

[Serializable, NetSerializable]
public sealed class ItemColorizerApplyColorMessage(Color color) : BoundUserInterfaceMessage
{
    public readonly Color Color = color;
}

[Serializable, NetSerializable]
public enum ItemColorizerUiKey : byte
{
    Key
}
