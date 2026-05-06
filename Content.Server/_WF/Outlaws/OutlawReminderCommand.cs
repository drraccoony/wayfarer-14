using System.Linq;
using Content.Server._WF.Outlaws.Systems;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._WF.Outlaws;

/// <summary>
///     Re-opens the Outlaw disclaimer popup for a connected player.
///     Reads their current <see cref="OutlawDisclaimerComponent"/> to decide which variant to show,
///     defaulting to the basic Outlaw disclaimer if they don't have the component.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class OutlawReminderCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "outlawreminder";
    public override string Description => "Re-shows the Outlaw disclaimer popup to a player.";
    public override string Help => "Usage: outlawreminder <username>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError($"Invalid arguments. {Help}");
            return;
        }

        var found = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (found == null)
        {
            shell.WriteError($"Could not find a player with name or ID '{args[0]}'.");
            return;
        }

        if (!_players.TryGetSessionById(found.UserId, out var session))
        {
            shell.WriteError($"'{args[0]}' is not currently connected.");
            return;
        }

        // Determine which variant to show based on their attached entity's component.
        var isWanted = false;
        if (session.AttachedEntity is { } entity
            && _entities.TryGetComponent<OutlawDisclaimerComponent>(entity, out var comp))
        {
            isWanted = comp.IsWanted;
        }

        _eui.OpenEui(new OutlawDisclaimerEui(isWanted), session);
        shell.WriteLine($"Sent Outlaw disclaimer to {session.Name}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(s => s.Name).Select(s => s.Name).ToArray();
            return CompletionResult.FromHintOptions(options, "<username>");
        }

        return CompletionResult.Empty;
    }
}
