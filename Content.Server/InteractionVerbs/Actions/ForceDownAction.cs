using Content.Shared.InteractionVerbs;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Server.InteractionVerbs.Actions;

/// <summary>
///     Forces the target entity prone (knocked down) until they manually stand back up.
/// </summary>
[Serializable]
public sealed partial class ForceDownAction : InteractionAction
{
    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (isBefore)
            return true;

        // Don't apply if the target is already knocked down.
        var standing = deps.EntityManager.System<StandingStateSystem>();
        return !standing.IsDown(args.Target);
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var stunSystem = deps.EntityManager.System<SharedStunSystem>();
        return stunSystem.TryKnockdown(args.Target, time: null, refresh: true, autoStand: false, drop: true, force: true);
    }
}
