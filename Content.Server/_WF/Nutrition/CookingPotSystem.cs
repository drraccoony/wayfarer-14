using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Placeable;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._WF.Nutrition;

public sealed class CookingPotSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CookingPotComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnRemoved(Entity<CookingPotComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        entity.Comp.CookingProgress.Remove(args.Entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var heated = new HashSet<EntityUid>();
        var heaters = EntityQueryEnumerator<ActiveSolutionHeaterComponent, ItemPlacerComponent>();
        while (heaters.MoveNext(out _, out _, out var placer))
            heated.UnionWith(placer.PlacedEntities);

        var pots = EntityQueryEnumerator<CookingPotComponent, StorageComponent>();
        while (pots.MoveNext(out var uid, out var pot, out var storage))
        {
            if (!_solutions.TryGetSolution(uid, pot.Solution, out var solutionEntity, out var solution))
                continue;

            var water = solution.GetTotalPrototypeQuantity("Water");
            var boiledWater = solution.GetTotalPrototypeQuantity("BoiledWater");
            if (!heated.Contains(uid) || solution.Temperature < pot.BoilingTemperature ||
                water + boiledWater < pot.MinimumWater)
            {
                pot.BoilingProgress = 0;
                pot.CookingProgress.Clear();
                if (!heated.Contains(uid) && solution.Temperature > 293.15f)
                    _solutions.SetTemperature(solutionEntity.Value, Math.Max(293.15f, solution.Temperature - frameTime));
                continue;
            }

            if (water > FixedPoint2.Zero)
            {
                pot.BoilingProgress += frameTime;
                if (pot.BoilingProgress >= pot.BoilingTime)
                {
                    solution.RemoveReagent("Water", water);
                    solution.AddReagent("BoiledWater", water);
                    _solutions.UpdateChemicals(solutionEntity.Value);
                    pot.BoilingProgress = 0;
                }
            }
            else
            {
                pot.BoilingProgress = 0;
            }

            if (boiledWater < pot.MinimumWater)
            {
                pot.CookingProgress.Clear();
                continue;
            }

            foreach (var ingredient in storage.Container.ContainedEntities.ToArray())
            {
                if (!TryComp<BoilableFoodComponent>(ingredient, out var boilable))
                    continue;

                var progress = pot.CookingProgress.GetValueOrDefault(ingredient) + frameTime;
                pot.CookingProgress[ingredient] = progress;
                if (progress >= boilable.CookingTime && TryCook(uid, storage, ingredient, boilable))
                    pot.CookingProgress.Remove(ingredient);
            }
        }
    }

    private bool TryCook(EntityUid pot, StorageComponent storage, EntityUid ingredient, BoilableFoodComponent boilable)
    {
        if (!TryComp<EdibleComponent>(ingredient, out var edible) ||
            !_solutions.TryGetSolution(ingredient, edible.Solution, out var sourceEntity, out var source))
            return false;

        var result = Spawn(boilable.Result, _transform.GetMapCoordinates(pot));
        if (!TryComp<EdibleComponent>(result, out var cookedEdible) ||
            !_solutions.TryGetSolution(result, cookedEdible.Solution, out var targetEntity, out var target))
        {
            Del(result);
            return false;
        }

        var cooked = source.Clone();
        if (_solutions.TryGetSolution(pot, Comp<CookingPotComponent>(pot).Solution, out _, out var cookingSolution))
            cooked.Temperature = cookingSolution.Temperature;
        foreach (var (from, to) in boilable.ReagentConversions)
        {
            var amount = cooked.GetTotalPrototypeQuantity(from);
            cooked.RemoveReagent(from, amount);
            cooked.AddReagent(to, amount);
        }

        target.MaxVolume = FixedPoint2.Max(target.MaxVolume, cooked.Volume);
        _solutions.RemoveAllSolution(targetEntity.Value);
        _solutions.TryAddSolution(targetEntity.Value, cooked);

        _containers.Remove(ingredient, storage.Container);
        if (!_storage.Insert(pot, result, out _, storageComp: storage, playSound: false))
        {
            _storage.Insert(pot, ingredient, out _, storageComp: storage, playSound: false);
            Del(result);
            return false;
        }

        _solutions.RemoveAllSolution(sourceEntity.Value);
        QueueDel(ingredient);
        return true;
    }
}