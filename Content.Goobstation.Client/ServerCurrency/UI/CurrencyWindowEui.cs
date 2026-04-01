// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Goobstation.Shared.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency.UI;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.ServerCurrency.UI
{
    public sealed class CurrencyEui : BaseEui
    {
        private readonly CurrencyWindow _window;
        public CurrencyEui()
        {
            _window = new CurrencyWindow();
            _window.OnClose += () => SendMessage(new CloseEuiMessage());
            _window.OnBuy += OnBuyMsg;
            _window.OnSpinRoulette += OnSpinRouletteMsg;
        }

        private void OnBuyMsg(ProtoId<TokenListingPrototype> tokenId)
        {
            SendMessage(new CurrencyEuiMsg.Buy
            {
                TokenId = tokenId
            });
            SendMessage(new CloseEuiMessage());
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            if (state is not CurrencyEuiState cast || !cast.HasRouletteResult)
                return;

            _window.SetRouletteResult(cast.LastRouletteSpinId, cast.LastRouletteBet, cast.LastRoulettePayout, cast.LastRouletteMultiplier);
        }

        private void OnSpinRouletteMsg(int bet, int spinId, RouletteMode mode, bool fastSpin)
        {
            SendMessage(new CurrencyEuiMsg.SpinRoulette
            {
                Bet = bet,
                SpinId = spinId,
                Mode = mode,
                FastSpin = fastSpin
            });
        }
    }
}
