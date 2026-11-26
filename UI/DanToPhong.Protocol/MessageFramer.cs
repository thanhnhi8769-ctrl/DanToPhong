using System;
using System.Buffers.Binary;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DanToPhong.Protocol
{
    public static class MessageFramer
    {
        private const int MaxPacketBytes = 64 * 1024;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static async Task SendAsync<T>(Stream stream, T payload, CancellationToken token = default)
        {
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            if (body.Length > MaxPacketBytes)
                throw new InvalidDataException("Kích thước gói tin vượt quá giới hạn cho phép.");

            byte[] frame = new byte[4 + body.Length];
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), body.Length);
            Buffer.BlockCopy(body, 0, frame, 4, body.Length);

            await stream.WriteAsync(frame, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        public static async Task<T?> ReceiveAsync<T>(Stream stream, CancellationToken token = default)
        {
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, token).ConfigureAwait(false))
                return default;

            int length = BinaryPrimitives.ReadInt32BigEndian(lenBuf);
            if (length <= 0 || length > MaxPacketBytes)
                throw new InvalidDataException($"Độ dài gói tin không hợp lệ: {length}");

            byte[] body = new byte[length];
            if (!await ReadExactAsync(stream, body, token).ConfigureAwait(false))
                throw new EndOfStreamException("Kết nối bị ngắt giữa chừng khi đang đọc dữ liệu.");

            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), token)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    if (offset == 0) return false;
                    throw new EndOfStreamException("Kết nối đóng đột ngột khi chưa nhận đủ byte.");
                }
                offset += read;
            }
            return true;
        }
    }
}
