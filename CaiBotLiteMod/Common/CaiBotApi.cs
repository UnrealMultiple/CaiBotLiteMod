using CaiBotLiteMod.Hooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

public static class CaiBotApi
{
    public static bool IsWebsocketConnected =>
        CaiBotLiteMod.WebSocket.State == WebSocketState.Open;

    public static async Task HandleMessageAsync(string receivedData)
    {
        var jsonObject = JObject.Parse(receivedData);
        var type = (string) jsonObject["type"]!;

        var group = "";
        var msgId = "";
        if (jsonObject.ContainsKey("group"))
        {
            group = jsonObject["group"]!.ToObject<string>()!;
        }

        if (jsonObject.ContainsKey("msg_id"))
        {
            msgId = jsonObject["msg_id"]!.ToObject<string>()!;
        }

        var packetWriter = new PacketWriter(group, msgId);
        switch (type)
        {
            case "delserver":
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[CaiLite]BOT发送解绑命令...");
                Console.ResetColor();
                Config.Settings.Token = string.Empty;
                Config.Settings.Write();
                Config.Settings.Token = "";
                Config.Settings.Write();
                CaiBotLiteMod.GenCode();
                break;
            case "hello":
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CaiLite]CaiBOT连接成功...");
                Console.ResetColor();

                packetWriter.SetType("hello")
                    .Write("tshock_version", "None")
                    .Write("plugin_version", CaiBotLiteMod.PluginVersion!)
                    .Write("terraria_version", ModLoader.versionedName)
                    .Write("cai_whitelist", Config.Settings.WhiteList)
                    .Write("os", RuntimeInformation.RuntimeIdentifier)
                    .Write("world", Main.worldName)
                    .Send();
                break;
            case "cmd":
                var cmd = (string) jsonObject["cmd"]!;

                var caller = new CaiBotCommandCaller();
                ExecuteCommandHook.StartHook = true;
                ExecuteCommandHook.Clean();
                try
                {
                    Main.ExecuteCommand(cmd[1..], caller);
                }
                catch (Exception e)
                {
                    ExecuteCommandHook.Reply(e.ToString());
                }
                finally
                {
                    ExecuteCommandHook.StartHook = false;
                }


                Console.WriteLine($"[CaiBotLite]`{(string) jsonObject["at"]!}`来自群`{(string) jsonObject["group"]!}`执行了: {(string) jsonObject["cmd"]!}");

                packetWriter.SetType("cmd")
                    .Write("result", string.Join('\n', ExecuteCommandHook.GetCommandOutput()[2..]))
                    .Send();
                break;
            case "online":
                var online = Main.player.Where(i => i is { active: true }).ToArray();
                var onlineResult = new StringBuilder();
                if (online.Length == 0)
                {
                    onlineResult.Append("没有玩家在线捏...");
                }
                else
                {
                    onlineResult.AppendLine($"在线玩家({online.Length}/{Main.maxNetPlayers})");
                    onlineResult.Append(string.Join(',', online.Select(i => i.name)));
                }

                var bigBossList = BossCheckList.GetBossList().Where(x=>x.IsBoss && !x.IsMiniboss).OrderByDescending(x=>x.Progression).ToList();
                var onlineProcess = "不可用";
                if (bigBossList.Count != 0)
                {
                    if (bigBossList[0].Downed())
                    {
                        onlineProcess = "已毕业";
                    
                    }
                    else if (!bigBossList[^1].Downed())
                    {
                        onlineProcess = bigBossList[^1].DisplayName + "前";
                    }
                    else
                    {
                        for (var i = 0; i < bigBossList.Count; i++)
                        {
                            if (bigBossList[i].Downed())
                            {
                                onlineProcess = bigBossList[i-1].DisplayName + "前";
                                break;
                            }
                        }
                    }
                    
                }
                packetWriter.SetType("online")
                    .Write("result", onlineResult.ToString()) // “怎么有种我是男的的感觉” -- 张芷睿大人 (24.12.22)
                    .Write("worldname", string.IsNullOrEmpty(Main.worldName) ? "地图还没加载捏~" : Main.worldName)
                    .Write("process", onlineProcess)
                    .Send();
                break;

            case "process":
                var bossList = BossCheckList.GetBossList().Where(x => x.IsBoss && !x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                var eventList = BossCheckList.GetBossList().Where(x => x.IsEvent || x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                if (!bossList.Any())
                {
                    packetWriter.SetType("process_text")
                        .Write("process", "⚠️需要安装BossChecklist模组才能使用进度查询!")
                        .Send();
                    break;
                }

                StringBuilder processResult = new ();
                processResult.AppendLine("🖼️肉前:" + string.Join(',', bossList.Where(x => x.Progression <= 7).Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                processResult.AppendLine("🔥肉后:" + string.Join(',', bossList.Where(x => x.Progression > 7).Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                processResult.Append("🚩事件:" + string.Join(',', eventList.Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                packetWriter.SetType("process_text")
                    .Write("process", processResult.ToString())
                    .Send();
                break;
            case "whitelist":
                var name = (string) jsonObject["name"]!;
                var code = (int) jsonObject["code"]!;
                Login.CheckWhite(name, code);
                break;
            case "selfkick":
                name = (string) jsonObject["name"]!;
                var playerList2 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList2.Count == 0)
                {
                    return;
                }

                playerList2[0].Kick("在群中使用自踢命令.");

                break;
            case "mappng":
                var bitmap = MapGenerator.CreateMapImg();
                using (MemoryStream ms = new ())
                {
                    await bitmap.SaveAsync(ms, new PngEncoder());
                    var imageBytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(imageBytes);
                    packetWriter.SetType("mappngV2")
                        .Write("result", Utils.CompressBase64(base64))
                        .Send();
                }
                break;
            case "mapfile":
                var mapfile = MapGenerator.CreateMapFile();
                packetWriter.SetType("mapfileV2")
                    .Write("name", mapfile.Item2)
                    .Write("base64", Utils.CompressBase64(mapfile.Item1) )
                    .Send();
                    
                break;
            case "worldfile":
            {
                var zipName = Main.worldName + ".zip";
                var modWorldPath = Path.ChangeExtension(Main.worldPathName, ".twld");
                await using (var zipToOpen = new FileStream(zipName, FileMode.Create))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(Main.worldPathName, Main.worldName + ".wld");

                        if (File.Exists(modWorldPath))
                        {
                            archive.CreateEntryFromFile(modWorldPath, Main.worldName + ".twld");
                        }
                    }
                }

                packetWriter.SetType("worldfileV2")
                    .Write("name", zipName)
                    .Write("base64", Utils.CompressBase64(Utils.FileToBase64String(zipName)))
                    .Send();

                break;
                
            }
            case "lookbag":
                name = (string) jsonObject["name"]!;
                var playerList3 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList3.Count != 0)
                {
                    var plr = playerList3[0].TPlayer;
                    ;
                    packetWriter.SetType("lookbag_text")
                        .Write("name", name)
                        .Write("exist", 1)
                        .Write("inventory", LookBag.LookOnline(plr))
                        .Send();
                }
                else
                {
                    packetWriter.SetType("lookbag")
                        .Write("name", name)
                        .Write("exist", 0)
                        .Send();
                }

                break;
            case "pluginlist":
                var mods = ModLoader.Mods.Skip(1);
                var modList = mods.Select(p => new ModInfo(p.DisplayName, p.Version)).ToList();
                packetWriter.SetType("modlist")
                    .Write("mods", modList)
                    .Send();
                break;
        }

        return;
    }
}