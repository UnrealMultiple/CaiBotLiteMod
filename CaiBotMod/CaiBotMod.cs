using CaiBotMod.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Config = CaiBotMod.Common.Config;

namespace CaiBotMod;

public class CaiBotMod : Mod
{
    private static int _initCode = -1;
    public static ClientWebSocket WebSocket = new ();
    private static Task _webSocketTask = Task.CompletedTask;
    private static bool _stopWebSocket = false;
    private static readonly CancellationTokenSource TokenSource = new ();
    private static readonly CancellationToken Ct = TokenSource.Token;
    public static readonly Version? PluginVersion = ModLoader.GetMod("CaiBotMod").Version;
    public static readonly Dictionary<string, Point> PlayerDeath = new ();
    public static readonly TSPlayer[] Players = new TSPlayer[256];
    

    public override void Load()
    {
        if (!Main.dedServ)
        {
            return;
        }

        Commands.ChatCommands.Add(new Command(this.TpNpc, "tpnpc", "tpn"));
        Commands.ChatCommands.Add(new Command(Home, "home", "spawn"));
        Commands.ChatCommands.Add(new Command(this.Who, "who", "online"));
        Commands.ChatCommands.Add(new Command(this.Back, "back", "b"));
        Commands.ChatCommands.Add(new Command(Help, "help"));
        Config.Read();

        if (Config.config.Token == "")
        {
            GenCode();
        }

        _webSocketTask = Task.Run(async () =>
        {
            while (!_stopWebSocket)
            {
                try
                {
                    while (Config.config.Token == "")
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        HttpClient client = new ();
                        client.Timeout = TimeSpan.FromSeconds(5.0);
                        var response = client.GetAsync($"http://api.terraria.ink:22334/bot/get_token?" +
                                                       $"code={_initCode}")
                            .Result;
                        if (response.StatusCode == HttpStatusCode.OK && Config.config.Token == "")
                        {
                            var responseBody = await response.Content.ReadAsStringAsync();
                            var json = JObject.Parse(responseBody);
                            var token = json["token"]!.ToString();
                            Config.config.Token = token;
                            Config.config.Write();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[CaiAPI]被动绑定成功!");
                            Console.ResetColor();
                        }
                    }

                    WebSocket = new ClientWebSocket();
                    if (Program.LaunchParameters.ContainsKey("-cailocaldebug"))
                    {
                        await WebSocket.ConnectAsync(new Uri("ws://127.0.0.1:22334/bot/" + Config.config.Token),
                            CancellationToken.None);
                    }
                    else
                    {
                        await WebSocket.ConnectAsync(new Uri("ws://api.terraria.ink:22334/bot/" + Config.config.Token),
                            CancellationToken.None);
                    }

                    while (true)
                    {
                        var buffer = new byte[1024];
                        var result =
                            await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        var receivedData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (Program.LaunchParameters.ContainsKey("-caidebug"))
                        {
                            Console.WriteLine($"[CaiAPI]收到BOT数据包: {receivedData}");
                        }

                        _ = MessageHandle.HandleMessageAsync(receivedData);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[CaiAPI]CaiBot断开连接...");
                    if (Program.LaunchParameters.ContainsKey("-caidebug"))
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    else
                    {
                        Console.WriteLine("链接失败原因: " + ex.Message);
                    }

                    Console.ResetColor();
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }, Ct);
    }

    public override void Unload()
    {
        _stopWebSocket = true;
        WebSocket.Dispose();
        if (!_webSocketTask.IsCompleted)
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
    }


    private static void Help(CommandArgs args)
    {
        if (args.Parameters.Count > 1)
        {
            args.Player.SendErrorMessage("无效格式.正确格式: /help <命令/页码>");
            return;
        }

        if (args.Parameters.Count == 0 || int.TryParse(args.Parameters[0], out var pageNumber))
        {
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
            {
                return;
            }

            IEnumerable<string> cmdNames = from cmd in Commands.ChatCommands
                where cmd.CanRun(args.Player) && cmd.Name != "setup"
                select "/" + cmd.Name;

            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
                new PaginationTools.Settings { HeaderFormat = "命令列表:", FooterFormat = "输入/help {{0}}翻页." });
        }
        else
        {
            var commandName = args.Parameters[0].ToLower();
            if (commandName.StartsWith('/'))
            {
                commandName = commandName.Substring(1);
            }

            var command = Commands.ChatCommands.Find(c => c.Names.Contains(commandName));
            if (command == null)
            {
                args.Player.SendErrorMessage("无效命令.");
                return;
            }

            args.Player.SendSuccessMessage("/{0}的帮助清单: ", command.Name);

            foreach (var line in command.HelpDesc)
            {
                args.Player.SendInfoMessage(line);
            }
        }
    }


    public static void GenCode()
    {
        if (Config.config.Token != "")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[CaiBot]你已经绑定过了!");
            Console.ResetColor();
            return;
        }

        Random rnd = new ();
        _initCode = rnd.Next(10000000, 99999999);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[CaiBot]您的服务器绑定码为: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_initCode);
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("*你可以在启动服务器后使用'/生成绑定码'重新生成");
        Console.ResetColor();
    }

    private void Back(CommandArgs args)
    {
        if (args.Player.Dead)
        {
            args.Player.SendErrorMessage("[i:3457]你还没复活呢,不能回到出生点！");
            return;
        }

        if (!PlayerDeath.TryGetValue(args.Player.Name, out var value))
        {
            args.Player.SendErrorMessage("[i:3457]你还没有去世过呢！");
            return;
        }

        args.Player.Teleport(value.X, value.Y);
        args.Player.SendSuccessMessage("[i:3457]你已经回到上一次死亡点啦！");
    }

    private void Who(CommandArgs args)
    {
        var result = "";
        List<string> strings =
        [
            $"[i:603]在线玩家 ({Main.player.Count(p => p is { active: true })}/{Main.maxNetPlayers})"
        ];
        for (var k = 0; k < 255; k++)
        {
            if (Main.player[k].active)
            {
                result += Main.player[k].name + ",";
            }
        }

        strings.Add($"{result.TrimEnd(',')}");
        args.Player.SendSuccessMessage(string.Join("\n", strings));
    }

    private static void Home(CommandArgs args)
    {
        if (args.Player.Dead)
        {
            args.Player.SendErrorMessage("[i:50]你死了,不能回到出生点!");
            return;
        }

        args.Player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY * 16) - 3);
        args.Player.SendSuccessMessage("[i:50]已经将你传送到世界出生点~");
    }

    private void TpNpc(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            args.Player.SendErrorMessage("[i:267]无效用法.正确用法: /tpnpc <NPC>.");
            return;
        }

        var npcStr = string.Join(" ", args.Parameters);
        List<NPC> matches = new ();
        var isIndex = false;
        var npcId = 0;
        try
        {
            npcId = int.Parse(npcStr);
            if (npcId >= 0 && Main.npc[npcId] != null)
            {
                isIndex = true;
            }
        }
        catch
        {
            // ignored
        }

        if (isIndex)
        {
            var target2 = Main.npc[npcId];
            args.Player.Teleport(target2.position.X, target2.position.Y);
            args.Player.SendSuccessMessage("[i:267]已传送到'{0}'附近.", target2.FullName);
            return;
        }

        foreach (var npc in Main.npc.Where(npc => npc.active))
        {
            if (string.Equals(npc.FullName, npcStr, StringComparison.InvariantCultureIgnoreCase))
            {
                matches = [npc];
                break;
            }

            if (npc.FullName.StartsWith(npcStr, StringComparison.InvariantCultureIgnoreCase))
            {
                matches.Add(npc);
            }
        }

        if (matches.Count > 1)
        {
            args.Player.SendMultipleMatchError(matches.Select(n => $"{Lang.GetNPCNameValue(n.netID)}({n.whoAmI})"));
            return;
        }

        if (matches.Count == 0)
        {
            args.Player.SendErrorMessage("[i:267]没有找到指定的NPC.");
            return;
        }

        var target = matches[0];
        args.Player.Teleport(target.position.X, target.position.Y);
        args.Player.SendSuccessMessage("[i:267]已传送到'{0}'附近.", target.FullName);
    }
}