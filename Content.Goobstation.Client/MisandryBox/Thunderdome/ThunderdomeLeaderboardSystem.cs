using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.MisandryBox.Thunderdome;

/// <summary>
/// System that manages the Thunderdome leaderboard overlay.
/// </summary>
public sealed class ThunderdomeLeaderboardSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private ThunderdomeLeaderboardOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new ThunderdomeLeaderboardOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
