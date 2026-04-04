// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server._FreakyStation.ERP;

public readonly record struct ERPExposureState(
    bool HasJumpsuit,
    bool HasOuterClothing,
    bool HasBreastCover,
    bool HasUnderwear)
{
    public bool HasRelevantCoverage =>
        HasJumpsuit || HasOuterClothing || HasBreastCover || HasUnderwear;
}

public sealed class ERPExposureSystem : EntitySystem
{
    private const string JumpsuitSlot = "jumpsuit";
    private const string OuterClothingSlot = "outerClothing";
    private const string BreastSlot = "breast";
    private const string UnderwearSlot = "underwear";

    [Dependency] private readonly InventorySystem _inventory = default!;

    public ERPExposureState GetExposureState(EntityUid uid)
    {
        if (!_inventory.TryGetSlots(uid, out var slotDefinitions))
            return default;

        var hasJumpsuit = HasCoveringItem(uid, slotDefinitions, JumpsuitSlot);
        var hasOuterClothing = HasCoveringItem(uid, slotDefinitions, OuterClothingSlot);
        var hasBreastCover = HasCoveringItem(uid, slotDefinitions, BreastSlot);
        var hasUnderwear = HasCoveringItem(uid, slotDefinitions, UnderwearSlot);

        return new ERPExposureState(
            hasJumpsuit,
            hasOuterClothing,
            hasBreastCover,
            hasUnderwear);
    }

    public bool HasRelevantCoverage(EntityUid uid)
    {
        return GetExposureState(uid).HasRelevantCoverage;
    }

    private bool HasCoveringItem(EntityUid uid, IReadOnlyList<SlotDefinition> slotDefinitions, string slotName)
    {
        if (!slotDefinitions.Any(slot => slot.Name == slotName))
            return false;

        return _inventory.TryGetSlotEntity(uid, slotName, out _);
    }
}
