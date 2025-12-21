using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Bot;

public static class WebsocketManager
{
    public static ClientWebSocket WebSocket = null!;

    private const string BotServerUrl = "api.terraria.ink:22338";
    internal static bool IsWebsocketConnected => WebSocket.State == WebSocketState.Open;
    private static bool _isStopWebsocket;

    public static void Init()
    {
        Task.Factory.StartNew(StartCaiApi, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(StartHeartBeat, TaskCreationOptions.LongRunning);
        _isStopWebsocket = false;
    }

    public static void StopWebsocket()
    {
        _isStopWebsocket = true;
        WebSocket.Dispose();
    }

    private static async Task? StartHeartBeat()
    {
        while (!_isStopWebsocket)
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    var packetWriter = new PacketWriter(PackageType.Heartbeat, false, null);
                    packetWriter.Send();
                }
            }
            catch
            {
                Log.WriteLine("[CaiBotLite]心跳包发送失败!", ConsoleColor.Red);
            }
        }
    }

    private static async Task? StartCaiApi()
    {
        while (!_isStopWebsocket)
        {
            try
            {
                WebSocket = new ClientWebSocket();
                while (string.IsNullOrEmpty(ClientConfig.Instance.Token))
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    HttpClient client = new ();
                    client.Timeout = TimeSpan.FromSeconds(5.0);
                    var response = await client.GetAsync($"https://{BotServerUrl}/server/token/{CaiBotLiteMod.InitCode}");
                    if (response.StatusCode != HttpStatusCode.OK || ClientConfig.Instance.Token != "")
                    {
                        continue;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(responseBody);
                    var token = json["token"]!.ToString();
                    var groupOpenId = json["group_open_id"]!.ToString();
                    ClientConfig.Instance.Token = token;
                    ClientConfig.Instance.GroupOpenId = groupOpenId;
                    ClientConfig.Instance.Save();
                    Log.WriteLine("[CaiBotLite]被动绑定成功!", ConsoleColor.Green);
                }

                WebSocket.Options.SetRequestHeader("authorization", $"Bearer {ClientConfig.Instance.Token}");
                await WebSocket.ConnectAsync(new Uri($"wss://{BotServerUrl}/server/ws/{ClientConfig.Instance.GroupOpenId}/tModLoader/"), CancellationToken.None);

                new PacketWriter(PackageType.Hello, false, null)
                    .Write("server_core_version", ModLoader.versionedName)
                    .Write("plugin_version", CaiBotLiteMod.PluginVersion)
                    .Write("game_version", ModLoader.versionedName)
                    .Write("enable_whitelist", ServerConfig.Instance.EnableWhiteList)
                    .Write("system", RuntimeInformation.RuntimeIdentifier)
                    .Write("server_name", Main.worldName)
                    .Write("settings", new Dictionary<string, object>())
                    .Send();

                Log.WriteLine("[CaiBotLite]Bot连接成功...", ConsoleColor.Green);

                while (true)
                {
                    var buffer = new byte[1024];
                    var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // 连接关闭时获取原因
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        var statusCode = (int) result.CloseStatus!;
                        switch (statusCode)
                        {
                            case 4003:
                                ClientConfig.Instance.Token = "";
                                ClientConfig.Instance.Save();
                                Log.WriteLine("[CaiBotLite]服务器认证失败, 请重新绑定!", ConsoleColor.Red);
                                Log.WriteLine($"原因({statusCode}): {result.CloseStatusDescription}", ConsoleColor.Red);
                                CaiBotLiteMod.GenCode();
                                break;
                            default:
                                Log.WriteLine("[CaiBotLite]Bot主动断开连接!", ConsoleColor.Red);
                                Log.WriteLine($"原因({statusCode}): {result.CloseStatusDescription}", ConsoleColor.Red);
                                break;
                        }

                        break;
                    }

                    var receivedData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (CaiBotLiteMod.DebugMode)
                    {
                        Log.WriteLine($"[CaiBotLite]收到BOT数据包: {receivedData}", ConsoleColor.Blue);
                    }

                    await Api.HandleMessageAsync(receivedData);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("[CaiBotLite]Bot断开连接...", ConsoleColor.Red);
                if (!_isStopWebsocket)
                {
                    Log.WriteLine(ex.ToString(), ConsoleColor.Red);
                }
            }

            await Task.Delay(5000);
        }
    }
}