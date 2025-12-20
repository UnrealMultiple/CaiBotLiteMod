using CaiBotLiteMod.Enums;
using CaiBotLiteMod.Moudles;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace CaiBotLiteMod.Common;

[Serializable]
public class PackageWriter(PackageType packageType, bool isRequest, string? requestId)
{
    private static bool Debug => CaiBotLiteMod.DebugMode;
    public Package Package = new (Direction.ToBot, packageType, isRequest, requestId);

    public PackageWriter Write(string key, object value)
    {
        this.Package.Payload.Add(key, value);
        return this;
    }

    public void Send()
    {
        try
        {
            var message = this.Package.ToJson();
            if (Debug)
            {
                Console.WriteLine($"[CaiBotLite]发送BOT数据包：{message}");
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            _ = WebsocketManager.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (Exception e)
        {
            Utils.WriteLine($"[CaiBotLite]发送数据包时发生错误：{e}", ConsoleColor.Red);
        }
    }
}