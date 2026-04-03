// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._FreakyStation;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._FreakyStation;

public sealed class ItemColorizerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public const string ItemSlotName = "itemSlot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemColorizerComponent, ComponentStartup>(OnUpdateUiState);
        SubscribeLocalEvent<ItemColorizerComponent, EntInsertedIntoContainerMessage>(OnUpdateUiState);
        SubscribeLocalEvent<ItemColorizerComponent, EntRemovedFromContainerMessage>(OnUpdateUiState);
        SubscribeLocalEvent<ItemColorizerComponent, BoundUIOpenedEvent>(OnUpdateUiState);
        SubscribeLocalEvent<ItemColorizerComponent, ItemColorizerApplyColorMessage>(OnApplyColor);
    }

    private void OnApplyColor(Entity<ItemColorizerComponent> ent, ref ItemColorizerApplyColorMessage args)
    {
        var item = _itemSlots.GetItemOrNull(ent.Owner, ItemSlotName);
        if (item is not { Valid: true } itemUid || !HasComp<ItemComponent>(itemUid))
            return;

        var colorable = EnsureComp<ColorableItemComponent>(itemUid);
        var changed = colorable.CurrentColor != args.Color;

        colorable.CurrentColor = args.Color;
        Dirty(itemUid, colorable);

        _audio.PlayPvs(changed ? ent.Comp.ColorizeSound : ent.Comp.ClickSound, ent);
        UpdateUiState(ent);
    }

    private void OnUpdateUiState(Entity<ItemColorizerComponent> ent, ref ComponentStartup args)
    {
        UpdateUiState(ent);
    }

    private void OnUpdateUiState(Entity<ItemColorizerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUiState(ent);
    }

    private void OnUpdateUiState(Entity<ItemColorizerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUiState(ent);
    }

    private void OnUpdateUiState(Entity<ItemColorizerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<ItemColorizerComponent> ent)
    {
        var item = _itemSlots.GetItemOrNull(ent.Owner, ItemSlotName);
        string? itemName = null;
        var color = Color.White;

        if (item is { Valid: true } itemUid)
        {
            itemName = Name(itemUid);

            if (TryComp<ColorableItemComponent>(itemUid, out var colorable))
                color = colorable.CurrentColor;
        }

        _ui.SetUiState(ent.Owner, ItemColorizerUiKey.Key, new ItemColorizerBoundUserInterfaceState(itemName, color));
    }
}
