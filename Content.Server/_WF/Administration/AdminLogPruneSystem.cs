using Content.Server._NF.RoundNotifications.Events;
using Content.Server.Database;
using Content.Shared._WF.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._WF.Administration;

public sealed class AdminLogPruneSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("admin_log_prune");
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private async void OnRoundStarted(RoundStartedEvent ev)
    {
        var days = _cfg.GetCVar(WFCCVars.AdminLogPruneDays);
        if (days <= 0)
            return;

        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(days);
        var pruned = await _db.PruneAdminLogs(cutoff);
        _sawmill.Info($"Pruned {pruned} admin log entries older than {days} day(s).");
    }
}
