using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ofl.Hashing.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<ReadOnlyMemory<byte>> ComputeHashAsync(this IAsyncEnumerable<byte> enumerable,
            IHashAlgorithm hashAlgorithm, CancellationToken cancellationToken)
        {
            // Validate parameters.
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));

            // Get the enumerator.
            using (IAsyncEnumerator<byte> enumerator = enumerable.GetEnumerator())
            {
                // The moved task.
                Task<bool> movedTask = enumerator.MoveNext(cancellationToken);

                // If it wasn't moved, get out.
                if (!(await movedTask.ConfigureAwait(false))) return hashAlgorithm.Hash;

                // Memory
                var buffer = new Memory<byte>(new byte[1]);

                // While there's stuff to process.
                do
                {
                    // Get the byte.
                    byte b = enumerator.Current;

                    // Get the current value.
                    MemoryMarshal.Write(buffer.Span, ref b);

                    // Move to the next immediately.
                    movedTask = enumerator.MoveNext(cancellationToken);

                    // Process in the meantime.
                    hashAlgorithm.TransformBlock(buffer.Span);
                } while (!(await movedTask.ConfigureAwait(false)));

                // Return the hash.
                return hashAlgorithm.Hash;
            }
        }
    }
}
