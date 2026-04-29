using System.Threading.Tasks;
using Content.Server._NF.Bank;
using Content.Server.Database;
using Content.Shared._NF.Bank.Components;
using Content.Shared._WF.Corporations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;

namespace Content.Server._WF.Corporations;

public sealed class CorporationAtmSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorporationAtmComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CorporationAtmComponent, CorporationAtmDepositMessage>(OnDeposit);
        SubscribeLocalEvent<CorporationAtmComponent, CorporationAtmWithdrawMessage>(OnWithdraw);
    }

    private void OnUiOpened(EntityUid uid, CorporationAtmComponent comp, BoundUIOpenedEvent args)
    {
        _ = UpdateUiAsync(uid, comp, args.Actor, string.Empty);
    }

    private void OnDeposit(EntityUid uid, CorporationAtmComponent comp, CorporationAtmDepositMessage args)
    {
        _ = HandleDepositAsync(uid, comp, args);
    }

    private void OnWithdraw(EntityUid uid, CorporationAtmComponent comp, CorporationAtmWithdrawMessage args)
    {
        _ = HandleWithdrawAsync(uid, comp, args);
    }

    private async Task HandleDepositAsync(EntityUid uid, CorporationAtmComponent comp, CorporationAtmDepositMessage args)
    {
        var player = args.Actor;
        if (!TryGetUserId(player, out var userId))
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-no-account");
            return;
        }

        if (args.Amount <= 0)
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-invalid-amount");
            return;
        }

        var member = await _db.GetCorporationForPlayer(userId);
        if (member == null)
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-not-member");
            return;
        }

        // Deduct from player's bank
        if (!_bank.TryBankWithdraw(player, args.Amount))
        {
            _audio.PlayPvs(_audio.ResolveSound(comp.ErrorSound), uid);
            await UpdateUiAsync(uid, comp, player, "corp-atm-insufficient-funds");
            return;
        }

        // Credit to corporation
        await _db.TryDepositToCorporation(member.Id, args.Amount);
        _audio.PlayPvs(_audio.ResolveSound(comp.ConfirmSound), uid);
        await UpdateUiAsync(uid, comp, player, string.Empty);
    }

    private async Task HandleWithdrawAsync(EntityUid uid, CorporationAtmComponent comp, CorporationAtmWithdrawMessage args)
    {
        var player = args.Actor;
        if (!TryGetUserId(player, out var userId))
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-no-account");
            return;
        }

        if (args.Amount <= 0)
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-invalid-amount");
            return;
        }

        var member = await _db.GetCorporationForPlayer(userId);
        if (member == null)
        {
            await UpdateUiAsync(uid, comp, player, "corp-atm-not-member");
            return;
        }

        // Check rank — only Manager (2) or Leader (3) can withdraw
        var myMember = member.Members.Find(m => m.UserId == userId);
        if (myMember == null || myMember.Rank < 2)
        {
            _audio.PlayPvs(_audio.ResolveSound(comp.ErrorSound), uid);
            await UpdateUiAsync(uid, comp, player, "corp-atm-no-permission");
            return;
        }

        if (!await _db.TryWithdrawFromCorporation(member.Id, args.Amount))
        {
            _audio.PlayPvs(_audio.ResolveSound(comp.ErrorSound), uid);
            await UpdateUiAsync(uid, comp, player, "corp-atm-insufficient-corp-funds");
            return;
        }

        if (!_bank.TryBankDeposit(player, args.Amount))
        {
            // Refund to corp if deposit to player failed
            await _db.TryDepositToCorporation(member.Id, args.Amount);
            _audio.PlayPvs(_audio.ResolveSound(comp.ErrorSound), uid);
            await UpdateUiAsync(uid, comp, player, "corp-atm-deposit-failed");
            return;
        }

        _audio.PlayPvs(_audio.ResolveSound(comp.ConfirmSound), uid);
        await UpdateUiAsync(uid, comp, player, string.Empty);
    }

    private async Task UpdateUiAsync(EntityUid uid, CorporationAtmComponent comp, EntityUid player, string statusKey)
    {
        if (!TryGetUserId(player, out var userId))
        {
            _uiSystem.SetUiState(uid, CorporationAtmUiKey.Key,
                new CorporationAtmUiState(null, -1, 0, false, 0, statusKey));
            return;
        }

        var corp = await _db.GetCorporationForPlayer(userId);
        var playerBalance = TryComp<BankAccountComponent>(player, out var bankComp) ? bankComp.Balance : 0;

        if (corp == null)
        {
            _uiSystem.SetUiState(uid, CorporationAtmUiKey.Key,
                new CorporationAtmUiState(null, -1, 0, false, playerBalance, statusKey));
            return;
        }

        var myMember = corp.Members.Find(m => m.UserId == userId);
        var canWithdraw = myMember != null && myMember.Rank >= 2;

        _uiSystem.SetUiState(uid, CorporationAtmUiKey.Key,
            new CorporationAtmUiState(corp.Name, corp.Id, corp.Balance, canWithdraw, playerBalance, statusKey));
    }

    private bool TryGetUserId(EntityUid player, out Guid userId)
    {
        userId = Guid.Empty;
        if (!_playerManager.TryGetSessionByEntity(player, out var session))
            return false;
        userId = session.UserId.UserId;
        return true;
    }
}
