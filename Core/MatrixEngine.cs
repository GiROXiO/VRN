using System;
using System.Collections.Generic;
using System.Linq;

namespace VRN.Core
{
    /// <summary>
    /// Provides matrix operations over finite fields GF(q) for Reed-Solomon code analysis.
    /// Includes generator/parity-check matrix construction, Gaussian elimination, and verification.
    /// </summary>
    public static class MatrixEngine
    {
        /// <summary>
        /// Constructs the Vandermonde generator matrix G where G[i,j] = evaluationPoints[j]^i mod q.
        /// Dimensions: k rows × n columns.
        /// </summary>
        /// <param name="evaluationPoints">Evaluation points array of length n.</param>
        /// <param name="k">Number of rows (dimension of the code).</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>A k×n generator matrix.</returns>
        public static int[,] GeneratorMatrix(int[] evaluationPoints, int k, int q)
        {
            int n = evaluationPoints.Length;
            var G = new int[k, n];

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    G[i, j] = FiniteFieldMath.ModPow(evaluationPoints[j], i, q);
                }
            }

            return G;
        }

        /// <summary>
        /// Computes the parity-check matrix H such that G·Hᵀ ≡ 0 (mod q).
        /// Uses Gaussian elimination to compute RREF of G and extracts the null space.
        /// </summary>
        /// <param name="G">The k×n generator matrix.</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>
        /// An (n-k)×n parity-check matrix, or <c>null</c> if the null space is empty.
        /// </returns>
        public static int[,]? ParityCheckMatrix(int[,] G, int q)
        {
            int k = G.GetLength(0);
            int n = G.GetLength(1);

            // Step 1: Copy G into a working matrix for RREF
            var rref = new int[k, n];
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    rref[i, j] = FiniteFieldMath.Mod(G[i, j], q);
                }
            }

            // Track which columns are pivot columns
            var pivotCol = new int[k];    // pivotCol[row] = column index of pivot in that row
            var pivotRow = new int[n];    // pivotRow[col] = row index of pivot in that col, or -1
            for (int i = 0; i < n; i++) pivotRow[i] = -1;

            int currentRow = 0;

            // Step 2–5: Forward elimination + back-substitution (full RREF)
            for (int col = 0; col < n && currentRow < k; col++)
            {
                // Find pivot: first non-zero entry in this column from currentRow down
                int pivotIdx = -1;
                for (int row = currentRow; row < k; row++)
                {
                    if (rref[row, col] != 0)
                    {
                        pivotIdx = row;
                        break;
                    }
                }

                if (pivotIdx == -1) continue; // No pivot in this column → free column

                // Swap pivot row to currentRow
                if (pivotIdx != currentRow)
                {
                    for (int j = 0; j < n; j++)
                    {
                        (rref[currentRow, j], rref[pivotIdx, j]) = (rref[pivotIdx, j], rref[currentRow, j]);
                    }
                }

                // Scale pivot row so pivot element becomes 1
                int pivotVal = rref[currentRow, col];
                int inv = FiniteFieldMath.ModInverse(pivotVal, q);
                for (int j = 0; j < n; j++)
                {
                    rref[currentRow, j] = FiniteFieldMath.ModMul(rref[currentRow, j], inv, q);
                }

                // Eliminate all other entries in this column (above and below for full RREF)
                for (int row = 0; row < k; row++)
                {
                    if (row == currentRow) continue;
                    int factor = rref[row, col];
                    if (factor == 0) continue;

                    for (int j = 0; j < n; j++)
                    {
                        rref[row, j] = FiniteFieldMath.Mod(
                            rref[row, j] - FiniteFieldMath.ModMul(factor, rref[currentRow, j], q),
                            q);
                    }
                }

                // Record pivot
                pivotCol[currentRow] = col;
                pivotRow[col] = currentRow;
                currentRow++;
            }

            int rank = currentRow;

            // Step 6: Identify free columns (those without pivots)
            var freeColumns = new List<int>();
            for (int col = 0; col < n; col++)
            {
                if (pivotRow[col] == -1)
                {
                    freeColumns.Add(col);
                }
            }

            int nullity = freeColumns.Count;
            if (nullity == 0) return null;

            // Step 7: Construct null space basis vectors → rows of H
            var H = new int[nullity, n];

            for (int f = 0; f < nullity; f++)
            {
                int freeCol = freeColumns[f];

                // Set the free variable to 1
                H[f, freeCol] = 1;

                // For each pivot column, set the corresponding entry
                for (int row = 0; row < rank; row++)
                {
                    int pCol = pivotCol[row];
                    // v[pCol] = -rref[row, freeCol] mod q
                    H[f, pCol] = FiniteFieldMath.Mod(-rref[row, freeCol], q);
                }
                // Non-pivot, non-free entries remain 0
            }

            return H;
        }

        /// <summary>
        /// Multiplies two matrices A (m×p) and B (p×n) over GF(q).
        /// </summary>
        /// <param name="A">Left matrix of dimensions m×p.</param>
        /// <param name="B">Right matrix of dimensions p×n.</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>The product matrix of dimensions m×n.</returns>
        /// <exception cref="ArgumentException">When inner dimensions do not match.</exception>
        public static int[,] Multiply(int[,] A, int[,] B, int q)
        {
            int m = A.GetLength(0);
            int p = A.GetLength(1);
            int p2 = B.GetLength(0);
            int n = B.GetLength(1);

            if (p != p2)
            {
                throw new ArgumentException(
                    $"Matrix dimension mismatch: A is {m}×{p}, B is {p2}×{n}.");
            }

            var C = new int[m, n];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    long sum = 0;
                    for (int idx = 0; idx < p; idx++)
                    {
                        sum += (long)A[i, idx] * B[idx, j];
                    }
                    C[i, j] = (int)((sum % q + q) % q);
                }
            }

            return C;
        }

        /// <summary>
        /// Computes the transpose of matrix M.
        /// </summary>
        /// <param name="M">The input matrix of dimensions m×n.</param>
        /// <returns>The transposed matrix of dimensions n×m.</returns>
        public static int[,] Transpose(int[,] M)
        {
            int rows = M.GetLength(0);
            int cols = M.GetLength(1);
            var T = new int[cols, rows];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    T[j, i] = M[i, j];
                }
            }

            return T;
        }

        /// <summary>
        /// Verifies that all codewords satisfy the parity-check equation c·Hᵀ ≡ 0 (mod q).
        /// </summary>
        /// <param name="codewords">The list of codewords to verify.</param>
        /// <param name="H">The parity-check matrix ((n-k)×n).</param>
        /// <param name="q">The field modulus.</param>
        /// <returns><c>true</c> if all codewords are valid; <c>false</c> otherwise.</returns>
        public static bool VerifyCodewords(List<int[]> codewords, int[,] H, int q)
        {
            int hRows = H.GetLength(0); // n-k
            int n = H.GetLength(1);

            foreach (var c in codewords)
            {
                if (!IsValidCodeword(c, H, hRows, n, q))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Counts how many codewords satisfy the parity-check equation c·Hᵀ ≡ 0 (mod q).
        /// </summary>
        /// <param name="codewords">The list of codewords to check.</param>
        /// <param name="H">The parity-check matrix.</param>
        /// <param name="q">The field modulus.</param>
        /// <returns>The count of valid codewords.</returns>
        public static int CountValidCodewords(List<int[]> codewords, int[,] H, int q)
        {
            int hRows = H.GetLength(0);
            int n = H.GetLength(1);
            int count = 0;

            foreach (var c in codewords)
            {
                if (IsValidCodeword(c, H, hRows, n, q))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Computes the minimum Hamming distance among all pairs of distinct codewords.
        /// Uses early exit: returns immediately if a distance of 1 is found.
        /// </summary>
        /// <param name="codewords">The list of codewords.</param>
        /// <returns>
        /// The minimum Hamming distance, or 0 if fewer than 2 codewords exist.
        /// </returns>
        public static int MinimumDistance(List<int[]> codewords)
        {
            if (codewords.Count < 2) return 0;

            int n = codewords[0].Length;
            int dMin = n + 1; // Start higher than any possible distance

            for (int i = 0; i < codewords.Count; i++)
            {
                ReadOnlySpan<int> ci = codewords[i].AsSpan();
                for (int j = i + 1; j < codewords.Count; j++)
                {
                    ReadOnlySpan<int> cj = codewords[j].AsSpan();
                    int dist = 0;

                    for (int pos = 0; pos < n; pos++)
                    {
                        if (ci[pos] != cj[pos])
                        {
                            dist++;
                            // Early exit within comparison if we can't beat current min
                            if (dist >= dMin) break;
                        }
                    }

                    if (dist < dMin)
                    {
                        dMin = dist;
                        if (dMin == 1) return 1; // Can't get lower (except 0 for duplicates, but we have unique codewords)
                    }
                }
            }

            return dMin > n ? 0 : dMin;
        }

        /// <summary>
        /// Cross-verifies that encoding via matrix multiplication m·G (mod q)
        /// produces the same codeword set as polynomial evaluation.
        /// Generates all q^k message vectors, computes m·G, and compares to the provided codewords.
        /// </summary>
        /// <param name="codewords">Codewords from polynomial evaluation.</param>
        /// <param name="G">The k×n generator matrix.</param>
        /// <param name="q">The field modulus.</param>
        /// <param name="k">Code dimension.</param>
        /// <returns><c>true</c> if both methods produce identical codeword sets.</returns>
        public static bool CrossVerify(List<int[]> codewords, int[,] G, int q, int k)
        {
            int n = G.GetLength(1);

            // Build set of codewords from polynomial evaluation for fast lookup
            var polySet = new HashSet<string>(codewords.Count);
            foreach (var cw in codewords)
            {
                polySet.Add(string.Join(",", cw));
            }

            // Generate all q^k message vectors and encode via m·G
            var messages = PolynomialEngine.GenerateAllCoefficients(q, k);
            var matrixSet = new HashSet<string>(messages.Count);

            foreach (var m in messages)
            {
                var encoded = new int[n];
                for (int j = 0; j < n; j++)
                {
                    long sum = 0;
                    for (int i = 0; i < k; i++)
                    {
                        sum += (long)m[i] * G[i, j];
                    }
                    encoded[j] = (int)((sum % q + q) % q);
                }

                matrixSet.Add(string.Join(",", encoded));
            }

            // Both sets must be identical
            return polySet.SetEquals(matrixSet);
        }

        /// <summary>
        /// Checks if a single codeword satisfies c·Hᵀ ≡ 0 (mod q).
        /// </summary>
        private static bool IsValidCodeword(int[] c, int[,] H, int hRows, int n, int q)
        {
            // For each row r of H, compute dot product c · H[r, :]
            for (int r = 0; r < hRows; r++)
            {
                long dot = 0;
                for (int j = 0; j < n; j++)
                {
                    dot += (long)c[j] * H[r, j];
                }
                int result = (int)((dot % q + q) % q);
                if (result != 0) return false;
            }
            return true;
        }
    }
}
