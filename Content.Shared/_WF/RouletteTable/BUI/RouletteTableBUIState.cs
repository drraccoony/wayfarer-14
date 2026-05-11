using Content.Shared._WF.RouletteTable.Events;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.RouletteTable.BUI;

[Serializable, NetSerializable]
public sealed class RouletteTableBUIState : BoundUserInterfaceState
{
    public List<RouletteBetInfo> Bets { get; }
    public bool IsSpinning { get; }
    public int? LastResult { get; }
    /// <summary>All deposited table balances keyed by NetEntity so each client can find its own.</summary>
    public Dictionary<NetEntity, int> TableBalances { get; }

    public RouletteTableBUIState(List<RouletteBetInfo> bets, bool isSpinning, int? lastResult, Dictionary<NetEntity, int> tableBalances)
    {
        Bets = bets;
        IsSpinning = isSpinning;
        LastResult = lastResult;
        TableBalances = tableBalances;
    }
}

[Serializable, NetSerializable]
public sealed class RouletteBetInfo
{
    public NetEntity PlayerEntity { get; }
    public string PlayerName { get; }
    public RouletteBetType BetType { get; }
    public int BetValue { get; }
    public int Amount { get; }

    public RouletteBetInfo(NetEntity playerEntity, string playerName, RouletteBetType betType, int betValue, int amount)
    {
        PlayerEntity = playerEntity;
        PlayerName = playerName;
        BetType = betType;
        BetValue = betValue;
        Amount = amount;
    }
}
