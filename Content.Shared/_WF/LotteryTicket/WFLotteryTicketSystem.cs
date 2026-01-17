using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared._WF.LotteryTicket;

/// <summary>
/// System that handles lottery ticket scratching and prize determination.
/// </summary>
public sealed class WFLotteryTicketSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WFLotteryTicketComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WFLotteryTicketComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WFLotteryTicketComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnExamined(Entity<WFLotteryTicketComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.IsScratched)
        {
            args.PushMarkup(Loc.GetString("wf-lottery-ticket-examine-unscratched"));
            return;
        }

        if (ent.Comp.PrizeAmount > 0)
        {
            args.PushMarkup(Loc.GetString("wf-lottery-ticket-examine-winner", 
                ("amount", ent.Comp.PrizeAmount)));
        }
        else
        {
            args.PushMarkup(Loc.GetString("wf-lottery-ticket-examine-loser"));
        }
    }

    private void OnUseInHand(Entity<WFLotteryTicketComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.IsScratched || ent.Comp.IsBeingScratched)
        {
            if (ent.Comp.IsScratched)
                _popup.PopupEntity(Loc.GetString("wf-lottery-ticket-already-scratched"), ent, args.User);
            return;
        }

        // Mark as being scratched to prevent simultaneous attempts
        ent.Comp.IsBeingScratched = true;

        // Scratch the ticket and determine the prize
        ScratchTicket(ent, args.User);
    }

    private void OnActivated(Entity<WFLotteryTicketComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.IsScratched || ent.Comp.IsBeingScratched)
        {
            if (ent.Comp.IsScratched)
                _popup.PopupEntity(Loc.GetString("wf-lottery-ticket-already-scratched"), ent, args.User);
            return;
        }

        // Mark as being scratched to prevent simultaneous attempts
        ent.Comp.IsBeingScratched = true;

        // Scratch the ticket and determine the prize
        ScratchTicket(ent, args.User);
    }

    private void ScratchTicket(Entity<WFLotteryTicketComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;
        comp.IsScratched = true;

        // Determine if the ticket is a winner based on weighted probabilities
        var prizeAmount = DeterminePrize(comp);
        comp.PrizeAmount = prizeAmount;

        // Update the sprite to show scratched state
        // TODO: Implement sprite state change when scratched/won/lost

        // Show result to user
        if (prizeAmount > 0)
        {
            _popup.PopupEntity(
                Loc.GetString("wf-lottery-ticket-scratch-winner", ("amount", prizeAmount)),
                ent,
                user,
                PopupType.LargeCaution);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("wf-lottery-ticket-scratch-loser"),
                ent,
                user);
        }

        Dirty(ent);
    }

    private int DeterminePrize(WFLotteryTicketComponent comp)
    {
        // Roll to see if we win anything
        if (!_random.Prob(comp.WinChance))
            return 0; // Losing ticket

        // If we won, pick a prize from the payout tiers using weighted random
        var totalWeight = comp.PayoutTiers.Values.Sum();
        var roll = _random.NextFloat() * totalWeight;
        var accumulated = 0f;

        foreach (var (prize, weight) in comp.PayoutTiers.OrderByDescending(x => x.Key))
        {
            accumulated += weight;
            if (roll <= accumulated)
                return prize;
        }

        // Fallback to minimum (shouldn't normally happen)
        return 0;
    }
}
