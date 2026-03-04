using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Networking.Shared.Services
{
    internal class TCP_Socket
    {
        private const int MaxAllowedMessageSize = 1024 * 1024; // 1 MB

        public async Task SendTCPMessageAsync(NetworkStream stream, string data, CancellationToken CancelToken)
        {
            // converting the message into a byte array.
            byte[] messageBytes = Encoding.UTF8.GetBytes(data);

            // converting the length of the message into a byte array (4 bytes for an int).
            byte[] lengthPrefix = BitConverter.GetBytes(messageBytes.Length);

            // Send length first as an array of 4 bytes
            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, CancelToken);

            // Send actual message as an array of bytes
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length, CancelToken);
        }

        public async Task<string> ReceiveTCPMessageAsync(NetworkStream stream, CancellationToken CancelToken)
        {
            // Read length (4 bytes)
            byte[] lengthBuffer = await ReadExactAsync(stream, 4, CancelToken);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            // safety check for message length to prevent potential DoS attacks with extremely large messages.
            if (messageLength > MaxAllowedMessageSize)
            {
                Console.WriteLine("[!] Received a message that exceeds the maximum allowed size. Disconnecting client.");
                throw new OperationCanceledException(); // Treat as disconnection
            }

            // Read actual message
            byte[] messageBuffer = await ReadExactAsync(stream, messageLength, CancelToken);

            if (messageBuffer == Array.Empty<byte>())
            {
                throw new OperationCanceledException(); // Treat as disconnection
            }

            string message = Encoding.UTF8.GetString(messageBuffer);

            return message;
        }

        private async Task<byte[]> ReadExactAsync(NetworkStream stream, int size, CancellationToken CancelToken)
        {
            byte[] buffer = new byte[size]; // creating an empty buffer array of bytes of the expected message size.
            int totalRead = 0; // total count of the bytes read so far.

            while (totalRead < size) // loop until we have read the expected number of bytes.
            {
                // ReadAsync Modifies buffer in place, so we pass the same buffer and adjust the offset and count based on how many bytes we've already read.
                int bytesRead = await stream.ReadAsync(buffer, totalRead, size - totalRead, CancelToken);

                if (bytesRead == 0) return Array.Empty<byte>();

                totalRead += bytesRead; // add the number of bytes read in this iteration to the total count.
            }
            return buffer; // return the modified buffer which now contains the complete message read from the stream.
        }
    }
}
