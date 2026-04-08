// SPDX-FileCopyrightText: 2024 Your Name <you@example.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Maths;

namespace Content.Client.Stylesheets
{
    public static class YellowButtonStyles
    {
        // Константы цветов - адаптированы для прозрачного тёмного интерфейса
        // Добавлена прозрачность (alpha = 0.85-0.95) и более светлые оттенки

        // Основные жёлтые (светлые, полупрозрачные)
        public static readonly Color ButtonColorDefaultYellow = Color.FromHex("#F0C415CC"); // 0.80 прозрачности
        public static readonly Color ButtonColorHoveredYellow = Color.FromHex("#FFD54FCC");
        public static readonly Color ButtonColorPressedYellow = Color.FromHex("#D4A800CC");
        public static readonly Color ButtonColorDisabledYellow = Color.FromHex("#6B5A2ACC");

        // Текст - светлый для контраста с тёмным фоном
        public static readonly Color ButtonColorTextYellow = Color.FromHex("#FFFFFF");
        public static readonly Color ButtonColorTextYellowDark = Color.FromHex("#1A1A1A");

        // Классы стилей
        public const string StyleClassButtonColorYellow = "ButtonColorYellow";
        public const string StyleClassButtonColorYellowBright = "ButtonColorYellowBright";
        public const string StyleClassButtonColorYellowDark = "ButtonColorYellowDark";
        public const string StyleClassButtonColorYellowCaution = "ButtonColorYellowCaution";

        // Яркие варианты (ещё светлее, почти белые с жёлтым оттенком)
        public static readonly Color ButtonColorDefaultYellowBright = Color.FromHex("#FFF59DE6"); // ~0.90 прозрачности
        public static readonly Color ButtonColorHoveredYellowBright = Color.FromHex("#FFF9C4E6");
        public static readonly Color ButtonColorPressedYellowBright = Color.FromHex("#FFE082E6");
        public static readonly Color ButtonColorDisabledYellowBright = Color.FromHex("#A09850E6");

        // Тёмные варианты (но всё равно светлее оригинала, с прозрачностью)
        public static readonly Color ButtonColorDefaultYellowDark = Color.FromHex("#FFB300CC");
        public static readonly Color ButtonColorHoveredYellowDark = Color.FromHex("#FFCA28CC");
        public static readonly Color ButtonColorPressedYellowDark = Color.FromHex("#FF8F00CC");
        public static readonly Color ButtonColorDisabledYellowDark = Color.FromHex("#805E00CC");

        // Предупреждающие варианты (оранжево-жёлтые, яркие)
        public static readonly Color ButtonColorDefaultYellowCaution = Color.FromHex("#FFAB40E6");
        public static readonly Color ButtonColorHoveredYellowCaution = Color.FromHex("#FFC107E6");
        public static readonly Color ButtonColorPressedYellowCaution = Color.FromHex("#FF9800E6");
        public static readonly Color ButtonColorDisabledYellowCaution = Color.FromHex("#A85A00E6");

        // Текст для разных вариантов
        public static readonly Color ButtonColorTextYellowBright = Color.FromHex("#1A1A1A"); // тёмный текст для ярких кнопок
        public static readonly Color ButtonColorTextYellowCaution = Color.FromHex("#FFFFFF"); // белый текст для предупреждений
    }
}
