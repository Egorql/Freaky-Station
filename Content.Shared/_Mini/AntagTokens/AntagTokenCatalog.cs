// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;

namespace Content.Shared._Mini.AntagTokens;

public static class AntagTokenCatalog
{
    public const string BalanceEntryId = "balance";
    public const string MonthlyEarnedEntryId = "monthly-earned";
    public const string MonthlyYearEntryId = "monthly-year";
    public const string MonthlyMonthEntryId = "monthly-month";
    public const string DepositSelectionTokenId = "deposit";

    public const string CurrencyIconPath = "/Textures/_Mini/AntagStore/token_currency.png";

    public const string ThiefRole = "thief";
    public const string AgentRole = "agent";
    public const string NinjaRole = "ninja";
    public const string DragonRole = "dragon";
    public const string AbductorRole = "abductor";
    public const string InitialInfectedRole = "initial_infected";
    public const string RevenantRole = "revenant";
    public const string YaoRole = "yao";
    public const string HeadRevRole = "headrev";
    public const string CosmicCultRole = "cosmic_cult";
    public const string DevilRole = "devil";
    public const string BlobRole = "blob";
    public const string WizardRole = "wizard";
    public const string SlaughterDemonRole = "slaughter_demon";
    public const string ChangelingRole = "changeling";
    public const string HereticRole = "heretic";
    public const string ShadowlingRole = "shadowling";

    private static readonly Dictionary<int, int> SponsorMonthlyCaps = new()
    {
        [1] = 10,
        [2] = 15,
        [3] = 20,
        [4] = 30,
        [5] = 50,
    };

    private static readonly Dictionary<string, AntagRoleDefinition> RoleDefinitions = new()
    {
        [ThiefRole] = new(ThiefRole, "antag-store-role-thief-name", "antag-store-role-thief-description", 1,
            "/Textures/_Mini/AntagStore/thief.png", AntagPurchaseMode.LobbyDeposit, "Thief", "OopsAllThieves"),
        [AgentRole] = new(AgentRole, "antag-store-role-agent-name", "antag-store-role-agent-description", 2,
            "/Textures/_Mini/AntagStore/agent.png", AntagPurchaseMode.LobbyDeposit, "Traitor", "Traitor"),
        [NinjaRole] = new(NinjaRole, "antag-store-role-ninja-name", "antag-store-role-ninja-description", 3,
            "/Textures/_Mini/AntagStore/ninja.png", AntagPurchaseMode.GhostRule, null, "TokenNinjaSpawn"),
        [DragonRole] = new(DragonRole, "antag-store-role-dragon-name", "antag-store-role-dragon-description", 3,
            "/Textures/_Mini/AntagStore/dragon.png", AntagPurchaseMode.GhostRule, null, "TokenDragonSpawn"),
        [AbductorRole] = new(AbductorRole, "antag-store-role-abductor-name", "antag-store-role-abductor-description", 3,
            "/Textures/_Mini/AntagStore/abductor.png", AntagPurchaseMode.GhostRule, null, "TokenLoneAbductorSpawn"),
        [InitialInfectedRole] = new(InitialInfectedRole, "antag-store-role-initial-infected-name", "antag-store-role-initial-infected-description", 3,
            "/Textures/_Mini/AntagStore/initial_infected.png", AntagPurchaseMode.LobbyDeposit, "InitialInfected", "Zombie"),
        [RevenantRole] = new(RevenantRole, "antag-store-role-revenant-name", "antag-store-role-revenant-description", 2,
            "/Textures/_Mini/AntagStore/revenant.png", AntagPurchaseMode.GhostRule, null, "TokenRevenantSpawn"),
        [YaoRole] = new(YaoRole, "antag-store-role-yao-name", "antag-store-role-yao-description", 5,
            "/Textures/_Mini/AntagStore/yao.png", AntagPurchaseMode.GhostRule, null, "TokenLoneOpsSpawn"),
        [HeadRevRole] = new(HeadRevRole, "antag-store-role-headrev-name", "antag-store-role-headrev-description", 5,
            "/Textures/_Mini/AntagStore/headrev.png", AntagPurchaseMode.LobbyDeposit, "HeadRev", "Revolutionary"),
        [CosmicCultRole] = new(CosmicCultRole, "antag-store-role-cosmic-cult-name", "antag-store-role-cosmic-cult-description", 4,
            "/Textures/_Mini/AntagStore/cosmic_cult.png", AntagPurchaseMode.LobbyDeposit, "CosmicAntagCultist", "CosmicCult"),
        [DevilRole] = new(DevilRole, "antag-store-role-devil-name", "antag-store-role-devil-description", 3,
            "/Textures/_Mini/AntagStore/devil.png", AntagPurchaseMode.LobbyDeposit, "Devil", "Devil"),
        [BlobRole] = new(BlobRole, "antag-store-role-blob-name", "antag-store-role-blob-description", 10,
            "/Textures/_Mini/AntagStore/blob.png", AntagPurchaseMode.GhostRule, null, "TokenBlobMidround"),
        [WizardRole] = new(WizardRole, "antag-store-role-wizard-name", "antag-store-role-wizard-description", 8,
            "/Textures/_Mini/AntagStore/wizard.png", AntagPurchaseMode.GhostRule, null, "TokenWizard"),
        [SlaughterDemonRole] = new(SlaughterDemonRole, "antag-store-role-slaughter-demon-name", "antag-store-role-slaughter-demon-description", 10,
            "/Textures/_Mini/AntagStore/slaughter_demon.png", AntagPurchaseMode.GhostRule, null, "TokenSlaughterDemon"),
        [ChangelingRole] = new(ChangelingRole, "antag-store-role-changeling-name", "antag-store-role-changeling-description", 8,
            "/Textures/_Mini/AntagStore/changeling.png", AntagPurchaseMode.LobbyDeposit, "Changeling", "Changeling"),
        [HereticRole] = new(HereticRole, "antag-store-role-heretic-name", "antag-store-role-heretic-description", 6,
            "/Textures/_Mini/AntagStore/heretic.png", AntagPurchaseMode.LobbyDeposit, "Heretic", "Heretic"),
        [ShadowlingRole] = new(ShadowlingRole, "antag-store-role-shadowling-name", "antag-store-role-shadowling-description", 6,
            "/Textures/_Mini/AntagStore/shadowling.png", AntagPurchaseMode.LobbyDeposit, "Shadowling", "Shadowling"),
    };

    public static IReadOnlyDictionary<string, AntagRoleDefinition> Roles => RoleDefinitions;

    public static bool TryGetRole(string roleId, out AntagRoleDefinition definition)
    {
        return RoleDefinitions.TryGetValue(roleId, out definition!);
    }

    public static int? GetSponsorMonthlyCap(int sponsorLevel)
    {
        return SponsorMonthlyCaps.GetValueOrDefault(sponsorLevel);
    }
}

public enum AntagPurchaseMode : byte
{
    LobbyDeposit,
    GhostRule,
    Unavailable,
}

public sealed record AntagRoleDefinition(
    string Id,
    string NameLocKey,
    string DescriptionLocKey,
    int Cost,
    string IconPath,
    AntagPurchaseMode Mode,
    string? AntagId = null,
    string? GameRuleId = null,
    string? UnavailableReasonLocKey = null);
