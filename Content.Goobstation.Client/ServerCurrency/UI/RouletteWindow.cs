// SPDX-FileCopyrightText: 2026 ChatGPT
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Numerics;
using System.Linq;
using System.Threading;
using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency.UI;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Goobstation.Client.ServerCurrency.UI;

public sealed class RouletteWindow : DefaultWindow
{
    private const int MinBet = 10;
    private static readonly (ResPath RsiPath, string StateId)[] SlotSprites =
    {
        (new("/Textures/Clothing/Head/Misc/fancycrown.rsi"), "icon"),
        (new("/Textures/Objects/Misc/coins.rsi"), "coin_gold"),
        (new("/Textures/Objects/Misc/coins.rsi"), "coin_diamond"),
        (new("/Textures/_Goobstation/Objects/Specific/Hydroponics/cherry.rsi"), "produce"),
        (new("/Textures/Structures/Wallmounts/signs.rsi"), "seven"),
        (new("/Textures/Objects/Misc/coins.rsi"), "coin_iron"),
        (new("/Textures/Objects/Misc/coins.rsi"), "coin_silver"),
        (new("/Textures/Objects/Misc/books.rsi"), "icon_skull"),
    };

    [Dependency] private readonly ICommonCurrencyManager _currency = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    public event Action<int, int, RouletteMode, bool>? SpinRequested;

    private readonly Label _balanceLabel;
    private readonly TextureRect _slotOne;
    private readonly TextureRect _slotTwo;
    private readonly TextureRect _slotThree;
    private readonly LineEdit _betInput;
    private readonly Button _spinButton;
    private readonly Label _resultLabel;
    private readonly Label _modeLabel;
    private readonly CheckBox _fastSpinCheck;

    private static List<Texture>? _sharedSlotTextures;

    private readonly Dictionary<RouletteMode, Button> _modeButtons = new();
    private readonly List<Texture> _slotTextures;
    private RouletteMode _selectedMode = RouletteMode.X2;

    private bool _isSpinning;
    private int _pendingSpinId;
    private DateTime _spinStartedAt;
    private int _spinFrame;
    private int _decelerationStep;
    private bool _resultPending;
    private int _resultBet;
    private int _resultPayout;
    private float _resultMultiplier;
    private bool _fastSpinActive;
    private DateTime _nextSpinFrameAt;
    private double _frameDelayMs;
    private CancellationTokenSource? _spinLoopCts;
    private int _slotOneIndex = -1;
    private int _slotTwoIndex = -1;
    private int _slotThreeIndex = -1;

    public RouletteWindow()
    {
        IoCManager.InjectDependencies(this);
        _slotTextures = GetOrLoadSlotTextures();

        Title = Loc.GetString("gs-balanceui-roulette-label");
        MinSize = new Vector2(560, 430);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8)
        };
        Contents.AddChild(root);

        _balanceLabel = new Label { HorizontalAlignment = HAlignment.Center };
        root.AddChild(_balanceLabel);
        root.AddChild(new Control { MinSize = new Vector2(0, 8) });

        var slotsRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 8
        };
        root.AddChild(slotsRow);

        _slotOne = BuildSlot(slotsRow);
        _slotTwo = BuildSlot(slotsRow);
        _slotThree = BuildSlot(slotsRow);

        root.AddChild(new Control { MinSize = new Vector2(0, 10) });

        var controlsRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 6
        };
        root.AddChild(controlsRow);

        _betInput = new LineEdit
        {
            PlaceHolder = Loc.GetString("gs-balanceui-roulette-bet"),
            MinWidth = 170
        };
        controlsRow.AddChild(_betInput);

        _spinButton = new Button
        {
            Text = Loc.GetString("gs-balanceui-roulette-spin")
        };
        _spinButton.AddStyleClass("ButtonColorRed");
        controlsRow.AddChild(_spinButton);

        _fastSpinCheck = new CheckBox
        {
            Text = Loc.GetString("gs-balanceui-roulette-fast-spin")
        };
        controlsRow.AddChild(_fastSpinCheck);

        root.AddChild(new Control { MinSize = new Vector2(0, 8) });
        root.AddChild(new Label
        {
            Text = Loc.GetString("gs-balanceui-roulette-mode-mult"),
            HorizontalAlignment = HAlignment.Center
        });
        root.AddChild(BuildModeRow(new[] { RouletteMode.X2, RouletteMode.X5, RouletteMode.X10 }));

        root.AddChild(new Control { MinSize = new Vector2(0, 6) });
        _modeLabel = new Label { HorizontalAlignment = HAlignment.Center };
        root.AddChild(_modeLabel);
        root.AddChild(new Control { MinSize = new Vector2(0, 6) });

        _resultLabel = new Label { HorizontalAlignment = HAlignment.Center };
        root.AddChild(_resultLabel);

        _spinButton.OnPressed += _ => RequestSpin();
        _currency.ClientBalanceChange += UpdateBalanceLabel;
        OnClose += () =>
        {
            _currency.ClientBalanceChange -= UpdateBalanceLabel;
            StopSpinLoop();
        };

        UpdateModeButtons();
        UpdateSlotsForResult(0f, 0);
        UpdateBalanceLabel();
    }

    private List<Texture> GetOrLoadSlotTextures()
    {
        if (_sharedSlotTextures != null)
            return _sharedSlotTextures;

        var textures = new List<Texture>(SlotSprites.Length);
        foreach (var (rsiPath, stateId) in SlotSprites)
        {
            if (!_resources.TryGetResource(rsiPath, out RSIResource? rsiResource))
                continue;

            if (!rsiResource.RSI.TryGetState(stateId, out var state))
                continue;

            textures.Add(state.GetFrame(RsiDirection.South, 0));
        }

        // If no slot textures are available, fall back to built-in UI textures.
        if (textures.Count == 0)
        {
            if (_resources.TryGetResource("/Textures/Interface/white.png", out TextureResource? fallbackTexture))
            {
                textures = Enumerable.Repeat<Texture>(fallbackTexture, 8).ToList();
            }
            else
            {
                var noSpriteTexture = _resources.GetResource<TextureResource>("/Textures/noSprite.png");
                textures = Enumerable.Repeat<Texture>(noSpriteTexture, 8).ToList();
            }
        }

        _sharedSlotTextures = textures;
        return _sharedSlotTextures;
    }

    private BoxContainer BuildModeRow(IEnumerable<RouletteMode> modes)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 6
        };

        foreach (var mode in modes)
        {
            var button = new Button
            {
                Text = Loc.GetString(GetModeTextKey(mode)),
                MinWidth = 80
            };

            button.OnPressed += _ =>
            {
                if (_isSpinning)
                    return;

                _selectedMode = mode;
                UpdateModeButtons();
            };

            _modeButtons[mode] = button;
            row.AddChild(button);
        }

        return row;
    }

    private void UpdateModeButtons()
    {
        foreach (var (mode, button) in _modeButtons)
        {
            var selected = mode == _selectedMode;
            var key = GetModeTextKey(mode);
            button.Text = selected
                ? $"> {Loc.GetString(key)} <"
                : Loc.GetString(key);
            button.Disabled = _isSpinning && !selected;
        }

        var (chance, multiplier) = GetModeOdds(_selectedMode);
        _modeLabel.Text = Loc.GetString("gs-balanceui-roulette-mode-selected",
            ("mode", Loc.GetString(GetModeTextKey(_selectedMode))),
            ("chance", (int) MathF.Round(chance * 100f)),
            ("multiplier", multiplier.ToString("0.##", CultureInfo.InvariantCulture)));
    }

    private TextureRect BuildSlot(Control parent)
    {
        var panel = new PanelContainer
        {
            MinSize = new Vector2(150, 90)
        };
        parent.AddChild(panel);

        var slot = new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Texture = _slotTextures[0]
        };
        panel.AddChild(slot);
        return slot;
    }

    private void RequestSpin()
    {
        if (_isSpinning)
            return;

        if (!TryParsePositiveInt(_betInput.Text, out var bet) || bet < MinBet)
        {
            _resultLabel.Text = Loc.GetString("gs-balanceui-roulette-invalid-bet", ("min", MinBet));
            return;
        }

        _isSpinning = true;
        _resultPending = false;
        _decelerationStep = 0;
        _spinFrame = 0;
        _pendingSpinId++;
        _spinStartedAt = DateTime.UtcNow;
        _spinButton.Disabled = true;
        _fastSpinCheck.Disabled = true;
        UpdateModeButtons();
        _resultLabel.Text = Loc.GetString("gs-balanceui-roulette-spinning");

        var fastSpin = _fastSpinCheck.Pressed;
        StartSpinLoop(fastSpin);
        SpinRequested?.Invoke(bet, _pendingSpinId, _selectedMode, fastSpin);
    }

    public void ApplyResult(int spinId, int bet, int payout, float multiplier)
    {
        if (spinId != _pendingSpinId)
            return;

        _resultPending = true;
        _resultBet = bet;
        _resultPayout = payout;
        _resultMultiplier = multiplier;
    }

    public void SetBalance(int balance)
    {
        _balanceLabel.Text = Loc.GetString("gs-balanceui-roulette-balance", ("balance", _currency.Stringify(balance)));
    }

    private void StartSpinLoop(bool fastSpin)
    {
        StopSpinLoop();

        _fastSpinActive = fastSpin;
        _frameDelayMs = fastSpin ? 36 : 74;
        _nextSpinFrameAt = DateTime.UtcNow;
        _spinLoopCts = new CancellationTokenSource();
        Timer.SpawnRepeating(16, SpinFrameUpdate, _spinLoopCts.Token);
    }

    private void StopSpinLoop()
    {
        if (_spinLoopCts == null)
            return;

        _spinLoopCts.Cancel();
        _spinLoopCts.Dispose();
        _spinLoopCts = null;
    }

    private void SpinFrameUpdate()
    {
        if (!_isSpinning)
        {
            StopSpinLoop();
            return;
        }

        var now = DateTime.UtcNow;
        if (now < _nextSpinFrameAt)
            return;

        _nextSpinFrameAt = now.AddMilliseconds(_frameDelayMs);
        _spinFrame++;

        var (step1, step2, step3) = (2, 3, 5);

        var i1 = (_spinFrame * step1) % _slotTextures.Count;
        var i2 = (_spinFrame * step2 + 1) % _slotTextures.Count;
        var i3 = (_spinFrame * step3 + 3) % _slotTextures.Count;
        SetSlotTexture(_slotOne, ref _slotOneIndex, i1);
        if (!_fastSpinActive || (_spinFrame % 3) == 0)
            SetSlotTexture(_slotTwo, ref _slotTwoIndex, i2);
        if (!_fastSpinActive || (_spinFrame % 5) == 0)
            SetSlotTexture(_slotThree, ref _slotThreeIndex, i3);

        var elapsed = now - _spinStartedAt;
        var minSpinTime = _fastSpinActive ? TimeSpan.FromMilliseconds(650) : TimeSpan.FromMilliseconds(2800);

        if (_resultPending && elapsed >= minSpinTime)
        {
            _decelerationStep++;
            var decelCap = _fastSpinActive ? 4 : 8;
            if (_decelerationStep >= decelCap)
            {
                FinalizeResult(_resultBet, _resultPayout, _resultMultiplier);
                return;
            }

            var baseMs = _fastSpinActive ? 32 : 86;
            _frameDelayMs = baseMs + _decelerationStep * (_fastSpinActive ? 10 : 20);
        }
        else
        {
            var floor = _fastSpinActive ? 22 : 44;
            if (_frameDelayMs > floor)
                _frameDelayMs -= _fastSpinActive ? 1 : 2;
        }
    }

    private void FinalizeResult(int bet, int payout, float multiplier)
    {
        _isSpinning = false;
        StopSpinLoop();
        _spinButton.Disabled = false;
        _fastSpinCheck.Disabled = false;
        UpdateModeButtons();
        UpdateSlotsForResult(multiplier, payout);

        var formattedMultiplier = multiplier.ToString("0.##", CultureInfo.InvariantCulture);
        _resultLabel.Text = payout <= 0
            ? Loc.GetString("gs-balanceui-roulette-result-lose", ("bet", bet), ("multiplier", formattedMultiplier))
            : Loc.GetString("gs-balanceui-roulette-result-win", ("bet", bet), ("multiplier", formattedMultiplier), ("payout", payout));
    }

    private void UpdateSlotsForResult(float multiplier, int payout)
    {
        if (Math.Abs(multiplier - 10f) < 0.001f)
        {
            SetSlotTexture(_slotOne, ref _slotOneIndex, 0);
            SetSlotTexture(_slotTwo, ref _slotTwoIndex, 0);
            SetSlotTexture(_slotThree, ref _slotThreeIndex, 0);
            return;
        }

        if (payout <= 0)
        {
            SetSlotTexture(_slotOne, ref _slotOneIndex, 5 % _slotTextures.Count);
            SetSlotTexture(_slotTwo, ref _slotTwoIndex, 6 % _slotTextures.Count);
            SetSlotTexture(_slotThree, ref _slotThreeIndex, 7 % _slotTextures.Count);
            return;
        }

        var winIndex = Math.Clamp((int) MathF.Round(multiplier), 1, _slotTextures.Count - 1);
        SetSlotTexture(_slotOne, ref _slotOneIndex, winIndex);
        SetSlotTexture(_slotTwo, ref _slotTwoIndex, (winIndex + 1) % _slotTextures.Count);
        SetSlotTexture(_slotThree, ref _slotThreeIndex, (winIndex + 2) % _slotTextures.Count);
    }

    private void SetSlotTexture(TextureRect slot, ref int currentIndex, int newIndex)
    {
        if (currentIndex == newIndex)
            return;

        currentIndex = newIndex;
        slot.Texture = _slotTextures[newIndex];
    }

    private static string GetModeTextKey(RouletteMode mode)
    {
        return mode switch
        {
            RouletteMode.X2 => "gs-balanceui-roulette-mode-x2",
            RouletteMode.X5 => "gs-balanceui-roulette-mode-x5",
            RouletteMode.X10 => "gs-balanceui-roulette-mode-x10",
            _ => "gs-balanceui-roulette-mode-x2"
        };
    }

    private static (float Chance, float Multiplier) GetModeOdds(RouletteMode mode)
    {
        return mode switch
        {
            RouletteMode.X2 => (0.50f, 2f),
            RouletteMode.X5 => (0.20f, 5f),
            RouletteMode.X10 => (0.10f, 10f),
            _ => (0.50f, 2f)
        };
    }

    private void UpdateBalanceLabel()
    {
        SetBalance(_currency.GetBalance());
    }

    private static bool TryParsePositiveInt(string? value, out int amount)
    {
        if (!int.TryParse(value, out amount))
            return false;

        return amount > 0;
    }
}
