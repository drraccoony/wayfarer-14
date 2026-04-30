using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._WF.Corporations;

/// <summary>
/// Manages persistent corporation player stations: loading at round start, saving every 4 hours and at round end.
/// </summary>
public sealed class CorporationStationSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedShuttleSystem _shuttle = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private ISawmill _log = default!;

    /// <summary>Maps corpId → loaded grid EntityUid for all active stations this round.</summary>
    private readonly Dictionary<int, EntityUid> _activeStations = new();

    /// <summary>Maps corpId → whether the station FTL beacon is visible to shuttle consoles.</summary>
    private readonly Dictionary<int, bool> _stationVisible = new();

    private TimeSpan _nextAutosave = TimeSpan.MaxValue;

    private static readonly ResPath TemplatePath = new("/Maps/_WF/PlayerStation/playerStation.yml");

    /// <summary>Cost in spesos to purchase a corporation station.</summary>
    public const int StationCost = 5_000_000;

    public override void Initialize()
    {
        base.Initialize();
        _log = _logManager.GetSawmill("wf.corp_stations");

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextAutosave)
            return;

        _nextAutosave = _timing.CurTime + TimeSpan.FromHours(4);
        SaveAllStations();
    }

    private async void OnRoundStart(RoundStartingEvent ev)
    {
        _activeStations.Clear();
        _stationVisible.Clear();
        _nextAutosave = _timing.CurTime + TimeSpan.FromHours(4);

        List<(int corpId, string stationName, string savePath)> toLoad = new();
        try
        {
            var allCorps = await _db.GetAllCorporations();
            foreach (var corp in allCorps)
            {
                var station = await _db.GetCorporationStation(corp.Id);
                if (station != null)
                    toLoad.Add((corp.Id, station.StationName, station.SavePath));
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load corp stations from DB: {ex}");
            return;
        }

        foreach (var (corpId, stationName, savePath) in toLoad)
        {
            SpawnStation(corpId, stationName, savePath, RandomOffset());
        }
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PostRound)
            SaveAllStations();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Purchases a station for the given corporation: withdraws the cost, creates the DB record, and spawns the grid.
    /// Returns false if the corp already has a station or cannot afford it.
    /// </summary>
    public async Task<bool> PurchaseStation(int corpId, string stationName)
    {
        var existing = await _db.GetCorporationStation(corpId);
        if (existing != null)
            return false;

        if (!await _db.TryWithdrawFromCorporation(corpId, StationCost))
            return false;

        var savePath = $"corp_stations/corp_{corpId}.yml";
        await _db.CreateCorporationStation(corpId, stationName, savePath);

        SpawnStation(corpId, stationName, savePath, RandomOffset());
        return true;
    }

    /// <summary>Toggles shuttle-console visibility of the station FTL beacon. Returns the new visibility state.</summary>
    public bool ToggleStationVisibility(int corpId)
    {
        var visible = !IsStationVisible(corpId);
        _stationVisible[corpId] = visible;

        if (!_activeStations.TryGetValue(corpId, out var gridUid))
            return visible;

        if (visible)
            _shuttle.RemoveIFFFlag(gridUid, IFFFlags.Hide);
        else
            _shuttle.AddIFFFlag(gridUid, IFFFlags.Hide);

        return visible;
    }

    /// <summary>Returns whether the station is currently visible on shuttle scanners.</summary>
    public bool IsStationVisible(int corpId)
        => _stationVisible.TryGetValue(corpId, out var v) && v;

    /// <summary>Returns the world coordinates of the active station grid, or null if not loaded.</summary>
    public Vector2? GetStationCoordinates(int corpId)
    {
        if (!_activeStations.TryGetValue(corpId, out var gridUid))
            return null;
        if (!EntityManager.EntityExists(gridUid))
            return null;
        return _xforms.GetWorldPosition(gridUid);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a corporation station grid into the world.
    /// Loads from the saved user-data file if it exists, otherwise from the template.
    /// </summary>
    private EntityUid? SpawnStation(int corpId, string stationName, string savePath, Vector2 offset)
    {
        var saveResPath = new ResPath($"/{savePath}");
        var opts = DeserializationOptions.Default with { InitializeMaps = true };

        if (!_map.TryGetMap(_gameTicker.DefaultMap, out var sectorMapUid))
        {
            _log.Error($"Could not find sector map to spawn station for corp {corpId}");
            return null;
        }
        var mapId = _gameTicker.DefaultMap;

        EntityUid gridUid;

        if (_res.UserData.Exists(saveResPath))
        {
            // Saved file is category: Grid (written by TrySaveGrid)
            if (!_loader.TryLoadGrid(mapId, saveResPath, out var gridEnt, opts, offset: offset))
            {
                _log.Error($"Failed to load saved station for corp {corpId} from {saveResPath}");
                return null;
            }
            gridUid = gridEnt.Value;
        }
        else
        {
            // Template is category: Grid
            if (!_loader.TryLoadGrid(mapId, TemplatePath, out var gridEnt, opts, offset: offset))
            {
                _log.Error($"Failed to load station template for corp {corpId} from {TemplatePath}");
                return null;
            }
            gridUid = gridEnt.Value;
        }

        // Name the grid.
        _meta.SetEntityName(gridUid, stationName);

        _activeStations[corpId] = gridUid;
        _stationVisible.TryAdd(corpId, false);
        // Start hidden by default — add IFF with Hide flag.
        var iff = EnsureComp<IFFComponent>(gridUid);
        _shuttle.AddIFFFlag(gridUid, IFFFlags.Hide, iff);
        _log.Info($"Spawned station '{stationName}' for corp {corpId} at offset {offset}");
        return gridUid;
    }

    public void SaveAllStations()
    {
        foreach (var (corpId, gridUid) in _activeStations)
        {
            if (!EntityManager.EntityExists(gridUid))
                continue;

            var savePath = new ResPath($"/corp_stations/corp_{corpId}.yml");
            if (_loader.TrySaveGrid(gridUid, savePath))
                _log.Info($"Saved station for corp {corpId}");
            else
                _log.Error($"Failed to save station for corp {corpId}");
        }
    }

    private static Vector2 RandomOffset()
    {
        var rng = new Random();
        var angle = rng.NextDouble() * Math.PI * 2;
        var dist = rng.NextDouble() * 2000 + 5000; // 5000–7000 units from center
        return new Vector2((float)(Math.Cos(angle) * dist), (float)(Math.Sin(angle) * dist));
    }
}
