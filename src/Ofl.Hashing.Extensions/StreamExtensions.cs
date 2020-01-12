using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ofl.Hashing.Extensions
{
    public static class StreamExtensions
    {
        public static readonly int DefaultBufferSize = 4096;

        public static Task<ReadOnlyMemory<byte>> ComputeHashAsync(
            this Stream stream, 
            IHashAlgorithm hashAlgorithm, 
            CancellationToken cancellationToken
        ) => stream.ComputeHashAsync(hashAlgorithm, DefaultBufferSize, cancellationToken);

        public static Task<ReadOnlyMemory<byte>> ComputeHashAsync(
            this Stream stream,
            IHashAlgorithm hashAlgorithm,
            int bufferSize,
            CancellationToken cancellationToken
        ) => stream.ComputeHashAsync(hashAlgorithm, ArrayPool<byte>.Shared, bufferSize, cancellationToken);

        public static async Task<ReadOnlyMemory<byte>> ComputeHashAsync(
            this Stream stream, 
            IHashAlgorithm hashAlgorithm,
            ArrayPool<byte> arrayPool,
            int bufferSize,
            CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (arrayPool == null) throw new ArgumentNullException(nameof(arrayPool));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, $"The { nameof(bufferSize) } parameter must be a positive value.");

            // The declarations for the buffer and the copy.
            byte[]? buffer = null;
            byte[]? copy = null;

            // Wrap in a try/finally.
            try
            {
                // Allocate.
                buffer = arrayPool.Rent(bufferSize);
                copy = arrayPool.Rent(bufferSize);

                // The bytes read task.
                Task<int> bytesRead = stream.ReadAsync(buffer, 0, bufferSize, cancellationToken);

                // No bytes read, get out.
                if ((await bytesRead.ConfigureAwait(false)) == 0) return hashAlgorithm.Hash;

                // While there are items to process.
                do
                {
                    // Copy the buffer.
                    // NOTE: Need to do this because it's a shared resource.
                    // If tests show there's a sweet spot between the copy time and hash time < next ReadAsync time,
                    // move buffer size accordingly.
                    Array.Copy(buffer, copy, bufferSize);

                    // Read the next task.
                    bytesRead = stream.ReadAsync(buffer, 0, bufferSize, cancellationToken);

                    // Hash.
                    hashAlgorithm.TransformBlock(copy);

                    // Get the next stream immediately.
                } while (await bytesRead.ConfigureAwait(false) > 0);

                // Return.
                return hashAlgorithm.Hash;
            }
            finally
            {
                // Release.
                arrayPool.Return(buffer);
                arrayPool.Return(copy);
            }
        }

        public static ref readonly ReadOnlyMemory<byte> ComputeHash(
            this Stream stream, 
            IHashAlgorithm hashAlgorithm
        ) => ref stream.ComputeHash(hashAlgorithm, DefaultBufferSize);

        public static ref readonly ReadOnlyMemory<byte> ComputeHash(
            this Stream stream,
            IHashAlgorithm hashAlgorithm,
            int bufferSize
        ) => ref stream.ComputeHash(hashAlgorithm, ArrayPool<byte>.Shared, bufferSize);

        public static ref readonly ReadOnlyMemory<byte> ComputeHash(
            this Stream stream, 
            IHashAlgorithm hashAlgorithm, 
            ArrayPool<byte> arrayPool,
            int bufferSize
        )
        {
            // Validate parameters.
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (arrayPool == null) throw new ArgumentNullException(nameof(arrayPool));
            if (bufferSize <= 0) 
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, 
                    $"The { nameof(bufferSize) } parameter must be a positive value.");

            // The buffer.
            byte[]? buffer = null;

            // Wrap in a try/finally.
            try
            {
                // Rent.
                buffer = arrayPool.Rent(bufferSize);

                // Cycle through the stream.
                while (stream.Read(buffer, 0, bufferSize) != 0)
                    // Run through the hash.
                    hashAlgorithm.TransformBlock(buffer);

                // Return.
                return ref hashAlgorithm.Hash;
            }
            finally
            {
                // Release.
                arrayPool.Return(buffer);
            }
        }
    }
}
