using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared._WF.ShuttleShunter.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using static Content.Shared._WF.ShuttleShunter.Components.ShuttleShunterComponent;

namespace Content.Server._WF.ShuttleShunter.Systems;

public sealed class ShuttleShunterSystem : EntitySystem
{
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShuttleShunterComponent, UseInHandEvent>(OnUseInHand);
        Subs.BuiEvents<ShuttleShunterComponent>(ShuttleShunterUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUiOpened);
            subs.Event<ShuttleShunterShuntMessage>(OnShuntMessage);
        });
    }

    private void OnUseInHand(Entity<ShuttleShunterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var userXform = Transform(args.User);
        if (!TryFindNearbyShuttle(ent.Comp, userXform, out var shuttleUid))
        {
            _popup.PopupEntity(Loc.GetString("shuttle-shunter-no-shuttle"), args.User, args.User);
            return;
        }

        UpdateUiState(ent, shuttleUid!.Value);
        _ui.OpenUi(ent.Owner, ShuttleShunterUiKey.Key, args.User);
    }

    private void OnBoundUiOpened(Entity<ShuttleShunterComponent> ent, ref BoundUIOpenedEvent args)
    {
        var userXform = Transform(args.Actor);
        if (!TryFindNearbyShuttle(ent.Comp, userXform, out var shuttleUid))
        {
            _ui.CloseUi(ent.Owner, ShuttleShunterUiKey.Key, args.Actor);
            return;
        }
        UpdateUiState(ent, shuttleUid!.Value);
    }

    private void OnShuntMessage(Entity<ShuttleShunterComponent> ent, ref ShuttleShunterShuntMessage args)
    {
        var userXform = Transform(args.Actor);
        if (!TryFindNearbyShuttle(ent.Comp, userXform, out var shuttleUid) || shuttleUid == null)
            return;

        var shuttleXform = Transform(shuttleUid.Value);
        var userPos = _transform.GetWorldPosition(userXform);
        var shuttlePos = _transform.GetWorldPosition(shuttleXform);

        _docking.UndockDocks(shuttleUid.Value);

        if (!TryComp<PhysicsComponent>(shuttleUid, out var body))
            return;

        var direction = shuttlePos - userPos;
        if (direction.LengthSquared() < 0.001f)
            direction = new Vector2(0f, 1f);
        else
            direction = Vector2.Normalize(direction);

        _physics.SetLinearVelocity(shuttleUid.Value, direction * ent.Comp.ShuntForce, body: body);
        _ui.CloseUi(ent.Owner, ShuttleShunterUiKey.Key, args.Actor);
    }

    private void UpdateUiState(Entity<ShuttleShunterComponent> ent, EntityUid shuttleUid)
    {
        var shuttleName = MetaData(shuttleUid).EntityName;
        var shuttleOwner = Loc.GetString("shuttle-shunter-unknown-owner");

        if (TryComp<ShuttleDeedComponent>(shuttleUid, out var deed))
        {
            if (!string.IsNullOrEmpty(deed.ShuttleName))
            {
                shuttleName = deed.ShuttleNameSuffix != null
                    ? $"{deed.ShuttleName} {deed.ShuttleNameSuffix}"
                    : deed.ShuttleName;
            }
            if (!string.IsNullOrEmpty(deed.ShuttleOwner))
                shuttleOwner = deed.ShuttleOwner;
        }

        var appraisalValue = _pricing.AppraiseGrid(shuttleUid);
        var state = new ShuttleShunterBoundUserInterfaceState(
            shuttleName,
            shuttleOwner,
            appraisalValue.ToString("N0"),
            GetNetEntity(shuttleUid)
        );
        _ui.SetUiState(ent.Owner, ShuttleShunterUiKey.Key, state);
    }

    private bool TryFindNearbyShuttle(ShuttleShunterComponent component, TransformComponent userXform, out EntityUid? shuttleUid)
    {
        shuttleUid = null;

        if (userXform.MapID == MapId.Nullspace)
            return false;

        var userPos = _transform.GetWorldPosition(userXform);
        var bestDistSq = component.Range * component.Range;
        EntityUid? best = null;

        var query = EntityQueryEnumerator<IFFComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var iff, out _, out var xform))
        {
            if (xform.MapID != userXform.MapID)
                continue;

            if ((iff.Flags & IFFFlags.IsPlayerShuttle) == 0)
                continue;

            var gridPos = _transform.GetWorldPosition(xform);
            var distSq = (gridPos - userPos).LengthSquared();

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = uid;
            }
        }

        if (best == null)
            return false;

        shuttleUid = best;
        return true;
    }
}
