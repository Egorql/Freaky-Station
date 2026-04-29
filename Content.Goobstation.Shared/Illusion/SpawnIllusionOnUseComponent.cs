namespace Content.Goobstation.Shared.Illusion;

[RegisterComponent]
public sealed partial class SpawnIllusionOnUseComponent : Component
{
    [DataField]
    public float Lifetime = 8f;

    [DataField]
    public float HealthMultiplier = 0.1f;

    [DataField]
    public int CloneCount = 1;
}
