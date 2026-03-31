using System.Threading.Tasks;
using Content.Goobstation.Common.CCVar;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Chat;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : SharedSpeechBarksSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const float MinimalVolume = -10f;
    private const float WhisperFade = 4f;

    private float _volume;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.BarksVolume, OnVolumeChanged, true);
        SubscribeNetworkEvent<PlaySpeechBarksEvent>(OnEntitySpoke);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(GoobCVars.BarksVolume, OnVolumeChanged);
    }

    private readonly List<string> _sampleText = new()
    {
        "Тест мессЭдж 1.",
        "Тест мессЭдж 2!",
        "Тест мессЭдж 3?",
        "Здесь был котя.",
        "Здесь был КВЕРТИ :3.",
        "Сьешь этих французких булок, да выпей чаю."
    };

    private void OnVolumeChanged(float volume) => _volume = volume;

    private float AdjustVolume(bool isWhisper)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);

        return volume;
    }

    private async void OnEntitySpoke(PlaySpeechBarksEvent ev)
    {
        if (!_cfg.GetCVar(GoobCVars.BarksEnabled) || ev.Message == null || ev.Source == null)
            return;

        var entity = GetEntity(ev.Source.Value);
        if (!Exists(entity) || TerminatingOrDeleted(entity) || !HasComp<TransformComponent>(entity))
            return;

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(ev.IsWhisper))
            .WithMaxDistance(ev.IsWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange);

        if (ev.Message.EndsWith('!'))
            audioParams = audioParams.WithVolume(audioParams.Volume * 1.2f);

        var count = (int) (ev.Message.Length / 3f);

        for (var i = 0; i < count; i++)
        {
            if (_player.LocalSession == null || TerminatingOrDeleted(entity))
                break;

            _audio.PlayEntity(
                ev.SoundSpecifier,
                _player.LocalSession,
                entity,
                audioParams.WithPitchScale(_random.NextFloat(ev.Pitch - 0.1f, ev.Pitch + 0.1f)));

            await Task.Delay(TimeSpan.FromSeconds(_random.NextFloat(ev.LowVar, ev.HighVar)));
        }
    }

    public async void PlayDataPreview(string protoId, float pitch, float lowVar, float highVar)
    {
        if (!_proto.TryIndex<BarkPrototype>(protoId, out var proto))
            return;

        var message = _random.Pick(_sampleText);
        var audioParams = AudioParams.Default.WithVolume(AdjustVolume(false));

        if (message.EndsWith('!'))
            audioParams = audioParams.WithVolume(audioParams.Volume * 1.2f);

        var count = (int) (message.Length / 3f);

        for (var i = 0; i < count; i++)
        {
            if (_player.LocalSession == null)
                break;

            _audio.PlayGlobal(
                proto.Sound,
                _player.LocalSession,
                audioParams.WithPitchScale(_random.NextFloat(pitch - 0.1f, pitch + 0.1f)));

            await Task.Delay(TimeSpan.FromSeconds(_random.NextFloat(lowVar, highVar)));
        }
    }
}
