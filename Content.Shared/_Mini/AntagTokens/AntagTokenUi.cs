using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.AntagTokens;

[Serializable, NetSerializable]
public sealed class AntagTokenOpenRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenPurchaseRequestEvent(string roleId) : EntityEventArgs
{
    public string RoleId { get; } = roleId;
}

[Serializable, NetSerializable]
public sealed class AntagTokenClearRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenStateEvent(AntagTokenState state) : EntityEventArgs
{
    public AntagTokenState State { get; } = state;
}

[Serializable, NetSerializable]
public sealed class AntagTokenState(
    int balance,
    int monthlyEarned,
    int? monthlyCap,
    string? activeDepositRoleId,
    List<AntagTokenRoleEntry> roles) : BoundUserInterfaceState
{
    public int Balance { get; } = balance;
    public int MonthlyEarned { get; } = monthlyEarned;
    public int? MonthlyCap { get; } = monthlyCap;
    public string? ActiveDepositRoleId { get; } = activeDepositRoleId;
    public List<AntagTokenRoleEntry> Roles { get; } = roles;
}

[Serializable, NetSerializable]
public sealed class AntagTokenRoleEntry(
    string roleId,
    int cost,
    AntagPurchaseMode mode,
    bool purchased,
    bool canAfford,
    bool saturated,
    bool available,
    string? statusLocKey)
{
    public string RoleId { get; } = roleId;
    public int Cost { get; } = cost;
    public AntagPurchaseMode Mode { get; } = mode;
    public bool Purchased { get; } = purchased;
    public bool CanAfford { get; } = canAfford;
    public bool Saturated { get; } = saturated;
    public bool Available { get; } = available;
    public string? StatusLocKey { get; } = statusLocKey;
}
