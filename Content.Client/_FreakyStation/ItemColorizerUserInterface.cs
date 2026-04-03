// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._FreakyStation;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FreakyStation;

[UsedImplicitly]
public sealed class ItemColorizerUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ItemColorizerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ItemColorizerWindow>();
        _window.ApplyButton.OnPressed += _ => SendMessage(new ItemColorizerApplyColorMessage(_window.ColorPicker.Color));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.UpdateState((ItemColorizerBoundUserInterfaceState) state);
    }
}
