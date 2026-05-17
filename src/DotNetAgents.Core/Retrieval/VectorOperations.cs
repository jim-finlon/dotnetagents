using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace DotNetAgents.Core.Retrieval;

/// <summary>
/// Optimized vector operations using SIMD and .NET 10 features for AI workloads.
/// </summary>
public static class VectorOperations
{
    /// <summary>
    /// Calculates cosine similarity between two vectors using SIMD optimizations.
    /// </summary>
    /// <param name="vectorA">First vector.</param>
    /// <param name="vectorB">Second vector.</param>
    /// <returns>Cosine similarity score between -1 and 1.</returns>
    /// <exception cref="ArgumentException">Thrown when vectors have different lengths.</exception>
    public static float CosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length.", nameof(vectorB));
        }

        if (vectorA.Length == 0)
        {
            return 0f;
        }

        // Use SIMD-optimized path if available
        if (Avx.IsSupported && vectorA.Length >= 8)
        {
            return CosineSimilarityAvx(vectorA, vectorB);
        }
        else if (Sse.IsSupported && vectorA.Length >= 4)
        {
            return CosineSimilaritySse(vectorA, vectorB);
        }
        else
        {
            // Fallback to scalar implementation
            return CosineSimilarityScalar(vectorA, vectorB);
        }
    }

    /// <summary>
    /// Calculates cosine similarity using AVX SIMD instructions (256-bit).
    /// </summary>
    private static float CosineSimilarityAvx(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var length = vectorA.Length;
        var dotProduct = Vector256<float>.Zero;
        var magnitudeA = Vector256<float>.Zero;
        var magnitudeB = Vector256<float>.Zero;

        int i = 0;
        var vectorSize = Vector256<float>.Count; // 8 floats

        // Process 8 elements at a time
        for (; i <= length - vectorSize; i += vectorSize)
        {
            var a = Vector256.Create(vectorA.Slice(i, vectorSize));
            var b = Vector256.Create(vectorB.Slice(i, vectorSize));

            dotProduct = Avx.Add(dotProduct, Avx.Multiply(a, b));
            magnitudeA = Avx.Add(magnitudeA, Avx.Multiply(a, a));
            magnitudeB = Avx.Add(magnitudeB, Avx.Multiply(b, b));
        }

        // Horizontal sum
        var dotSum = HorizontalSum(dotProduct);
        var magASum = HorizontalSum(magnitudeA);
        var magBSum = HorizontalSum(magnitudeB);

        // Handle remaining elements
        for (; i < length; i++)
        {
            dotSum += vectorA[i] * vectorB[i];
            magASum += vectorA[i] * vectorA[i];
            magBSum += vectorB[i] * vectorB[i];
        }

        var magnitudeAVal = MathF.Sqrt(magASum);
        var magnitudeBVal = MathF.Sqrt(magBSum);

        if (magnitudeAVal == 0f || magnitudeBVal == 0f)
        {
            return 0f;
        }

        return dotSum / (magnitudeAVal * magnitudeBVal);
    }

    /// <summary>
    /// Calculates cosine similarity using SSE SIMD instructions (128-bit).
    /// </summary>
    private static float CosineSimilaritySse(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var length = vectorA.Length;
        var dotProduct = Vector128<float>.Zero;
        var magnitudeA = Vector128<float>.Zero;
        var magnitudeB = Vector128<float>.Zero;

        int i = 0;
        var vectorSize = Vector128<float>.Count; // 4 floats

        // Process 4 elements at a time
        for (; i <= length - vectorSize; i += vectorSize)
        {
            var a = Vector128.Create(vectorA.Slice(i, vectorSize));
            var b = Vector128.Create(vectorB.Slice(i, vectorSize));

            dotProduct = Sse.Add(dotProduct, Sse.Multiply(a, b));
            magnitudeA = Sse.Add(magnitudeA, Sse.Multiply(a, a));
            magnitudeB = Sse.Add(magnitudeB, Sse.Multiply(b, b));
        }

        // Horizontal sum
        var dotSum = HorizontalSum(dotProduct);
        var magASum = HorizontalSum(magnitudeA);
        var magBSum = HorizontalSum(magnitudeB);

        // Handle remaining elements
        for (; i < length; i++)
        {
            dotSum += vectorA[i] * vectorB[i];
            magASum += vectorA[i] * vectorA[i];
            magBSum += vectorB[i] * vectorB[i];
        }

        var magnitudeAVal = MathF.Sqrt(magASum);
        var magnitudeBVal = MathF.Sqrt(magBSum);

        if (magnitudeAVal == 0f || magnitudeBVal == 0f)
        {
            return 0f;
        }

        return dotSum / (magnitudeAVal * magnitudeBVal);
    }

    /// <summary>
    /// Calculates cosine similarity using scalar operations (fallback).
    /// </summary>
    private static float CosineSimilarityScalar(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var dotProduct = 0f;
        var magnitudeA = 0f;
        var magnitudeB = 0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = MathF.Sqrt(magnitudeA);
        magnitudeB = MathF.Sqrt(magnitudeB);

        if (magnitudeA == 0f || magnitudeB == 0f)
        {
            return 0f;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Calculates the dot product of two vectors using SIMD optimizations.
    /// </summary>
    /// <param name="vectorA">First vector.</param>
    /// <param name="vectorB">Second vector.</param>
    /// <returns>The dot product.</returns>
    /// <exception cref="ArgumentException">Thrown when vectors have different lengths.</exception>
    public static float DotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length.", nameof(vectorB));
        }

        if (vectorA.Length == 0)
        {
            return 0f;
        }

        // Use SIMD-optimized path if available
        if (Avx.IsSupported && vectorA.Length >= 8)
        {
            return DotProductAvx(vectorA, vectorB);
        }
        else if (Sse.IsSupported && vectorA.Length >= 4)
        {
            return DotProductSse(vectorA, vectorB);
        }
        else
        {
            return DotProductScalar(vectorA, vectorB);
        }
    }

    /// <summary>
    /// Calculates dot product using AVX SIMD instructions.
    /// </summary>
    private static float DotProductAvx(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var length = vectorA.Length;
        var result = Vector256<float>.Zero;
        int i = 0;
        var vectorSize = Vector256<float>.Count;

        for (; i <= length - vectorSize; i += vectorSize)
        {
            var a = Vector256.Create(vectorA.Slice(i, vectorSize));
            var b = Vector256.Create(vectorB.Slice(i, vectorSize));
            result = Avx.Add(result, Avx.Multiply(a, b));
        }

        var sum = HorizontalSum(result);

        for (; i < length; i++)
        {
            sum += vectorA[i] * vectorB[i];
        }

        return sum;
    }

    /// <summary>
    /// Calculates dot product using SSE SIMD instructions.
    /// </summary>
    private static float DotProductSse(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var length = vectorA.Length;
        var result = Vector128<float>.Zero;
        int i = 0;
        var vectorSize = Vector128<float>.Count;

        for (; i <= length - vectorSize; i += vectorSize)
        {
            var a = Vector128.Create(vectorA.Slice(i, vectorSize));
            var b = Vector128.Create(vectorB.Slice(i, vectorSize));
            result = Sse.Add(result, Sse.Multiply(a, b));
        }

        var sum = HorizontalSum(result);

        for (; i < length; i++)
        {
            sum += vectorA[i] * vectorB[i];
        }

        return sum;
    }

    /// <summary>
    /// Calculates dot product using scalar operations.
    /// </summary>
    private static float DotProductScalar(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        var sum = 0f;
        for (int i = 0; i < vectorA.Length; i++)
        {
            sum += vectorA[i] * vectorB[i];
        }
        return sum;
    }

    /// <summary>
    /// Calculates the Euclidean distance (L2) between two vectors.
    /// </summary>
    /// <param name="vectorA">First vector.</param>
    /// <param name="vectorB">Second vector.</param>
    /// <returns>The Euclidean distance.</returns>
    /// <exception cref="ArgumentException">Thrown when vectors have different lengths.</exception>
    public static float EuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length.", nameof(vectorB));
        }

        if (vectorA.Length == 0)
        {
            return 0f;
        }

        var sumSquaredDiff = 0f;

        // Use SIMD-optimized path if available
        if (Avx.IsSupported && vectorA.Length >= 8)
        {
            var diff = Vector256<float>.Zero;
            int i = 0;
            var vectorSize = Vector256<float>.Count;

            for (; i <= vectorA.Length - vectorSize; i += vectorSize)
            {
                var a = Vector256.Create(vectorA.Slice(i, vectorSize));
                var b = Vector256.Create(vectorB.Slice(i, vectorSize));
                var d = Avx.Subtract(a, b);
                diff = Avx.Add(diff, Avx.Multiply(d, d));
            }

            sumSquaredDiff = HorizontalSum(diff);

            for (; i < vectorA.Length; i++)
            {
                var d = vectorA[i] - vectorB[i];
                sumSquaredDiff += d * d;
            }
        }
        else if (Sse.IsSupported && vectorA.Length >= 4)
        {
            var diff = Vector128<float>.Zero;
            int i = 0;
            var vectorSize = Vector128<float>.Count;

            for (; i <= vectorA.Length - vectorSize; i += vectorSize)
            {
                var a = Vector128.Create(vectorA.Slice(i, vectorSize));
                var b = Vector128.Create(vectorB.Slice(i, vectorSize));
                var d = Sse.Subtract(a, b);
                diff = Sse.Add(diff, Sse.Multiply(d, d));
            }

            sumSquaredDiff = HorizontalSum(diff);

            for (; i < vectorA.Length; i++)
            {
                var d = vectorA[i] - vectorB[i];
                sumSquaredDiff += d * d;
            }
        }
        else
        {
            for (int i = 0; i < vectorA.Length; i++)
            {
                var d = vectorA[i] - vectorB[i];
                sumSquaredDiff += d * d;
            }
        }

        return MathF.Sqrt(sumSquaredDiff);
    }

    /// <summary>
    /// Calculates the magnitude (L2 norm) of a vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The magnitude.</returns>
    public static float Magnitude(ReadOnlySpan<float> vector)
    {
        if (vector.Length == 0)
        {
            return 0f;
        }

        var sumSquared = 0f;

        // Use SIMD-optimized path if available
        if (Avx.IsSupported && vector.Length >= 8)
        {
            var squared = Vector256<float>.Zero;
            int i = 0;
            var vectorSize = Vector256<float>.Count;

            for (; i <= vector.Length - vectorSize; i += vectorSize)
            {
                var v = Vector256.Create(vector.Slice(i, vectorSize));
                squared = Avx.Add(squared, Avx.Multiply(v, v));
            }

            sumSquared = HorizontalSum(squared);

            for (; i < vector.Length; i++)
            {
                sumSquared += vector[i] * vector[i];
            }
        }
        else if (Sse.IsSupported && vector.Length >= 4)
        {
            var squared = Vector128<float>.Zero;
            int i = 0;
            var vectorSize = Vector128<float>.Count;

            for (; i <= vector.Length - vectorSize; i += vectorSize)
            {
                var v = Vector128.Create(vector.Slice(i, vectorSize));
                squared = Sse.Add(squared, Sse.Multiply(v, v));
            }

            sumSquared = HorizontalSum(squared);

            for (; i < vector.Length; i++)
            {
                sumSquared += vector[i] * vector[i];
            }
        }
        else
        {
            for (int i = 0; i < vector.Length; i++)
            {
                sumSquared += vector[i] * vector[i];
            }
        }

        return MathF.Sqrt(sumSquared);
    }

    /// <summary>
    /// Normalizes a vector in-place to unit length.
    /// </summary>
    /// <param name="vector">The vector to normalize (modified in-place).</param>
    public static void Normalize(Span<float> vector)
    {
        var magnitude = Magnitude(vector);
        if (magnitude == 0f)
        {
            return; // Cannot normalize zero vector
        }

        var invMagnitude = 1f / magnitude;

        // Use SIMD-optimized path if available
        if (Avx.IsSupported && vector.Length >= 8)
        {
            var scale = Vector256.Create(invMagnitude);
            int i = 0;
            var vectorSize = Vector256<float>.Count;

            unsafe
            {
                fixed (float* ptr = vector)
                {
                    for (; i <= vector.Length - vectorSize; i += vectorSize)
                    {
                        var v = Avx.LoadVector256(ptr + i);
                        var normalized = Avx.Multiply(v, scale);
                        Avx.Store(ptr + i, normalized);
                    }
                }
            }

            for (; i < vector.Length; i++)
            {
                vector[i] *= invMagnitude;
            }
        }
        else if (Sse.IsSupported && vector.Length >= 4)
        {
            var scale = Vector128.Create(invMagnitude);
            int i = 0;
            var vectorSize = Vector128<float>.Count;

            unsafe
            {
                fixed (float* ptr = vector)
                {
                    for (; i <= vector.Length - vectorSize; i += vectorSize)
                    {
                        var v = Sse.LoadVector128(ptr + i);
                        var normalized = Sse.Multiply(v, scale);
                        Sse.Store(ptr + i, normalized);
                    }
                }
            }

            for (; i < vector.Length; i++)
            {
                vector[i] *= invMagnitude;
            }
        }
        else
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] *= invMagnitude;
            }
        }
    }

    /// <summary>
    /// Computes horizontal sum of a Vector256.
    /// </summary>
    private static float HorizontalSum(Vector256<float> vector)
    {
        // Add upper and lower halves
        var sum128 = Avx.Add(
            Avx.ExtractVector128(vector, 0),
            Avx.ExtractVector128(vector, 1));

        // Add remaining pairs using Shuffle
        var sum64 = Sse.Add(sum128, Sse.Shuffle(sum128, sum128, 0b01001110));
        var sum32 = Sse.Add(sum64, Sse.Shuffle(sum64, sum64, 0b10110001));

        return sum32.GetElement(0);
    }

    /// <summary>
    /// Computes horizontal sum of a Vector128.
    /// </summary>
    private static float HorizontalSum(Vector128<float> vector)
    {
        // Add pairs using Shuffle
        var sum64 = Sse.Add(vector, Sse.Shuffle(vector, vector, 0b01001110));
        var sum32 = Sse.Add(sum64, Sse.Shuffle(sum64, sum64, 0b10110001));

        return sum32.GetElement(0);
    }
}
