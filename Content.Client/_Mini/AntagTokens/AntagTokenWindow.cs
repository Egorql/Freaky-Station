// SPDX-FileCopyrightText: 2026 Casha
using System;
using System.Numerics;
using Content.Shared._Mini.AntagTokens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Mini.AntagTokens;

public sealed class AntagTokenWindow : DefaultWindow
{
    private static readonly Color WindowBackgroundColor = Color.FromHex("#101826");
    private static readonly Color HeroPanelColor = Color.FromHex("#16263f");
    private static readonly Color AccentColor = Color.FromHex("#f2c14e");

    public event Action<string>? OnPurchasePressed;
    public event Action? OnClearPressed;

    private Label _balanceLabel = null!;
    private Label _capLabel = null!;
    private Label _depositLabel = null!;
    private Button _clearButton = null!;
    private BoxContainer _roleList = null!;

    public AntagTokenWindow()
    {
        Title = Loc.GetString("antag-token-window-title");
        MinSize = new Vector2(1100, 690);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 12,
            Margin = new Thickness(14)
        };

        var backdrop = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = WindowBackgroundColor
            }
        };
        Contents.AddChild(backdrop);
        backdrop.AddChild(root);

        root.AddChild(BuildHero());

        var section = new PanelContainer
        {
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1623"),
                BorderColor = Color.FromHex("#31415f"),
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };
        root.AddChild(section);

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true
        };
        section.AddChild(scroll);

        _roleList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10
        };
        scroll.AddChild(_roleList);

        _clearButton.OnPressed += _ => OnClearPressed?.Invoke();
    }

    public void UpdateState(AntagTokenState state)
    {
        _balanceLabel.Text = Loc.GetString("antag-token-window-balance", ("amount", state.Balance));
        _capLabel.Text = state.MonthlyCap.HasValue
            ? Loc.GetString("antag-token-window-cap", ("earned", state.MonthlyEarned), ("cap", state.MonthlyCap.Value))
            : Loc.GetString("antag-token-window-cap-free", ("earned", state.MonthlyEarned));

        _depositLabel.Text = state.ActiveDepositRoleId != null &&
                             AntagTokenCatalog.TryGetRole(state.ActiveDepositRoleId, out var role)
            ? Loc.GetString("antag-token-window-deposit", ("role", Loc.GetString(role.NameLocKey)))
            : Loc.GetString("antag-token-window-no-deposit");

        _clearButton.Disabled = state.ActiveDepositRoleId == null;

        _roleList.RemoveAllChildren();
        foreach (var roleEntry in state.Roles)
        {
            _roleList.AddChild(CreateRoleCard(roleEntry));
        }
    }

    private Control BuildHero()
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = HeroPanelColor,
                BorderColor = AccentColor,
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 14,
                ContentMarginTopOverride = 14,
                ContentMarginRightOverride = 14,
                ContentMarginBottomOverride = 14
            }
        };

        var content = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 14
        };
        panel.AddChild(content);

        var left = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true
        };
        content.AddChild(left);

        left.AddChild(new Label
        {
            Text = Loc.GetString("antag-token-window-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        left.AddChild(new Label
        {
            Text = Loc.GetString("antag-token-window-subtitle"),
            Modulate = Color.FromHex("#c5d3ed")
        });

        _balanceLabel = new Label { Modulate = Color.White };
        left.AddChild(_balanceLabel);

        _capLabel = new Label { Modulate = Color.FromHex("#f3e3a1") };
        left.AddChild(_capLabel);

        _depositLabel = new Label { Modulate = Color.FromHex("#c5d3ed") };
        left.AddChild(_depositLabel);

        var right = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            MinWidth = 280
        };
        content.AddChild(right);

        right.AddChild(new Label
        {
            Text = Loc.GetString("antag-token-window-action-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = AccentColor
        });

        _clearButton = new Button
        {
            Text = Loc.GetString("antag-token-window-clear"),
            MinSize = new Vector2(220, 42)
        };
        right.AddChild(_clearButton);

        return panel;
    }

    private Control CreateRoleCard(AntagTokenRoleEntry entry)
    {
        AntagTokenCatalog.TryGetRole(entry.RoleId, out var roleDef);

        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#172235"),
                BorderColor = entry.Purchased ? AccentColor : Color.FromHex("#415578"),
                BorderThickness = new Thickness(entry.Purchased ? 2 : 1),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10
        };
        panel.AddChild(root);

        var info = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true
        };
        root.AddChild(info);

        info.AddChild(new Label
        {
            Text = roleDef == null
                ? entry.RoleId
                : Loc.GetString("antag-token-window-role-title",
                    ("name", Loc.GetString(roleDef.NameLocKey)),
                    ("cost", entry.Cost)),
            StyleClasses = { "LabelHeading" }
        });

        if (roleDef != null)
        {
            info.AddChild(new Label
            {
                Text = Loc.GetString(roleDef.DescriptionLocKey),
                Modulate = Color.FromHex("#c3d0e6")
            });
        }

        var modeLoc = entry.Mode == AntagPurchaseMode.GhostRule
            ? "antag-token-window-mode-ghost"
            : entry.Mode == AntagPurchaseMode.LobbyDeposit
                ? "antag-token-window-mode-deposit"
                : "antag-store-status-unavailable";

        info.AddChild(new Label
        {
            Text = Loc.GetString(modeLoc),
            Modulate = Color.FromHex("#f3e3a1")
        });

        if (entry.StatusLocKey != null)
        {
            info.AddChild(new Label
            {
                Text = Loc.GetString(entry.StatusLocKey),
                Modulate = entry.Purchased ? AccentColor : Color.FromHex("#ffb4a8")
            });
        }

        var buyButton = new Button
        {
            Text = GetButtonText(entry),
            MinSize = new Vector2(220, 44),
            Disabled = IsButtonDisabled(entry),
            HorizontalAlignment = HAlignment.Left
        };

        var roleId = entry.RoleId;
        buyButton.OnPressed += _ => OnPurchasePressed?.Invoke(roleId);
        root.AddChild(buyButton);

        return panel;
    }

    private static string GetButtonText(AntagTokenRoleEntry entry)
    {
        if (entry.Purchased)
            return Loc.GetString("antag-token-window-button-deposited");

        return entry.Mode switch
        {
            AntagPurchaseMode.GhostRule => Loc.GetString("antag-token-window-button-ghost"),
            AntagPurchaseMode.LobbyDeposit => Loc.GetString("antag-token-window-button-deposit"),
            _ => Loc.GetString("antag-token-window-button-unavailable")
        };
    }

    private static bool IsButtonDisabled(AntagTokenRoleEntry entry)
    {
        if (entry.Purchased || !entry.Available || !entry.CanAfford)
            return true;

        if (entry.StatusLocKey == "antag-store-status-has-other-deposit")
            return true;

        return entry.Mode == AntagPurchaseMode.LobbyDeposit && entry.Saturated;
    }
}
