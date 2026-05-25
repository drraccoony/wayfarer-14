using Content.Server.Database;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Analyzers;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Content.IntegrationTests.Tests.Preferences
{
    [TestFixture]
    public sealed class ServerDbSqliteTests
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: dataset
  id: sqlite_test_names_first_male
  values:
  - Aaden

- type: dataset
  id: sqlite_test_names_first_female
  values:
  - Aaliyah

- type: dataset
  id: sqlite_test_names_last
  values:
  - Ackerley";

        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new HumanoidCharacterProfile() // Frontier - added HumanoidCharacterProfile
            {
                Name = "Charlie Charlieson",
                FlavorText = "The biggest boy around.",
                Species = "Human",
                Age = 21,
                Appearance = new(
                    "Afro",
                    Color.Aqua,
                    "Shaved",
                    Color.Aquamarine,
                    Color.Azure,
                    Color.Beige,
                    new ())
            }.WithBankBalance(27000); // Frontier - accessor issue
        }

        private static ServerDbSqlite GetDb(RobustIntegrationTest.ServerIntegrationInstance server)
        {
            var cfg = server.ResolveDependency<IConfigurationManager>();
            var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            builder.UseSqlite(conn);
            // Coyote Test Addition
            var deps = IoCManager.InitThread();
            try
            {
                IoCManager.Register<IPrototypeManager, DummyPrototypeManager>(false);
            } catch (Exception ex) { }
            deps.BuildGraph();
            // Coyote end
            return new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog);
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);
            // Database should be empty so a new GUID should do it.
            Assert.That(await db.GetPlayerPreferencesAsync(NewUserId()), Is.Null);

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestInitPrefs()
        {
            var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            const int slot = 0;
            var originalProfile = CharlieCharlieson();
            await db.InitPrefsAsync(username, originalProfile);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.Single(p => p.Key == slot).Value.MemberwiseEquals(originalProfile));
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestDeleteCharacter()
        {
            var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var db = GetDb(server);
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            await db.InitPrefsAsync(username, new HumanoidCharacterProfile());
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), 1);
            await db.SaveSelectedCharacterIndexAsync(username, 1);
            await db.SaveCharacterSlotAsync(username, null, 1);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(!prefs.Characters.Any(p => p.Key != 0));
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestNoPendingDatabaseChanges()
        {
            var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var db = GetDb(server);
            Assert.That(async () => await db.HasPendingModelChanges(), Is.False,
                "The database has pending model changes. Add a new migration to apply them. See https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations");
            await pair.CleanReturnAsync();
        }

        private static NetUserId NewUserId()
        {
            return new(Guid.NewGuid());
        }

        // Coyote: Yuck, if anyone knows a better way to inject a mock, let me know
        private sealed class DummyPrototypeManager : IPrototypeManager
        {
            public FrozenDictionary<ProtoId<EntityCategoryPrototype>, IReadOnlyList<EntityPrototype>> Categories => throw new NotImplementedException();

            public event Action<PrototypesReloadedEventArgs> PrototypesReloaded;

            public void AbstractDirectory(ResPath path)
            {
                throw new NotImplementedException();
            }

            public void AbstractFile(ResPath path)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Type> EnumeratePrototypeKinds()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IPrototype> EnumeratePrototypes(Type kind)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IPrototype> EnumeratePrototypes(string variant)
            {
                throw new NotImplementedException();
            }

            public FrozenDictionary<string, T> GetInstances<T>() where T : IPrototype
            {
                throw new NotImplementedException();
            }

            public Type GetKindType(string kind)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyDictionary<string, MappingDataNode> GetPrototypeData(EntityPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetPrototypeKinds()
            {
                throw new NotImplementedException();
            }

            public bool HasIndex(EntProtoId id)
            {
                throw new NotImplementedException();
            }

            public bool HasIndex(EntProtoId? id)
            {
                throw new NotImplementedException();
            }

            public bool HasKind(string kind)
            {
                throw new NotImplementedException();
            }

            public bool HasMapping<T>(string id)
            {
                throw new NotImplementedException();
            }

            public EntityPrototype Index(EntProtoId id)
            {
                throw new NotImplementedException();
            }

            public IPrototype Index(Type kind, string id)
            {
                throw new NotImplementedException();
            }

            public void Initialize()
            {
                throw new NotImplementedException();
            }

            public bool IsIgnored(string name)
            {
                throw new NotImplementedException();
            }

            public void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>> loaded = null)
            {
                throw new NotImplementedException();
            }

            public void LoadDirectory(ResPath path, bool overwrite = false, Dictionary<Type, HashSet<string>> changed = null)
            {
                throw new NotImplementedException();
            }

            public void LoadFromStream(TextReader stream, bool overwrite = false, Dictionary<Type, HashSet<string>> changed = null)
            {
                throw new NotImplementedException();
            }

            public void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>> changed = null)
            {
                throw new NotImplementedException();
            }

            public void RegisterIgnore(string name)
            {
                throw new NotImplementedException();
            }

            public void RegisterKind(params Type[] kinds)
            {
                throw new NotImplementedException();
            }

            public void ReloadPrototypeKinds()
            {
                throw new NotImplementedException();
            }

            public void ReloadPrototypes(Dictionary<Type, HashSet<string>> modified, Dictionary<Type, HashSet<string>> removed = null)
            {
                throw new NotImplementedException();
            }

            public void RemoveString(string prototypes)
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public bool Resolve([ForbidLiteral] EntProtoId id, [NotNullWhen(true)] out EntityPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public bool Resolve([ForbidLiteral] EntProtoId? id, [NotNullWhen(true)] out EntityPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public void ResolveResults()
            {
                throw new NotImplementedException();
            }

            public bool TryGetInstances<T>([NotNullWhen(true)] out FrozenDictionary<string, T> instances) where T : IPrototype
            {
                throw new NotImplementedException();
            }

            public bool TryGetKindFrom(Type type, [NotNullWhen(true)] out string kind)
            {
                throw new NotImplementedException();
            }

            public bool TryGetKindFrom(IPrototype prototype, [NotNullWhen(true)] out string kind)
            {
                throw new NotImplementedException();
            }

            public bool TryGetKindType(string kind, [NotNullWhen(true)] out Type prototype)
            {
                throw new NotImplementedException();
            }

            public bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode mappings)
            {
                throw new NotImplementedException();
            }

            public bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public bool TryIndex(EntProtoId id, [NotNullWhen(true)] out EntityPrototype prototype, bool logError = true)
            {
                throw new NotImplementedException();
            }

            public bool TryIndex(EntProtoId? id, [NotNullWhen(true)] out EntityPrototype prototype, bool logError = true)
            {
                throw new NotImplementedException();
            }

            public bool TryIndex([ForbidLiteral] EntProtoId id, [NotNullWhen(true)] out EntityPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public bool TryIndex([ForbidLiteral] EntProtoId? id, [NotNullWhen(true)] out EntityPrototype prototype)
            {
                throw new NotImplementedException();
            }

            public Dictionary<Type, Dictionary<string, HashSet<ErrorNode>>> ValidateAllPrototypesSerializable(ISerializationContext ctx)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path, out Dictionary<Type, HashSet<string>> prototypes)
            {
                throw new NotImplementedException();
            }

            public List<string> ValidateStaticFields(Dictionary<Type, HashSet<string>> prototypes)
            {
                throw new NotImplementedException();
            }

            public List<string> ValidateStaticFields(Type type, Dictionary<Type, HashSet<string>> prototypes)
            {
                throw new NotImplementedException();
            }

            int IPrototypeManager.Count<T>()
            {
                throw new NotImplementedException();
            }

            IEnumerable<(string id, T)> IPrototypeManager.EnumerateAllParents<T>(string id, bool includeSelf) where T : class
            {
                throw new NotImplementedException();
            }

            IEnumerable<T> IPrototypeManager.EnumerateParents<T>(T proto, bool includeSelf)
            {
                throw new NotImplementedException();
            }

            IEnumerable<T> IPrototypeManager.EnumerateParents<T>(string id, bool includeSelf)
            {
                throw new NotImplementedException();
            }

            IEnumerable<T> IPrototypeManager.EnumeratePrototypes<T>()
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.HasIndex<T>(string id)
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.HasIndex<T>(ProtoId<T> id)
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.HasIndex<T>(ProtoId<T>? id)
            {
                throw new NotImplementedException();
            }

            T IPrototypeManager.Index<T>(string id)
            {
                throw new NotImplementedException();
            }

            T IPrototypeManager.Index<T>(ProtoId<T> id)
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.Resolve<T>(ProtoId<T> id, out T prototype) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.Resolve<T>(ProtoId<T>? id, out T prototype) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryGetKindFrom<T>(out string kind)
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryGetRandom<T>(IRobustRandom random, out IPrototype prototype)
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryIndex<T>(string id, out T prototype) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryIndex<T>(ProtoId<T> id, out T prototype, bool logError) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryIndex<T>(ProtoId<T>? id, out T prototype, bool logError) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryIndex<T>(ProtoId<T> id, out T prototype) where T : class
            {
                throw new NotImplementedException();
            }

            bool IPrototypeManager.TryIndex<T>(ProtoId<T>? id, out T prototype) where T : class
            {
                throw new NotImplementedException();
            }
        }
    }
}
