using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Stack;
using Content.Shared._WF.RouletteTable.BUI;
using Content.Shared._WF.RouletteTable.Components;
using Content.Shared._WF.RouletteTable.Events;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._WF.RouletteTable;

public sealed class RouletteTableSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private static readonly HashSet<int> RedNumbers = new()
    {
        1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
    };

    private const int MaxBet = 10000;
    private static readonly TimeSpan SpinDuration = TimeSpan.FromSeconds(3);

    /// <summary>SpaceCash stack prototype used for spawning cash on cashout.</summary>
    private static readonly ProtoId<StackPrototype> CashStackProto = "Credit";

    private readonly Dictionary<EntityUid, RouletteTableData> _tableData = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RouletteTableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RouletteTableComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<RouletteTableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RouletteTableComponent, RoulettePlaceBetMessage>(OnPlaceBet);
        SubscribeLocalEvent<RouletteTableComponent, RouletteSpinMessage>(OnSpin);
        SubscribeLocalEvent<RouletteTableComponent, RouletteClearMyBetsMessage>(OnClearMyBets);
        SubscribeLocalEvent<RouletteTableComponent, RouletteCashOutMessage>(OnCashOut);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _gameTiming.CurTime;
        // Iterate over a snapshot to avoid mutation during iteration.
        foreach (var (uid, data) in _tableData.ToList())
        {
            if (data.IsSpinning && data.SpinEndTime.HasValue && now >= data.SpinEndTime.Value)
                ResolveSpinResult(uid, data);
        }
    }

    private void OnShutdown(EntityUid uid, RouletteTableComponent comp, ComponentShutdown args)
    {
        if (!_tableData.Remove(uid, out var data))
            return;

        // Refund all outstanding bets and table balances when the table is destroyed.
        foreach (var bet in data.Bets)
        {
            var playerUid = GetEntity(bet.PlayerEntity);
            data.TableBalances.TryGetValue(bet.PlayerEntity, out var bal);
            data.TableBalances[bet.PlayerEntity] = bal + bet.Amount;
        }
        data.Bets.Clear();

        foreach (var (netPlayer, balance) in data.TableBalances)
        {
            if (balance <= 0)
                continue;
            var playerUid = GetEntity(netPlayer);
            SpawnCashForPlayer(playerUid, balance, Transform(uid).Coordinates);
        }
    }

    private RouletteTableData GetOrCreate(EntityUid uid)
    {
        if (!_tableData.TryGetValue(uid, out var data))
        {
            data = new RouletteTableData();
            _tableData[uid] = data;
        }
        return data;
    }

    // ── Deposit: click table with SpaceCash stack ─────────────────────────────

    private void OnInteractUsing(EntityUid uid, RouletteTableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (!TryComp<StackComponent>(used, out var stack))
            return;

        // Only accept SpaceCash stacks.
        if (stack.StackTypeId != CashStackProto)
            return;

        var amount = stack.Count;
        if (amount <= 0)
            return;

        var data = GetOrCreate(uid);
        var netPlayer = GetNetEntity(args.User);
        data.TableBalances.TryGetValue(netPlayer, out var current);
        data.TableBalances[netPlayer] = current + amount;

        QueueDel(used);
        args.Handled = true;

        _popup.PopupEntity(
            Loc.GetString("roulette-deposited", ("amount", amount)),
            args.User, args.User);

        UpdateUI(uid);
    }

    // ── UI open ───────────────────────────────────────────────────────────────

    private void OnUiOpened(EntityUid uid, RouletteTableComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUI(uid);
    }

    // ── Place bet ─────────────────────────────────────────────────────────────

    private void OnPlaceBet(EntityUid uid, RouletteTableComponent comp, RoulettePlaceBetMessage args)
    {
        if (args.Actor is not { Valid: true } playerUid)
            return;

        var data = GetOrCreate(uid);
        if (data.IsSpinning)
            return;

        var amount = args.Amount;
        if (amount <= 0 || amount > MaxBet)
            return;

        if (args.BetType == RouletteBetType.Number && (args.BetValue < 0 || args.BetValue > 36))
            return;

        var netPlayer = GetNetEntity(playerUid);
        data.TableBalances.TryGetValue(netPlayer, out var balance);
        if (balance < amount)
        {
            _popup.PopupEntity(Loc.GetString("roulette-insufficient-balance"), playerUid, playerUid);
            return;
        }

        data.TableBalances[netPlayer] = balance - amount;

        var playerName = MetaData(playerUid).EntityName;
        data.Bets.Add(new RouletteBet(netPlayer, playerName, args.BetType, args.BetValue, amount));
        UpdateUI(uid);
    }

    // ── Spin ──────────────────────────────────────────────────────────────────

    private void OnSpin(EntityUid uid, RouletteTableComponent comp, RouletteSpinMessage args)
    {
        var data = GetOrCreate(uid);
        if (data.IsSpinning || data.Bets.Count == 0)
            return;

        data.IsSpinning = true;
        data.SpinEndTime = _gameTiming.CurTime + SpinDuration;
        data.PendingResult = _random.Next(0, 37); // 0–36 inclusive

        _chat.TrySendInGameICMessage(uid,
            Loc.GetString("roulette-chat-spin-start"),
            InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);

        UpdateUI(uid);
    }

    private void ResolveSpinResult(EntityUid uid, RouletteTableData data)
    {
        var result = data.PendingResult ?? _random.Next(0, 37);
        data.LastResult = result;

        var colorKey = result == 0 ? "roulette-color-green"
            : RedNumbers.Contains(result) ? "roulette-color-red"
            : "roulette-color-black";
        var colorStr = Loc.GetString(colorKey);

        _chat.TrySendInGameICMessage(uid,
            Loc.GetString("roulette-chat-result", ("number", result), ("color", colorStr)),
            InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);

        foreach (var bet in data.Bets)
        {
            var playerUid = GetEntity(bet.PlayerEntity);

            if (BetWins(bet, result))
            {
                var multiplier = GetPayoutMultiplier(bet.BetType);
                var payout = bet.Amount * (multiplier + 1); // stake returned + winnings
                data.TableBalances.TryGetValue(bet.PlayerEntity, out var bal);
                data.TableBalances[bet.PlayerEntity] = bal + payout;

                _chat.TrySendInGameICMessage(uid,
                    Loc.GetString("roulette-chat-win", ("player", bet.PlayerName), ("amount", payout)),
                    InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);
            }
            else
            {
                _chat.TrySendInGameICMessage(uid,
                    Loc.GetString("roulette-chat-lose", ("player", bet.PlayerName)),
                    InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);
            }
        }

        data.Bets.Clear();
        data.IsSpinning = false;
        data.SpinEndTime = null;
        data.PendingResult = null;
        UpdateUI(uid);
    }

    // ── Clear my bets (refunds bets back to table balance) ────────────────────

    private void OnClearMyBets(EntityUid uid, RouletteTableComponent comp, RouletteClearMyBetsMessage args)
    {
        if (args.Actor is not { Valid: true } playerUid)
            return;

        var data = GetOrCreate(uid);
        if (data.IsSpinning)
            return;

        var netPlayer = GetNetEntity(playerUid);
        var myBets = data.Bets.Where(b => b.PlayerEntity == netPlayer).ToList();
        foreach (var bet in myBets)
        {
            data.TableBalances.TryGetValue(netPlayer, out var bal);
            data.TableBalances[netPlayer] = bal + bet.Amount;
            data.Bets.Remove(bet);
        }
        UpdateUI(uid);
    }

    // ── Cash out ──────────────────────────────────────────────────────────────

    private void OnCashOut(EntityUid uid, RouletteTableComponent comp, RouletteCashOutMessage args)
    {
        if (args.Actor is not { Valid: true } playerUid)
            return;

        var data = GetOrCreate(uid);
        if (data.IsSpinning)
            return;

        var netPlayer = GetNetEntity(playerUid);
        if (!data.TableBalances.TryGetValue(netPlayer, out var balance) || balance <= 0)
        {
            _popup.PopupEntity(Loc.GetString("roulette-no-balance"), playerUid, playerUid);
            return;
        }

        // Remove any placed bets first and refund them.
        var myBets = data.Bets.Where(b => b.PlayerEntity == netPlayer).ToList();
        foreach (var bet in myBets)
        {
            balance += bet.Amount;
            data.Bets.Remove(bet);
        }

        data.TableBalances.Remove(netPlayer);

        var cashEntity = _stackSystem.Spawn(balance, CashStackProto, Transform(uid).Coordinates);
        _hands.PickupOrDrop(playerUid, cashEntity);

        _popup.PopupEntity(
            Loc.GetString("roulette-cashed-out", ("amount", balance)),
            playerUid, playerUid);

        UpdateUI(uid);
    }

    // ── UI update ─────────────────────────────────────────────────────────────

    private void UpdateUI(EntityUid uid)
    {
        var data = GetOrCreate(uid);
        var betInfos = data.Bets
            .Select(b => new RouletteBetInfo(b.PlayerEntity, b.PlayerName, b.BetType, b.BetValue, b.Amount))
            .ToList();

        var state = new RouletteTableBUIState(betInfos, data.IsSpinning, data.LastResult, data.TableBalances);
        _uiSystem.SetUiState(uid, RouletteTableUiKey.Key, state);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SpawnCashForPlayer(EntityUid playerUid, int amount, EntityCoordinates coords)
    {
        var cash = _stackSystem.Spawn(amount, CashStackProto, coords);
        if (playerUid.Valid)
            _hands.PickupOrDrop(playerUid, cash);
    }

    private static bool BetWins(RouletteBet bet, int result)
    {
        return bet.BetType switch
        {
            RouletteBetType.Number       => bet.BetValue == result,
            RouletteBetType.Red          => result != 0 && RedNumbers.Contains(result),
            RouletteBetType.Black        => result != 0 && !RedNumbers.Contains(result),
            RouletteBetType.Odd          => result != 0 && result % 2 == 1,
            RouletteBetType.Even         => result != 0 && result % 2 == 0,
            RouletteBetType.Low          => result is >= 1 and <= 18,
            RouletteBetType.High         => result is >= 19 and <= 36,
            RouletteBetType.DozenFirst   => result is >= 1 and <= 12,
            RouletteBetType.DozenSecond  => result is >= 13 and <= 24,
            RouletteBetType.DozenThird   => result is >= 25 and <= 36,
            RouletteBetType.ColumnFirst  => result != 0 && result % 3 == 1,
            RouletteBetType.ColumnSecond => result != 0 && result % 3 == 2,
            RouletteBetType.ColumnThird  => result != 0 && result % 3 == 0,
            _                            => false,
        };
    }

    private static int GetPayoutMultiplier(RouletteBetType betType)
    {
        return betType switch
        {
            RouletteBetType.Number => 35,
            RouletteBetType.DozenFirst or RouletteBetType.DozenSecond or RouletteBetType.DozenThird => 2,
            RouletteBetType.ColumnFirst or RouletteBetType.ColumnSecond or RouletteBetType.ColumnThird => 2,
            _ => 1,
        };
    }
}

public sealed class RouletteTableData
{
    public List<RouletteBet> Bets { get; } = new();
    /// <summary>Per-player deposited balance at this table (NetEntity → spesos).</summary>
    public Dictionary<NetEntity, int> TableBalances { get; } = new();
    public bool IsSpinning { get; set; }
    public TimeSpan? SpinEndTime { get; set; }
    public int? PendingResult { get; set; }
    public int? LastResult { get; set; }
}

public sealed class RouletteBet
{
    public NetEntity PlayerEntity { get; }
    public string PlayerName { get; }
    public RouletteBetType BetType { get; }
    public int BetValue { get; }
    public int Amount { get; }

    public RouletteBet(NetEntity playerEntity, string playerName, RouletteBetType betType, int betValue, int amount)
    {
        PlayerEntity = playerEntity;
        PlayerName = playerName;
        BetType = betType;
        BetValue = betValue;
        Amount = amount;
    }
}
