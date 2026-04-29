using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._WF.Corporations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client._WF.Corporations;

public sealed partial class CorporationUiFragment : BoxContainer
{
    // ─── Events ──────────────────────────────────────────────────────────────
    public event Action? OnRefresh;
    public event Action<CorporationView>? OnNavigate;
    public event Action<string, string, CorporationPrivacy>? OnCreate;
    public event Action<int>? OnJoin;
    public event Action? OnLeave;
    public event Action? OnDisband;
    public event Action<string>? OnEditDescription;
    public event Action<CorporationPrivacy>? OnSetPrivacy;
    public event Action<string>? OnSendInvite;
    public event Action<int, bool>? OnRespondInvite;
    public event Action<string>? OnKick;
    public event Action<string, CorporationRank>? OnChangeRank;

    // ─── Controls ────────────────────────────────────────────────────────────
    private readonly Button _backButton;
    private readonly Button _refreshButton;
    private readonly Label _feedbackLabel;

    // List panel
    private readonly ScrollContainer _listPanel;
    private readonly BoxContainer _invitesSection;
    private readonly BoxContainer _invitesList;
    private readonly BoxContainer _myCorporationSection;
    private readonly Label _corpNameLabel;
    private readonly Label _corpPrivacyLabel;
    private readonly RichTextLabel _corpDescriptionLabel;
    private readonly Button _editDescriptionButton;
    private readonly Button _togglePrivacyButton;
    private readonly Button _inviteMemberButton;
    private readonly BoxContainer _membersList;
    private readonly Label _corpBankBalanceLabel;
    private readonly ConfirmButton _leaveCorpButton;
    private readonly ConfirmButton _disbandCorpButton;
    private readonly BoxContainer _noCorporationSection;
    private readonly Button _createCorpButton;
    private readonly BoxContainer _publicCorpsList;

    // Create panel
    private readonly BoxContainer _createPanel;
    private readonly LineEdit _corpNameEdit;
    private readonly TextEdit _corpDescEdit;
    private readonly Button _privacyToggleButton;
    private readonly Button _foundCorpButton;
    private readonly Button _cancelCreateButton;

    // Invite panel
    private readonly BoxContainer _invitePanel;
    private readonly OptionButton _characterSelector;
    private readonly Button _sendInviteButton;
    private readonly Button _cancelInviteButton;

    // Edit description panel
    private readonly BoxContainer _editDescPanel;
    private readonly TextEdit _editDescText;
    private readonly Button _saveDescButton;
    private readonly Button _cancelEditDescButton;

    // ─── State ───────────────────────────────────────────────────────────────
    private CorporationListUiState? _lastListState;
    private bool _isPrivate;
    private List<string> _inviteCharacters = new();

    // ─── Constructor ─────────────────────────────────────────────────────────
    public CorporationUiFragment()
    {
        RobustXamlLoader.Load(this);

        _backButton = FindControl<Button>("BackButton");
        _refreshButton = FindControl<Button>("RefreshButton");
        _feedbackLabel = FindControl<Label>("FeedbackLabel");

        _listPanel = FindControl<ScrollContainer>("ListPanel");
        _invitesSection = FindControl<BoxContainer>("InvitesSection");
        _invitesList = FindControl<BoxContainer>("InvitesList");
        _myCorporationSection = FindControl<BoxContainer>("MyCorporationSection");
        _corpNameLabel = FindControl<Label>("CorpNameLabel");
        _corpPrivacyLabel = FindControl<Label>("CorpPrivacyLabel");
        _corpDescriptionLabel = FindControl<RichTextLabel>("CorpDescriptionLabel");
        _editDescriptionButton = FindControl<Button>("EditDescriptionButton");
        _togglePrivacyButton = FindControl<Button>("TogglePrivacyButton");
        _inviteMemberButton = FindControl<Button>("InviteMemberButton");
        _membersList = FindControl<BoxContainer>("MembersList");
        _corpBankBalanceLabel = FindControl<Label>("CorpBankBalanceLabel");
        _leaveCorpButton = FindControl<ConfirmButton>("LeaveCorpButton");
        _disbandCorpButton = FindControl<ConfirmButton>("DisbandCorpButton");
        _noCorporationSection = FindControl<BoxContainer>("NoCorporationSection");
        _createCorpButton = FindControl<Button>("CreateCorpButton");
        _publicCorpsList = FindControl<BoxContainer>("PublicCorpsList");

        _createPanel = FindControl<BoxContainer>("CreatePanel");
        _corpNameEdit = FindControl<LineEdit>("CorpNameEdit");
        _corpDescEdit = FindControl<TextEdit>("CorpDescEdit");
        _privacyToggleButton = FindControl<Button>("PrivacyToggleButton");
        _foundCorpButton = FindControl<Button>("FoundCorpButton");
        _cancelCreateButton = FindControl<Button>("CancelCreateButton");

        _invitePanel = FindControl<BoxContainer>("InvitePanel");
        _characterSelector = FindControl<OptionButton>("CharacterSelector");
        _sendInviteButton = FindControl<Button>("SendInviteButton");
        _cancelInviteButton = FindControl<Button>("CancelInviteButton");

        _editDescPanel = FindControl<BoxContainer>("EditDescPanel");
        _editDescText = FindControl<TextEdit>("EditDescText");
        _saveDescButton = FindControl<Button>("SaveDescButton");
        _cancelEditDescButton = FindControl<Button>("CancelEditDescButton");

        WireEvents();
    }

    private void WireEvents()
    {
        _refreshButton.OnPressed += _ => OnRefresh?.Invoke();
        _backButton.OnPressed += _ =>
        {
            OnNavigate?.Invoke(CorporationView.List);
        };

        // Create flow
        _createCorpButton.OnPressed += _ =>
        {
            _corpNameEdit.Clear();
            _corpDescEdit.TextRope = Rope.Leaf.Empty;
            _corpDescEdit.CursorPosition = default;
            _isPrivate = false;
            _privacyToggleButton.Pressed = false;
            UpdatePrivacyButtonText();
            ShowPanel(PanelMode.Create);
        };
        _privacyToggleButton.OnToggled += args =>
        {
            _isPrivate = args.Pressed;
            UpdatePrivacyButtonText();
        };
        _foundCorpButton.OnPressed += _ =>
        {
            var name = _corpNameEdit.Text.Trim();
            var desc = Rope.Collapse(_corpDescEdit.TextRope).Trim();
            var privacy = _isPrivate ? CorporationPrivacy.Private : CorporationPrivacy.Public;
            OnCreate?.Invoke(name, desc, privacy);
        };
        _cancelCreateButton.OnPressed += _ => OnNavigate?.Invoke(CorporationView.List);

        // Corp actions
        _leaveCorpButton.OnPressed += _ => OnLeave?.Invoke();
        _disbandCorpButton.OnPressed += _ => OnDisband?.Invoke();
        // ConfirmButton.OnPressed fires only after the second (confirmed) click
        _inviteMemberButton.OnPressed += _ => OnNavigate?.Invoke(CorporationView.Invite);

        _editDescriptionButton.OnPressed += _ =>
        {
            _editDescText.TextRope = _lastListState?.MyCorporation != null
                ? new Rope.Leaf(_lastListState.MyCorporation.Description)
                : Rope.Leaf.Empty;
            _editDescText.CursorPosition = default;
            ShowPanel(PanelMode.EditDesc);
        };
        _saveDescButton.OnPressed += _ =>
        {
            OnEditDescription?.Invoke(Rope.Collapse(_editDescText.TextRope).Trim());
        };
        _cancelEditDescButton.OnPressed += _ => OnNavigate?.Invoke(CorporationView.List);

        _togglePrivacyButton.OnPressed += _ =>
        {
            if (_lastListState?.MyCorporation == null)
                return;
            var newPrivacy = _lastListState.MyCorporation.Privacy == CorporationPrivacy.Public
                ? CorporationPrivacy.Private
                : CorporationPrivacy.Public;
            OnSetPrivacy?.Invoke(newPrivacy);
        };

        // Invite
        _sendInviteButton.OnPressed += _ =>
        {
            if (_inviteCharacters.Count == 0)
                return;
            var idx = _characterSelector.SelectedId;
            if (idx < 0 || idx >= _inviteCharacters.Count)
                return;
            OnSendInvite?.Invoke(_inviteCharacters[idx]);
        };
        _cancelInviteButton.OnPressed += _ => OnNavigate?.Invoke(CorporationView.List);
    }

    // ─── State update entry points ────────────────────────────────────────────

    public void ShowListState(CorporationListUiState state)
    {
        _lastListState = state;
        ShowPanel(PanelMode.List);

        // Feedback message
        if (!string.IsNullOrEmpty(state.ErrorMessage))
        {
            _feedbackLabel.Text = Loc.GetString(state.ErrorMessage);
            _feedbackLabel.Visible = true;
        }
        else
        {
            _feedbackLabel.Visible = false;
        }

        // Pending invites
        _invitesList.DisposeAllChildren();
        _invitesList.RemoveAllChildren();

        if (state.PendingInvites.Count > 0)
        {
            _invitesSection.Visible = true;
            foreach (var invite in state.PendingInvites)
                _invitesList.AddChild(BuildInviteRow(invite));
        }
        else
        {
            _invitesSection.Visible = false;
        }

        if (state.MyCorporation != null)
        {
            _myCorporationSection.Visible = true;
            _noCorporationSection.Visible = false;
            PopulateMyCorporation(state);
        }
        else
        {
            _myCorporationSection.Visible = false;
            _noCorporationSection.Visible = true;
            PopulatePublicCorps(state);
        }
    }

    public void ShowInviteState(CorporationInviteUiState state)
    {
        _inviteCharacters = state.AvailableCharacters;
        _characterSelector.Clear();

        for (var i = 0; i < _inviteCharacters.Count; i++)
            _characterSelector.AddItem(_inviteCharacters[i], i);

        if (!string.IsNullOrEmpty(state.ErrorMessage))
        {
            _feedbackLabel.Text = Loc.GetString(state.ErrorMessage);
            _feedbackLabel.Visible = true;
        }
        else
        {
            _feedbackLabel.Visible = false;
        }

        ShowPanel(PanelMode.Invite);
    }

    // ─── Corp detail population ───────────────────────────────────────────────

    private void PopulateMyCorporation(CorporationListUiState state)
    {
        var corp = state.MyCorporation!;
        var myRank = state.MyRank;

        _corpNameLabel.Text = corp.Name;
        _corpPrivacyLabel.Text = corp.Privacy == CorporationPrivacy.Private
            ? Loc.GetString("corp-privacy-private")
            : Loc.GetString("corp-privacy-public");

        _corpDescriptionLabel.Text = string.IsNullOrWhiteSpace(corp.Description)
            ? Loc.GetString("corp-no-description")
            : corp.Description;

        var isManager = myRank >= CorporationRank.Manager;
        var isLeader = myRank == CorporationRank.Leader;
        var isRecruiter = myRank >= CorporationRank.Recruiter;

        _editDescriptionButton.Visible = isManager;
        _inviteMemberButton.Visible = isRecruiter;
        _disbandCorpButton.Visible = isLeader;
        _disbandCorpButton.Disabled = corp.Balance > 0;

        _corpBankBalanceLabel.Text = Loc.GetString("corp-bank-balance", ("balance", corp.Balance.ToString("N0")));

        // Toggle privacy button label
        _togglePrivacyButton.Text = corp.Privacy == CorporationPrivacy.Public
            ? Loc.GetString("corp-btn-make-private")
            : Loc.GetString("corp-btn-make-public");

        // Build members list
        _membersList.DisposeAllChildren();
        _membersList.RemoveAllChildren();

        var sorted = state.Members
            .OrderByDescending(m => m.Rank)
            .ThenBy(m => m.DisplayName)
            .ToList();

        foreach (var member in sorted)
            _membersList.AddChild(BuildMemberRow(member, myRank, state.MyUserId));
    }

    private void PopulatePublicCorps(CorporationListUiState state)
    {
        _publicCorpsList.DisposeAllChildren();
        _publicCorpsList.RemoveAllChildren();

        if (state.PublicCorporations.Count == 0)
        {
            _publicCorpsList.AddChild(new Label { Text = Loc.GetString("corp-no-public-corps") });
            return;
        }

        foreach (var corp in state.PublicCorporations.OrderBy(c => c.Name))
            _publicCorpsList.AddChild(BuildPublicCorpRow(corp));
    }

    // ─── Row builders ─────────────────────────────────────────────────────────

    private Control BuildInviteRow(CorporationInfo corp)
    {
        var panel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 4),
            HorizontalExpand = true,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        var nameLabel = new Label
        {
            Text = Loc.GetString("corp-invite-row", ("name", corp.Name), ("members", corp.MemberCount)),
            HorizontalExpand = true,
        };

        var acceptBtn = new Button { Text = Loc.GetString("corp-btn-accept"), Margin = new Thickness(4, 0, 0, 0) };
        var declineBtn = new Button { Text = Loc.GetString("corp-btn-decline"), Margin = new Thickness(4, 0, 0, 0) };

        var capturedCorpId = corp.Id;
        acceptBtn.OnPressed += _ => OnRespondInvite?.Invoke(capturedCorpId, true);
        declineBtn.OnPressed += _ => OnRespondInvite?.Invoke(capturedCorpId, false);

        row.AddChild(nameLabel);
        row.AddChild(acceptBtn);
        row.AddChild(declineBtn);
        panel.AddChild(row);
        return panel;
    }

    private Control BuildPublicCorpRow(CorporationInfo corp)
    {
        var panel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 4),
            HorizontalExpand = true,
            StyleClasses = { "AngleRect" },
            ModulateSelfOverride = Color.FromHex("#3F3F3F"),
        };

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(4, 4, 4, 4),
        };

        var headerRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, HorizontalExpand = true };

        var nameLabel = new Label
        {
            Text = corp.Name,
            StyleClasses = { "LabelSubText" },
            HorizontalExpand = true,
        };

        var memberCount = new Label
        {
            Text = Loc.GetString("corp-member-count", ("count", corp.MemberCount)),
            Margin = new Thickness(8, 0, 0, 0),
        };

        var joinBtn = new Button
        {
            Text = Loc.GetString("corp-btn-join"),
            Margin = new Thickness(8, 0, 0, 0),
        };

        var capturedCorpId = corp.Id;
        joinBtn.OnPressed += _ => OnJoin?.Invoke(capturedCorpId);

        headerRow.AddChild(nameLabel);
        headerRow.AddChild(memberCount);
        headerRow.AddChild(joinBtn);
        container.AddChild(headerRow);

        if (!string.IsNullOrWhiteSpace(corp.Description))
        {
            var descLabel = new RichTextLabel
            {
                HorizontalExpand = true,
                Margin = new Thickness(0, 2, 0, 0),
            };
            descLabel.StyleClasses.Add("LabelSmall");
            descLabel.SetMessage(FormattedMessage.FromMarkupPermissive(corp.Description));
            container.AddChild(descLabel);
        }

        panel.AddChild(container);
        return panel;
    }

    private Control BuildMemberRow(CorporationMemberInfo member, CorporationRank myRank, string myUserId)
    {
        var isSelf = member.UserId == myUserId;
        var memberRank = member.Rank;

        var panel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 2),
            HorizontalExpand = true,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        var rankLabel = new Label
        {
            Text = $"[{Loc.GetString($"corp-rank-{member.Rank.ToString().ToLowerInvariant()}")}]",
            MinWidth = 100,
            StyleClasses = { "LabelSmall" },
        };

        var nameLabel = new Label
        {
            Text = member.DisplayName + (isSelf ? Loc.GetString("corp-member-self-suffix") : ""),
            HorizontalExpand = true,
            StyleClasses = { "LabelSmall" },
        };

        row.AddChild(rankLabel);
        row.AddChild(nameLabel);

        // Management actions - only for non-self and when I outrank them
        if (!isSelf && myRank > memberRank)
        {
            var capturedUserId = member.UserId;
            var capturedRank = memberRank;

            // Promote button (if target can be promoted further and still below my rank)
            if (memberRank + 1 < myRank)
            {
                var promoteBtn = new Button
                {
                    Text = Loc.GetString("corp-btn-promote"),
                    Margin = new Thickness(4, 0, 0, 0),
                    StyleClasses = { "ButtonSmall" },
                };
                promoteBtn.OnPressed += _ => OnChangeRank?.Invoke(capturedUserId, capturedRank + 1);
                row.AddChild(promoteBtn);
            }

            // Demote button (if target is above Member)
            if (memberRank > CorporationRank.Member)
            {
                var demoteBtn = new Button
                {
                    Text = Loc.GetString("corp-btn-demote"),
                    Margin = new Thickness(4, 0, 0, 0),
                    StyleClasses = { "ButtonSmall" },
                };
                demoteBtn.OnPressed += _ => OnChangeRank?.Invoke(capturedUserId, capturedRank - 1);
                row.AddChild(demoteBtn);
            }

            // Kick button
            var kickBtn = new ConfirmButton
            {
                Text = Loc.GetString("corp-btn-kick"),
                ConfirmationText = Loc.GetString("corp-btn-kick-confirm"),
                Margin = new Thickness(4, 0, 0, 0),
                StyleClasses = { "ButtonSmall" },
            };
            kickBtn.OnPressed += _ => OnKick?.Invoke(capturedUserId);
            row.AddChild(kickBtn);
        }

        panel.AddChild(row);
        return panel;
    }

    // ─── Panel switching ──────────────────────────────────────────────────────

    private enum PanelMode { List, Create, Invite, EditDesc }

    private void ShowPanel(PanelMode mode)
    {
        _listPanel.Visible = mode == PanelMode.List;
        _createPanel.Visible = mode == PanelMode.Create;
        _invitePanel.Visible = mode == PanelMode.Invite;
        _editDescPanel.Visible = mode == PanelMode.EditDesc;

        _backButton.Visible = mode != PanelMode.List;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void UpdatePrivacyButtonText()
    {
        _privacyToggleButton.Text = _isPrivate
            ? Loc.GetString("corp-privacy-private")
            : Loc.GetString("corp-privacy-public");
    }
}
