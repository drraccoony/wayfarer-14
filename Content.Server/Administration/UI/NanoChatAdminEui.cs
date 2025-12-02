using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared._DeltaV.CartridgeLoader.Cartridges;
using Content.Shared._DeltaV.NanoChat;
using Content.Shared.Eui;

namespace Content.Server.Administration.UI;

/// <summary>
/// Admin EUI for viewing all NanoChat messages between players
/// </summary>
public sealed class NanoChatAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public NanoChatAdminEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        // Check if the player has admin permissions
        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            return new NanoChatAdminEuiState();
        }

        var cards = new List<NanoChatCardData>();

        // Query all NanoChat cards in the game
        var query = _entityManager.EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var nanoChatCard))
        {
            // Get ID card info if available
            string ownerName = "Unknown";
            string? jobTitle = null;
            
            if (_entityManager.TryGetComponent<IdCardComponent>(uid, out var idCard))
            {
                ownerName = idCard.FullName ?? "Unknown";
                jobTitle = idCard.LocalizedJobTitle;
            }

            var cardData = new NanoChatCardData
            {
                CardEntity = _entityManager.GetNetEntity(uid),
                Number = nanoChatCard.Number,
                OwnerName = ownerName,
                JobTitle = jobTitle,
                Recipients = new Dictionary<uint, NanoChatRecipient>(nanoChatCard.Recipients),
                Messages = new Dictionary<uint, List<NanoChatMessage>>()
            };

            // Deep copy messages to avoid modification issues
            foreach (var (recipientNumber, messageList) in nanoChatCard.Messages)
            {
                cardData.Messages[recipientNumber] = new List<NanoChatMessage>(messageList);
            }

            cards.Add(cardData);
        }

        // Sort cards by owner name for easier browsing
        cards.Sort((a, b) => string.Compare(a.OwnerName, b.OwnerName, StringComparison.OrdinalIgnoreCase));

        return new NanoChatAdminEuiState
        {
            Cards = cards
        };
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case NanoChatAdminEuiMsg.Refresh:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
                {
                    Close();
                    break;
                }
                StateDirty();
                break;

            case NanoChatAdminEuiMsg.SelectCard selectCard:
                // Could be used for future functionality like highlighting or filtering
                break;
        }
    }
}
