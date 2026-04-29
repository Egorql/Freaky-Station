using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MisandryBox.Thunderdome;

[Serializable, NetSerializable]
public sealed class ThunderdomeLoadoutEuiState : EuiStateBase
{
    public List<ThunderdomeLoadoutOption> Weapons { get; }
    public List<ThunderdomeLoadoutOption> Grenades { get; }
    public List<ThunderdomeLoadoutOption> Medicals { get; }
    public List<ThunderdomeLoadoutOption> Heads { get; }
    public List<ThunderdomeLoadoutOption> Necks { get; }
    public List<ThunderdomeLoadoutOption> Glasses { get; }
    public List<ThunderdomeLoadoutOption> Backpacks { get; }
    public List<ThunderdomeLoadoutOption> Utilities { get; }
    public int PlayerCount { get; }
    public int LastWeaponSelection { get; }
    public int LastGrenadeSelection { get; }
    public int LastMedicalSelection { get; }
    public int LastHeadSelection { get; }
    public int LastNeckSelection { get; }
    public int LastGlassesSelection { get; }
    public int LastBackpackSelection { get; }
    public int LastUtilitySelection { get; }

    public ThunderdomeLoadoutEuiState(
        List<ThunderdomeLoadoutOption> weapons,
        List<ThunderdomeLoadoutOption> grenades,
        List<ThunderdomeLoadoutOption> medicals,
        List<ThunderdomeLoadoutOption> heads,
        List<ThunderdomeLoadoutOption> necks,
        List<ThunderdomeLoadoutOption> glasses,
        List<ThunderdomeLoadoutOption> backpacks,
        List<ThunderdomeLoadoutOption> utilities,
        int playerCount,
        int lastWeaponSelection = -1,
        int lastGrenadeSelection = 0,
        int lastMedicalSelection = 0,
        int lastHeadSelection = 0,
        int lastNeckSelection = 0,
        int lastGlassesSelection = 0,
        int lastBackpackSelection = 0,
        int lastUtilitySelection = 0)
    {
        Weapons = weapons;
        Grenades = grenades;
        Medicals = medicals;
        Heads = heads;
        Necks = necks;
        Glasses = glasses;
        Backpacks = backpacks;
        Utilities = utilities;
        PlayerCount = playerCount;
        LastWeaponSelection = lastWeaponSelection;
        LastGrenadeSelection = lastGrenadeSelection;
        LastMedicalSelection = lastMedicalSelection;
        LastHeadSelection = lastHeadSelection;
        LastNeckSelection = lastNeckSelection;
        LastGlassesSelection = lastGlassesSelection;
        LastBackpackSelection = lastBackpackSelection;
        LastUtilitySelection = lastUtilitySelection;
    }
}

[Serializable, NetSerializable]
public sealed class ThunderdomeLoadoutOption
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SpritePrototype { get; set; } = string.Empty;
}

[Serializable, NetSerializable]
public readonly record struct ThunderdomeLoadoutSelection(
    int WeaponIndex,
    int GrenadeIndex,
    int MedicalIndex,
    int HeadIndex,
    int NeckIndex,
    int GlassesIndex,
    int BackpackIndex,
    int UtilityIndex);

[Serializable, NetSerializable]
public sealed class ThunderdomeLoadoutSelectedMessage : EuiMessageBase
{
    public ThunderdomeLoadoutSelection Selection { get; }

    public ThunderdomeLoadoutSelectedMessage(ThunderdomeLoadoutSelection selection)
    {
        Selection = selection;
    }

    // Backward compatibility properties
    public int WeaponIndex => Selection.WeaponIndex;
    public int GrenadeIndex => Selection.GrenadeIndex;
    public int MedicalIndex => Selection.MedicalIndex;
    public int HeadIndex => Selection.HeadIndex;
    public int NeckIndex => Selection.NeckIndex;
    public int GlassesIndex => Selection.GlassesIndex;
    public int BackpackIndex => Selection.BackpackIndex;
    public int UtilityIndex => Selection.UtilityIndex;
}
