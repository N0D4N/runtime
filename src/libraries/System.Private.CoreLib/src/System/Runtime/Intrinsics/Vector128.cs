// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    // Many of the instance methods were moved to be extension methods as it results
    // in overall better codegen. This is because instance methods require the C# compiler
    // to generate extra locals as the `this` parameter has to be passed by reference.
    // Having them be extension methods means that the `this` parameter can be passed by
    // value instead, thus reducing the number of locals and helping prevent us from hitting
    // the internal inlining limits of the JIT.

    public static class Vector128
    {
        internal const int Size = 16;

#if TARGET_ARM
        internal const int Alignment = 8;
#else
        internal const int Alignment = 16;
#endif

        /// <summary>Gets a value that indicates whether 128-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 128-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>128-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 128-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => IsHardwareAccelerated;
        }

        /// <summary>Computes the absolute value of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its absolute value computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Abs<T>(Vector128<T> vector)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return vector;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return vector;
            }
            else if (typeof(T) == typeof(uint))
            {
                return vector;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return vector;
            }
            else
            {
                return SoftwareFallback(vector);
            }

            static Vector128<T> SoftwareFallback(Vector128<T> vector)
            {
                Unsafe.SkipInit(out Vector128<T> result);

                for (int index = 0; index < Vector128<T>.Count; index++)
                {
                    T value = Scalar<T>.Abs(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Add<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> AndNot<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left & ~right;

        /// <summary>Reinterprets a <see cref="Vector128{TFrom}" /> as a new <see cref="Vector128{TTo}" />.</summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{TTo}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<TTo> As<TFrom, TTo>(this Vector128<TFrom> vector)
            where TFrom : struct
            where TTo : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<TTo>();

            return Unsafe.As<Vector128<TFrom>, Vector128<TTo>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<byte> AsByte<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<double> AsDouble<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<short> AsInt16<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<int> AsInt32<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<long> AsInt64<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<nint> AsNInt<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<nuint> AsNUInt<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<float> AsSingle<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector)
            where T : struct => vector.As<T, ulong>();

        /// <summary>Reinterprets a <see cref="Vector2" /> as a new <see cref="Vector128{Single}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        public static Vector128<float> AsVector128(this Vector2 value)
            => new Vector4(value, 0.0f, 0.0f).AsVector128();

        /// <summary>Reinterprets a <see cref="Vector3" /> as a new <see cref="Vector128{Single}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        public static Vector128<float> AsVector128(this Vector3 value)
            => new Vector4(value, 0.0f).AsVector128();

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector128{Single}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128(this Vector4 value)
            => Unsafe.As<Vector4, Vector128<float>>(ref value);

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the vectors.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<T> AsVector128<T>(this Vector<T> value)
            where T : struct
        {
            Debug.Assert(Vector<T>.Count >= Vector128<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            return Unsafe.As<Vector<T>, Vector128<T>>(ref value);
        }

        /// <summary>Reinterprets a <see cref="Vector128{Single}" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        public static Vector2 AsVector2(this Vector128<float> value)
            => Unsafe.As<Vector128<float>, Vector2>(ref value);

        /// <summary>Reinterprets a <see cref="Vector128{Single}" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
        public static Vector3 AsVector3(this Vector128<float> value)
            => Unsafe.As<Vector128<float>, Vector3>(ref value);

        /// <summary>Reinterprets a <see cref="Vector128{Single}" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector4" />.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Vector128<float> value)
            => Unsafe.As<Vector128<float>, Vector4>(ref value);

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector{T}" />.</summary>
        /// <typeparam name="T">The type of the vectors.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<T> AsVector<T>(this Vector128<T> value)
            where T : struct
        {
            Debug.Assert(Vector<T>.Count >= Vector128<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            Vector<T> result = default;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector<T>, byte>(ref result), value);
            return result;
        }

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> BitwiseAnd<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> BitwiseOr<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        public static Vector128<float> Ceiling(Vector128<float> vector)
        {
            Unsafe.SkipInit(out Vector128<float> result);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                float value = Scalar<float>.Ceiling(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        public static Vector128<double> Ceiling(Vector128<double> vector)
        {
            Unsafe.SkipInit(out Vector128<double> result);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                double value = Scalar<double>.Ceiling(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> ConditionalSelect<T>(Vector128<T> condition, Vector128<T> left, Vector128<T> right)
            where T : struct => (left & condition) | (right & ~condition);

        /// <summary>Converts a <see cref="Vector128{Int64}" /> to a <see cref="Vector128{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> ConvertToDouble(Vector128<long> vector)
        {
            if (Sse2.IsSupported)
            {
                // Based on __m256d int64_to_double_fast_precise(const __m256i v)
                // from https://stackoverflow.com/a/41223013/12860347. CC BY-SA 4.0

                Vector128<int> lowerBits;

                if (Avx2.IsSupported)
                {
                    lowerBits = vector.AsInt32();
                    lowerBits = Avx2.Blend(lowerBits, Create(0x43300000_00000000).AsInt32(), 0b1010);           // Blend the 32 lowest significant bits of vector with the bit representation of double(2^52)
                }
                else
                {
                    lowerBits = Sse2.And(vector, Create(0x00000000_FFFFFFFFL)).AsInt32();
                    lowerBits = Sse2.Or(lowerBits, Create(0x43300000_00000000).AsInt32());
                }

                Vector128<long> upperBits = Sse2.ShiftRightLogical(vector, 32);                                             // Extract the 32 most significant bits of vector
                upperBits = Sse2.Xor(upperBits, Create(0x45300000_80000000));                                   // Flip the msb of upperBits and blend with the bit representation of double(2^84 + 2^63)

                Vector128<double> result = Sse2.Subtract(upperBits.AsDouble(), Create(0x45300000_80100000).AsDouble());       // Compute in double precision: (upper - (2^84 + 2^63 + 2^52)) + lower
                return Sse2.Add(result, lowerBits.AsDouble());
            }
            else
            {
                return SoftwareFallback(vector);
            }

            static Vector128<double> SoftwareFallback(Vector128<long> vector)
            {
                Unsafe.SkipInit(out Vector128<double> result);

                for (int i = 0; i < Vector128<double>.Count; i++)
                {
                    double value = vector.GetElementUnsafe(i);
                    result.SetElementUnsafe(i, value);
                }

                return result;
            }
        }

        /// <summary>Converts a <see cref="Vector128{UInt64}" /> to a <see cref="Vector128{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> ConvertToDouble(Vector128<ulong> vector)
        {
            if (Sse2.IsSupported)
            {
                // Based on __m256d uint64_to_double_fast_precise(const __m256i v)
                // from https://stackoverflow.com/a/41223013/12860347. CC BY-SA 4.0

                Vector128<uint> lowerBits;

                if (Avx2.IsSupported)
                {
                    lowerBits = vector.AsUInt32();
                    lowerBits = Avx2.Blend(lowerBits, Create(0x43300000_00000000UL).AsUInt32(), 0b1010);        // Blend the 32 lowest significant bits of vector with the bit representation of double(2^52)
                }
                else
                {
                    lowerBits = Sse2.And(vector, Create(0x00000000_FFFFFFFFUL)).AsUInt32();
                    lowerBits = Sse2.Or(lowerBits, Create(0x43300000_00000000UL).AsUInt32());
                }

                Vector128<ulong> upperBits = Sse2.ShiftRightLogical(vector, 32);                                             // Extract the 32 most significant bits of vector
                upperBits = Sse2.Xor(upperBits, Create(0x45300000_00000000UL));                                 // Blend upperBits with the bit representation of double(2^84)

                Vector128<double> result = Sse2.Subtract(upperBits.AsDouble(), Create(0x45300000_00100000UL).AsDouble());     // Compute in double precision: (upper - (2^84 + 2^52)) + lower
                return Sse2.Add(result, lowerBits.AsDouble());
            }
            else
            {
                return SoftwareFallback(vector);
            }

            static Vector128<double> SoftwareFallback(Vector128<ulong> vector)
            {
                Unsafe.SkipInit(out Vector128<double> result);

                for (int i = 0; i < Vector128<double>.Count; i++)
                {
                    double value = vector.GetElementUnsafe(i);
                    result.SetElementUnsafe(i, value);
                }

                return result;
            }
        }

        /// <summary>Converts a <see cref="Vector128{Single}" /> to a <see cref="Vector128{Int32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> ConvertToInt32(Vector128<float> vector)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int i = 0; i < Vector128<int>.Count; i++)
            {
                int value = (int)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector128{Double}" /> to a <see cref="Vector128{Int64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector128<long> ConvertToInt64(Vector128<double> vector)
        {
            Unsafe.SkipInit(out Vector128<long> result);

            for (int i = 0; i < Vector128<long>.Count; i++)
            {
                long value = (long)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector128{Int32}" /> to a <see cref="Vector128{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> ConvertToSingle(Vector128<int> vector)
        {
            Unsafe.SkipInit(out Vector128<float> result);

            for (int i = 0; i < Vector128<float>.Count; i++)
            {
                float value = vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector128{UInt32}" /> to a <see cref="Vector128{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> ConvertToSingle(Vector128<uint> vector)
        {
            if (Sse2.IsSupported)
            {
                // This first bit of magic works because float can exactly represent integers up to 2^24
                //
                // This means everything between 0 and 2^16 (ushort.MaxValue + 1) are exact and so
                // converting each of the upper and lower halves will give an exact result

                Vector128<int> lowerBits = Sse2.And(vector, Create(0x0000FFFFU)).AsInt32();
                Vector128<int> upperBits = Sse2.ShiftRightLogical(vector, 16).AsInt32();

                Vector128<float> lower = Sse2.ConvertToVector128Single(lowerBits);
                Vector128<float> upper = Sse2.ConvertToVector128Single(upperBits);

                // This next bit of magic works because all multiples of 65536, at least up to 65535
                // are likewise exactly representable
                //
                // This means that scaling upper by 65536 gives us the exactly representable base value
                // and then the remaining lower value, which is likewise up to 65535 can be added on
                // giving us a result that will correctly round to the nearest representable value

                if (Fma.IsSupported)
                {
                    return Fma.MultiplyAdd(upper, Vector128.Create(65536.0f), lower);
                }
                else
                {
                    Vector128<float> result = Sse.Multiply(upper, Vector128.Create(65536.0f));
                    return Sse.Add(result, lower);
                }
            }
            else
            {
                return SoftwareFallback(vector);
            }

            static Vector128<float> SoftwareFallback(Vector128<uint> vector)
            {
                Unsafe.SkipInit(out Vector128<float> result);

                for (int i = 0; i < Vector128<float>.Count; i++)
                {
                    float value = vector.GetElementUnsafe(i);
                    result.SetElementUnsafe(i, value);
                }

                return result;
            }
        }

        /// <summary>Converts a <see cref="Vector128{Single}" /> to a <see cref="Vector128{UInt32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<uint> ConvertToUInt32(Vector128<float> vector)
        {
            Unsafe.SkipInit(out Vector128<uint> result);

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                uint value = (uint)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector128{Double}" /> to a <see cref="Vector128{UInt64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<ulong> ConvertToUInt64(Vector128<double> vector)
        {
            Unsafe.SkipInit(out Vector128<ulong> result);

            for (int i = 0; i < Vector128<ulong>.Count; i++)
            {
                ulong value = (ulong)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Copies a <see cref="Vector128{T}" /> to a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        public static void CopyTo<T>(this Vector128<T> vector, T[] destination)
            where T : struct => vector.CopyTo(destination, startIndex: 0);

        /// <summary>Copies a <see cref="Vector128{T}" /> to a given array starting at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        public static unsafe void CopyTo<T>(this Vector128<T> vector, T[] destination, int startIndex)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (destination is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Vector128<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
        }

        /// <summary>Copies a <see cref="Vector128{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which the <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        public static void CopyTo<T>(this Vector128<T> vector, Span<T> destination)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if ((uint)destination.Length < (uint)Vector128<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<T> Create<T>(T value)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return Create((byte)(object)value).As<byte, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return Create((double)(object)value).As<double, T>();
            }
            else if (typeof(T) == typeof(short))
            {
                return Create((short)(object)value).As<short, T>();
            }
            else if (typeof(T) == typeof(int))
            {
                return Create((int)(object)value).As<int, T>();
            }
            else if (typeof(T) == typeof(long))
            {
                return Create((long)(object)value).As<long, T>();
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return Create((sbyte)(object)value).As<sbyte, T>();
            }
            else if (typeof(T) == typeof(float))
            {
                return Create((float)(object)value).As<float, T>();
            }
            else if (typeof(T) == typeof(ushort))
            {
                return Create((ushort)(object)value).As<ushort, T>();
            }
            else if (typeof(T) == typeof(uint))
            {
                return Create((uint)(object)value).As<uint, T>();
            }
            else if (typeof(T) == typeof(ulong))
            {
                return Create((ulong)(object)value).As<ulong, T>();
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi8</remarks>
        /// <returns>A new <see cref="Vector128{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<byte> Create(byte value)
        {
            byte* pResult = stackalloc byte[16]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128d _mm_set1_pd</remarks>
        /// <returns>A new <see cref="Vector128{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<double> Create(double value)
        {
            double* pResult = stackalloc double[2]
            {
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi16</remarks>
        /// <returns>A new <see cref="Vector128{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<short> Create(short value)
        {
            short* pResult = stackalloc short[8]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi32</remarks>
        /// <returns>A new <see cref="Vector128{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> Create(int value)
        {
            int* pResult = stackalloc int[4]
            {
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi64x</remarks>
        /// <returns>A new <see cref="Vector128{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<long> Create(long value)
        {
            long* pResult = stackalloc long[2]
            {
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{IntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{IntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<nint> Create(nint value)
        {
#if TARGET_64BIT
            return Create((long)value).AsNInt();
#else
            return Create((int)value).AsNInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{UIntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UIntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<nuint> Create(nuint value)
        {
#if TARGET_64BIT
            return Create((ulong)value).AsNUInt();
#else
            return Create((uint)value).AsNUInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi8</remarks>
        /// <returns>A new <see cref="Vector128{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte value)
        {
            sbyte* pResult = stackalloc sbyte[16]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128 _mm_set1_ps</remarks>
        /// <returns>A new <see cref="Vector128{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> Create(float value)
        {
            float* pResult = stackalloc float[4]
            {
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi16</remarks>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort value)
        {
            ushort* pResult = stackalloc ushort[8]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi32</remarks>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint value)
        {
            uint* pResult = stackalloc uint[4]
            {
                value,
                value,
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_set1_epi64x</remarks>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong value)
        {
            ulong* pResult = stackalloc ulong[2]
            {
                value,
                value,
            };
            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        public static Vector128<T> Create<T>(T[] values)
            where T : struct => Create(values, index: 0);

        /// <summary>Creates a new <see cref="Vector128{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector128{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Create<T>(T[] values, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (values is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((index < 0) || ((values.Length - index) < Vector128<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Create<T>(ReadOnlySpan<T> values)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (values.Length < Vector128<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi8</remarks>
        /// <returns>A new <see cref="Vector128{Byte}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
        {
            byte* pResult = stackalloc byte[16]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
                e8,
                e9,
                e10,
                e11,
                e12,
                e13,
                e14,
                e15,
            };
            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128d _mm_setr_pd</remarks>
        /// <returns>A new <see cref="Vector128{Double}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<double> Create(double e0, double e1)
        {
            double* pResult = stackalloc double[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi16</remarks>
        /// <returns>A new <see cref="Vector128{Int16}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
        {
            short* pResult = stackalloc short[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };
            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi32</remarks>
        /// <returns>A new <see cref="Vector128{Int32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> Create(int e0, int e1, int e2, int e3)
        {
            int* pResult = stackalloc int[4]
            {
                e0,
                e1,
                e2,
                e3,
            };
            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi64x</remarks>
        /// <returns>A new <see cref="Vector128{Int64}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<long> Create(long e0, long e1)
        {
            long* pResult = stackalloc long[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi8</remarks>
        /// <returns>A new <see cref="Vector128{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
        {
            sbyte* pResult = stackalloc sbyte[16]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
                e8,
                e9,
                e10,
                e11,
                e12,
                e13,
                e14,
                e15,
            };
            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128 _mm_setr_ps</remarks>
        /// <returns>A new <see cref="Vector128{Single}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> Create(float e0, float e1, float e2, float e3)
        {
            float* pResult = stackalloc float[4]
            {
                e0,
                e1,
                e2,
                e3,
            };
            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi16</remarks>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
        {
            ushort* pResult = stackalloc ushort[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };
            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi32</remarks>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint e0, uint e1, uint e2, uint e3)
        {
            uint* pResult = stackalloc uint[4]
            {
                e0,
                e1,
                e2,
                e3,
            };
            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi64x</remarks>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong e0, ulong e1)
        {
            ulong* pResult = stackalloc ulong[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance from two <see cref="Vector64{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<byte> SoftwareFallback(Vector64<byte> lower, Vector64<byte> upper)
            {
                Vector128<byte> result128 = Vector128<byte>.Zero;

                ref Vector64<byte> result64 = ref Unsafe.As<Vector128<byte>, Vector64<byte>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance from two <see cref="Vector64{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> Create(Vector64<double> lower, Vector64<double> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<double> SoftwareFallback (Vector64<double> lower, Vector64<double> upper)
            {
                Vector128<double> result128 = Vector128<double>.Zero;

                ref Vector64<double> result64 = ref Unsafe.As<Vector128<double>, Vector64<double>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance from two <see cref="Vector64{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<short> Create(Vector64<short> lower, Vector64<short> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<short> SoftwareFallback(Vector64<short> lower, Vector64<short> upper)
            {
                Vector128<short> result128 = Vector128<short>.Zero;

                ref Vector64<short> result64 = ref Unsafe.As<Vector128<short>, Vector64<short>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance from two <see cref="Vector64{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi64</remarks>
        /// <returns>A new <see cref="Vector128{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<int> Create(Vector64<int> lower, Vector64<int> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<int> SoftwareFallback(Vector64<int> lower, Vector64<int> upper)
            {
                Vector128<int> result128 = Vector128<int>.Zero;

                ref Vector64<int> result64 = ref Unsafe.As<Vector128<int>, Vector64<int>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance from two <see cref="Vector64{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<long> Create(Vector64<long> lower, Vector64<long> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<long> SoftwareFallback(Vector64<long> lower, Vector64<long> upper)
            {
                Vector128<long> result128 = Vector128<long>.Zero;

                ref Vector64<long> result64 = ref Unsafe.As<Vector128<long>, Vector64<long>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance from two <see cref="Vector64{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<sbyte> Create(Vector64<sbyte> lower, Vector64<sbyte> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<sbyte> SoftwareFallback(Vector64<sbyte> lower, Vector64<sbyte> upper)
            {
                Vector128<sbyte> result128 = Vector128<sbyte>.Zero;

                ref Vector64<sbyte> result64 = ref Unsafe.As<Vector128<sbyte>, Vector64<sbyte>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance from two <see cref="Vector64{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> Create(Vector64<float> lower, Vector64<float> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<float> SoftwareFallback(Vector64<float> lower, Vector64<float> upper)
            {
                Vector128<float> result128 = Vector128<float>.Zero;

                ref Vector64<float> result64 = ref Unsafe.As<Vector128<float>, Vector64<float>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance from two <see cref="Vector64{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<ushort> Create(Vector64<ushort> lower, Vector64<ushort> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<ushort> SoftwareFallback(Vector64<ushort> lower, Vector64<ushort> upper)
            {
                Vector128<ushort> result128 = Vector128<ushort>.Zero;

                ref Vector64<ushort> result64 = ref Unsafe.As<Vector128<ushort>, Vector64<ushort>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance from two <see cref="Vector64{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m128i _mm_setr_epi64</remarks>
        /// <returns>A new <see cref="Vector128{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<uint> Create(Vector64<uint> lower, Vector64<uint> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<uint> SoftwareFallback(Vector64<uint> lower, Vector64<uint> upper)
            {
                Vector128<uint> result128 = Vector128<uint>.Zero;

                ref Vector64<uint> result64 = ref Unsafe.As<Vector128<uint>, Vector64<uint>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance from two <see cref="Vector64{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<ulong> Create(Vector64<ulong> lower, Vector64<ulong> upper)
        {
            if (AdvSimd.IsSupported)
            {
                return lower.ToVector128Unsafe().WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector128<ulong> SoftwareFallback(Vector64<ulong> lower, Vector64<ulong> upper)
            {
                Vector128<ulong> result128 = Vector128<ulong>.Zero;

                ref Vector64<ulong> result64 = ref Unsafe.As<Vector128<ulong>, Vector64<ulong>>(ref result128);
                result64 = lower;
                Unsafe.Add(ref result64, 1) = upper;

                return result128;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<byte> CreateScalar(byte value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<byte>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we call
                // the UInt32 overload to ensure zero extension. We can then just treat the result as byte and return.
                return Sse2.ConvertScalarToVector128UInt32(value).AsByte();
            }

            return SoftwareFallback(value);

            static Vector128<byte> SoftwareFallback(byte value)
            {
                Vector128<byte> result = Vector128<byte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<byte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> CreateScalar(double value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<double>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                return Sse2.MoveScalar(Vector128<double>.Zero, CreateScalarUnsafe(value));
            }

            return SoftwareFallback(value);

            static Vector128<double> SoftwareFallback(double value)
            {
                Vector128<double> result = Vector128<double>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<short> CreateScalar(short value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<short>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we cast
                // to ushort and call the UInt32 overload to ensure zero extension. We can then just treat the result as short and return.
                return Sse2.ConvertScalarToVector128UInt32((ushort)(value)).AsInt16();
            }

            return SoftwareFallback(value);

            static Vector128<short> SoftwareFallback(short value)
            {
                Vector128<short> result = Vector128<short>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<short>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<int> CreateScalar(int value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<int>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                return Sse2.ConvertScalarToVector128Int32(value);
            }

            return SoftwareFallback(value);

            static Vector128<int> SoftwareFallback(int value)
            {
                Vector128<int> result = Vector128<int>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<long> CreateScalar(long value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<long>.Zero, 0, value);
            }

            if (Sse2.X64.IsSupported)
            {
                return Sse2.X64.ConvertScalarToVector128Int64(value);
            }

            return SoftwareFallback(value);

            static Vector128<long> SoftwareFallback(long value)
            {
                Vector128<long> result = Vector128<long>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<long>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<nint> CreateScalar(nint value)
        {
#if TARGET_64BIT
            return CreateScalar((long)value).AsNInt();
#else
            return CreateScalar((int)value).AsNInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<nuint> CreateScalar(nuint value)
        {
#if TARGET_64BIT
            return CreateScalar((ulong)value).AsNUInt();
#else
            return CreateScalar((uint)value).AsNUInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalar(sbyte value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<sbyte>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we cast
                // to byte and call the UInt32 overload to ensure zero extension. We can then just treat the result as sbyte and return.
                return Sse2.ConvertScalarToVector128UInt32((byte)(value)).AsSByte();
            }

            return SoftwareFallback(value);

            static Vector128<sbyte> SoftwareFallback(sbyte value)
            {
                Vector128<sbyte> result = Vector128<sbyte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<sbyte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> CreateScalar(float value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<float>.Zero, 0, value);
            }

            if (Sse.IsSupported)
            {
                return Sse.MoveScalar(Vector128<float>.Zero, CreateScalarUnsafe(value));
            }

            return SoftwareFallback(value);

            static Vector128<float> SoftwareFallback(float value)
            {
                Vector128<float> result = Vector128<float>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalar(ushort value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<ushort>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we call
                // the UInt32 overload to ensure zero extension. We can then just treat the result as ushort and return.
                return Sse2.ConvertScalarToVector128UInt32(value).AsUInt16();
            }

            return SoftwareFallback(value);

            static Vector128<ushort> SoftwareFallback(ushort value)
            {
                Vector128<ushort> result = Vector128<ushort>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalar(uint value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<uint>.Zero, 0, value);
            }

            if (Sse2.IsSupported)
            {
                return Sse2.ConvertScalarToVector128UInt32(value);
            }

            return SoftwareFallback(value);

            static Vector128<uint> SoftwareFallback(uint value)
            {
                Vector128<uint> result = Vector128<uint>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalar(ulong value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector128<ulong>.Zero, 0, value);
            }

            if (Sse2.X64.IsSupported)
            {
                return Sse2.X64.ConvertScalarToVector128UInt64(value);
            }

            return SoftwareFallback(value);

            static Vector128<ulong> SoftwareFallback(ulong value)
            {
                Vector128<ulong> result = Vector128<ulong>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ulong>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            byte* pResult = stackalloc byte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<double> CreateScalarUnsafe(double value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            double* pResult = stackalloc double[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            short* pResult = stackalloc short[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            int* pResult = stackalloc int[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<long> CreateScalarUnsafe(long value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            long* pResult = stackalloc long[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<nint> CreateScalarUnsafe(nint value)
        {
#if TARGET_64BIT
            return CreateScalarUnsafe((long)value).AsNInt();
#else
            return CreateScalarUnsafe((int)value).AsNInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<nuint> CreateScalarUnsafe(nuint value)
        {
#if TARGET_64BIT
            return CreateScalarUnsafe((ulong)value).AsNUInt();
#else
            return CreateScalarUnsafe((uint)value).AsNUInt();
#endif
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            sbyte* pResult = stackalloc sbyte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            float* pResult = stackalloc float[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ushort* pResult = stackalloc ushort[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            uint* pResult = stackalloc uint[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalarUnsafe(ulong value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ulong* pResult = stackalloc ulong[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Divide<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static T Dot<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            T result = default;

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.Multiply(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result = Scalar<T>.Add(result, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector128<T> Equals<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => Equals(left, right).As<T, ulong>() != Vector128<ulong>.Zero;

        /// <summary>Extracts the most significant bit from each element in a vector.</summary>
        /// <param name="vector">The vector whose elements should have their most significant bit extracted.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The packed most significant bits extracted from the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ExtractMostSignificantBits<T>(this Vector128<T> vector)
            where T : struct
        {
            uint result = 0;

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                uint value = Scalar<T>.ExtractMostSignificantBit(vector.GetElementUnsafe(index));
                value <<= index;
                result |= value;
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        public static Vector128<float> Floor(Vector128<float> vector)
        {
            Unsafe.SkipInit(out Vector128<float> result);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                float value = Scalar<float>.Floor(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        public static Vector128<double> Floor(Vector128<double> vector)
        {
            Unsafe.SkipInit(out Vector128<double> result);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                double value = Scalar<double>.Floor(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static T GetElement<T>(this Vector128<T> vector, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if ((uint)(index) >= (uint)(Vector128<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Gets the value of the lower 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 64-bits from.</param>
        /// <returns>The value of the lower 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<T> GetLower<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            return Unsafe.As<Vector128<T>, Vector64<T>>(ref vector);
        }

        /// <summary>Gets the value of the upper 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 64-bits from.</param>
        /// <returns>The value of the upper 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<T> GetUpper<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref vector);
            return Unsafe.Add(ref lower, 1);
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector128<T> GreaterThan<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => GreaterThan(left, right).As<T, ulong>() == Vector128<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => GreaterThan(left, right).As<T, ulong>() != Vector128<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector128<T> GreaterThanOrEqual<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, ulong>() == Vector128<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, ulong>() != Vector128<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector128<T> LessThan<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => LessThan(left, right).As<T, ulong>() == Vector128<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => LessThan(left, right).As<T, ulong>() != Vector128<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector128<T> LessThanOrEqual<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, ulong>() == Vector128<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, ulong>() != Vector128<ulong>.Zero;

        /// <summary>Loads a vector from the given source.</summary>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<T> Load<T>(T* source)
            where T : unmanaged
        {
            return *(Vector128<T>*)source;
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<T> LoadAligned<T>(T* source)
            where T : unmanaged
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (((nuint)source % Alignment) != 0)
            {
                throw new AccessViolationException();
            }

            return *(Vector128<T>*)source;
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<T> LoadAlignedNonTemporal<T>(T* source)
            where T : unmanaged
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (((nuint)source % Alignment) != 0)
            {
                throw new AccessViolationException();
            }

            return *(Vector128<T>*)source;
        }

        /// <summary>Loads a vector from the given source.</summary>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> LoadUnsafe<T>(ref T source)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref source));
        }

        /// <summary>Loads a vector from the given source and element offset.</summary>
        /// <param name="source">The source to which <paramref name="elementOffset" /> will be added before loading the vector.</param>
        /// <param name="elementOffset">The element offset from <paramref name="source" /> from which the vector will be loaded.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The vector loaded from <paramref name="source" /> plus <paramref name="elementOffset" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            source = ref Unsafe.Add(ref source, (nint)elementOffset);
            return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref source));
        }

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector128<T> Max<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the minimum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector128<T> Min<T>(Vector128<T> left, Vector128<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Multiply<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Multiply<T>(Vector128<T> left, T right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Multiply<T>(T left, Vector128<T> right)
            where T : struct => left * right;

        /// <summary>Narrows two <see cref="Vector128{Double}"/> instances into one <see cref="Vector128{Single}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{Single}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> Narrow(Vector128<double> lower, Vector128<double> upper)
        {
            Unsafe.SkipInit(out Vector128<float> result);

            for (int i = 0; i < Vector128<double>.Count; i++)
            {
                float value = (float)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<double>.Count; i < Vector128<float>.Count; i++)
            {
                float value = (float)upper.GetElementUnsafe(i - Vector128<double>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{Int16}"/> instances into one <see cref="Vector128{SByte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{SByte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<sbyte> Narrow(Vector128<short> lower, Vector128<short> upper)
        {
            Unsafe.SkipInit(out Vector128<sbyte> result);

            for (int i = 0; i < Vector128<short>.Count; i++)
            {
                sbyte value = (sbyte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<short>.Count; i < Vector128<sbyte>.Count; i++)
            {
                sbyte value = (sbyte)upper.GetElementUnsafe(i - Vector128<short>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{Int32}"/> instances into one <see cref="Vector128{Int16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{Int16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<short> Narrow(Vector128<int> lower, Vector128<int> upper)
        {
            Unsafe.SkipInit(out Vector128<short> result);

            for (int i = 0; i < Vector128<int>.Count; i++)
            {
                short value = (short)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<int>.Count; i < Vector128<short>.Count; i++)
            {
                short value = (short)upper.GetElementUnsafe(i - Vector128<int>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{Int64}"/> instances into one <see cref="Vector128{Int32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{Int32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> Narrow(Vector128<long> lower, Vector128<long> upper)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int i = 0; i < Vector128<long>.Count; i++)
            {
                int value = (int)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<long>.Count; i < Vector128<int>.Count; i++)
            {
                int value = (int)upper.GetElementUnsafe(i - Vector128<long>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{UInt16}"/> instances into one <see cref="Vector128{Byte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{Byte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<byte> Narrow(Vector128<ushort> lower, Vector128<ushort> upper)
        {
            Unsafe.SkipInit(out Vector128<byte> result);

            for (int i = 0; i < Vector128<ushort>.Count; i++)
            {
                byte value = (byte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<ushort>.Count; i < Vector128<byte>.Count; i++)
            {
                byte value = (byte)upper.GetElementUnsafe(i - Vector128<ushort>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{UInt32}"/> instances into one <see cref="Vector128{UInt16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{UInt16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<ushort> Narrow(Vector128<uint> lower, Vector128<uint> upper)
        {
            Unsafe.SkipInit(out Vector128<ushort> result);

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                ushort value = (ushort)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<uint>.Count; i < Vector128<ushort>.Count; i++)
            {
                ushort value = (ushort)upper.GetElementUnsafe(i - Vector128<uint>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector128{UInt64}"/> instances into one <see cref="Vector128{UInt32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector128{UInt32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector128<uint> Narrow(Vector128<ulong> lower, Vector128<ulong> upper)
        {
            Unsafe.SkipInit(out Vector128<uint> result);

            for (int i = 0; i < Vector128<ulong>.Count; i++)
            {
                uint value = (uint)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector128<ulong>.Count; i < Vector128<uint>.Count; i++)
            {
                uint value = (uint)upper.GetElementUnsafe(i - Vector128<ulong>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Negates a vector.</summary>
        /// <param name="vector">The vector to negate.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the negation of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Negate<T>(Vector128<T> vector)
            where T : struct => -vector;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> OnesComplement<T>(Vector128<T> vector)
            where T : struct => ~vector;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftLeft(Vector128<byte> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<byte> result);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                byte element = Scalar<byte>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> ShiftLeft(Vector128<short> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<short> result);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                short element = Scalar<short>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ShiftLeft(Vector128<int> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                int element = Scalar<int>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<long> ShiftLeft(Vector128<long> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<long> result);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                long element = Scalar<long>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<nint> ShiftLeft(Vector128<nint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<nint> result);

            for (int index = 0; index < Vector128<nint>.Count; index++)
            {
                nint element = Scalar<nint>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<nuint> ShiftLeft(Vector128<nuint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<nuint> result);

            for (int index = 0; index < Vector128<nuint>.Count; index++)
            {
                nuint element = Scalar<nuint>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> ShiftLeft(Vector128<sbyte> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<sbyte> result);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                sbyte element = Scalar<sbyte>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ushort> ShiftLeft(Vector128<ushort> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<ushort> result);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                ushort element = Scalar<ushort>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> ShiftLeft(Vector128<uint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<uint> result);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                uint element = Scalar<uint>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> ShiftLeft(Vector128<ulong> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<ulong> result);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                ulong element = Scalar<ulong>.ShiftLeft(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<short> result);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                short element = Scalar<short>.ShiftRightArithmetic(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                int element = Scalar<int>.ShiftRightArithmetic(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<long> result);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                long element = Scalar<long>.ShiftRightArithmetic(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<nint> ShiftRightArithmetic(Vector128<nint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<nint> result);

            for (int index = 0; index < Vector128<nint>.Count; index++)
            {
                nint element = Scalar<nint>.ShiftRightArithmetic(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<sbyte> result);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                sbyte element = Scalar<sbyte>.ShiftRightArithmetic(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftRightLogical(Vector128<byte> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<byte> result);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                byte element = Scalar<byte>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> ShiftRightLogical(Vector128<short> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<short> result);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                short element = Scalar<short>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ShiftRightLogical(Vector128<int> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                int element = Scalar<int>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<long> ShiftRightLogical(Vector128<long> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<long> result);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                long element = Scalar<long>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<nint> ShiftRightLogical(Vector128<nint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<nint> result);

            for (int index = 0; index < Vector128<nint>.Count; index++)
            {
                nint element = Scalar<nint>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<nuint> ShiftRightLogical(Vector128<nuint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<nuint> result);

            for (int index = 0; index < Vector128<nuint>.Count; index++)
            {
                nuint element = Scalar<nuint>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<sbyte> result);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                sbyte element = Scalar<sbyte>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<ushort> result);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                ushort element = Scalar<ushort>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<uint> result);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                uint element = Scalar<uint>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> vector, int shiftCount)
        {
            Unsafe.SkipInit(out Vector128<ulong> result);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                ulong element = Scalar<ulong>.ShiftRightLogical(vector.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<byte> Shuffle(Vector128<byte> vector, Vector128<byte> indices)
        {
            Unsafe.SkipInit(out Vector128<byte> result);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                byte selectedIndex = indices.GetElementUnsafe(index);
                byte selectedValue = 0;

                if (selectedIndex < Vector128<byte>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> vector, Vector128<sbyte> indices)
        {
            Unsafe.SkipInit(out Vector128<sbyte> result);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                byte selectedIndex = (byte)indices.GetElementUnsafe(index);
                sbyte selectedValue = 0;

                if (selectedIndex < Vector128<sbyte>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<short> Shuffle(Vector128<short> vector, Vector128<short> indices)
        {
            Unsafe.SkipInit(out Vector128<short> result);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                ushort selectedIndex = (ushort)indices.GetElementUnsafe(index);
                short selectedValue = 0;

                if (selectedIndex < Vector128<short>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ushort> Shuffle(Vector128<ushort> vector, Vector128<ushort> indices)
        {
            Unsafe.SkipInit(out Vector128<ushort> result);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                ushort selectedIndex = indices.GetElementUnsafe(index);
                ushort selectedValue = 0;

                if (selectedIndex < Vector128<ushort>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<int> Shuffle(Vector128<int> vector, Vector128<int> indices)
        {
            Unsafe.SkipInit(out Vector128<int> result);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                int selectedValue = 0;

                if (selectedIndex < Vector128<int>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<uint> Shuffle(Vector128<uint> vector, Vector128<uint> indices)
        {
            Unsafe.SkipInit(out Vector128<uint> result);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                uint selectedIndex = indices.GetElementUnsafe(index);
                uint selectedValue = 0;

                if (selectedIndex < Vector128<uint>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<float> Shuffle(Vector128<float> vector, Vector128<int> indices)
        {
            Unsafe.SkipInit(out Vector128<float> result);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                float selectedValue = 0;

                if (selectedIndex < Vector128<float>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<long> Shuffle(Vector128<long> vector, Vector128<long> indices)
        {
            Unsafe.SkipInit(out Vector128<long> result);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                long selectedValue = 0;

                if (selectedIndex < (uint)Vector128<long>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ulong> Shuffle(Vector128<ulong> vector, Vector128<ulong> indices)
        {
            Unsafe.SkipInit(out Vector128<ulong> result);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                ulong selectedIndex = indices.GetElementUnsafe(index);
                ulong selectedValue = 0;

                if (selectedIndex < (uint)Vector128<ulong>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector128<double> Shuffle(Vector128<double> vector, Vector128<long> indices)
        {
            Unsafe.SkipInit(out Vector128<double> result);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                double selectedValue = 0;

                if (selectedIndex < (uint)Vector128<double>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <param name="vector">The vector whose square root is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        public static Vector128<T> Sqrt<T>(Vector128<T> vector)
            where T : struct
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                T value = Scalar<T>.Sqrt(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Store<T>(this Vector128<T> source, T* destination)
            where T : unmanaged
        {
            *(Vector128<T>*)destination = source;
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void StoreAligned<T>(this Vector128<T> source, T* destination)
            where T : unmanaged
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (((nuint)destination % Alignment) != 0)
            {
                throw new AccessViolationException();
            }

            *(Vector128<T>*)destination = source;
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void StoreAlignedNonTemporal<T>(this Vector128<T> source, T* destination)
            where T : unmanaged
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (((nuint)destination % Alignment) != 0)
            {
                throw new AccessViolationException();
            }

            *(Vector128<T>*)destination = source;
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Subtract<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left - right;

        /// <summary>Computes the sum of all elements in a vector.</summary>
        /// <param name="vector">The vector whose elements will be summed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of all elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        public static T Sum<T>(Vector128<T> vector)
            where T : struct
        {
            T sum = default;

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                sum = Scalar<T>.Add(sum, vector.GetElementUnsafe(index));
            }

            return sum;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static T ToScalar<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the given vector and the upper 128-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of <paramref name="vector" /> and the upper 128-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<T> ToVector256<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            Vector256<T> result = Vector256<T>.Zero;
            Unsafe.As<Vector256<T>, Vector128<T>>(ref result) = vector;
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the given vector and the upper 128-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of <paramref name="vector" /> and the upper 128-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static unsafe Vector256<T> ToVector256Unsafe<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            byte* pResult = stackalloc byte[Vector256.Size];
            Unsafe.AsRef<Vector128<T>>(pResult) = vector;
            return Unsafe.AsRef<Vector256<T>>(pResult);
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector128{T}.Count" />.</returns>
        public static bool TryCopyTo<T>(this Vector128<T> vector, Span<T> destination)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if ((uint)destination.Length < (uint)Vector128<T>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
            return true;
        }

        /// <summary>Widens a <see cref="Vector128{Byte}" /> into two <see cref="Vector128{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector128<ushort> Lower, Vector128<ushort> Upper) Widen(Vector128<byte> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{Int16}" /> into two <see cref="Vector128{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector128<int> Lower, Vector128<int> Upper) Widen(Vector128<short> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{Int32}" /> into two <see cref="Vector128{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector128<long> Lower, Vector128<long> Upper) Widen(Vector128<int> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{SByte}" /> into two <see cref="Vector128{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector128<short> Lower, Vector128<short> Upper) Widen(Vector128<sbyte> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{Single}" /> into two <see cref="Vector128{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector128<double> Lower, Vector128<double> Upper) Widen(Vector128<float> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{UInt16}" /> into two <see cref="Vector128{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector128<uint> Lower, Vector128<uint> Upper) Widen(Vector128<ushort> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector128{UInt32}" /> into two <see cref="Vector128{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector128<ulong> Lower, Vector128<ulong> Upper) Widen(Vector128<uint> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector128{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Vector128<T> WithElement<T>(this Vector128<T> vector, int index, T value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if ((uint)(index) >= (uint)(Vector128<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector128<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the lower 64-bits set to the specified value and the upper 64-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 64-bits from.</param>
        /// <param name="value">The value of the lower 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to <paramref name="value" /> and the upper 64-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> WithLower<T>(this Vector128<T> vector, Vector64<T> value)
            where T : struct
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.InsertScalar(vector.AsUInt64(), 0, value.AsUInt64()).As<ulong, T>();
            }

            return SoftwareFallback(vector, value);

            static Vector128<T> SoftwareFallback(Vector128<T> vector, Vector64<T> value)
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

                Vector128<T> result = vector;
                Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = value;
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the upper 64-bits set to the specified value and the upper 64-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 64-bits from.</param>
        /// <param name="value">The value of the upper 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the upper 64-bits set to <paramref name="value" /> and the lower 64-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> WithUpper<T>(this Vector128<T> vector, Vector64<T> value)
            where T : struct
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.InsertScalar(vector.AsUInt64(), 1, value.AsUInt64()).As<ulong, T>();
            }

            return SoftwareFallback(vector, value);

            static Vector128<T> SoftwareFallback(Vector128<T> vector, Vector64<T> value)
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

                Vector128<T> result = vector;
                ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref result);
                Unsafe.Add(ref lower, 1) = value;
                return result;
            }
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Xor<T>(Vector128<T> left, Vector128<T> right)
            where T : struct => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector128<T> vector, int index)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector128<T>.Count));
            return Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in vector)), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector128<T> vector, int index, T value)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector128<T>.Count));
            Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in vector)), index) = value;
        }

        [Intrinsic]
        internal static Vector128<ushort> WidenLower(Vector128<byte> source)
        {
            Unsafe.SkipInit(out Vector128<ushort> lower);

            for (int i = 0; i < Vector128<ushort>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<int> WidenLower(Vector128<short> source)
        {
            Unsafe.SkipInit(out Vector128<int> lower);

            for (int i = 0; i < Vector128<int>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<long> WidenLower(Vector128<int> source)
        {
            Unsafe.SkipInit(out Vector128<long> lower);

            for (int i = 0; i < Vector128<long>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<short> WidenLower(Vector128<sbyte> source)
        {
            Unsafe.SkipInit(out Vector128<short> lower);

            for (int i = 0; i < Vector128<short>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<double> WidenLower(Vector128<float> source)
        {
            Unsafe.SkipInit(out Vector128<double> lower);

            for (int i = 0; i < Vector128<double>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<uint> WidenLower(Vector128<ushort> source)
        {
            Unsafe.SkipInit(out Vector128<uint> lower);

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector128<ulong> WidenLower(Vector128<uint> source)
        {
            Unsafe.SkipInit(out Vector128<ulong> lower);

            for (int i = 0; i < Vector128<ulong>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static Vector128<ushort> WidenUpper(Vector128<byte> source)
        {
            Unsafe.SkipInit(out Vector128<ushort> upper);

            for (int i = Vector128<ushort>.Count; i < Vector128<byte>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<ushort>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<int> WidenUpper(Vector128<short> source)
        {
            Unsafe.SkipInit(out Vector128<int> upper);

            for (int i = Vector128<int>.Count; i < Vector128<short>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<int>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<long> WidenUpper(Vector128<int> source)
        {
            Unsafe.SkipInit(out Vector128<long> upper);

            for (int i = Vector128<long>.Count; i < Vector128<int>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<long>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<short> WidenUpper(Vector128<sbyte> source)
        {
            Unsafe.SkipInit(out Vector128<short> upper);

            for (int i = Vector128<short>.Count; i < Vector128<sbyte>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<short>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<double> WidenUpper(Vector128<float> source)
        {
            Unsafe.SkipInit(out Vector128<double> upper);

            for (int i = Vector128<double>.Count; i < Vector128<float>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<double>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<uint> WidenUpper(Vector128<ushort> source)
        {
            Unsafe.SkipInit(out Vector128<uint> upper);

            for (int i = Vector128<uint>.Count; i < Vector128<ushort>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<uint>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector128<ulong> WidenUpper(Vector128<uint> source)
        {
            Unsafe.SkipInit(out Vector128<ulong> upper);

            for (int i = Vector128<ulong>.Count; i < Vector128<uint>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector128<ulong>.Count, value);
            }

            return upper;
        }
    }
}
