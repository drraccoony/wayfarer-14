using Content.Shared.Humanoid.Markings;

namespace Content.Client.Humanoid;

public sealed partial class MarkingPicker
{
    private void SetCheckboxVisibility()
    {
        if (CanPutOnToggle.Pressed)
        {
            PutOnTextEdit.Visible = true;
            TakeOffTextEdit.Visible = true;
            PutOnTextEditLabel.Visible = true;
            TakeOffTextEditLabel.Visible = true;
        }
        else
        {
            PutOnTextEdit.Visible = false;
            TakeOffTextEdit.Visible = false;
            PutOnTextEditLabel.Visible = false;
            TakeOffTextEditLabel.Visible = false;
        }
        if (CanPutOnByOtherToggle.Pressed)
        {
            PutOnOtherTextEdit.Visible = true;
            TakeOffOtherTextEdit.Visible = true;
            PutOnOtherTextEditLabel.Visible = true;
            TakeOffOtherTextEditLabel.Visible = true;
        }
        else
        {
            PutOnOtherTextEdit.Visible = false;
            TakeOffOtherTextEdit.Visible = false;
            PutOnOtherTextEditLabel.Visible = false;
            TakeOffOtherTextEditLabel.Visible = false;
        }
    }
    private void SetCanToggle(bool canToggle)
    {
        if (_selectedMarking is null) return;
        var markingPrototype = (MarkingPrototype)_selectedMarking.Metadata!;
        int markingIndex = _currentMarkings.FindIndexOf(_selectedMarkingCategory, markingPrototype.ID);

        if (markingIndex < 0) return;

        var marking = new Marking(_currentMarkings.Markings[_selectedMarkingCategory][markingIndex]);
        marking.CanToggleVisible = canToggle;
        _currentMarkings.Replace(_selectedMarkingCategory, markingIndex, marking);
        SetCheckboxVisibility();

        OnMarkingDataChanged?.Invoke(_currentMarkings);
    }

    private void SetOtherCanToggle(bool canToggle)
    {
        if (_selectedMarking is null) return;
        var markingPrototype = (MarkingPrototype)_selectedMarking.Metadata!;
        int markingIndex = _currentMarkings.FindIndexOf(_selectedMarkingCategory, markingPrototype.ID);

        if (markingIndex < 0) return;

        var marking = new Marking(_currentMarkings.Markings[_selectedMarkingCategory][markingIndex]);
        marking.OtherCanToggleVisible = canToggle;
        _currentMarkings.Replace(_selectedMarkingCategory, markingIndex, marking);
        SetCheckboxVisibility();

        OnMarkingDataChanged?.Invoke(_currentMarkings);
    }

    private void SetVisible(bool visible)
    {
        if (_selectedMarking is null) return;
        var markingPrototype = (MarkingPrototype)_selectedMarking.Metadata!;
        int markingIndex = _currentMarkings.FindIndexOf(_selectedMarkingCategory, markingPrototype.ID);

        if (markingIndex < 0) return;

        var marking = new Marking(_currentMarkings.Markings[_selectedMarkingCategory][markingIndex]);
        marking.ShowAtStart = visible;
        _currentMarkings.Replace(_selectedMarkingCategory, markingIndex, marking);

        OnMarkingDataChanged?.Invoke(_currentMarkings);
    }

    private void SetCustomText()
    {
        if (_selectedMarking is null) return;
        var markingPrototype = (MarkingPrototype)_selectedMarking.Metadata!;
        int markingIndex = _currentMarkings.FindIndexOf(_selectedMarkingCategory, markingPrototype.ID);

        if (markingIndex < 0) return;

        var marking = new Marking(_currentMarkings.Markings[_selectedMarkingCategory][markingIndex]);

        marking.CustomName = CustomNameTextEdit.Text;
        marking.PutOnVerb = PutOnTextEdit.Text;
        marking.PutOnVerb2p = PutOnOtherTextEdit.Text;
        marking.TakeOffVerb = TakeOffTextEdit.Text;
        marking.TakeOffVerb2p = TakeOffOtherTextEdit.Text;

        SampleText.Text = GetSampleText((string.IsNullOrEmpty(marking.CustomName) ? markingPrototype.ID : marking.CustomName),
        (string.IsNullOrEmpty(marking.PutOnVerb) ? Loc.GetString("marking-toggle-self-default-verb-on") : marking.PutOnVerb),
        (string.IsNullOrEmpty(marking.PutOnVerb2p) ? Loc.GetString("marking-toggle-other-default-verb-on") : marking.PutOnVerb2p))
            + "\n" + GetSampleText((string.IsNullOrEmpty(marking.CustomName) ? markingPrototype.ID : marking.CustomName),
        (string.IsNullOrEmpty(marking.TakeOffVerb) ? Loc.GetString("marking-toggle-self-default-verb-off") : marking.TakeOffVerb),
        (string.IsNullOrEmpty(marking.TakeOffVerb2p) ? Loc.GetString("marking-toggle-other-default-verb-off") : marking.TakeOffVerb2p));

        _currentMarkings.Replace(_selectedMarkingCategory, markingIndex, marking);

        OnMarkingDataChanged?.Invoke(_currentMarkings);
    }

    private void ToggleSample()
    {
        if (SampleBox.Visible)
        {
            SampleBox.Visible = false;
            SampleButton.Text = Loc.GetString("marking-show-sample-text");
        }
        else
        {
            SetCustomText();
            SampleBox.Visible = true;
            SampleButton.Text = Loc.GetString("marking-hide-sample-text");
        }
    }

    private string GetSampleText(string name, string verb, string verb2p)
    {
        return Loc.GetString("marking-toggle-self-start",
            ("marking-name", name),
            ("verb", verb))
            + "\n" + Loc.GetString("marking-toggle-self",
            ("marking-name", name),
            ("verb", verb))
            + "\n" + Loc.GetString("marking-toggle-other-start",
            ("marking-name", name),
            ("verb", verb))
            + "\n" + Loc.GetString("marking-toggle-other",
            ("marking-name", name),
            ("verb", verb))
            + "\n" + Loc.GetString("marking-toggle-by-other-start",
            ("other", "Someone"),
            ("marking-name", name),
            ("verb", verb))
            + "\n" + Loc.GetString("marking-toggle-by-other",
            ("other", "Someone"),
            ("marking-name", name),
            ("verb", verb2p));
    }
}
