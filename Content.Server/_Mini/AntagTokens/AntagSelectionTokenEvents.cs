// SPDX-FileCopyrightText: 2026 Casha
using System.Collections.Generic;
using Content.Server.Antag.Components;
using Robust.Shared.Player;

namespace Content.Server._Mini.AntagTokens;

public sealed class AntagSelectionGetForcedCandidatesEvent(
    AntagSelectionDefinition definition,
    IList<ICommonSession> playerPool) : EntityEventArgs
{
    public AntagSelectionDefinition Definition { get; } = definition;
    public IList<ICommonSession> PlayerPool { get; } = playerPool;
    public List<ICommonSession> ForcedSessions { get; } = new();
}

public sealed class AntagSelectionBypassPreferenceCheckEvent(
    ICommonSession? session,
    AntagSelectionDefinition definition) : EntityEventArgs
{
    public ICommonSession? Session { get; } = session;
    public AntagSelectionDefinition Definition { get; } = definition;
    public bool Bypass { get; set; }
}

public sealed class AntagSelectionExcludeSessionEvent(
    ICommonSession session,
    AntagSelectionDefinition definition) : EntityEventArgs
{
    public ICommonSession Session { get; } = session;
    public AntagSelectionDefinition Definition { get; } = definition;
    public bool Excluded { get; set; }
}
