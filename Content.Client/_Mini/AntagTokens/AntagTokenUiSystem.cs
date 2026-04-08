// SPDX-FileCopyrightText: 2026 Casha
using Content.Shared._Mini.AntagTokens;
using Robust.Shared.Localization;

namespace Content.Client._Mini.AntagTokens;

public sealed class AntagTokenUiSystem : EntitySystem
{
    private AntagTokenWindow? _window;
    private bool _awaitingOpen;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagTokenStateEvent>(OnState);
    }

    public void RequestOpen()
    {
        _awaitingOpen = true;
        RaiseNetworkEvent(new AntagTokenOpenRequestEvent());
    }

    private void OnState(AntagTokenStateEvent ev)
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

        _window = new AntagTokenWindow();
        _window.Title = Loc.GetString("antag-token-window-title");
        _window.OnPurchasePressed += OnPurchasePressed;
        _window.OnClearPressed += OnClearPressed;
        _window.OnClose += OnClosed;
        _window.OpenCentered();
    }

    private void OnPurchasePressed(string roleId)
    {
        RaiseNetworkEvent(new AntagTokenPurchaseRequestEvent(roleId));
    }

    private void OnClearPressed()
    {
        RaiseNetworkEvent(new AntagTokenClearRequestEvent());
    }

    private void OnClosed()
    {
        CleanupWindow();
        _awaitingOpen = false;
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

        _window.OnPurchasePressed -= OnPurchasePressed;
        _window.OnClearPressed -= OnClearPressed;
        _window.OnClose -= OnClosed;

        if (!_window.Disposed)
            _window.Dispose();

        _window = null;
    }
}
