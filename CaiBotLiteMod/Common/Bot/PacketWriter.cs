using CaiBotLiteMod.Common.Model;
using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.Utils;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace CaiBotLiteMod.Common.Bot;

[Serializable]
public class PacketWriter(PackageType packageType, bool isRequest, string? requestId)
{
    private static bool Debug => CaiBotLiteMod.DebugMode;
    public Packet Packet = new (Direction.ToBot, packageType, isRequest, requestId);

    public PacketWriter Write(string key, object value)
    {
        this.Packet.Payload.Add(key, value);
        return this;
    }

    public void Send()
    {
        try
        {
            var message = this.Packet.ToJson();
            if (Debug)
            {
                Log.WriteLine($"[CaiBotLite]发送BOT数据包：{message}", ConsoleColor.Blue);
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            _ = WebsocketManager.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (Exception e)
        {
            Log.WriteLine($"[CaiBotLite]发送数据包时发生错误：{e}", ConsoleColor.Red);
        }
    }
}