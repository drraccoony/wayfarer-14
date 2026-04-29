using Robust.Shared.Configuration;

namespace Content.Shared._WF.CCVar;

[CVarDefs]
public sealed class WFCCVars
{
    /// <summary>
    /// The cost in spesos to found a new player corporation.
    /// </summary>
    public static readonly CVarDef<int> CorporationCreationCost =
        CVarDef.Create("wf.corporation.creation_cost", 1000000, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum number of characters allowed in a corporation name.
    /// </summary>
    public static readonly CVarDef<int> CorporationNameMaxLength =
        CVarDef.Create("wf.corporation.name_max_length", 40, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum number of characters allowed in a corporation description.
    /// </summary>
    public static readonly CVarDef<int> CorporationDescriptionMaxLength =
        CVarDef.Create("wf.corporation.description_max_length", 500, CVar.SERVER | CVar.REPLICATED);
}
