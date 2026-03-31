using System;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Inventory;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeBarks()
    {
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeakerBarkEvent>>(OnSpeakerBarkTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkMessage>(OnChangeBark);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkPitchMessage>(OnChangeBarkPitch);
    }

    private void OnSpeakerBarkTransform(EntityUid uid, VoiceMaskComponent component, ref InventoryRelayedEvent<TransformSpeakerBarkEvent> args)
    {
        if (!_proto.TryIndex<BarkPrototype>(component.BarkId, out var proto))
            return;

        args.Args.Data.Pitch = Math.Clamp(component.BarkPitch, BarkDefaults.MinPitch, BarkDefaults.MaxPitch);
        args.Args.Data.Sound = proto.Sound;
    }

    private void OnChangeBark(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkMessage message)
    {
        if (!_proto.HasIndex<BarkPrototype>(message.Proto))
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-invalid"), uid);
            return;
        }

        component.BarkId = message.Proto;
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);
        UpdateUI((uid, component));
    }

    private void OnChangeBarkPitch(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkPitchMessage message)
    {
        if (!float.TryParse(message.Pitch, out var pitchValue))
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-invalid-pitch"), uid);
            return;
        }

        component.BarkPitch = pitchValue;
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);
        UpdateUI((uid, component));
    }
}
