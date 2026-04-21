using Content.Shared.DoAfter;
using Robust.Shared.Random;
using Content.Shared._FreakyStation;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Server.Explosion.EntitySystems;


namespace Content.Server._FreakyStation;

public sealed class NukebriefcaseSystem : EntitySystem
{

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NukebriefcaseComponent, OpenNukebriefcaseDoAfterEvent>(OnDoAfterFinished);
        SubscribeLocalEvent<NukebriefcaseComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<NukebriefcaseComponent, ExaminedEvent>(OnExamined);

    }
    private void OnUseInHand(Entity<NukebriefcaseComponent> entity, ref UseInHandEvent args)
    {
        var user = args.User;
        StartDoAfter(user, entity);
        args.Handled = true;
    }

    private void OnExamined(Entity<NukebriefcaseComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=yellow]На крышке чемоданчика мигает красный индикатор.[/color]");
    }
    private void StartDoAfter(EntityUid user, EntityUid item)
    {
        if (!TryComp<NukebriefcaseComponent>(item, out var component))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.Doaftertime, new OpenNukebriefcaseDoAfterEvent(), item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,

        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }
    private void OnDoAfterFinished(EntityUid uid, NukebriefcaseComponent comp, OpenNukebriefcaseDoAfterEvent ev)
    {
        var user = ev.User;
        var used = uid;

        if (ev.Cancelled)
            return;

        if (!TryComp<NukebriefcaseComponent>(used, out var component))
            return;


        if (_random.Prob(component.Succeschance))
        {
            Succesful(user, used, component);
        }
        else
        {
            Fail(user, used, component);
            _chatSystem.TrySendInGameICMessage(user, component.Failedmessage, InGameICChatType.Speak, false);

        }
    }
    private void Succesful(EntityUid? user, EntityUid? used, NukebriefcaseComponent? component)
    {
        if (user == null || used == null || component == null)
            return;

        var coords = Transform(user.Value).Coordinates;
        var spawned = Spawn(component.SpawnOnSuccess, coords);
    }
    private void Fail(EntityUid user, EntityUid used, NukebriefcaseComponent component)
    {
        _explosion.QueueExplosion(
        used,
        "Radioactive",
        5f,
        1f,
        100f,
        tileBreakScale: 1f
        );
    }

}



