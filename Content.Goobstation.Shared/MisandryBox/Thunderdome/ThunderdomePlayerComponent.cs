using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Goobstation.Shared.MisandryBox.Thunderdome;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThunderdomePlayerComponent : Component
{
    [DataField]
    public EntityUid? RuleEntity;

    [DataField]
    public int Kills;

    [DataField]
    public int Deaths;

    [DataField]
    public int CurrentStreak;

    [DataField]
    public int BestStreak;

    [DataField]
    public int WeaponSelection;

    [DataField]
    public int GrenadeSelection;

    [DataField]
    public int MedicalSelection;

    [DataField]
    public int HeadSelection;

    [DataField]
    public int NeckSelection;

    [DataField]
    public int GlassesSelection;

    [DataField]
    public int BackpackSelection;

    [DataField]
    public string CharacterName = string.Empty;

    public EntityUid? LastAttacker;
}
