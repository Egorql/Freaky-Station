using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Goobstation.Shared.MisandryBox.Thunderdome;

/// <summary>
/// Represents a single entry in the Thunderdome leaderboard.
/// </summary>
[Serializable, NetSerializable]
public sealed record ThunderdomeLeaderboardEntry(
    string Name,
    int Kills,
    int Deaths,
    float KD,
    int BestStreak,
    int Rank);

/// <summary>
/// Component for the leaderboard display entity in ThunderDome arena.
/// Stores leaderboard data that is rendered by ThunderdomeLeaderboardOverlay on the client.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThunderdomeLeaderboardComponent : Component
{
    /// <summary>
    /// Reference to the rule entity managing this leaderboard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RuleEntity;

    /// <summary>
    /// If true, this leaderboard displays global all-time stats.
    /// If false, displays current round stats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsGlobal;

    /// <summary>
    /// Title text displayed at the top of the leaderboard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Title = "";

    /// <summary>
    /// Color of the leaderboard text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Gold;

    /// <summary>
    /// Font size for the leaderboard text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FontSize = 10;

    /// <summary>
    /// Offset from the entity position where the leaderboard should be rendered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Offset = new(0, 1.5f);

    /// <summary>
    /// List of top players to display on the leaderboard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ThunderdomeLeaderboardEntry> Entries = new();
}
