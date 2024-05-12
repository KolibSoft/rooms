using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using KolibSoft.Rooms.Core.Protocol;

namespace KolibSoft.Rooms.Core.Sockets
{
    public class RoomWebStream : RoomStream
    {

        public WebSocket Socket { get; private set; }

        protected override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token)
        {
            if (Socket.State != WebSocketState.Open) return 0;
            var result = await Socket.ReceiveAsync(buffer, token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, token);
                return 0;
            }
            if (result.MessageType == WebSocketMessageType.Text)
            {
                await Socket.CloseOutputAsync(WebSocketCloseStatus.InvalidMessageType, null, token);
                return 0;
            }
            return result.Count;
        }

        protected override async ValueTask<int> WriteAsync(Memory<byte> buffer, CancellationToken token)
        {
            if (Socket.State != WebSocketState.Open) return 0;
            await Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, token);
            return buffer.Length;
        }

        public RoomWebStream(WebSocket socket, ArraySegment<byte> buffer) : base(buffer) => Socket = socket;
        public RoomWebStream(WebSocket socket, int buffering = 1024) : this(socket, new byte[buffering]) { }

    }
}