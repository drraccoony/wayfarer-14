using Robust.Shared.GameStates;

namespace Content.Shared._WF.LotteryTicket;

/// <summary>
/// Component for lottery scratch tickets with configurable odds and payouts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WFLotteryTicketComponent : Component
{
    /// <summary>
    /// The cost to purchase this ticket.
    /// </summary>
    [DataField]
    public int PurchaseCost = 500;

    /// <summary>
    /// Minimum possible winnings (usually 0).
    /// </summary>
    [DataField]
    public int MinWinnings = 0;

    /// <summary>
    /// Maximum possible winnings (jackpot amount).
    /// </summary>
    [DataField]
    public int MaxWinnings = 5000;

    /// <summary>
    /// Overall chance to win anything (0.0 to 1.0).
    /// </summary>
    [DataField]
    public float WinChance = 0.15f;

    /// <summary>
    /// Chance to win exactly the purchase cost back (0.0 to 1.0).
    /// </summary>
    [DataField]
    public float BreakEvenChance = 0.05f;

    /// <summary>
    /// Payout tiers mapping prize amounts to their probability weights.
    /// The probabilities should sum to 1.0 for proper distribution.
    /// Key: Prize amount in credits
    /// Value: Probability weight (0.0 to 1.0)
    /// </summary>
    [DataField]
    public Dictionary<int, float> PayoutTiers = new()
    {
        { 5000, 0.01f },
        { 2500, 0.02f },
        { 1000, 0.05f },
        { 500, 0.05f },
        { 0, 0.87f }
    };

    /// <summary>
    /// Whether this ticket has been scratched/used.
    /// </summary>
    [DataField]
    public bool IsScratched = false;

    /// <summary>
    /// The prize amount won (if scratched). -1 if not yet scratched.
    /// </summary>
    [DataField]
    public int PrizeAmount = -1;

    /// <summary>
    /// Prevents multiple simultaneous scratch attempts during network sync.
    /// </summary>
    [DataField]
    public bool IsBeingScratched = false;
}
