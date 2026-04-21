using Content.Shared.DoAfter;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._FreakyStation;

[Serializable, NetSerializable]
public sealed partial class OpenNukebriefcaseDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;

}

