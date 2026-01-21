using Content.Shared._CS.Body.Components;
using Content.Shared.Construction.Components;
using Content.Shared._CS.HeightAdjust;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server._CS.Body.Systems;

/// <summary>
/// System that handles size reverters - items that revert players to acceptable sizes
/// when they walk past within a certain range.
/// </summary>
public sealed class SizeReverterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SizeManipulationSystem _sizeManipulation = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SizeReverterComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<SizeReverterComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    private void OnAnchorChanged(EntityUid uid, SizeReverterComponent component, ref AnchorStateChangedEvent args)
    {
        component.IsActive = args.Anchored;
        Dirty(uid, component);

        // Update appearance
        _appearance.SetData(uid, SizeReverterVisuals.Active, args.Anchored);
    }

    private void OnUnanchorAttempt(EntityUid uid, SizeReverterComponent component, UnanchorAttemptEvent args)
    {
        // Add a 30 second delay to unwrenching
        args.Delay += (float)component.UnanchorDelay.TotalSeconds;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SizeReverterComponent, TransformComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var reverter, out var xform))
        {
            // Only process if active (anchored) and update interval has passed
            if (!reverter.IsActive || curTime < reverter.NextUpdate)
                continue;

            reverter.NextUpdate = curTime + TimeSpan.FromSeconds(reverter.UpdateInterval);

            // Get all entities within range
            var reverterPos = _transform.GetWorldPosition(xform);
            var nearbyEntities = new List<Entity<MobStateComponent, SizeAffectedComponent, TransformComponent>>();

            var mobQuery = EntityQueryEnumerator<MobStateComponent, SizeAffectedComponent, TransformComponent>();
            while (mobQuery.MoveNext(out var mobUid, out var mobState, out var sizeAffected, out var mobXform))
            {
                // Skip if not in same map
                if (mobXform.MapID != xform.MapID)
                    continue;

                var mobPos = _transform.GetWorldPosition(mobXform);
                var distance = (mobPos - reverterPos).Length();

                // Check if within range
                if (distance <= reverter.Range)
                {
                    nearbyEntities.Add((mobUid, mobState, sizeAffected, mobXform));
                }
            }

            // Process each nearby entity
            foreach (var entity in nearbyEntities)
            {
                var currentScale = entity.Comp2.ScaleMultiplier;

                // Check if size is out of acceptable range
                if (currentScale > reverter.MaxAcceptableSize)
                {
                    // Too large, revert down
                    entity.Comp2.ScaleMultiplier = reverter.RevertToLarge;
                    Dirty(entity, entity.Comp2);

                    // Request size recalculation
                    var recalcEvent = new RequestSizeRecalcEvent();
                    RaiseLocalEvent(entity, ref recalcEvent);

                    // Play subtle effect
                    PlaySizeReversionEffect(entity);

                    _popup.PopupEntity(
                        Loc.GetString("size-reverter-normalized-large"),
                        entity,
                        PopupType.Medium);
                }
                else if (currentScale < reverter.MinAcceptableSize)
                {
                    // Too small, revert up
                    entity.Comp2.ScaleMultiplier = reverter.RevertToSmall;
                    Dirty(entity, entity.Comp2);

                    // Request size recalculation
                    var recalcEvent = new RequestSizeRecalcEvent();
                    RaiseLocalEvent(entity, ref recalcEvent);

                    // Play subtle effect
                    PlaySizeReversionEffect(entity);

                    _popup.PopupEntity(
                        Loc.GetString("size-reverter-normalized-small"),
                        entity,
                        PopupType.Medium);
                }
            }
        }
    }

    /// <summary>
    /// Plays a subtle visual and audio effect when a player's size is reverted
    /// </summary>
    private void PlaySizeReversionEffect(EntityUid target)
    {
        // Spawn a subtle blue sparkle effect at the target's location
        var effect = Spawn("EffectFlashBluespaceQuiet", Transform(target).Coordinates);

        // Play a quiet shimmer/whoosh sound
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg"), target,
            AudioParams.Default.WithVolume(-8f).WithVariation(0.05f));
    }
}
