using Content.Server.Body.Systems;
using Content.Shared.Inventory.Events;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Inventory;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Forensics.Components;
using Content.Server.Popups;
using Content.Shared.Actions.Components;
using Content.Shared.Actions;
using Content.Shared._Mini.DNALocker;
using Content.Shared.Interaction.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Chat;
using Robust.Shared.Audio;
using Content.Shared.Verbs;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;
using Content.Server.Forensics;

namespace Content.Server._Mini.DNALocker;

public sealed class DNALockerSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly VocalSystem _vocal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DNALockerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<DNALockerComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<DNALockerComponent, StoreDNAActionEvent>(OnDNAStore);
        SubscribeLocalEvent<DNALockerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DNALockerComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
    }

    public void OnEquip(EntityUid uid, DNALockerComponent comp, GotEquippedEvent args)
    {
        if (comp.Activated == true || comp.DNAWasStored == false)
        {
            return;
        }

        if (TryComp(args.Equipee, out DnaComponent? dna))
        {
            if (comp.DNA == dna.DNA)
            {
                return;
            }
        }

        if (comp.Nonlethal)
        {
            Timer.Spawn(0000,
                () =>
                {
                    _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-error"), args.Equipee, args.Equipee);
                    _inventory.TryUnequip(args.Equipee, "outerClothing", true, true);
                });
            return;
        }

        comp.Activated = true;

        _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
            $"{ToPrettyString(args.Equipee):user} activated hardsuit self destruction system of {ToPrettyString(args.Equipment):target}");

        EnsureComp<UnremoveableComponent>(args.Equipment);
        _audio.PlayPvs(comp.LockerExplodeSound, uid);
        _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-error-spikes"), args.Equipee, args.Equipee, Shared.Popups.PopupType.Large);

        Timer.Spawn(1000,
            () =>
            {
                _chat.TrySendInGameICMessage(args.Equipment, Loc.GetString("hardsuit-identification-error"), InGameICChatType.Speak, true);
            });

        Timer.Spawn(1500,
            () =>
            {
                if (TryComp(args.Equipee, out VocalComponent? vocal))
                {
                    _vocal.TryPlayScreamSound(args.Equipee, vocal);
                }
            });

        Timer.Spawn(2000,
            () =>
            {
                _chat.TrySendInGameICMessage(args.Equipment, "3", InGameICChatType.Speak, true);
            });

        Timer.Spawn(2500,
            () =>
            {
                if (TryComp(args.Equipee, out VocalComponent? vocal))
                {
                    _vocal.TryPlayScreamSound(args.Equipee, vocal);
                }
            });

        Timer.Spawn(3000,
            () =>
            {
                _chat.TrySendInGameICMessage(args.Equipment, "2", InGameICChatType.Speak, true);
            });

        Timer.Spawn(3500,
            () =>
            {
                if (TryComp(args.Equipee, out VocalComponent? vocal))
                {
                    _vocal.TryPlayScreamSound(args.Equipee, vocal);
                }
            });

        Timer.Spawn(4000,
            () =>
            {
                _chat.TrySendInGameICMessage(args.Equipment, "1", InGameICChatType.Speak, true);

                if (TryComp(args.Equipee, out VocalComponent? vocal))
                {
                    _vocal.TryPlayScreamSound(args.Equipee, vocal);
                }
            });

        Timer.Spawn(5000,
            () =>
            {
                if (!EntityManager.EntityExists(args.Equipment))
                {
                    return;
                }

                _explosionSystem.QueueExplosion(args.Equipment, ExplosionSystem.DefaultExplosionPrototypeId,
                    4, 1, 2, maxTileBreak: 0);

                if (_inventory.TryGetSlotEntity(args.Equipee, "outerClothing", out var hardsuitEntity) &&
                hardsuitEntity == args.Equipment && TryComp<BodyComponent>(args.Equipee, out var body))
                {
                    var ents = _bodySystem.GibBody(args.Equipee, true, body, false);
                    foreach (var part in ents)
                    {
                        if (HasComp<BodyPartComponent>(part))
                        {
                            QueueDel(part);
                        }
                    }
                }

                EntityManager.DeleteEntity(args.Equipment);
            });
    }

    private void OnGetActions(EntityUid uid, DNALockerComponent comp, GetItemActionsEvent args)
    {
        if (comp.DNAWasStored == false)
        {
            args.AddAction(ref comp.ActionEntity, comp.Action);
        }
    }

    public void OnDNAStore(EntityUid uid, DNALockerComponent comp, StoreDNAActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.DNAWasStored == true)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-already-stored"), args.Performer, args.Performer);
        }
        else
        {
            if (TryComp(args.Performer, out DnaComponent? dna) && dna.DNA != null)
            {
                comp.DNA = dna.DNA;
                comp.DNAWasStored = true;
                _audio.PlayPvs(comp.LockSound, uid);
                _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-was-stored"), args.Performer, args.Performer);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-not-presented"), args.Performer, args.Performer);
            }
        }

        args.Handled = true;
    }

    public void OnEmagged(EntityUid uid, DNALockerComponent comp, GotEmaggedEvent args)
    {
        _audio.PlayPvs(comp.SparkSound, uid);

        if (comp.Activated)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-on-emagged-late"), uid);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-on-emagged"), uid);
        }

        EntityManager.RemoveComponent<DNALockerComponent>(uid);

        args.Handled = true;
    }
    private void OnAltVerb(EntityUid uid, DNALockerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.IsLocked)
            return;

        AlternativeVerb verbDNALock = new()
        {
            Act = () => MakeUnlocked(uid, component, args.User),
            Text = Loc.GetString("dna-locker-verb-name"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
        };
        args.Verbs.Add(verbDNALock);
    }
    private void MakeUnlocked(EntityUid uid, DNALockerComponent component, EntityUid userUid)
    {
        if (TryComp<DnaComponent>(userUid, out var userDNAComponent) && component.DNA == userDNAComponent.DNA)
        {
            var unlocked = Loc.GetString("dna-locker-unlock");
            _audio.PlayPvs(component.LockSound, userUid);
            _popupSystem.PopupEntity(unlocked, uid, userUid);
            component.DNA = string.Empty;
            component.DNAWasStored = false;
        }
        else
        {
            var denied = Loc.GetString("dna-locker-failure");
            _audio.PlayPvs(component.DeniedSound, userUid);
            _popupSystem.PopupEntity(denied, uid, userUid);
        }
    }
}
