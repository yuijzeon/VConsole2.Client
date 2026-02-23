using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babyduck.VConsole2.Client
{
    public class VConsole2Client : IDisposable
    {
        private readonly string _hostname;
        private readonly int _port;
        private readonly ushort _version;
        private readonly TcpClient _client = new TcpClient();
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event Action<MessageChunk>? OnMessageReceived;
        public event Action<Exception>? OnException;

        public VConsole2Client(string hostname = "localhost", int port = 29000, ushort version = 0x00D4)
        {
            _hostname = hostname;
            _port = port;
            _version = version;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _stream.Dispose();
            _client.Dispose();
        }

        public async Task Connect()
        {
            await _client.ConnectAsync(_hostname, _port).ConfigureAwait(false);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && _client.Connected)
                    {
                        await ReadMessage(token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    OnException?.Invoke(ex);
                }
            }, token);
        }

        public async Task SendCommand(string command)
        {
            var totalLength = Encoding.ASCII.GetByteCount(command) + Marshal.SizeOf<ChunkHeader>() + 1;

            var header = new ChunkHeader
            {
                Type = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("CMND")),
                Version = BinaryPrimitives.ReverseEndianness(_version),
                Length = BinaryPrimitives.ReverseEndianness((uint)totalLength),
                Handle = BinaryPrimitives.ReverseEndianness((ushort)0x0000),
            };

            var payload = new List<byte>();
            payload.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)).ToArray());
            payload.AddRange(Encoding.ASCII.GetBytes(command));
            payload.Add(0x00);

            await _stream.WriteAsync(payload.ToArray()).ConfigureAwait(false);
        }

        private async Task ReadMessage(CancellationToken token)
        {
            var headerBuf = new byte[Marshal.SizeOf<ChunkHeader>()];
            await ReadExactlyAsync(headerBuf, token);

            var header = MemoryMarshal.Read<ChunkHeader>(headerBuf);
            header.Version = BinaryPrimitives.ReverseEndianness(header.Version);
            header.Length = BinaryPrimitives.ReverseEndianness(header.Length);
            header.Handle = BinaryPrimitives.ReverseEndianness(header.Handle);

            byte[] payloadBuf = { };
            var payloadSize = (int)header.Length - Marshal.SizeOf<ChunkHeader>();
            if (payloadSize > 0)
            {
                payloadBuf = new byte[payloadSize];
                await ReadExactlyAsync(payloadBuf, token);
            }

            OnMessageReceived?.Invoke(new MessageChunk
            {
                Header = header,
                Payload = payloadBuf
            });
        }

        private async Task ReadExactlyAsync(Memory<byte> buffer, CancellationToken token)
        {
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await _stream.ReadAsync(buffer[totalRead..], token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                totalRead += read;
            }
        }
    }
}