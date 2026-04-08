// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Network;

namespace Content.Server._Mini.AntagTokens.Components;

[RegisterComponent]
public sealed partial class ReservedGhostRoleComponent : Component
{
    public NetUserId ReservedUserId;
}
