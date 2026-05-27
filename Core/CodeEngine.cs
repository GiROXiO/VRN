using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VRN.Models;

namespace VRN.Core
{
    /// <summary>
    /// Orchestrates the complete Reed-Solomon code computation pipeline.
    /// Reports progress for each step and supports cancellation.
    /// </summary>
    public class CodeEngine
    {
        /// <summary>
        /// Raised when a calculation step changes status (Running, Completed, etc.).
        /// </summary>
        public event Action<CalculationStep>? OnProgress;

        /// <summary>
        /// Executes the full RS code computation asynchronously on a background thread.
        /// </summary>
        /// <param name="parameters">The code parameters (n, k, q).</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>A <see cref="CodeResult"/> containing all computed data.</returns>
        public async Task<CodeResult> ComputeAsync(CodeParameters parameters, CancellationToken ct = default)
        {
            return await Task.Run(() => Compute(parameters, ct), ct);
        }

        /// <summary>
        /// Synchronous computation pipeline. Called on a background thread by <see cref="ComputeAsync"/>.
        /// </summary>
        private CodeResult Compute(CodeParameters parameters, CancellationToken ct)
        {
            var result = new CodeResult();
            int n = parameters.N;
            int k = parameters.K;
            int q = parameters.Q;

            // Step 1: Validate parameters
            ReportProgress("validate", CalculationStatus.Running);

            var (isPrimePower, p, m) = FiniteFieldMath.IsPrimePower(q);

            if (!isPrimePower)
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Error, $"q={q} no es una potencia de primo."));
                result.IsValid = false;
                ReportProgress("validate", CalculationStatus.Failed);
                return result;
            }

            if (m > 1)
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Warning,
                    $"q={q} = {p}^{m}. Se requiere aritmética de campo de extensión GF({p}^{m})."));
            }

            if (k < 1)
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Error, $"k={k} debe ser ≥ 1."));
                result.IsValid = false;
                ReportProgress("validate", CalculationStatus.Failed);
                return result;
            }

            if (k > n)
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Error, $"k={k} debe ser ≤ n={n}."));
                result.IsValid = false;
                ReportProgress("validate", CalculationStatus.Failed);
                return result;
            }

            if (n > q)
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Warning,
                    $"n={n} > q={q}. El código resultante no será un código RS estándar."));
            }

            if (WouldExceedLimit(q, k))
            {
                result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Warning,
                    $"q^k = {q}^{k} excede el límite recomendado de palabras código."));
            }

            result.IsValid = true;
            result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Ok,
                $"Parámetros válidos: RS({n},{k}) sobre GF({q}), p={p}, m={m}."));

            ReportProgress("validate", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 2: Generate evaluation points and field tables
            ReportProgress("field", CalculationStatus.Running);

            // A = {0, 1, ..., q-1}  (for prime fields, use first n points if n <= q)
            int[] evalPoints = Enumerable.Range(0, n).ToArray();
            result.EvaluationPoints = evalPoints;
            result.AdditionTable = FiniteFieldMath.AdditionTable(q);
            result.MultiplicationTable = FiniteFieldMath.MultiplicationTable(q);

            ReportProgress("field", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 3: Generate polynomials
            ReportProgress("polynomials", CalculationStatus.Running);

            var coefficients = PolynomialEngine.GenerateAllCoefficients(q, k);
            result.Polynomials = coefficients
                .Select(c => PolynomialEngine.FormatPolynomial(c))
                .ToList();
            result.PolynomialCount = coefficients.Count;

            ReportProgress("polynomials", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 4: Generate codewords
            ReportProgress("codewords", CalculationStatus.Running);

            result.Codewords = PolynomialEngine.GenerateCodewords(evalPoints, coefficients, q);
            result.CodewordCount = result.Codewords.Count;

            ReportProgress("codewords", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 5: Generator matrix
            ReportProgress("matrixG", CalculationStatus.Running);

            result.GeneratorMatrix = MatrixEngine.GeneratorMatrix(evalPoints, k, q);

            ReportProgress("matrixG", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 6: Parity check matrix
            ReportProgress("matrixH", CalculationStatus.Running);

            result.ParityCheckMatrix = MatrixEngine.ParityCheckMatrix(result.GeneratorMatrix, q);

            ReportProgress("matrixH", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 7: Verify codewords
            if (result.ParityCheckMatrix != null)
            {
                ReportProgress("verify", CalculationStatus.Running);

                result.ValidCodewordCount = MatrixEngine.CountValidCodewords(
                    result.Codewords, result.ParityCheckMatrix, q);
                result.AllCodewordsValid = result.ValidCodewordCount == result.CodewordCount;

                ReportProgress("verify", CalculationStatus.Completed);
                ct.ThrowIfCancellationRequested();
            }

            // Step 8: Minimum distance
            ReportProgress("distance", CalculationStatus.Running);

            result.MinimumDistance = MatrixEngine.MinimumDistance(result.Codewords);
            result.SingletonBound = n - k + 1;
            result.IsRS = n <= q;

            ReportProgress("distance", CalculationStatus.Completed);
            ct.ThrowIfCancellationRequested();

            // Step 9: Cross verification
            ReportProgress("crossverify", CalculationStatus.Running);

            result.CrossVerificationPassed = MatrixEngine.CrossVerify(
                result.Codewords, result.GeneratorMatrix, q, k);

            ReportProgress("crossverify", CalculationStatus.Completed);

            return result;
        }

        /// <summary>
        /// Raises the <see cref="OnProgress"/> event with the given step information.
        /// </summary>
        /// <param name="stepId">Identifier for the calculation step.</param>
        /// <param name="status">Current status of the step.</param>
        private void ReportProgress(string stepId, CalculationStatus status)
        {
            OnProgress?.Invoke(new CalculationStep(stepId, status));
            if (status == CalculationStatus.Completed)
                Thread.Sleep(300);
        }

        /// <summary>
        /// Checks whether the computation would exceed the recommended codeword limit.
        /// Useful for UI-level warnings before starting a potentially expensive calculation.
        /// </summary>
        /// <param name="q">The field order.</param>
        /// <param name="k">The code dimension.</param>
        /// <param name="maxCodewords">Maximum allowed codewords (default 50,000).</param>
        /// <returns><c>true</c> if q^k exceeds the limit; <c>false</c> otherwise.</returns>
        public static bool WouldExceedLimit(int q, int k, int maxCodewords = 50000)
        {
            long total = 1;
            for (int i = 0; i < k; i++)
            {
                total *= q;
                if (total > maxCodewords) return true;
            }
            return false;
        }
    }
}
