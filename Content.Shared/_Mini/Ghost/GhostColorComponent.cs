using Robust.Shared.GameStates;

namespace Content.Shared._Mini.AghostColor;

[RegisterComponent,NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class AghostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
