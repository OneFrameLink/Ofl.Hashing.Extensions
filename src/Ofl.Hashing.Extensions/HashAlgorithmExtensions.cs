using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ofl.Hashing.Extensions
{
    public static class HashAlgorithmExtensions
    {
        public static int GetHashCode(this IHashAlgorithm hashAlgorithm, params int[] hashCodes) =>
            hashAlgorithm.GetHashCode((IEnumerable<int>) hashCodes);

        public static int GetHashCode(this IHashAlgorithm hashAlgorithm, IEnumerable<int> hashCodes)
        {
            // Validate parameters.
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (hashCodes == null) throw new ArgumentNullException(nameof(hashCodes));

            // Unsafe.
            unsafe
            {
                // Cycle through
                // the hashcodes, transforming each block that's returned.
                foreach (int hashCode in hashCodes)
                {
                    // Copy.
                    var hashCodeCopy = hashCode;

                    // Get the pointer.
                    int* p = &hashCodeCopy;

                    // Get the bytes.
                    var bytes = new ReadOnlySpan<byte>(p, sizeof(int));

                    // Transform the block.
                    hashAlgorithm.TransformBlock(bytes);
                }
            }

            // Return the hash.
            return MemoryMarshal.Read<int>(hashAlgorithm.Hash.Span);
        }

        public static ref readonly ReadOnlyMemory<byte> ComputeHash(this IHashAlgorithm hashAlgorithm, ReadOnlySpan<byte> bytes)
        {
            // Validate parameters.
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            // Transform the block.
            hashAlgorithm.TransformBlock(bytes);

            // Return the hash.
            return ref hashAlgorithm.Hash;
        }
    }
}
