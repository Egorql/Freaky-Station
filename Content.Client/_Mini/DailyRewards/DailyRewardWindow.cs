// SPDX-FileCopyrightText: 2026 Casha
using System;
using Content.Client.Resources;
using Content.Shared._Mini.DailyRewards;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System.Numerics;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Mini.DailyRewards;

public sealed class DailyRewardWindow : DefaultWindow
{
    private static readonly Color WindowBackgroundColor = Color.FromHex("#101826");
    private static readonly Color HeroPanelColor = Color.FromHex("#16263f");
    private static readonly Color AccentColor = Color.FromHex("#f2c14e");
    private static readonly Color ClaimReadyColor = Color.FromHex("#49c27a");
    private static readonly Color ClaimedCardColor = Color.FromHex("#1c5a46");
    private static readonly Color CurrentCardColor = Color.FromHex("#69511a");
    private static readonly Color FutureCardColor = Color.FromHex("#202b3d");
    private static readonly Color ClaimedBorderColor = Color.FromHex("#7de2b4");
    private static readonly Color CurrentBorderColor = Color.FromHex("#ffd978");
    private static readonly Color FutureBorderColor = Color.FromHex("#5d6d8a");

    public event Action? OnClaimPressed;

    private readonly IResourceCache _resourceCache;
    private readonly Label _streakValueLabel;
    private readonly Label _activeProgressLabel;
    private readonly Label _cooldownLabel;
    private readonly Label _expiryLabel;
    private readonly ProgressBar _activeProgressBar;
    private readonly Button _claimButton;
    private readonly BoxContainer _rewardTrack;
    private DailyRewardUpdateMessage? _state;

    public DailyRewardWindow()
    {
        IoCManager.InjectDependencies(this);
        _resourceCache = IoCManager.Resolve<IResourceCache>();

        Title = Loc.GetString("daily-reward-window-title");
        MinSize = new Vector2(980, 620);

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

        root.AddChild(BuildHeroSection(out _streakValueLabel, out _activeProgressLabel, out _cooldownLabel, out _expiryLabel, out _activeProgressBar, out _claimButton));

        var roadHeader = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 3
        };
        root.AddChild(roadHeader);

        roadHeader.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-road-title"),
            StyleClasses = { "LabelHeading" }
        });

        var rewardsPanel = new PanelContainer
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
        root.AddChild(rewardsPanel);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };
        rewardsPanel.AddChild(scroll);

        _rewardTrack = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 0
        };
        scroll.AddChild(_rewardTrack);

        _claimButton.OnPressed += _ => OnClaimPressed?.Invoke();
    }

    public void UpdateState(DailyRewardUpdateMessage state)
    {
        _state = state;
        RefreshState();
    }

    public void AdvanceTimers(float frameTime)
    {
        if (_state == null)
            return;

        var step = TimeSpan.FromSeconds(frameTime);
        var timeUntilExpiration = MaxZero(_state.TimeUntilExpiration - step);
        var timeUntilNextClaim = MaxZero(_state.TimeUntilNextClaim - step);
        var currentActiveTime = _state.CurrentActiveTime;

        if (_state.IsTrackingActiveTime && currentActiveTime < _state.RequiredActiveTime)
            currentActiveTime = Min(currentActiveTime + step, _state.RequiredActiveTime);

        var canClaim = currentActiveTime >= _state.RequiredActiveTime && timeUntilNextClaim == TimeSpan.Zero;
        _state = new DailyRewardUpdateMessage(
            _state.CurrentStreak,
            _state.NextRewardDay,
            canClaim,
            _state.IsTrackingActiveTime,
            _state.HasLastClaim,
            timeUntilExpiration,
            timeUntilNextClaim,
            currentActiveTime,
            _state.RequiredActiveTime,
            _state.Rewards);

        RefreshState();
    }

    private void RefreshState()
    {
        if (_state == null)
            return;

        var state = _state;

        _streakValueLabel.Text = Loc.GetString("daily-reward-window-streak-value",
            ("current", state.CurrentStreak),
            ("max", state.Rewards.Count));

        var progressRatio = state.RequiredActiveTime <= TimeSpan.Zero
            ? 1f
            : Math.Clamp((float) (state.CurrentActiveTime.TotalSeconds / state.RequiredActiveTime.TotalSeconds), 0f, 1f);

        _activeProgressBar.MinValue = 0;
        _activeProgressBar.MaxValue = 100;
        _activeProgressBar.Value = progressRatio * 100f;

        _activeProgressLabel.Text = progressRatio >= 1f
            ? Loc.GetString("daily-reward-window-active-ready")
            : Loc.GetString("daily-reward-window-active-progress",
                ("current", Format(state.CurrentActiveTime)),
                ("required", Format(state.RequiredActiveTime)));

        _cooldownLabel.Text = GetAvailabilityText(state);

        _expiryLabel.Text = state.HasLastClaim
            ? Loc.GetString("daily-reward-window-expiry", ("time", Format(state.TimeUntilExpiration)))
            : Loc.GetString("daily-reward-window-expiry-idle");

        _claimButton.Disabled = !state.CanClaim;
        _claimButton.Text = Loc.GetString(state.CanClaim
            ? "daily-reward-window-claim-ready"
            : "daily-reward-window-claim-locked");
        _claimButton.Modulate = state.CanClaim ? Color.White : Color.FromHex("#d5dbe8");

        _rewardTrack.RemoveAllChildren();
        for (var i = 0; i < state.Rewards.Count; i++)
        {
            var reward = state.Rewards[i];
            _rewardTrack.AddChild(CreateRewardCard(reward));

            if (i < state.Rewards.Count - 1)
                _rewardTrack.AddChild(CreateConnector(reward, state.Rewards[i + 1]));
        }
    }

    private Control BuildHeroSection(
        out Label streakValueLabel,
        out Label activeProgressLabel,
        out Label cooldownLabel,
        out Label expiryLabel,
        out ProgressBar activeProgressBar,
        out Button claimButton)
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
            Text = Loc.GetString("daily-reward-window-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        left.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-subtitle"),
            Modulate = Color.FromHex("#c5d3ed")
        });

        var streakPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1725"),
                BorderColor = Color.FromHex("#455674"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 8,
                ContentMarginRightOverride = 10,
                ContentMarginBottomOverride = 8
            }
        };
        left.AddChild(streakPanel);

        var streakBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2
        };
        streakPanel.AddChild(streakBox);

        streakBox.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-streak"),
            Modulate = Color.FromHex("#9fb4d8")
        });

        streakValueLabel = new Label
        {
            Modulate = AccentColor
        };
        streakBox.AddChild(streakValueLabel);

        activeProgressLabel = new Label
        {
            Modulate = Color.White
        };
        left.AddChild(activeProgressLabel);

        activeProgressBar = new ProgressBar
        {
            MinSize = new Vector2(0, 22),
            BackgroundStyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0c1220")
            },
            ForegroundStyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = ClaimReadyColor
            }
        };
        left.AddChild(activeProgressBar);

        var metaRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 10
        };
        left.AddChild(metaRow);

        cooldownLabel = new Label
        {
            Modulate = Color.FromHex("#d7e2f4")
        };
        metaRow.AddChild(cooldownLabel);

        expiryLabel = new Label
        {
            Modulate = Color.FromHex("#9fb4d8")
        };
        metaRow.AddChild(expiryLabel);

        var right = new PanelContainer
        {
            MinSize = new Vector2(240, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1725"),
                BorderColor = Color.FromHex("#455674"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };
        content.AddChild(right);

        var rightBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8
        };
        right.AddChild(rightBox);

        rightBox.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-claim-panel-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        claimButton = new Button
        {
            Text = Loc.GetString("daily-reward-window-claim"),
            MinSize = new Vector2(0, 42),
            Modulate = Color.White
        };
        rightBox.AddChild(claimButton);

        return panel;
    }

    private Control CreateRewardCard(DailyRewardEntry reward)
    {
        var state = GetCardState(reward);
        var (backgroundColor, borderColor) = state switch
        {
            DailyRewardCardState.Claimed => (ClaimedCardColor, ClaimedBorderColor),
            DailyRewardCardState.Current => (CurrentCardColor, CurrentBorderColor),
            _ => (FutureCardColor, FutureBorderColor)
        };

        var panel = new PanelContainer
        {
            MinSize = new Vector2(170, 248),
            Margin = new Thickness(0, 0, 10, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = backgroundColor,
                BorderColor = borderColor,
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginBottomOverride = 10
            }
        };

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8
        };
        panel.AddChild(box);

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal
        };
        box.AddChild(header);

        header.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-card-day", ("day", reward.Day)),
            StyleClasses = { "LabelHeading" },
            HorizontalExpand = true,
            Modulate = Color.White
        });

        var badge = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = borderColor,
                ContentMarginLeftOverride = 8,
                ContentMarginTopOverride = 3,
                ContentMarginRightOverride = 8,
                ContentMarginBottomOverride = 3
            }
        };
        header.AddChild(badge);

        badge.AddChild(new Label
        {
            Text = Loc.GetString(GetStatusLocKey(state)),
            Modulate = Color.Black
        });

        var imagePanel = new PanelContainer
        {
            MinSize = new Vector2(0, 100),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0c1220"),
                BorderColor = borderColor,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginTopOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginBottomOverride = 6
            }
        };
        box.AddChild(imagePanel);

        var imageBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4
        };
        imagePanel.AddChild(imageBox);

        var texture = TryGetRewardTexture(reward.IconPath);
        imageBox.AddChild(new TextureRect
        {
            Texture = texture ?? _resourceCache.GetTexture("/Textures/Interface/info.svg.192dpi.png"),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
            VerticalExpand = true
        });

        box.AddChild(new Label
        {
            Text = reward.GrantsToken
                ? Loc.GetString("daily-reward-card-token", ("name", reward.TokenName ?? "-"))
                : Loc.GetString("daily-reward-card-step"),
            Modulate = Color.White
        });

        return panel;
    }

    private Control CreateConnector(DailyRewardEntry left, DailyRewardEntry right)
    {
        var claimedAhead = left.IsClaimed && right.IsClaimed;
        var currentAhead = left.IsCurrent || right.IsCurrent;
        var color = claimedAhead
            ? ClaimedBorderColor
            : currentAhead
                ? CurrentBorderColor
                : FutureBorderColor;

        return new PanelContainer
        {
            MinSize = new Vector2(44, 6),
            Margin = new Thickness(0, 121, 10, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color
            }
        };
    }

    private Texture? TryGetRewardTexture(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
            return null;

        if (!iconPath.StartsWith('/'))
            return null;

        try
        {
            var path = new ResPath(iconPath);
            return _resourceCache.TryGetResource<TextureResource>(path, out var textureResource)
                ? textureResource.Texture
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static DailyRewardCardState GetCardState(DailyRewardEntry reward)
    {
        if (reward.IsClaimed)
            return DailyRewardCardState.Claimed;

        if (reward.IsCurrent)
            return DailyRewardCardState.Current;

        return DailyRewardCardState.Future;
    }

    private static string GetStatusLocKey(DailyRewardCardState state)
    {
        return state switch
        {
            DailyRewardCardState.Claimed => "daily-reward-card-claimed",
            DailyRewardCardState.Current => "daily-reward-card-current",
            _ => "daily-reward-card-future"
        };
    }

    private static string Format(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)
            return "00:00:00";

        var totalHours = (int) span.TotalHours;
        return $"{totalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    private static string GetAvailabilityText(DailyRewardUpdateMessage state)
    {
        if (state.TimeUntilNextClaim > TimeSpan.Zero)
            return Loc.GetString("daily-reward-window-cooldown-wait", ("time", Format(state.TimeUntilNextClaim)));

        if (state.CurrentActiveTime < state.RequiredActiveTime)
            return Loc.GetString("daily-reward-window-active-needed");

        return Loc.GetString("daily-reward-window-cooldown-ready");
    }

    private static TimeSpan MaxZero(TimeSpan span)
    {
        return span < TimeSpan.Zero ? TimeSpan.Zero : span;
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right)
    {
        return left <= right ? left : right;
    }

    private enum DailyRewardCardState : byte
    {
        Claimed,
        Current,
        Future
    }
}
