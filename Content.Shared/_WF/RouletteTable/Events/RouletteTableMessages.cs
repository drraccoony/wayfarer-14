using Robust.Shared.Serialization;

namespace Content.Shared._WF.RouletteTable.Events;

[Serializable, NetSerializable]
public enum RouletteBetType : byte
{
    Number,       // specific number 0-36, pays 35:1
    Red,          // pays 1:1
    Black,        // pays 1:1
    Odd,          // pays 1:1 (0 loses)
    Even,         // pays 1:1 (0 loses)
    Low,          // 1-18, pays 1:1
    High,         // 19-36, pays 1:1
    DozenFirst,   // 1-12, pays 2:1
    DozenSecond,  // 13-24, pays 2:1
    DozenThird,   // 25-36, pays 2:1
    ColumnFirst,  // 1,4,7,...,34 (n%3==1), pays 2:1
    ColumnSecond, // 2,5,8,...,35 (n%3==2), pays 2:1
    ColumnThird,  // 3,6,9,...,36 (n%3==0), pays 2:1
}

[Serializable, NetSerializable]
public sealed class RoulettePlaceBetMessage : BoundUserInterfaceMessage
{
    public RouletteBetType BetType { get; }
    public int BetValue { get; }
    public int Amount { get; }

    public RoulettePlaceBetMessage(RouletteBetType betType, int betValue, int amount)
    {
        BetType = betType;
        BetValue = betValue;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class RouletteSpinMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RouletteClearMyBetsMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RouletteCashOutMessage : BoundUserInterfaceMessage { }
