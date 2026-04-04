// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._FreakyStation.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._FreakyStation.Clothing;

public sealed class CleaningFixturesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly ClothingStainSystem _stains = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowerComponent, ActivateInWorldEvent>(OnShowerActivated);
        SubscribeLocalEvent<WashingMachineComponent, ActivateInWorldEvent>(OnWashingMachineActivated);
        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<AlternativeVerb>>(OnWashingMachineVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var showers = EntityQueryEnumerator<ShowerComponent>();
        while (showers.MoveNext(out var showerUid, out var shower))
        {
            if (!shower.Enabled || _timing.CurTime < shower.NextCleanTime)
                continue;

            shower.NextCleanTime = _timing.CurTime + shower.CleanInterval;

            foreach (var target in _lookup.GetEntitiesInRange<InventoryComponent>(
                         Transform(showerUid).Coordinates,
                         0.6f,
                         LookupFlags.Dynamic | LookupFlags.Uncontained))
            {
                _stains.CleanEquippedClothing(target);
            }
        }

        var washers = EntityQueryEnumerator<WashingMachineComponent, StorageComponent>();
        while (washers.MoveNext(out var washerUid, out var washer, out var storage))
        {
            if (!washer.Running || _timing.CurTime < washer.EndTime)
                continue;

            washer.Running = false;
            FinishWashingCycle((washerUid, washer), (washerUid, storage));
        }
    }

    private void OnShowerActivated(Entity<ShowerComponent> ent, ref ActivateInWorldEvent args)
    {
        ent.Comp.Enabled = !ent.Comp.Enabled;
        ent.Comp.NextCleanTime = _timing.CurTime;
        _appearance.SetData(ent.Owner, CleaningFixtureVisuals.Active, ent.Comp.Enabled);

        var popup = ent.Comp.Enabled
            ? "clothing-cleaning-shower-on"
            : "clothing-cleaning-shower-off";
        _popup.PopupEntity(Loc.GetString(popup), ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnWashingMachineActivated(Entity<WashingMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        _storage.OpenStorageUI(ent.Owner, args.User, storage, false);
        args.Handled = true;
    }

    private void OnWashingMachineVerbs(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !TryComp<StorageComponent>(ent, out var storage))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = ent.Comp.Running
                ? Loc.GetString("clothing-cleaning-washer-running")
                : Loc.GetString("clothing-cleaning-washer-start"),
            Priority = 2,
            Disabled = ent.Comp.Running || storage.Container.ContainedEntities.Count == 0,
            Act = () => StartWashingCycle(ent, storage, user),
        });
    }

    private void StartWashingCycle(Entity<WashingMachineComponent> ent, StorageComponent storage, EntityUid user)
    {
        if (ent.Comp.Running)
            return;

        if (storage.Container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("clothing-cleaning-washer-empty"), ent.Owner, user);
            return;
        }

        ent.Comp.Running = true;
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        _appearance.SetData(ent.Owner, CleaningFixtureVisuals.Active, true);
        _popup.PopupEntity(Loc.GetString("clothing-cleaning-washer-begin"), ent.Owner, user);
    }

    private void FinishWashingCycle(Entity<WashingMachineComponent> washer, Entity<StorageComponent> storage)
    {
        foreach (var contained in storage.Comp.Container.ContainedEntities)
        {
            _stains.CleanStains(contained);
        }

        _appearance.SetData(washer.Owner, CleaningFixtureVisuals.Active, false);
    }
}
