using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server._FreakyStation.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.ERP;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Alert;
using System.IO;
namespace Content.Server.ERP
{
    public sealed class ERPSystem : EntitySystem
    {
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        [Dependency] protected readonly IGameTiming _gameTiming = default!;
        [Dependency] protected readonly ChatSystem _chat = default!;
        [Dependency] protected readonly IRobustRandom _random = default!;
        [Dependency] private readonly ClothingStainSystem _clothingStains = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ERPComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
            SubscribeLocalEvent<ERPComponent, ExaminedEvent>(OnExamine);
        }
        private void OnExamine(EntityUid uid, ERPComponent component, ExaminedEvent args)
        {
            if (component.Erp)
            {
                args.PushMarkup("[color=#FF1488][bold]ERP статус — ВКЛЮЧЕНО.[/color][/bold]"); //klyanus slychayno takoy hex postavil
            }
        }
        private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
        {
            if (ev.Profile is not HumanoidCharacterProfile profile)
                return;
            if (profile.ERPS == ERPS.Yes)
            {
                var component = EnsureComp<ERPComponent>(ev.Mob);
                component.Erp = true;
                Dirty(ev.Mob, component);
            }
        }
        public void AddLove(NetEntity entity, NetEntity target, int percent)
        {
            List<EntityUid> ents = new() { GetEntity(entity), GetEntity(target) };

            foreach (var ent in ents)
            {
                if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
                    continue;

                if (!TryComp<ERPComponent>(ent, out var comp))
                    continue;
                if (percent != 0 && comp.Erp && _gameTiming.CurTime > comp.LoveDelay)
                {
                    float added = (percent + _random.Next(-percent / 2, percent / 2)) / 100f;
                    comp.ActualLove += added;
                    comp.TimeFromLastErp = _gameTiming.CurTime;
                    Spawn("EffectHearts", Transform(ent).Coordinates);
                }
                if (comp.Love >= 1f)
                {
                    comp.ActualLove = 0f;
                    comp.Love = 0f;
                    comp.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
                    if (humanoid.Sex == Sex.Male)
                    {
                        Spawn("PuddleSperma", Transform(ent).Coordinates);
                        TryPlayErpSound("/Audio/ERP/male_orgasm.ogg", ent);
                        _clothingStains.ApplyStainsToEquippedClothing(ent, bio: true);
                    }
                    else if (humanoid.Sex == Sex.Female)
                    {
                        Spawn("EffectSquirt", Transform(ent).Coordinates);
                        TryPlayErpSound("/Audio/ERP/female_orgasm.ogg", ent);
                    }
                }
            }
        }
        private void TryPlayErpSound(string path, EntityUid uid)
        {
            try
            {
                _audioSystem.PlayPvs(path, uid);
            }
            catch (FileNotFoundException)
            {
                Log.Warning($"Missing ERP sound file: {path}");
            }
        }
        private void AddVerbs(GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;


            var player = actor.PlayerSession;
            if (args.User != args.Target)
            {
                if (!args.CanInteract || !args.CanAccess) return;
                args.Verbs.Add(new Verb
                {
                    Priority = -1,
                    Text = "ЕРП",
                    Icon = new SpriteSpecifier.Texture(new("/Textures/_FreakStation/Casha/ERPicon/erp.svg.192dpi.png")),
                    Act = () =>
                    {
                        if (!args.CanInteract || !args.CanAccess) return;
                        if (TryComp<ERPComponent>(args.Target, out var targetInteraction) && TryComp<ERPComponent>(args.User, out var userInteraction))
                        {
                            if (TryComp<HumanoidAppearanceComponent>(args.Target, out var targetHumanoid) && TryComp<HumanoidAppearanceComponent>(args.User, out var userHumanoid))
                            {
                                bool erp = true;
                                bool userClothing = false;
                                bool targetClothing = false;
                                if (!targetInteraction.Erp || !userInteraction.Erp) erp = false;
                                if (TryComp<ContainerManagerComponent>(args.User, out var container))
                                {
                                    if (container.Containers["jumpsuit"].ContainedEntities.Count != 0) userClothing = true;
                                    if (container.Containers["outerClothing"].ContainedEntities.Count != 0) userClothing = true;
                                    if (container.Containers["breast"].ContainedEntities.Count != 0) userClothing = true;
                                    if (container.Containers["underwear"].ContainedEntities.Count != 0) userClothing = true;
                                }

                                if (TryComp<ContainerManagerComponent>(args.Target, out var targetContainer))
                                {
                                    if (targetContainer.Containers["jumpsuit"].ContainedEntities.Count != 0) targetClothing = true;
                                    if (targetContainer.Containers["outerClothing"].ContainedEntities.Count != 0) targetClothing = true;
                                    if (targetContainer.Containers["breast"].ContainedEntities.Count != 0) targetClothing = true;
                                    if (targetContainer.Containers["underwear"].ContainedEntities.Count != 0) targetClothing = true;
                                }

                                _eui.OpenEui(new ERPEUI(GetNetEntity(args.Target), userHumanoid.Sex, userClothing, targetHumanoid.Sex, targetClothing, erp), player);
                            }
                        }
                    },
                    Impact = LogImpact.Low,
                });
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<ERPComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                comp.Love -= ((comp.Love - comp.ActualLove) / 1) * frameTime;
                if (_gameTiming.CurTime - comp.TimeFromLastErp > TimeSpan.FromSeconds(15) && comp.Love > 0)
                {
                    comp.ActualLove -= 0.001f;
                }
                if (comp.Love < 0) comp.Love = 0;
                if (comp.ActualLove < 0) comp.ActualLove = 0;
                comp.Love -= ((comp.Love - comp.ActualLove) / 1) * frameTime;
                if (_gameTiming.CurTime - comp.TimeFromLastErp > TimeSpan.FromSeconds(15) && comp.Love > 0)
                {
                    comp.ActualLove -= 0.001f;
                }
                comp.Love = MathHelper.Clamp(comp.Love, 0f, 1f);
                comp.ActualLove = MathHelper.Clamp(comp.ActualLove, 0f, 1f);
                float progress = comp.Love;

                if (!comp.Erp || progress < 0.1f)
                {
                    _alerts.ClearAlert(uid, "Arousal");
                    continue;
                }
                short severity = (short) Math.Clamp(Math.Floor(progress * 10) + 1, 1, 10);
                _alerts.ShowAlert(uid, "Arousal", severity);
            }
        }

        private void OnComponentInit(EntityUid uid, ERPComponent component, ComponentInit args)
        {
        }
    }
}
