using CaiBotLiteMod.Common;
using CaiBotLiteMod.Hooks;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Config = CaiBotLiteMod.Common.Config;

namespace CaiBotLiteMod;

public class CaiBotLiteMod : Mod
{
    private static int InitCode = -1;
    public static bool DebugMode;
    public static bool StopWebsocket;

    internal static ClientWebSocket? WebSocket
    {
        get => PacketWriter.WebSocket;
        set => PacketWriter.WebSocket = value;
    }
    
    public static readonly Version? PluginVersion = ModLoader.GetMod("CaiBotLiteMod").Version;
    public static readonly TSPlayer[] Players = new TSPlayer[256];


    public override void Load()
    {
        if (!Main.dedServ)
        {
            return;
        }

        DebugMode = Program.LaunchParameters.ContainsKey("-caidebug");
        Config.Read();

        if (Config.Settings.Token == "")
        {
            GenCode();
        }
        
        PacketWriter.Init(true, DebugMode);
        ExecuteCommandHook.Apply();
        Task.Factory.StartNew(StartCaiApi, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(StartHeartBeat, TaskCreationOptions.LongRunning);
    }

    public override void Unload()
    {
        StopWebsocket = true;
        WebSocket?.Dispose();
        ExecuteCommandHook.Dispose();
    }


    private static async Task? StartHeartBeat()
    {
        while (!StopWebsocket)
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            try
            {
                if (WebSocket is { State: WebSocketState.Open })
                {
                    var packetWriter = new PacketWriter();
                    packetWriter.SetType("HeartBeat")
                        .Send();
                }
            }
            catch
            {
                Console.WriteLine("[CaiBotLite]心跳包发送失败!");
            }
        }
    }

    private static async Task? StartCaiApi()
    {
        while (!StopWebsocket)
        {
            try
            {
                WebSocket = new ClientWebSocket();
                while (string.IsNullOrEmpty(Config.Settings.Token))
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    HttpClient client = new ();
                    client.Timeout = TimeSpan.FromSeconds(5.0);
                    var response = client.GetAsync($"https://api.terraria.ink:22338/bot/get_token?code={InitCode}")
                        .Result;
                    if (response.StatusCode != HttpStatusCode.OK || Config.Settings.Token != "")
                    {
                        continue;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(responseBody);
                    var token = json["token"]!.ToString();
                    Config.Settings.Token = token;
                    Config.Settings.Write();
                    Console.WriteLine("[CaiBotLite]被动绑定成功!");
                }


                await WebSocket.ConnectAsync(new Uri($"wss://api.terraria.ink:22338/bot/" + Config.Settings.Token), CancellationToken.None);


                while (true)
                {
                    var buffer = new byte[1024];
                    var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var receivedData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (DebugMode)
                    {
                        Console.WriteLine($"[CaiBotLite]收到BOT数据包: {receivedData}");
                    }

                    _ = CaiBotApi.HandleMessageAsync(receivedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CaiBotLite]CaiBot断开连接...");
                if (DebugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                else
                {
                    Console.WriteLine("链接失败原因: " + ex.Message);
                }
            }

            await Task.Delay(5000);
        }
    }


    public static void GenCode()
    {
        if (Config.Settings.Token != "")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[CaiBotLite]你已经绑定过了!");
            Console.ResetColor();
            return;
        }

        Random rnd = new ();
        InitCode = rnd.Next(10000000, 99999999);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[CaiBotLite]您的服务器绑定码为: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(InitCode);
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("*你可以在启动服务器后使用'/生成绑定码'重新生成");
        Console.ResetColor();
    }


    // private void Who(CommandArgs args)
    // {
    //     var result = "";
    //     List<string> strings =
    //     [
    //         $"[i:603]在线玩家 ({Main.player.Count(p => p is { active: true })}/{Main.maxNetPlayers})"
    //     ];
    //     for (var k = 0; k < 255; k++)
    //     {
    //         if (Main.player[k].active)
    //         {
    //             result += Main.player[k].name + ",";
    //         }
    //     }
    //
    //     strings.Add($"{result.TrimEnd(',')}");
    //     args.Player.SendSuccessMessage(string.Join("\n", strings));
    // }
}