// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Clothing;
using Content.Shared._FreakyStation.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._FreakyStation.Clothing;

public sealed class ClothingStainVisualizerSystem : EntitySystem
{
    private static readonly ResPath StainRsi = new("/Textures/_FreakStation/Effects/dirt_overlay.rsi");
    private static readonly Color BloodColor = new(122, 26, 28);
    private static readonly Color BioColor = new(214, 211, 198);
    private const string BloodLayerKey = "clothing-stain-blood";
    private const string BioLayerKey = "clothing-stain-bio";

    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingStainComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ClothingStainComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<ClothingStainComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ClothingStainComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
    }

    private void OnStartup(EntityUid uid, ClothingStainComponent component, ComponentStartup args)
    {
        Refresh(uid, component);
    }

    private void OnAfterState(EntityUid uid, ClothingStainComponent component, ref AfterAutoHandleStateEvent args)
    {
        Refresh(uid, component);
    }

    private void Refresh(EntityUid uid, ClothingStainComponent component)
    {
        UpdateSpriteLayers(uid, component);
        _item.VisualsChanged(uid);
    }

    private void OnShutdown(EntityUid uid, ClothingStainComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), BloodLayerKey, out var bloodLayer, false))
            _sprite.RemoveLayer((uid, sprite), bloodLayer);

        if (_sprite.LayerMapTryGet((uid, sprite), BioLayerKey, out var bioLayer, false))
            _sprite.RemoveLayer((uid, sprite), bioLayer);
    }

    private void OnGetEquipmentVisuals(Entity<ClothingStainComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        var state = ResolveEquippedState(ent);

        if (state == null)
            return;

        if (ent.Comp.BloodLevel > 0)
            args.Layers.Add(($"{args.Slot}-{BloodLayerKey}", MakeLayer(state, GetLevelColor(GetStainColor(ent.Comp.BloodColorHex, BloodColor), ent.Comp.BloodLevel, ent.Comp.MaxLevel))));

        if (ent.Comp.BioLevel > 0)
            args.Layers.Add(($"{args.Slot}-{BioLayerKey}", MakeLayer(state, GetLevelColor(GetStainColor(ent.Comp.BioColorHex, BioColor), ent.Comp.BioLevel, ent.Comp.MaxLevel))));
    }

    private void UpdateSpriteLayers(EntityUid uid, ClothingStainComponent component)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var state = ResolveItemState((uid, component));

        UpdateSpriteLayer((uid, sprite), BloodLayerKey, component.BloodLevel, component.MaxLevel, GetStainColor(component.BloodColorHex, BloodColor), state);
        UpdateSpriteLayer((uid, sprite), BioLayerKey, component.BioLevel, component.MaxLevel, GetStainColor(component.BioColorHex, BioColor), state);
    }

    private void UpdateSpriteLayer(Entity<SpriteComponent?> ent, string key, int level, int maxLevel, Color color, string? state)
    {
        var layer = _sprite.LayerMapReserve(ent, key);
        _sprite.LayerSetRsi(ent, layer, StainRsi);

        if (state != null)
            _sprite.LayerSetRsiState(ent, layer, state);

        _sprite.LayerSetColor(ent, layer, GetLevelColor(color, level, maxLevel));
        _sprite.LayerSetVisible(ent, layer, layer > -1 && level > 0 && state != null);
    }

    private static PrototypeLayerData MakeLayer(string state, Color color)
    {
        return new PrototypeLayerData
        {
            RsiPath = StainRsi.ToString(),
            State = state,
            Color = color,
        };
    }

    private string? ResolveEquippedState(Entity<ClothingStainComponent> ent)
    {
        if (ent.Comp.EquippedState == null)
            return null;

        if (TryComp<ClothingComponent>(ent, out var clothing) &&
            clothing.EquippedPrefix is { Length: > 0 } prefix)
        {
            var variant = $"{prefix}-{ent.Comp.EquippedState}";
            if (HasState(variant))
                return variant;
        }

        return HasState(ent.Comp.EquippedState)
            ? ent.Comp.EquippedState
            : null;
    }

    private string? ResolveItemState(Entity<ClothingStainComponent> ent)
    {
        if (ent.Comp.ItemState == null)
            return null;

        if (TryComp<ItemComponent>(ent, out var item) &&
            item.HeldPrefix is { Length: > 0 } prefix)
        {
            var underscoreVariant = $"{ent.Comp.ItemState}_{prefix}";
            if (HasState(underscoreVariant))
                return underscoreVariant;

            var dashedVariant = $"{prefix}-{ent.Comp.ItemState}";
            if (HasState(dashedVariant))
                return dashedVariant;
        }

        return HasState(ent.Comp.ItemState)
            ? ent.Comp.ItemState
            : null;
    }

    private static Color GetLevelColor(Color baseColor, int level, int maxLevel)
    {
        if (level <= 0 || maxLevel <= 0)
            return new Color(baseColor.R, baseColor.G, baseColor.B, 0);

        var progress = Math.Clamp(level / (float) maxLevel, 0.35f, 1f);
        var alpha = (byte) (85 + progress * 170);
        return new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
    }

    private static Color GetStainColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        return Color.FromHex(hex, fallback);
    }

    private bool HasState(string state)
    {
        var rsi = _resCache.GetResource<RSIResource>(StainRsi).RSI;
        return rsi.TryGetState(state, out _);
    }
}
