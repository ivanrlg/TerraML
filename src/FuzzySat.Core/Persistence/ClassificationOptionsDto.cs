namespace FuzzySat.Core.Persistence;

/// <summary>
/// DTO for persisting the classification parameters used in a run.
/// </summary>
public sealed class ClassificationOptionsDto
{
    /// <summary>Membership function type (e.g., "Gaussian", "Triangular").</summary>
    public string MembershipFunctionType { get; set; } = "";

    /// <summary>AND operator (e.g., "Minimum", "Product").</summary>
    public string AndOperator { get; set; } = "";

    /// <summary>Defuzzifier type (e.g., "MaxWeight", "WeightedAverage").</summary>
    public string DefuzzifierType { get; set; } = "";

    /// <summary>Classification method (e.g., "PureFuzzy", "RandomForest", "SDCA").</summary>
    public string ClassificationMethod { get; set; } = "";
}
