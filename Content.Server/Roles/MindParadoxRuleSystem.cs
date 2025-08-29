using Content.Server.Antag;
using Content.Server.Cloning;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Medical.SuitSensors;
using Content.Server.Objectives.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gibbing.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mind;
using Robust.Shared.Random;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Content.Shared.CombatMode;
using Content.Shared.Throwing;
using Content.Server.Popups;
using Content.Shared.Cuffs.Components;
using Content.Shared.Cuffs;
using Content.Shared.Damage;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Hands.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Server.Popups;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Server.Atmos.Components;
using Content.Shared.Popups;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared.Climbing.Components;
using Content.Server.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Actions;
using Content.Server.Animals.Components;
using Content.Server.Administration.Components;
using Content.Server.Body.Systems;
using Content.Server.Temperature.Components;
using Content.Shared._DV.Carrying;
using Content.Goobstation.Common.Footprints;
using Content.Server.Body.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using System.Linq;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class MindParadoxRuleSystem : GameRuleSystem<MindParadoxRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly SuitSensorSystem _sensor = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly InteractionPopupSystem _interactionPopup = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindParadoxRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);

    }


    protected override void Started(EntityUid uid, MindParadoxRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // check if we got enough potential cloning targets, otherwise cancel the gamerule so that the ghost role does not show up
        var allHumans = _mind.GetAliveHumans();

        if (allHumans.Count == 0)
        {
            Log.Info("Could not find any alive players to create a paradox clone from! Ending gamerule.");
            ForceEndSelf(uid, gameRule);
        }
    }

    // we have to do the spawning here so we can transfer the mind to the correct entity and can assign the objectives correctly
    private void OnAntagSelectEntity(Entity<MindParadoxRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Session?.AttachedEntity is not { } spawner)
            return;

        if (ent.Comp.OriginalBody != null) // target was overridden, for example by admin antag control
        {
            if (Deleted(ent.Comp.OriginalBody.Value) || !_mind.TryGetMind(ent.Comp.OriginalBody.Value, out var originalMindId, out var _))
            {
                Log.Warning("Could not find mind of target player to paradox clone!");
                return;
            }
            ent.Comp.OriginalMind = originalMindId;
        }
        else
        {
            // get possible targets
            var allAliveHumanoids = _mind.GetAliveHumans();

            // we already checked when starting the gamerule, but someone might have died since then.
            if (allAliveHumanoids.Count == 0)
            {
                Log.Warning("Could not find any alive players to create a mind paradox from!");
                return;
            }

            // pick a random player
            var randomHumanoidMind = _random.Pick(allAliveHumanoids);
            ent.Comp.OriginalMind = randomHumanoidMind;
            ent.Comp.OriginalBody = randomHumanoidMind.Comp.OwnedEntity;

        }

        if (ent.Comp.OriginalBody == null || !_cloning.TryCloning(ent.Comp.OriginalBody.Value, _transform.GetMapCoordinates(spawner), ent.Comp.Settings, out var clone))
        {
            Log.Error($"Unable to make a paradox clone of entity {ToPrettyString(ent.Comp.OriginalBody)}");
            return;
        }

        // Настраиваем видимость

        AddComp<VisibilityComponent>(clone.Value);

        var visibility = EnsureComp<VisibilityComponent>(clone.Value);

        _visibilitySystem.AddLayer((clone.Value, visibility), (int) VisibilityFlags.Paradox, false);
        _visibilitySystem.RefreshVisibility(clone.Value, visibilityComponent: visibility);

        if (TryComp(clone.Value, out EyeComponent? eyeComp))
            _eye.SetVisibilityMask(clone.Value, eyeComp.VisibilityMask & (int) ~VisibilityFlags.Paradox);
        _eye.SetVisibilityMask(clone.Value, 129);

        if (TryComp(ent.Comp.OriginalBody.Value, out EyeComponent? eye))
            _eye.SetVisibilityMask(ent.Comp.OriginalBody.Value, eye.VisibilityMask & (int) ~VisibilityFlags.Paradox);
        _eye.SetVisibilityMask(ent.Comp.OriginalBody.Value, 129);

        //добавляем компоненты


        AddComp<GodmodeComponent>(clone.Value);

        RemComp<CanEnterCryostorageComponent>(clone.Value);
        RemComp<CombatModeComponent>(clone.Value);
        RemComp<CuffableComponent>(clone.Value);
        RemComp<DamageableComponent>(clone.Value);
        RemComp<HandsComponent>(clone.Value);
        RemComp<InjectableSolutionComponent>(clone.Value);
        RemComp<MobCollisionComponent>(clone.Value);
        RemComp<FlammableComponent>(clone.Value);
        RemComp<PolymorphableComponent>(clone.Value);
        RemComp<BlindableComponent>(clone.Value);
        RemComp<PullableComponent>(clone.Value);
        RemComp<PullerComponent>(clone.Value);
        RemComp<RequireProjectileTargetComponent>(clone.Value);
        RemComp<StrippableComponent>(clone.Value);
        RemComp<StrippingComponent>(clone.Value);
        RemComp<TargetingComponent>(clone.Value);
        RemComp<SurgeryTargetComponent>(clone.Value);
        RemComp<TargetOverrideComponent>(clone.Value);
        RemComp<ComplexInteractionComponent>(clone.Value);
        RemComp<ClimbingComponent>(clone.Value);
        RemComp<FootprintOwnerComponent>(clone.Value);
        RemComp<RespiratorComponent>(clone.Value);
        RemComp<TemperatureSpeedComponent>(clone.Value);
        RemComp<CarriableComponent>(clone.Value);
        RemComp<TemperatureComponent>(clone.Value);






        if (TryComp<FixturesComponent>(clone.Value, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(clone.Value, fixture.Key, fixture.Value, (int) CollisionGroup.GhostImpassable, fixtures);
            _physics.SetCollisionLayer(clone.Value, fixture.Key, fixture.Value, 0, fixtures);
        }





        // Убираем хуйню лишнюю

        RemComp<CanEnterCryostorageComponent>(clone.Value);
        RemComp<CombatModeComponent>(clone.Value);
        RemComp<CuffableComponent>(clone.Value);
        RemComp<DamageableComponent>(clone.Value);
        RemComp<HandsComponent>(clone.Value);
        RemComp<InjectableSolutionComponent>(clone.Value);
        RemComp<MobCollisionComponent>(clone.Value);
        RemComp<FlammableComponent>(clone.Value);
        RemComp<PolymorphableComponent>(clone.Value);
        RemComp<BlindableComponent>(clone.Value);
        RemComp<PullableComponent>(clone.Value);
        RemComp<PullerComponent>(clone.Value);
        RemComp<RequireProjectileTargetComponent>(clone.Value);
        RemComp<StrippableComponent>(clone.Value);
        RemComp<StrippingComponent>(clone.Value);
        RemComp<TargetingComponent>(clone.Value);
        RemComp<SurgeryTargetComponent>(clone.Value);
        RemComp<TargetOverrideComponent>(clone.Value);
        RemComp<ComplexInteractionComponent>(clone.Value);
        RemComp<ClimbingComponent>(clone.Value);
        RemComp<FootprintOwnerComponent>(clone.Value);
        RemComp<RespiratorComponent>(clone.Value);
        RemComp<TemperatureSpeedComponent>(clone.Value);
        RemComp<CarriableComponent>(clone.Value);

        // turn their suit sensors off so they don't immediately get noticed
        _sensor.SetAllSensors(clone.Value, SuitSensorMode.SensorOff);




        args.Entity = clone;



    }



}
