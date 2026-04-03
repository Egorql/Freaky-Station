// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._FreakyStation;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._FreakyStation;

public sealed class ColorableItemSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorableItemComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<ColorableItemComponent, GetInhandVisualsEvent>(OnGetInhandVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<ColorableItemComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClothingSystem), typeof(ClientClothingSystem)]);
    }

    private void OnAfterState(EntityUid uid, ColorableItemComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            _sprite.SetColor((uid, sprite), component.CurrentColor.WithAlpha(sprite.Color.A));

        _item.VisualsChanged(uid);
    }

    private void OnGetInhandVisuals(Entity<ColorableItemComponent> ent, ref GetInhandVisualsEvent args)
    {
        TintLayers(args.Layers, ent.Comp.CurrentColor);
    }

    private void OnGetEquipmentVisuals(Entity<ColorableItemComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        TintLayers(args.Layers, ent.Comp.CurrentColor);
    }

    private static void TintLayers(List<(string, PrototypeLayerData)> layers, Color color)
    {
        for (var i = 0; i < layers.Count; i++)
        {
            var (key, layer) = layers[i];
            layer.Color = layer.Color is { } existing
                ? color.WithAlpha(existing.A)
                : color;
            layers[i] = (key, layer);
        }
    }
}
