using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._WF.Nutrition;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Placeable;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests._WF.Nutrition;

[TestFixture]
[TestOf(typeof(CookingPotSystem))]
public sealed class CookingPotTest
{
    private static void SetHeated(IEntityManager entities, EntityUid heater, EntityUid pot)
    {
        entities.EnsureComponent<ActiveSolutionHeaterComponent>(heater);
#pragma warning disable RA0002
        entities.GetComponent<ItemPlacerComponent>(heater).PlacedEntities.Add(pot);
#pragma warning restore RA0002
    }

    [TestCase("CookingPot", "food")]
    [TestCase("Beaker", "beaker")]
    public async Task RealHotplateDetectsAndHeatsPot(string containerPrototype, string solutionName)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();
        EntityUid pot = default;
        EntityUid heater = default;

        await server.WaitAssertion(() =>
        {
            heater = entities.SpawnEntity("ChemistryHotplate", map.GridCoords.Offset(new Vector2(0.5f, 0.5f)));
            pot = entities.SpawnEntity(containerPrototype, map.GridCoords.Offset(new Vector2(2, 0)));
            Assert.That(solutions.TryGetSolution(pot, solutionName, out var potSolution, out _), Is.True);
            solutions.TryAddReagent(potSolution.Value, "Water", FixedPoint2.New(20));
        });

        await pair.RunTicksSync(2);
        await server.WaitPost(() =>
        {
            entities.System<SharedTransformSystem>().SetCoordinates(pot, map.GridCoords.Offset(new Vector2(0.5f, 0.75f)));
            entities.System<CollisionWakeSystem>().SetEnabled(pot, false);
            entities.System<SharedPhysicsSystem>().WakeBody(heater);
            entities.System<SharedPhysicsSystem>().SetLinearVelocity(pot, new Vector2(0.01f, 0));
        });
        await pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var heaterBody = entities.GetComponent<PhysicsComponent>(heater);
            var potBody = entities.GetComponent<PhysicsComponent>(pot);
            Assert.That(entities.GetComponent<ItemPlacerComponent>(heater).PlacedEntities, Does.Contain(pot),
                $"Heater: collide={heaterBody.CanCollide}, awake={heaterBody.Awake}, type={heaterBody.BodyType}; " +
                $"container: collide={potBody.CanCollide}, awake={potBody.Awake}, type={potBody.BodyType}");
            Assert.That(solutions.TryGetSolution(pot, solutionName, out _, out var liquid), Is.True);
            var temperature = liquid.Temperature;
            entities.EnsureComponent<ActiveSolutionHeaterComponent>(heater);
            entities.System<SolutionHeaterSystem>().Update(1f);
            Assert.That(liquid.Temperature, Is.GreaterThan(temperature));
            entities.RemoveComponent<ActiveSolutionHeaterComponent>(heater);
            entities.System<CookingPotSystem>().Update(60f);
            Assert.That(liquid.GetTotalPrototypeQuantity("BoiledWater"), Is.EqualTo(FixedPoint2.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("FoodTomato", "FoodTomatoBoiled", 15f)]
    [TestCase("FoodTomatoSlice", "FoodTomatoBoiled", 15f)]
    [TestCase("FoodTomatoDiced", "FoodTomatoBoiled", 10f)]
    [TestCase("FoodPastaDry", "FoodNoodlesBoiled", 20f)]
    public async Task BoilingRequiresTimeAndPreservesFood(string ingredientPrototype, string resultPrototype, float cookingTime)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();
        var storageSystem = entities.System<SharedStorageSystem>();
        var cooking = entities.System<CookingPotSystem>();

        await server.WaitAssertion(() =>
        {
            var pot = entities.SpawnEntity("CookingPot", map.GridCoords);
            var heater = entities.SpawnEntity("ChemistryHotplate", map.GridCoords);
            SetHeated(entities, heater, pot);
            var ingredient = entities.SpawnEntity(ingredientPrototype, map.GridCoords);
            Assert.That(storageSystem.Insert(pot, ingredient, out _), Is.True);
            Assert.That(solutions.TryGetSolution(pot, "food", out var potSolution, out var liquid), Is.True);
            Assert.That(solutions.TryGetSolution(ingredient, "food", out var foodSolution, out var food), Is.True);
            solutions.SplitSolution(foodSolution.Value, food.Volume / 2);
            solutions.TryAddReagent(foodSolution.Value, "Toxin", FixedPoint2.New(1));
            var volume = food.Volume;
            var toxin = food.GetTotalPrototypeQuantity("Toxin");
            var sauce = food.GetTotalPrototypeQuantity("Nutriment") +
                        food.GetTotalPrototypeQuantity("Vitamin") + food.GetTotalPrototypeQuantity("Water");
            solutions.TryAddReagent(potSolution.Value, "Water", FixedPoint2.New(20));
            solutions.SetTemperature(potSolution.Value, 373.15f);

            cooking.Update(4f);
            Assert.That(liquid.GetTotalPrototypeQuantity("BoiledWater"), Is.EqualTo(FixedPoint2.Zero));
            cooking.Update(1f);
            Assert.That(liquid.GetTotalPrototypeQuantity("Water"), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(liquid.GetTotalPrototypeQuantity("BoiledWater"), Is.EqualTo(FixedPoint2.New(20)));
            cooking.Update(cookingTime - 1f);
            var storage = entities.GetComponent<StorageComponent>(pot);
            Assert.That(storage.Container.Contains(ingredient), Is.True);
            cooking.Update(1f);

            var result = storage.Container.ContainedEntities.Single();
            Assert.That(entities.GetComponent<MetaDataComponent>(result).EntityPrototype!.ID, Is.EqualTo(resultPrototype));
            Assert.That(solutions.TryGetSolution(result, "food", out _, out var cookedFood), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(cookedFood.Volume, Is.EqualTo(volume));
                Assert.That(cookedFood.GetTotalPrototypeQuantity("Toxin"), Is.EqualTo(toxin));
                Assert.That(cookedFood.Temperature, Is.EqualTo(liquid.Temperature));
            });
            if (resultPrototype == "FoodTomatoBoiled")
                Assert.That(cookedFood.GetTotalPrototypeQuantity("TomatoSauce"), Is.EqualTo(sauce));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(false, 20, 373.15f)]
    [TestCase(true, 0, 373.15f)]
    [TestCase(true, 5, 373.15f)]
    [TestCase(true, 20, 293.15f)]
    public async Task UnsafeCookingConditionsDoNotCook(bool heated, int waterAmount, float temperature)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var pot = entities.SpawnEntity("CookingPot", map.GridCoords);
            if (heated)
            {
                var heater = entities.SpawnEntity("ChemistryHotplate", map.GridCoords);
                SetHeated(entities, heater, pot);
            }
            var ingredient = entities.SpawnEntity("FoodTomato", map.GridCoords);
            Assert.That(entities.System<SharedStorageSystem>().Insert(pot, ingredient, out _), Is.True);
            Assert.That(solutions.TryGetSolution(pot, "food", out var potSolution, out var liquid), Is.True);
            solutions.TryAddReagent(potSolution.Value, "BoiledWater", FixedPoint2.New(waterAmount));
            solutions.SetTemperature(potSolution.Value, temperature);
            entities.System<CookingPotSystem>().Update(60f);
            Assert.That(entities.GetComponent<StorageComponent>(pot).Container.Contains(ingredient), Is.True);
            Assert.That(entities.GetComponent<CookingPotComponent>(pot).CookingProgress, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RemovingFoodResetsItsCookingTime()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var pot = entities.SpawnEntity("CookingPot", map.GridCoords);
            var heater = entities.SpawnEntity("ChemistryHotplate", map.GridCoords);
            SetHeated(entities, heater, pot);
            var ingredient = entities.SpawnEntity("FoodTomato", map.GridCoords);
            var storageSystem = entities.System<SharedStorageSystem>();
            Assert.That(storageSystem.Insert(pot, ingredient, out _), Is.True);
            Assert.That(solutions.TryGetSolution(pot, "food", out var potSolution, out _), Is.True);
            solutions.TryAddReagent(potSolution.Value, "BoiledWater", FixedPoint2.New(20));
            solutions.SetTemperature(potSolution.Value, 373.15f);
            var cooking = entities.System<CookingPotSystem>();
            cooking.Update(14f);
            var storage = entities.GetComponent<StorageComponent>(pot);
            entities.System<SharedContainerSystem>().Remove(ingredient, storage.Container);
            Assert.That(entities.GetComponent<CookingPotComponent>(pot).CookingProgress, Is.Empty);
            Assert.That(storageSystem.Insert(pot, ingredient, out _), Is.True);
            cooking.Update(1f);
            Assert.That(storage.Container.Contains(ingredient), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("TomatoSoup", "Cream", 1, 2, 2, 5)]
    [TestCase("TomatoStew", "Protein", 1, 4, 2, 7)]
    public async Task SoupAndStewRequireHeat(string result, string addition, int additionAmount, int brothAmount, int sauceAmount, int resultAmount)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var pot = entities.SpawnEntity("CookingPot", map.GridCoords);
            Assert.That(solutions.TryGetSolution(pot, "food", out var potSolution, out var liquid), Is.True);
            solutions.TryAddReagent(potSolution.Value, "TomatoSauce", FixedPoint2.New(sauceAmount));
            solutions.TryAddReagent(potSolution.Value, "Broth", FixedPoint2.New(brothAmount));
            solutions.TryAddReagent(potSolution.Value, addition, FixedPoint2.New(additionAmount));
            Assert.That(liquid.GetTotalPrototypeQuantity(result), Is.EqualTo(FixedPoint2.Zero));
            solutions.SetTemperature(potSolution.Value, 353.15f);
            Assert.That(liquid.GetTotalPrototypeQuantity(result), Is.EqualTo(FixedPoint2.New(resultAmount)));
            Assert.That(liquid.Volume, Is.EqualTo(FixedPoint2.New(resultAmount)));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BrothUsesBoiledWaterAndPreservesVolume()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var solutions = entities.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var pot = entities.SpawnEntity("CookingPot", map.GridCoords);
            Assert.That(solutions.TryGetSolution(pot, "food", out var potSolution, out var liquid), Is.True);
            solutions.TryAddReagent(potSolution.Value, "BoiledWater", FixedPoint2.New(8));
            solutions.TryAddReagent(potSolution.Value, "Nutriment", FixedPoint2.New(1));
            solutions.TryAddReagent(potSolution.Value, "TableSalt", FixedPoint2.New(1));
            Assert.That(liquid.GetTotalPrototypeQuantity("Broth"), Is.EqualTo(FixedPoint2.Zero));
            solutions.SetTemperature(potSolution.Value, 373.15f);
            Assert.That(liquid.GetTotalPrototypeQuantity("Broth"), Is.EqualTo(FixedPoint2.New(10)));
            Assert.That(liquid.Volume, Is.EqualTo(FixedPoint2.New(10)));
        });

        await pair.CleanReturnAsync();
    }
}

[TestFixture]
public sealed class FoodPreparationInteractionTest : InteractionTest
{
    [TestCase(20)]
    [TestCase(100)]
    public async Task PlacingPotOnPoweredHotplateBoilsWater(int waterAmount)
    {
        await SpawnTarget("ChemistryHotplate");
        var pot = await PlaceInHands("CookingPot");
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        await Server.WaitAssertion(() =>
        {
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(STarget.Value, false);
            Assert.That(solutions.TryGetSolution(ToServer(pot), "food", out var potSolution, out _), Is.True);
            solutions.TryAddReagent(potSolution.Value, "Water", FixedPoint2.New(waterAmount));
        });
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<ItemPlacerComponent>(STarget.Value).PlacedEntities,
                Does.Contain(ToServer(pot)));
            Assert.That(SEntMan.HasComponent<ActiveSolutionHeaterComponent>(STarget.Value), Is.True);
            Assert.That(solutions.TryGetSolution(ToServer(pot), "food", out _, out var liquid), Is.True);
            for (var second = 0; second < 90; second++)
            {
                SEntMan.System<SolutionHeaterSystem>().Update(1f);
                SEntMan.System<CookingPotSystem>().Update(1f);
            }
            Assert.That(liquid.GetTotalPrototypeQuantity("BoiledWater"), Is.EqualTo(FixedPoint2.New(waterAmount)));
        });
    }

    [TestCase("FoodTomatoSlice", "FoodTomatoDiced")]
    [TestCase("FoodOnionSlice", "FoodOnionDiced")]
    [TestCase("FoodOnionRedSlice", "FoodOnionRedDiced")]
    public async Task KnifeDicesSlicesAndPreservesReagents(string slicePrototype, string dicedPrototype)
    {
        await SpawnTarget(slicePrototype);
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        FixedPoint2 volume = default;
        FixedPoint2 toxin = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(solutions.TryGetSolution(STarget.Value, "food", out var foodSolution, out var food), Is.True);
            solutions.SplitSolution(foodSolution.Value, food.Volume / 2);
            solutions.TryAddReagent(foodSolution.Value, "Toxin", FixedPoint2.New(0.5));
            volume = food.Volume;
            toxin = food.GetTotalPrototypeQuantity("Toxin");
        });

        await InteractUsing("KitchenKnife");
        await Server.WaitAssertion(() =>
        {
            var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
            EntityUid? diced = null;
            while (query.MoveNext(out var uid, out var metadata))
            {
                if (metadata.EntityPrototype?.ID == dicedPrototype)
                {
                    Assert.That(diced, Is.Null);
                    diced = uid;
                }
            }
            Assert.That(diced, Is.Not.Null);
            Assert.That(solutions.TryGetSolution(diced!.Value, "food", out _, out var food), Is.True);
            Assert.That(food.Volume, Is.EqualTo(volume));
            Assert.That(food.GetTotalPrototypeQuantity("Toxin"), Is.EqualTo(toxin));
        });
    }
}