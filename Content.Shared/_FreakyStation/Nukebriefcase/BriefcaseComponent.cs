
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FreakyStation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class NukebriefcaseComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Succeschance = 0.3f;

    [DataField, AutoNetworkedField]
    public float Doaftertime = 10.5f;

    [DataField, AutoNetworkedField]
    public EntProtoId SpawnOnSuccess = "DrinkMonstreDrinkCan";

    [DataField, AutoNetworkedField]
    public string Failedmessage = "БЛЯТТТ";

}
