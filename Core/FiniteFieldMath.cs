using System;

namespace VRN.Core
{
    /// <summary>
    /// Provides optimized modular arithmetic operations over finite fields GF(q).
    /// All operations guarantee non-negative results within [0, q-1].
    /// </summary>
    public static class FiniteFieldMath
    {
        /// <summary>
        /// Determines whether <paramref name="n"/> is a prime number using trial division.
        /// Skips even numbers after checking 2.
        /// </summary>
        /// <param name="n">The integer to test for primality.</param>
        /// <returns><c>true</c> if <paramref name="n"/> is prime; otherwise <c>false</c>.</returns>
        public static bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;

            int limit = (int)Math.Sqrt(n);
            for (int i = 3; i <= limit; i += 2)
            {
                if (n % i == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether <paramref name="q"/> is a prime power (q = p^m).
        /// </summary>
        /// <param name="q">The integer to test.</param>
        /// <returns>
        /// A tuple containing: whether it is a prime power, the prime base p, and the exponent m.
        /// If <paramref name="q"/> is prime, returns (true, q, 1).
        /// If not a prime power, returns (false, 0, 0).
        /// </returns>
        public static (bool isPrimePower, int p, int m) IsPrimePower(int q)
        {
            if (q < 2) return (false, 0, 0);

            // Check if q itself is prime (p^1)
            if (IsPrime(q)) return (true, q, 1);

            // Try each prime base p up to sqrt(q)
            int limit = (int)Math.Sqrt(q);
            for (int p = 2; p <= limit; p++)
            {
                if (!IsPrime(p)) continue;

                // Check if q is a power of p
                int val = q;
                int m = 0;
                while (val > 1 && val % p == 0)
                {
                    val /= p;
                    m++;
                }

                if (val == 1 && m >= 1)
                {
                    return (true, p, m);
                }
            }

            return (false, 0, 0);
        }

        /// <summary>
        /// Computes <paramref name="a"/> mod <paramref name="q"/>, always returning a non-negative result in [0, q-1].
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="q">The modulus (must be positive).</param>
        /// <returns>The non-negative remainder of a divided by q.</returns>
        public static int Mod(int a, int q)
        {
            return ((a % q) + q) % q;
        }

        /// <summary>
        /// Computes (a + b) mod q, returning a non-negative result.
        /// </summary>
        /// <param name="a">First operand.</param>
        /// <param name="b">Second operand.</param>
        /// <param name="q">The modulus.</param>
        /// <returns>(a + b) mod q in [0, q-1].</returns>
        public static int ModAdd(int a, int b, int q)
        {
            return ((a + b) % q + q) % q;
        }

        /// <summary>
        /// Computes (a * b) mod q using <c>long</c> to prevent overflow, returning a non-negative result.
        /// </summary>
        /// <param name="a">First operand.</param>
        /// <param name="b">Second operand.</param>
        /// <param name="q">The modulus.</param>
        /// <returns>(a * b) mod q in [0, q-1].</returns>
        public static int ModMul(int a, int b, int q)
        {
            return (int)(((long)a * b % q + q) % q);
        }

        /// <summary>
        /// Computes (<paramref name="baseVal"/>^<paramref name="exp"/>) mod <paramref name="mod"/>
        /// using fast exponentiation by squaring.
        /// <para>By convention, ModPow(0, 0, q) returns 1.</para>
        /// </summary>
        /// <param name="baseVal">The base value.</param>
        /// <param name="exp">The exponent (must be non-negative).</param>
        /// <param name="mod">The modulus.</param>
        /// <returns>baseVal^exp mod mod in [0, mod-1].</returns>
        public static int ModPow(int baseVal, int exp, int mod)
        {
            if (mod == 1) return 0;

            // 0^0 = 1 by convention
            int result = 1;
            long b = ((long)baseVal % mod + mod) % mod;

            while (exp > 0)
            {
                if ((exp & 1) == 1)
                {
                    result = (int)(result * b % mod);
                }
                b = b * b % mod;
                exp >>= 1;
            }

            return result;
        }

        /// <summary>
        /// Computes the modular multiplicative inverse of <paramref name="a"/> modulo <paramref name="mod"/>
        /// using the Extended Euclidean Algorithm.
        /// </summary>
        /// <param name="a">The value to invert (must be coprime to mod).</param>
        /// <param name="mod">The modulus.</param>
        /// <returns>The inverse a⁻¹ such that a · a⁻¹ ≡ 1 (mod mod).</returns>
        /// <exception cref="ArithmeticException">Thrown when a is not invertible modulo mod.</exception>
        public static int ModInverse(int a, int mod)
        {
            a = ((a % mod) + mod) % mod;
            var (gcd, x, _) = ExtendedGcd(a, mod);

            if (gcd != 1)
            {
                throw new ArithmeticException(
                    $"No modular inverse exists for {a} mod {mod} (gcd = {gcd}).");
            }

            return ((x % mod) + mod) % mod;
        }

        /// <summary>
        /// Computes the Extended Euclidean Algorithm for integers <paramref name="a"/> and <paramref name="b"/>.
        /// Returns the GCD and Bézout coefficients x, y such that a·x + b·y = gcd(a,b).
        /// </summary>
        /// <param name="a">First integer.</param>
        /// <param name="b">Second integer.</param>
        /// <returns>A tuple (gcd, x, y) with Bézout coefficients.</returns>
        public static (int gcd, int x, int y) ExtendedGcd(int a, int b)
        {
            if (b == 0)
            {
                return (a, 1, 0);
            }

            var (g, x1, y1) = ExtendedGcd(b, a % b);
            return (g, y1, x1 - (a / b) * y1);
        }

        /// <summary>
        /// Generates the complete q×q addition table over GF(q).
        /// Element [i,j] = (i + j) mod q.
        /// </summary>
        /// <param name="q">The field order (prime).</param>
        /// <returns>A q×q matrix of addition results.</returns>
        public static int[,] AdditionTable(int q)
        {
            var table = new int[q, q];
            for (int i = 0; i < q; i++)
            {
                for (int j = 0; j < q; j++)
                {
                    table[i, j] = (i + j) % q;
                }
            }
            return table;
        }

        /// <summary>
        /// Generates the complete q×q multiplication table over GF(q).
        /// Element [i,j] = (i * j) mod q.
        /// </summary>
        /// <param name="q">The field order (prime).</param>
        /// <returns>A q×q matrix of multiplication results.</returns>
        public static int[,] MultiplicationTable(int q)
        {
            var table = new int[q, q];
            for (int i = 0; i < q; i++)
            {
                for (int j = 0; j < q; j++)
                {
                    table[i, j] = (int)((long)i * j % q);
                }
            }
            return table;
        }
    }
}
