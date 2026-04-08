using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Mini.Helpers;

public sealed class ChatIconsHelpersSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public const string NoIdIconRsiPath = "/Textures/Interface/Misc/job_icons.rsi";
    public const string JobIconsRsiPath = NoIdIconRsiPath;
    public const string NoIdIconState = "NoId";

    [PublicAPI]
    public string GetJobIcon(ProtoId<JobPrototype>? job, int scale = 1)
    {
        if (!_prototype.TryIndex(job, out var jobPrototype))
            return BuildIconMarkup(GetFallbackJobIconSpecifier(), scale);

        return BuildIconMarkup(GetJobIconSpecifier(jobPrototype), scale);
    }

    [PublicAPI]
    public SpriteSpecifier GetJobIconSpecifier(JobPrototype job)
    {
        var icon = _prototype.Index(job.Icon);

        return icon.Icon switch
        {
            SpriteSpecifier.Texture tex => tex,
            SpriteSpecifier.Rsi rsi => rsi,
            _ => GetFallbackJobIconSpecifier(),
        };
    }

    private static SpriteSpecifier.Rsi GetFallbackJobIconSpecifier()
    {
        return new SpriteSpecifier.Rsi(new ResPath(NoIdIconRsiPath), NoIdIconState);
    }

    private string BuildIconMarkup(SpriteSpecifier icon, int scale)
    {
        return icon switch
        {
            SpriteSpecifier.Texture tex => Loc.GetString("texture-tag",
                ("path", tex.TexturePath.CanonPath),
                ("scale", scale)),
            SpriteSpecifier.Rsi rsi => Loc.GetString("texture-rsi-tag",
                ("path", rsi.RsiPath.CanonPath),
                ("state", rsi.RsiState),
                ("scale", scale)),
            _ => Loc.GetString("texture-rsi-tag",
                ("path", NoIdIconRsiPath),
                ("state", NoIdIconState),
                ("scale", scale)),
        };
    }
}
