using Content.Goobstation.Common.CCVar;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.SpeechBarks;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : SharedSpeechBarksSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _isEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.BarksEnabled, v => _isEnabled = v, true);
        SubscribeLocalEvent<SpeechBarksComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(EntityUid uid, SpeechBarksComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled || !args.Language.SpeechOverride.RequireSpeech)
            return;

        var ev = new TransformSpeakerBarkEvent(uid, component.Data.Copy());
        RaiseLocalEvent(uid, ev);

        var soundSpecifier = ev.Data.Sound ?? _proto.Index(ev.Data.Proto).Sound;

        RaiseNetworkEvent(new PlaySpeechBarksEvent(
            GetNetEntity(uid),
            args.Message,
            soundSpecifier,
            ev.Data.Pitch,
            ev.Data.MinVar,
            ev.Data.MaxVar,
            args.IsWhisper),
            Filter.Pvs(uid));
    }
}
