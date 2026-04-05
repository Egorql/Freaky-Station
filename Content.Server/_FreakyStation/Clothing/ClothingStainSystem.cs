// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._FreakyStation.Clothing;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._FreakyStation.Clothing;

public sealed class ClothingStainSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly PuddleSystem _puddles = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextBloodStain = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextShoePuddleStain = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextBodyPuddleStain = new();
    private readonly Dictionary<EntityUid, EntityCoordinates> _lastCoordinates = new();
    private static readonly FixedPoint2 PassiveBloodStainThreshold = 2f;
    private static readonly FixedPoint2 BloodPuddleStainThreshold = 20f;
    private static readonly TimeSpan BloodStainInterval = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ShoePuddleStainInterval = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan BodyPuddleStainInterval = TimeSpan.FromSeconds(1.2);
    private static readonly ProtoId<ReagentPrototype>[] BloodReagents =
    [
        "Blood",
        "Slime",
        "CopperBlood",
        "BloodChangeling",
        "BlackBlood",
    ];

    private static readonly string[] DefaultBioDirtySlots =
    [
        "underwear",
        "jumpsuit",
        "outerClothing",
    ];

    private static readonly string[] DefaultBloodDirtySlots =
    [
        "jumpsuit",
        "outerClothing",
        "breast",
        "gloves",
        "shoes",
        "mask",
        "head",
    ];

    private static readonly string[] PassiveBloodDirtySlots =
    [
        "jumpsuit",
        "outerClothing",
        "breast",
        "gloves",
    ];

    private static readonly string[] PassiveHeadBloodDirtySlots =
    [
        "mask",
        "head",
    ];

    private static readonly string[] PassiveFootBloodDirtySlots =
    [
        "shoes",
    ];

    private static readonly string[] DefaultCleaningSlots =
    [
        "underwear",
        "jumpsuit",
        "outerClothing",
        "breast",
        "gloves",
        "shoes",
        "mask",
        "head",
        "neck",
        "back",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, ReactionEntityEvent>(OnInventoryReaction);
        SubscribeLocalEvent<ClothingStainComponent, ReactionEntityEvent>(OnReaction);
        SubscribeLocalEvent<ClothingStainComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (bloodstream.BleedAmount < PassiveBloodStainThreshold)
            {
                _nextBloodStain.Remove(uid);
                continue;
            }

            if (_nextBloodStain.TryGetValue(uid, out var next) && now < next)
                continue;

            _nextBloodStain[uid] = now + BloodStainInterval;
            ApplyStainsToEquippedClothing(uid, ResolvePassiveBloodSlots(uid), blood: true);
        }

        var movers = EntityQueryEnumerator<InventoryComponent, TransformComponent>();
        while (movers.MoveNext(out var uid, out _, out var xform))
        {
            if (!xform.Coordinates.IsValid(EntityManager))
                continue;

            var coords = xform.Coordinates;
            var moved = !_lastCoordinates.TryGetValue(uid, out var previous) || previous != coords;
            _lastCoordinates[uid] = coords;

            if (moved &&
                (!_nextShoePuddleStain.TryGetValue(uid, out var nextShoe) || now >= nextShoe) &&
                TryGetPuddleStainAt(coords, out var shoeStain))
            {
                _nextShoePuddleStain[uid] = now + ShoePuddleStainInterval;
                ApplyStainToEquippedClothing(uid, ["shoes"], shoeStain, 1);
            }

            if (!TryComp<StandingStateComponent>(uid, out var standing) || !_standing.IsDown(uid, standing))
            {
                _nextBodyPuddleStain.Remove(uid);
                continue;
            }

            if (_nextBodyPuddleStain.TryGetValue(uid, out var nextBody) && now < nextBody)
                continue;

            if (!TryGetPuddleStainAt(coords, out var bodyStain))
                continue;

            _nextBodyPuddleStain[uid] = now + BodyPuddleStainInterval;
            ApplyStainToEquippedClothing(
                uid,
                ["jumpsuit", "outerClothing", "breast", "gloves", "shoes", "mask", "head"],
                bodyStain,
                1);
        }
    }

    private void OnInventoryReaction(EntityUid uid, InventoryComponent component, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch || args.Reagent.ID != "SpaceCleaner")
            return;

        CleanEquippedClothing(uid);
    }

    private void OnReaction(Entity<ClothingStainComponent> ent, ref ReactionEntityEvent args)
    {
        var reagentId = args.Reagent.ID;

        if (reagentId == "Water" || reagentId == "SpaceCleaner")
        {
            CleanStains(ent.Owner);
            return;
        }

        var stain = new StainData(
            reagentId.Contains("Blood", StringComparison.OrdinalIgnoreCase),
            args.Reagent.SubstanceColor.WithAlpha(1f));

        AddStain(ent.Owner, stain);
    }

    private void OnExamined(Entity<ClothingStainComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.BloodLevel > 0)
        {
            var key = ent.Comp.BloodLevel >= ent.Comp.MaxLevel
                ? "clothing-stain-examine-blood-heavy"
                : "clothing-stain-examine-blood-light";
            args.PushText(Loc.GetString(key));
        }

        if (ent.Comp.BioLevel > 0)
        {
            var key = ent.Comp.BioLevel >= ent.Comp.MaxLevel
                ? "clothing-stain-examine-bio-heavy"
                : "clothing-stain-examine-bio-light";
            args.PushText(Loc.GetString(key));
        }
    }

    private void OnMeleeHit(EntityUid uid, MeleeWeaponComponent component, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.Weapon != args.User || args.HitEntities.Count == 0)
            return;

        if (!TryBlendBloodColors(args.HitEntities, out var bloodColor))
            return;

        ApplyStainToEquippedClothing(args.User, ["gloves"], new StainData(true, bloodColor), 1);
    }

    public void ApplyStainsToEquippedClothing(
        EntityUid wearer,
        IReadOnlyList<string>? slots = null,
        bool blood = false,
        bool bio = false,
        int amount = 1)
    {
        var targetSlots = slots ?? ResolveDefaultSlotsForApplication(blood, bio);

        foreach (var slot in targetSlots)
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot, out var clothingUid))
                continue;

            if (!HasComp<ClothingComponent>(clothingUid.Value))
                continue;

            if (blood)
                AddBloodStains(clothingUid.Value, amount);

            if (bio)
                AddBiologicalStains(clothingUid.Value, amount);
        }
    }

    public void ApplyStainToEquippedClothing(
        EntityUid wearer,
        IReadOnlyList<string>? slots,
        StainData stain,
        int amount = 1)
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot, out var clothingUid))
                continue;

            if (!HasComp<ClothingComponent>(clothingUid.Value))
                continue;

            AddStain(clothingUid.Value, stain, amount);
        }
    }

    public void CleanEquippedClothing(
        EntityUid wearer,
        IReadOnlyList<string>? slots = null,
        bool blood = true,
        bool bio = true)
    {
        var targetSlots = slots ?? DefaultCleaningSlots;

        foreach (var slot in targetSlots)
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot, out var clothingUid))
                continue;

            CleanStains(clothingUid.Value, blood, bio);
        }
    }

    public bool AddBloodStains(EntityUid uid, int amount = 1, Color? color = null, ClothingStainComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        component.BloodLevel = Math.Clamp(component.BloodLevel + amount, 0, component.MaxLevel);
        component.BloodColorHex = BlendColorHex(component.BloodColorHex, component.BloodLevel - amount, color ?? _prototype.Index<ReagentPrototype>("Blood").SubstanceColor, amount);
        Dirty(uid, component);
        return true;
    }

    public bool AddBiologicalStains(EntityUid uid, int amount = 1, Color? color = null, ClothingStainComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        component.BioLevel = Math.Clamp(component.BioLevel + amount, 0, component.MaxLevel);
        component.BioColorHex = BlendColorHex(component.BioColorHex, component.BioLevel - amount, color ?? Color.White, amount);
        Dirty(uid, component);
        return true;
    }

    public bool AddStain(EntityUid uid, StainData stain, int amount = 1, ClothingStainComponent? component = null)
    {
        return stain.IsBlood
            ? AddBloodStains(uid, amount, stain.Color, component)
            : AddBiologicalStains(uid, amount, stain.Color, component);
    }

    public bool CleanStains(EntityUid uid, bool blood = true, bool bio = true, ClothingStainComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        var changed = false;

        if (blood && component.BloodLevel != 0)
        {
            component.BloodLevel = 0;
            component.BloodColorHex = null;
            changed = true;
        }

        if (bio && component.BioLevel != 0)
        {
            component.BioLevel = 0;
            component.BioColorHex = null;
            changed = true;
        }

        if (changed)
            Dirty(uid, component);

        return changed;
    }

    private static IReadOnlyList<string> ResolveDefaultSlotsForApplication(bool blood, bool bio)
    {
        if (blood && bio)
        {
            return
            [
                .. DefaultBioDirtySlots,
                .. DefaultBloodDirtySlots,
            ];
        }

        if (blood)
            return DefaultBloodDirtySlots;

        if (bio)
            return DefaultBioDirtySlots;

        return Array.Empty<string>();
    }

    private IReadOnlyList<string> ResolvePassiveBloodSlots(EntityUid wearer)
    {
        var slots = new List<string>(PassiveBloodDirtySlots.Length + PassiveHeadBloodDirtySlots.Length + PassiveFootBloodDirtySlots.Length);
        slots.AddRange(PassiveBloodDirtySlots);

        if (!TryComp<BodyComponent>(wearer, out var body))
            return slots;

        if (HasBleedingPart(wearer, BodyPartType.Head, body))
            slots.AddRange(PassiveHeadBloodDirtySlots);

        if (HasBleedingPart(wearer, BodyPartType.Foot, body))
            slots.AddRange(PassiveFootBloodDirtySlots);

        return slots;
    }

    private bool HasBleedingPart(EntityUid wearer, BodyPartType partType, BodyComponent body)
    {
        foreach (var (_, _, woundable) in _body.GetBodyChildrenOfTypeWithComponent<WoundableComponent>(wearer, partType, body))
        {
            if (woundable.Bleeds > FixedPoint2.Zero)
                return true;
        }

        return false;
    }

    private bool TryGetPuddleStainAt(EntityCoordinates coords, out StainData stain)
    {
        stain = default;

        if (!_turf.TryGetTileRef(coords, out var tileRef) ||
            !_puddles.TryGetPuddle(tileRef.Value, out var puddleUid) ||
            !TryComp<PuddleComponent>(puddleUid, out var puddle) ||
            !_solutions.ResolveSolution(puddleUid, puddle.SolutionName, ref puddle.Solution, out var solution) ||
            solution.Volume <= 0)
        {
            return false;
        }

        var bloodQuantity = GetBloodQuantity(solution);
        if (bloodQuantity > 0 && bloodQuantity < BloodPuddleStainThreshold)
            return false;

        stain = new StainData(bloodQuantity > 0, GetSolutionLikePuddleColor(solution));
        return stain.Color.A > 0;
    }

    private bool TryBlendBloodColors(IReadOnlyList<EntityUid> targets, out Color color)
    {
        var colors = new List<Color>();

        foreach (var target in targets)
        {
            if (TryGetBloodColor(target, out var bloodColor))
                colors.Add(bloodColor);
        }

        if (colors.Count == 0)
        {
            color = default;
            return false;
        }

        color = BlendColors(colors);
        return true;
    }

    private bool TryGetBloodColor(EntityUid uid, out Color color)
    {
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) &&
            _prototype.TryIndex(bloodstream.BloodReagent, out ReagentPrototype? proto))
        {
            color = proto.SubstanceColor.WithAlpha(1f);
            return true;
        }

        color = default;
        return false;
    }

    private bool ContainsBlood(Solution solution)
    {
        return GetBloodQuantity(solution) > 0;
    }

    private FixedPoint2 GetBloodQuantity(Solution solution)
    {
        var total = FixedPoint2.Zero;

        foreach (var reagent in BloodReagents)
        {
            total += solution.GetTotalPrototypeQuantity(reagent);
        }

        return total;
    }

    private Color GetSolutionLikePuddleColor(Solution solution)
    {
        var color = solution.GetColorWithout(_prototype, BloodReagents.Select(id => id.Id).ToArray()).WithAlpha(0.7f);

        foreach (var standout in BloodReagents)
        {
            var quantity = solution.GetTotalPrototypeQuantity(standout);
            if (quantity <= 0 || !_prototype.TryIndex(standout, out ReagentPrototype? reagent))
                continue;

            var interpolateValue = quantity.Float() / solution.Volume.Float();
            color = Color.InterpolateBetween(color, reagent.SubstanceColor, interpolateValue);
        }

        return color.WithAlpha(1f);
    }

    private static string BlendColorHex(string? existingHex, int existingLevel, Color incomingColor, int incomingAmount)
    {
        var colors = new List<Color>(Math.Max(existingLevel, 0) + Math.Max(incomingAmount, 1));

        if (!string.IsNullOrWhiteSpace(existingHex))
        {
            var existingColor = Color.FromHex(existingHex, incomingColor).WithAlpha(1f);
            for (var i = 0; i < Math.Max(existingLevel, 1); i++)
            {
                colors.Add(existingColor);
            }
        }

        for (var i = 0; i < Math.Max(incomingAmount, 1); i++)
        {
            colors.Add(incomingColor.WithAlpha(1f));
        }

        return BlendColors(colors).ToHex();
    }

    private static Color BlendColors(IReadOnlyList<Color> colors)
    {
        if (colors.Count == 0)
            return Color.White;

        float red = 0;
        float green = 0;
        float blue = 0;

        foreach (var color in colors)
        {
            red += color.RByte;
            green += color.GByte;
            blue += color.BByte;
        }

        var count = colors.Count;
        return new Color((byte) (red / count), (byte) (green / count), (byte) (blue / count));
    }

    public readonly record struct StainData(bool IsBlood, Color Color);
}
