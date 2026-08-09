using System.Threading;
using Content.Server._NF.Trade;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Shared._NF.Trade;
using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Throwing;
using Timer = Robust.Shared.Timing.Timer;
using Content.Server.Station.Systems;

namespace Content.Server._WF.Cargo.Systems;

public sealed class WFCargoSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    // Bonus system to check out if a crate is in the destination station. Dependent on NF's system for crate checking

    public bool WFIsTradeCrateAtDestination(EntityUid uid, TradeCrateComponent comp)
    {
        var owningStation = _station.GetOwningStation(uid);

        return (comp.DestinationStation != EntityUid.Invalid &&
                owningStation == comp.DestinationStation)
               || HasComp<TradeCrateWildcardDestinationComponent>(owningStation);
    }
}
