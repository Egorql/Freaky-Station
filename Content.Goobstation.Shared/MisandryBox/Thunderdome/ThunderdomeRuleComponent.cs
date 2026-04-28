using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Goobstation.Shared.MisandryBox.Thunderdome;

[DataDefinition]
public sealed partial class ThunderdomeWeaponLoadout
{
    [DataField(required: true)]
    public string Gear = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField(required: true)]
    public string Category = string.Empty;

    [DataField(required: true)]
    public string Sprite = string.Empty;
}

[RegisterComponent]
public sealed partial class ThunderdomeRuleComponent : Component
{
    [DataField]
    public MapId? ArenaMap;

    [DataField]
    public HashSet<EntityUid> ArenaGrids = new();

    [DataField]
    public HashSet<NetEntity> Players = new();

    [DataField]
    public Dictionary<NetUserId, int> Kills = new();

    [DataField]
    public Dictionary<NetUserId, int> Deaths = new();

    [DataField]
    public Dictionary<NetUserId, string> CharacterNames = new();

    [DataField]
    public Dictionary<NetUserId, int> BestStreaks = new();

    public List<Entity<ThunderdomeLeaderboardComponent>> CachedLeaderboards = new();

    [DataField]
    public bool Active;

    [DataField]
    public string Gear = "ThunderdomeBaseGear";

    [DataField]
    public List<ThunderdomeWeaponLoadout> WeaponLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> GrenadeLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> MedicalLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> HeadLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> NeckLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> GlassesLoadouts = new();

    [DataField]
    public List<ThunderdomeWeaponLoadout> BackpackLoadouts = new();

    [DataField]
    public TimeSpan CleanupInterval = TimeSpan.FromSeconds(25);

    [DataField]
    public float SweepDespawnTime = 10f;

    [DataField]
    public TimeSpan NextCleanup;
}
