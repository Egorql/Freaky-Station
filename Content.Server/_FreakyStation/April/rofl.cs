using Content.Shared.EntityTable;
using Content.Shared.Humanoid;
using Robust.Shared.Player;

namespace Content.Server._Freakystation;

public sealed class AprilRoflSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, PlayerAttachedEvent>(OnAttached);
    }

    private void OnAttached(EntityUid uid, HumanoidAppearanceComponent component, PlayerAttachedEvent args)
    {
        var coords = Transform(uid).Coordinates;
        Spawn("BananaPeel", coords);

    }
}
