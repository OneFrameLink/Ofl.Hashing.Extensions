using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ofl.Hashing.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<ReadOnlyMemory<byte>> ComputeHashAsync(
            this IAsyncEnumerable<byte> enumerable,
            IHashAlgorithm hashAlgorithm,
            CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));

            // Memory
            var buffer = new Memory<byte>(new byte[1]);

            // Cycle through the items.
            await foreach (byte b in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Copy the byte.
                var copy = b;

                // Write the byte to the span
                MemoryMarshal.Write(buffer.Span, ref copy);

                // Transform.
                hashAlgorithm.TransformBlock(buffer.Span);
            }

            // Return the hash.
            return hashAlgorithm.Hash;
        }
    }
}
