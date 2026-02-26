using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private readonly Subject<MessageChunk> _messageSubject = new Subject<MessageChunk>();
        private readonly Subject<Exception> _exceptionSubject = new Subject<Exception>();

        public IObservable<MessageChunk> OnMessageReceived => _messageSubject.AsObservable();
        public IObservable<Exception> OnException => _exceptionSubject.AsObservable();

        public VConsole2Client(string hostname = "localhost", int port = 29000, ushort version = 0x00D4)
        {
            _hostname = hostname;
            _port = port;
            _version = version;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _stream?.Dispose();
            _client.Dispose();

            _messageSubject.OnCompleted();
            _exceptionSubject.OnCompleted();
            _messageSubject.Dispose();
            _exceptionSubject.Dispose();
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
                    _exceptionSubject.OnNext(ex);
                }
            }, token);
        }

        public async Task SendCommand(string command, CancellationToken ct = default)
        {
            var stream = GetRequireStream();
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

            await stream.WriteAsync(payload.ToArray(), ct).ConfigureAwait(false);
        }

        private async Task ReadMessage(CancellationToken ct = default)
        {
            var headerBuf = new byte[Marshal.SizeOf<ChunkHeader>()];
            await ReadExactlyAsync(headerBuf, ct);

            var header = MemoryMarshal.Read<ChunkHeader>(headerBuf);
            header.Version = BinaryPrimitives.ReverseEndianness(header.Version);
            header.Length = BinaryPrimitives.ReverseEndianness(header.Length);
            header.Handle = BinaryPrimitives.ReverseEndianness(header.Handle);

            byte[] payloadBuf = { };
            var payloadSize = (int)header.Length - Marshal.SizeOf<ChunkHeader>();
            if (payloadSize > 0)
            {
                payloadBuf = new byte[payloadSize];
                await ReadExactlyAsync(payloadBuf, ct);
            }

            _messageSubject.OnNext(new MessageChunk
            {
                Header = header,
                Payload = payloadBuf,
            });
        }

        private async Task ReadExactlyAsync(Memory<byte> buffer, CancellationToken token)
        {
            var stream = GetRequireStream();
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer[totalRead..], token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                totalRead += read;
            }
        }

        private NetworkStream GetRequireStream()
        {
            if (_stream == null || !_client.Connected)
            {
                throw new InvalidOperationException("VConsole2Client is not connected. Call Connect() before using this method.");
            }

            return _stream;
        }
    }
}