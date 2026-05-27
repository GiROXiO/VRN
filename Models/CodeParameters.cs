using System.Text.Json.Serialization;

namespace VRN.Models;

/// <summary>
/// Parameters for a Reed-Solomon / finite field code computation.
/// </summary>
public class CodeParameters
{
    /// <summary>Code length (number of symbols per codeword).</summary>
    [JsonPropertyName("n")]
    public int N { get; set; }

    /// <summary>Dimension (number of message symbols / max polynomial degree + 1).</summary>
    [JsonPropertyName("k")]
    public int K { get; set; }

    /// <summary>Field size (must be prime for GF(q)).</summary>
    [JsonPropertyName("q")]
    public int Q { get; set; }

    public CodeParameters() { }

    public CodeParameters(int n, int k, int q)
    {
        N = n;
        K = k;
        Q = q;
    }

    public override string ToString() => $"[{N},{K}] F{Q}";
}
