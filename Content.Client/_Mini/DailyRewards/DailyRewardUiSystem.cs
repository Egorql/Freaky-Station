// SPDX-FileCopyrightText: 2026 Casha
using Content.Shared._Mini.DailyRewards;
using Robust.Shared.Localization;

namespace Content.Client._Mini.DailyRewards;

public sealed class DailyRewardUiSystem : EntitySystem
{
    private DailyRewardWindow? _window;
    private bool _awaitingOpen;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DailyRewardStateEvent>(OnState);
    }

    public void RequestOpen()
    {
        _awaitingOpen = true;
        RaiseNetworkEvent(new DailyRewardOpenRequestEvent());
    }

    private void OnState(DailyRewardStateEvent ev)
    {
        if (_window == null || _window.Disposed)
        {
            if (!_awaitingOpen)
                return;

            EnsureWindow();
        }

        _window?.UpdateState(ev.State);
    }

    private void EnsureWindow()
    {
        if (_window != null && !_window.Disposed)
            return;

        CleanupWindow();

        _window = new DailyRewardWindow();
        _window.Title = Loc.GetString("daily-reward-window-title");
        _window.OnClaimPressed += OnClaimPressed;
        _window.OnClose += OnWindowClosed;
        _window.OpenCentered();
    }

    private void OnClaimPressed()
    {
        RaiseNetworkEvent(new DailyRewardClaimRequestEvent());
    }

    private void OnWindowClosed()
    {
        CleanupWindow();
        _awaitingOpen = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _window?.AdvanceTimers(frameTime);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CleanupWindow();
        _awaitingOpen = false;
    }

    private void CleanupWindow()
    {
        if (_window == null)
            return;

        _window.OnClaimPressed -= OnClaimPressed;
        _window.OnClose -= OnWindowClosed;

        if (!_window.Disposed)
            _window.Dispose();

        _window = null;
    }
}
