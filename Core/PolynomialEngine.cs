using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRN.Core
{
    /// <summary>
    /// Provides polynomial operations over finite fields GF(q) for Reed-Solomon code construction.
    /// Includes coefficient generation, Horner evaluation, formatting, and codeword generation.
    /// </summary>
    public static class PolynomialEngine
    {
        /// <summary>
        /// Unicode superscript characters indexed by digit value 0–9.
        /// </summary>
        private static readonly char[] Superscripts =
        {
            '\u2070', // ⁰
            '\u00B9', // ¹
            '\u00B2', // ²
            '\u00B3', // ³
            '\u2074', // ⁴
            '\u2075', // ⁵
            '\u2076', // ⁶
            '\u2077', // ⁷
            '\u2078', // ⁸
            '\u2079'  // ⁹
        };

        /// <summary>
        /// Generates all q^k coefficient vectors over GF(q).
        /// Each vector has k elements representing coefficients (a₀, a₁, …, a_{k-1}).
        /// Uses an iterative odometer/counter approach for maximum performance.
        /// </summary>
        /// <param name="q">The field order.</param>
        /// <param name="k">The polynomial degree bound (number of coefficients).</param>
        /// <returns>A list of all q^k coefficient vectors.</returns>
        public static List<int[]> GenerateAllCoefficients(int q, int k)
        {
            // Calculate total count q^k
            long total = 1;
            for (int i = 0; i < k; i++)
            {
                total *= q;
            }

            var result = new List<int[]>((int)total);

            // Counter array representing the current coefficient vector
            var counter = new int[k];

            for (long idx = 0; idx < total; idx++)
            {
                // Copy current counter state as a coefficient vector
                var coefficients = new int[k];
                Array.Copy(counter, coefficients, k);
                result.Add(coefficients);

                // Increment the counter (odometer-style, least significant index first)
                for (int pos = 0; pos < k; pos++)
                {
                    counter[pos]++;
                    if (counter[pos] < q)
                    {
                        break;
                    }
                    counter[pos] = 0;
                    // Carry to next position
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates a polynomial at point <paramref name="x"/> using Horner's method over GF(q).
        /// Polynomial: p(x) = coefficients[0] + coefficients[1]·x + … + coefficients[k-1]·x^{k-1}.
        /// Horner form: ((…((a_{k-1})·x + a_{k-2})·x + …)·x + a₀.
        /// </summary>
        /// <param name="coefficients">Coefficient array where index i holds the coefficient of x^i.</param>
        /// <param name="x">The evaluation point.</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>The polynomial value at x, in [0, q-1].</returns>
        public static int EvaluateHorner(int[] coefficients, int x, int q)
        {
            if (coefficients.Length == 0) return 0;

            int result = coefficients[coefficients.Length - 1];
            for (int i = coefficients.Length - 2; i >= 0; i--)
            {
                result = FiniteFieldMath.ModAdd(
                    FiniteFieldMath.ModMul(result, x, q),
                    coefficients[i],
                    q);
            }

            return FiniteFieldMath.Mod(result, q);
        }

        /// <summary>
        /// Formats a polynomial coefficient vector as a human-readable string with Unicode superscripts.
        /// Example: coefficients {3, 2, 1} → "3 + 2x + x²".
        /// Skips zero terms. Returns "0" when all coefficients are zero.
        /// </summary>
        /// <param name="coefficients">Coefficient array where index i holds the coefficient of x^i.</param>
        /// <returns>Formatted polynomial string.</returns>
        public static string FormatPolynomial(int[] coefficients)
        {
            if (coefficients.Length == 0) return "0";

            var sb = new StringBuilder();
            bool hasTerms = false;

            for (int i = 0; i < coefficients.Length; i++)
            {
                int c = coefficients[i];
                if (c == 0) continue;

                // Separator
                if (hasTerms)
                {
                    sb.Append(" + ");
                }

                if (i == 0)
                {
                    // Constant term: just the coefficient
                    sb.Append(c);
                }
                else if (i == 1)
                {
                    // Linear term: "cx" or "x" if c == 1
                    if (c == 1)
                        sb.Append('x');
                    else
                        sb.Append(c).Append('x');
                }
                else
                {
                    // Higher degree: "cx²" or "x²" if c == 1
                    if (c == 1)
                        sb.Append('x');
                    else
                        sb.Append(c).Append('x');

                    AppendSuperscript(sb, i);
                }

                hasTerms = true;
            }

            return hasTerms ? sb.ToString() : "0";
        }

        /// <summary>
        /// Generates all codewords by evaluating each coefficient vector at all evaluation points.
        /// Each codeword is the vector (p(a₀), p(a₁), …, p(a_{n-1})) for a polynomial p.
        /// Returns unique codewords only.
        /// </summary>
        /// <param name="evaluationPoints">The set of evaluation points (typically {0, 1, …, q-1}).</param>
        /// <param name="coefficients">All coefficient vectors to evaluate.</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>A list of unique codewords, each of length n = evaluationPoints.Length.</returns>
        public static List<int[]> GenerateCodewords(
            int[] evaluationPoints,
            List<int[]> coefficients,
            int q)
        {
            int n = evaluationPoints.Length;
            var seen = new HashSet<string>();
            var codewords = new List<int[]>();

            foreach (var coeff in coefficients)
            {
                var codeword = new int[n];
                for (int j = 0; j < n; j++)
                {
                    codeword[j] = EvaluateHorner(coeff, evaluationPoints[j], q);
                }

                // Use a string key for uniqueness (efficient for moderate sizes)
                string key = string.Join(",", codeword);
                if (seen.Add(key))
                {
                    codewords.Add(codeword);
                }
            }

            return codewords;
        }

        /// <summary>
        /// Appends the Unicode superscript representation of an integer exponent to the StringBuilder.
        /// For exponents ≥ 10, each digit is converted individually.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="exponent">The exponent value.</param>
        private static void AppendSuperscript(StringBuilder sb, int exponent)
        {
            if (exponent < 10)
            {
                sb.Append(Superscripts[exponent]);
            }
            else
            {
                // Multi-digit: convert each digit
                string digits = exponent.ToString();
                foreach (char d in digits)
                {
                    sb.Append(Superscripts[d - '0']);
                }
            }
        }
    }
}
