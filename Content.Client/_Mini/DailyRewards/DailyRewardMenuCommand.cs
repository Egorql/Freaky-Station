// SPDX-FileCopyrightText: 2026 Casha
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client._Mini.DailyRewards;

public sealed class DailyRewardMenuCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "dailyrewardmenu";
    public string Description => "Opens the daily rewards menu.";
    public string Help => "Usage: dailyrewardmenu";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Help);
            return;
        }

        _entities.System<DailyRewardUiSystem>().RequestOpen();
    }
}
