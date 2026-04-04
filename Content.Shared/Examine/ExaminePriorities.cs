// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Examine;

/// <summary>
/// Shared ordering priorities for detailed examine lines.
/// Higher values appear earlier in the final examine text.
/// </summary>
public static class ExaminePriorities
{
    /// <summary>
    /// Feature-specific status lines that should sit above generic condition text.
    /// </summary>
    public const int FeatureStatus = 5;

    /// <summary>
    /// Character condition/state text that should read like a footer, but stay above damage severity.
    /// </summary>
    public const int CharacterStateFooter = -5;
}
