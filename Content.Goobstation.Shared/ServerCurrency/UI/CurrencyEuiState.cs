// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.ServerCurrency.UI
{
    [Serializable, NetSerializable]
    public enum RouletteMode : byte
    {
        X2 = 0,
        X5 = 1,
        X10 = 2
    }

    [Serializable, NetSerializable]
    public sealed class CurrencyEuiState : EuiStateBase
    {
        public bool HasRouletteResult;
        public int LastRouletteBet;
        public int LastRoulettePayout;
        public float LastRouletteMultiplier;
        public int LastRouletteSpinId;

        public CurrencyEuiState(
            bool hasRouletteResult = false,
            int lastRouletteBet = 0,
            int lastRoulettePayout = 0,
            float lastRouletteMultiplier = 0f,
            int lastRouletteSpinId = 0)
        {
            HasRouletteResult = hasRouletteResult;
            LastRouletteBet = lastRouletteBet;
            LastRoulettePayout = lastRoulettePayout;
            LastRouletteMultiplier = lastRouletteMultiplier;
            LastRouletteSpinId = lastRouletteSpinId;
        }
    }

    public static class CurrencyEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Buy : EuiMessageBase
        {
            public ProtoId<TokenListingPrototype> TokenId;
        }

        [Serializable, NetSerializable]
        public sealed class SpinRoulette : EuiMessageBase
        {
            public int Bet;
            public int SpinId;
            public RouletteMode Mode = RouletteMode.X2;
            public bool FastSpin;
        }
    }
}
