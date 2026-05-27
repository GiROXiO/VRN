namespace VRN.Models;

/// <summary>
/// Status of a calculation step.
/// </summary>
public enum CalculationStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Represents a single step in the calculation pipeline for progress reporting.
/// </summary>
public class CalculationStep
{
    /// <summary>Internal step identifier (e.g., "validate", "field", "matrixG").</summary>
    public string StepId { get; }

    /// <summary>Current status of this step.</summary>
    public CalculationStatus Status { get; set; }

    /// <summary>Optional detail message.</summary>
    public string? Detail { get; set; }

    public CalculationStep(string stepId, CalculationStatus status, string? detail = null)
    {
        StepId = stepId;
        Status = status;
        Detail = detail;
    }
}
