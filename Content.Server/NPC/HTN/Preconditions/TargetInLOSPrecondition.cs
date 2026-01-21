using Content.Server.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Physics;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class TargetInLOSPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private InteractionSystem _interaction = default!;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    [DataField("targetKey")]
    public string TargetKey = "Target";

    [DataField("rangeKey")]
    public string RangeKey = "RangeKey";

    [DataField("opaqueKey")]
    public bool UseOpaqueForLOSChecksKey = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _interaction = sysManager.GetEntitySystem<InteractionSystem>();
        _fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return false;

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        var collisionGroup = UseOpaqueForLOSChecksKey ? CollisionGroup.Opaque : (CollisionGroup.Impassable | CollisionGroup.InteractImpassable);

        return _interaction.InRangeUnobstructed(owner, target, range, predicate: (EntityUid entity) =>
        {
            if (_fixturesQuery.TryGetComponent(entity, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    if ((fixture.CollisionLayer & (int)CollisionGroup.GlassLayer) != 0 ||
                        (fixture.CollisionLayer & (int)CollisionGroup.GlassAirlockLayer) != 0)
                    {
                        return true; // Ignore this entity for LOS
                    }
                }
            }
            return false; // Don't ignore
        });
    }
}
