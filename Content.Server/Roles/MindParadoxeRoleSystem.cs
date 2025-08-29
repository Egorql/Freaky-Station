using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     System responsible for giving a ghost of a paradox a name modifier.
/// </summary>
public sealed class MindParadoxRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindParadoxRoleComponent, MindRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
    }

    private void OnRefreshNameModifiers(Entity<MindParadoxRoleComponent> ent, ref MindRelayedEvent<RefreshNameModifiersEvent> args)
    {
        if (!TryComp<MindRoleComponent>(ent.Owner, out var roleComp))
            return;

        // only show for ghosts
        if (!HasComp<GhostComponent>(roleComp.Mind.Comp.OwnedEntity))
            return;

        if (ent.Comp.NameModifier != null)
            args.Args.AddModifier(ent.Comp.NameModifier.Value, 50);
    }
}
