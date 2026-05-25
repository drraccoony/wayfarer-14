using System.Linq;
using Content.Server.Humanoid;
using Content.Server.Consent;
using Content.Shared.Consent;
using Content.Shared.DoAfter;
using Content.Shared.FloofStation;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Server.FloofStation.ModifyUndies;


/// <summary>
/// This is a component that lets you show/hide specific underwear slots.
///
/// </summary>
public sealed class ModifyUndiesSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly ConsentSystem _consent = default!;

    private static readonly ProtoId<ConsentTogglePrototype> GenitalMarkingsConsent = "GenitalMarkings";

    public static readonly VerbCategory UndiesCat =
        new("verb-categories-undies", "/Textures/Interface/VerbIcons/undies.png");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModifyUndiesComponent, GetVerbsEvent<Verb>>(AddModifyUndiesVerb);
        SubscribeLocalEvent<ModifyUndiesComponent, ModifyUndiesDoAfterEvent>(ToggleUndies);
    }

    private void AddModifyUndiesVerb(EntityUid uid, ModifyUndiesComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humApp))
            return;
        if (args.User != args.Target && _entMan.System<InventorySystem>().TryGetSlotEntity(args.Target, "jumpsuit", out _))
            return; // mainly so people cant just spy on others undies *too* easily
        var isMine = args.User == args.Target;
        // okay go through their markings, and find all the undershirts and underwear markings
        // <marking_ID>, list:(localized name, bodypart enum, isvisible)
        foreach (var marking in humApp.MarkingSet.Markings.Values.SelectMany(markingLust => markingLust))
        {
            if (!_markingManager.TryGetMarking(marking, out var mProt))
                continue;

            /* Coyote: Commented for the Marking System Improvements
            // check if the Bodypart is in the component's BodyPartTargets
            if (!component.BodyPartTargets.Contains(mProt.BodyPart))
            */

            // Skip if we don't have permission to modify this marking
            if (isMine && !marking.CanToggleVisible || !isMine && !marking.OtherCanToggleVisible)
                continue;
            // Skip genital markings based on consent

            // Coyote: This can probably be removed now that users can control genital interactions through the marking interface.

            if (mProt.BodyPart == HumanoidVisualLayers.Genital)
            {
                // If user and target are the same person, they can always interact with their own markings
                if (args.User != args.Target)
                {
                    // For other players, only check the target's consent setting
                    var hasTargetConsent = _consent.HasConsent(args.Target, GenitalMarkingsConsent);
                    if (!hasTargetConsent)
                    {
                        continue;
                    }
                }
            }
            //var localizedName = Loc.GetString($"marking-{mProt.ID}"); // Coyote: See below
            var localizedName = string.IsNullOrEmpty(marking.CustomName) ? Loc.GetString($"marking-{mProt.ID}") : marking.CustomName; // Coyote: Marking system improvements.
            var partSlot = mProt.BodyPart;
            var isVisible = !humApp.HiddenMarkings.Contains(mProt.ID);
            if (mProt.Sprites.Count < 1)
                continue; // no sprites means its not visible means its kinda already off and you cant put it on
            var undieOrBra = partSlot switch
            {
                HumanoidVisualLayers.UndergarmentTop => new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bra.png")),
                HumanoidVisualLayers.UndergarmentBottom => new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/underpants.png")),
                HumanoidVisualLayers.Genital => new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/love.png")),
                //_ => new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/undies.png")) // Coyote: See below
                _ => mProt.Sprites.FirstOrDefault() ?? new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/undies.png")) // Coyote: Marking system improvements.
            };
            // add the verb
            Verb verb = new()
            {
                Text = Loc.GetString(
                    "modify-undies-verb-text",
                    ("undies", localizedName),
                    ("isVisible", isVisible),
                    ("isMine", isMine),
                    ("target", Identity.Entity(args.Target, EntityManager)),
                    ("putOnVerb", string.IsNullOrEmpty(marking.PutOnVerb) ? Loc.GetString("marking-toggle-self-default-verb-on") : marking.PutOnVerb), // Coyote: Marking system improvements.
                    ("takeOffVerb", string.IsNullOrEmpty(marking.TakeOffVerb) ? Loc.GetString("marking-toggle-self-default-verb-off") : marking.TakeOffVerb) // Coyote: Marking system improvements.
                    ),
                Icon = undieOrBra,
                Category = UndiesCat,
                Act = () =>
                {
                    var ev = new ModifyUndiesDoAfterEvent(
                        marking,
                        localizedName,
                        isVisible
                        );
                    var doAfterArgs = new DoAfterArgs(
                        EntityManager,
                        args.User,
                        0.25f,
                        ev,
                        args.Target,
                        args.Target,
                        used: args.User
                    )
                    {
                        Hidden = false,
                        MovementThreshold = 0,
                        RequireCanInteract = true,
                        BlockDuplicate = true
                    };
                    string gString;
                    if (args.User == args.Target)
                    {
                        /* Coyote: Commented out for the line below
                        gString = isVisible
                            ? "undies-removed-self-start"
                            : "undies-equipped-self-start";
                        */
                        gString = "marking-toggle-self-start"; // Coyote: Marking System Improvements
                        _popupSystem.PopupCoordinates(
                            Loc.GetString(
                                gString,
                                //("undie", localizedName) // Coyote: Commented out for the two lines below
                                ("marking-name", localizedName), // Coyote: Marking System Improvements
                                ("verb", isVisible ? marking.TakeOffVerb : marking.PutOnVerb) // Coyote: Marking System Improvements
                                ),
                            Transform(args.Target).Coordinates,
                            Filter.Entities(args.Target),
                            true,
                            PopupType.Medium);
                    }
                    // someone doing this to someone else
                    else
                    {
                        // to the user
                        /* Coyote: Commented out for the line below
                        gString = isVisible
                            ? "undies-removed-user-start"
                            : "undies-equipped-user-start";
                        */
                        gString = "marking-toggle-other-start"; // Coyote: Marking System Improvements
                        _popupSystem.PopupCoordinates(
                            Loc.GetString(
                                gString,
                                //("undie", localizedName) // Coyote: Commented out for the lines below
                                ("verb", isVisible ? marking.TakeOffVerb : marking.PutOnVerb), // Coyote: Marking System Improvements
                                ("marking-name", localizedName) // Coyote: Marking System Improvements
                                ),
                            Transform(args.Target).Coordinates,
                            Filter.Entities(args.User),
                            true,
                            PopupType.Medium);
                        // to the target
                        /* Coyote: Commented out for the line below
                        gString = isVisible
                            ? "undies-removed-target-start"
                            : "undies-equipped-target-start";
                        */
                        gString = "marking-toggle-by-other-start"; // Coyote: Marking System Improvements
                        _popupSystem.PopupCoordinates(
                            Loc.GetString(
                                gString,
                                /* Coyote: Commented out for the lines below
                                ("undie", localizedName),
                                ("user", Identity.Entity(args.User, EntityManager))
                                */
                                ("marking-name", localizedName), // Coyote: Marking System Improvements
                                ("verb", isVisible ? marking.TakeOffVerb : marking.PutOnVerb), // Coyote: Marking System Improvements
                                ("other", Identity.Entity(args.User, EntityManager)) // Coyote: Marking System Improvements
                                ),
                            Transform(args.Target).Coordinates,
                            Filter.Entities(args.Target),
                            true,
                            PopupType.MediumCaution);
                    }
                    // and then play a sound!
                    var rufthleAudio = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");
                    _audio.PlayEntity(
                        rufthleAudio,
                        Filter.Entities(args.User, args.Target),
                        args.Target,
                        false,
                        AudioParams.Default.WithVariation(0.5f).WithVolume(0.5f));
                    _doAfterSystem.TryStartDoAfter(doAfterArgs);
                    // ToggleUndies(uid, mProt, isVisible, localizedName, args.User, args.Target, humApp);
                },
                Disabled = false,
                Message = null
            };
            args.Verbs.Add(verb);
        }
    }
    private void ToggleUndies(
        EntityUid uid,
        ModifyUndiesComponent component,
        ModifyUndiesDoAfterEvent args
        )
    {
        if (!_markingManager.TryGetMarking(args.Marking, out var mProt))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humApp))
            return;

        _humanoid.SetMarkingVisibility(
            uid,
            humApp,
            mProt.ID,
            !args.IsVisible
        );
        // then make a text bubble!
        // one for the doer, one for the target
        // and one if the doer is the target
        // Effect targets for different players
        // Popups
        string gString;
        var marking = args.Marking; // Coyote: Marking System Improvements

        if (args.User == args.Target.Value)
        {
            /* Coyote: Commented out for the lines below
            gString = args.IsVisible
                ? "undies-removed-self"
                : "undies-equipped-self";
            */
            gString = "marking-toggle-self"; // Coyote: Marking System Improvements
            _popupSystem.PopupCoordinates(
                Loc.GetString(
                    gString,
                    //("undie", args.MarkingPrototypeName) // Coyote: Commented out for the lines below
                    ("marking-name", string.IsNullOrEmpty(marking.CustomName) ? args.MarkingPrototypeName : marking.CustomName), // Coyote: Marking System Improvements
                    ("verb", args.IsVisible ? marking.TakeOffVerb : marking.PutOnVerb) // Coyote: Marking System Improvements
                    ),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.Target.Value),
                true,
                PopupType.Medium);
        }
        // someone doing this to someone else
        else
        {
            // to the user
            /* Coyote: Commented out for the line below
            gString = args.IsVisible
                ? "undies-removed-user"
                : "undies-equipped-user";
            */
            gString = "marking-toggle-other"; // Coyote: Marking System Improvements
            _popupSystem.PopupCoordinates(
                Loc.GetString(
                    gString,
                    //("undie", args.MarkingPrototypeName) // Coyote: Commented out for the lines below
                    ("marking-name", string.IsNullOrEmpty(marking.CustomName) ? args.MarkingPrototypeName : marking.CustomName), // Coyote: Marking System Improvements
                    ("verb", args.IsVisible ? marking.TakeOffVerb : marking.PutOnVerb) // Coyote: Marking System Improvements
                    ),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.User),
                true,
                PopupType.Medium);
            // to the target
            /* Coyote: Commented out for the line below
            gString = args.IsVisible
                ? "undies-removed-target"
                : "undies-equipped-target";
            */
            gString = "marking-toggle-by-other"; // Coyote: Marking System Improvements
            _popupSystem.PopupCoordinates(
                Loc.GetString(
                    gString,
                    /* Coyote: Commented out for the lines below
                    ("undie", args.MarkingPrototypeName),
                    ("user", Identity.Entity(args.User, EntityManager))
                    */
                    ("marking-name", string.IsNullOrEmpty(marking.CustomName) ? args.MarkingPrototypeName : marking.CustomName), // Coyote: Marking System Improvements
                    ("verb", args.IsVisible ? marking.TakeOffVerb2p : marking.PutOnVerb2p), // Coyote: Marking System Improvements
                    ("other", Identity.Entity(args.User, EntityManager)) // Coyote: Marking System Improvements
                    ),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.Target.Value),
                true,
                PopupType.Medium);
        }
        // and then play a sound!
        var rufthleAudio = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");
        _audio.PlayEntity(
            rufthleAudio,
            Filter.Entities(args.User, args.Target.Value),
            args.Target.Value,
            false,
            AudioParams.Default.WithVariation(0.5f).WithVolume(0.5f));
    }
}
