using System.Text.Json.Serialization;

namespace VRN.Models;

/// <summary>
/// Severity level for validation messages.
/// </summary>
public enum MessageLevel
{
    Ok,
    Warning,
    Error
}

/// <summary>
/// A validation or informational message produced during computation.
/// </summary>
public class ValidationMessage
{
    [JsonPropertyName("level")]
    public MessageLevel Level { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public ValidationMessage() { }

    public ValidationMessage(MessageLevel level, string message)
    {
        Level = level;
        Message = message;
    }
}

/// <summary>
/// Complete result of a code computation, including all matrices, codewords, and verifications.
/// </summary>
public class CodeResult
{
    // --- Validation ---
    [JsonPropertyName("validationMessages")]
    public List<ValidationMessage> ValidationMessages { get; set; } = new();

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; } = true;

    // --- Field Info ---
    [JsonPropertyName("primeBase")]
    public int PrimeBase { get; set; }

    [JsonPropertyName("primeExponent")]
    public int PrimeExponent { get; set; }

    [JsonPropertyName("evaluationPoints")]
    public int[]? EvaluationPoints { get; set; }

    [JsonPropertyName("additionTable")]
    public int[,]? AdditionTable { get; set; }

    [JsonPropertyName("multiplicationTable")]
    public int[,]? MultiplicationTable { get; set; }

    // --- Polynomials ---
    [JsonPropertyName("polynomials")]
    public List<string> Polynomials { get; set; } = new();

    [JsonPropertyName("polynomialCount")]
    public int PolynomialCount { get; set; }

    // --- Codewords ---
    [JsonPropertyName("codewords")]
    public List<int[]> Codewords { get; set; } = new();

    [JsonPropertyName("codewordCount")]
    public int CodewordCount { get; set; }

    // --- Matrices ---
    [JsonPropertyName("generatorMatrix")]
    public int[,]? GeneratorMatrix { get; set; }

    [JsonPropertyName("parityCheckMatrix")]
    public int[,]? ParityCheckMatrix { get; set; }

    // --- Verifications ---
    [JsonPropertyName("validCodewordCount")]
    public int ValidCodewordCount { get; set; }

    [JsonPropertyName("allCodewordsValid")]
    public bool AllCodewordsValid { get; set; }

    [JsonPropertyName("minimumDistance")]
    public int MinimumDistance { get; set; }

    [JsonPropertyName("singletonBound")]
    public int SingletonBound { get; set; }

    [JsonPropertyName("isRS")]
    public bool IsRS { get; set; }

    [JsonPropertyName("crossVerificationPassed")]
    public bool CrossVerificationPassed { get; set; }

    // --- Timing ---
    [JsonPropertyName("computationTimeMs")]
    public long ComputationTimeMs { get; set; }
}
