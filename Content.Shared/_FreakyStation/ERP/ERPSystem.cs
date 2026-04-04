// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FreakyStation.ERP;

public abstract class SharedERPSystem : EntitySystem;

[Serializable, NetSerializable]
public enum ERPPanelMode : byte
{
    Self,
    Target,
}

[Flags]
[Serializable, NetSerializable]
public enum ERPInteractionDeniedReason : byte
{
    None = 0,
    NeedUserUncovered = 1 << 0,
    NeedTargetUncovered = 1 << 1,
    WrongUserSex = 1 << 2,
    WrongTargetSex = 1 << 3,
    TargetUnavailable = 1 << 4,
    DeadOrSsd = 1 << 5,
    OutOfRange = 1 << 6,
    Cooldown = 1 << 7,
}

[Serializable, NetSerializable]
public sealed class ERPInteractionEntryState
{
    public ProtoId<ERPPrototype> InteractionId;
    public bool Enabled;
    public ERPInteractionDeniedReason DeniedReason;
}

[Serializable, NetSerializable]
public sealed class ERPInteractionEuiState : EuiStateBase
{
    public ERPPanelMode Mode;
    public NetEntity Target;
    public TimeSpan CooldownEndTime;
    public Sex UserSex;
    public Sex TargetSex;
    public bool UserHasClothing;
    public bool TargetHasClothing;
    public ERPConsent UserConsent;
    public ERPConsent TargetConsent;
    public bool UserNonCon;
    public bool TargetNonCon;
    public float UserArousal;
    public float TargetArousal;
    public List<ERPInteractionEntryState> Interactions = new();
}

[NetSerializable, Serializable]
public sealed class PerformInteractionMessage : EuiMessageBase
{
    public ProtoId<ERPPrototype> InteractionId;

    public PerformInteractionMessage(ProtoId<ERPPrototype> interactionId)
    {
        InteractionId = interactionId;
    }
}
