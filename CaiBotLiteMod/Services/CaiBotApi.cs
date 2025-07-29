using CaiBotLiteMod.Enums;
using CaiBotLiteMod.Hooks;
using CaiBotLiteMod.Moudles;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Services;

public static class CaiBotApi
{
    public static async Task HandleMessageAsync(string receivedData)
    {
        try
        {
            var package = Package.Parse(receivedData);

            var packetWriter = new PackageWriter(package.Type, package.IsRequest, package.RequestId);
            switch (package.Type)
            {
                case PackageType.UnbindServer:
                    var reason = package.Read<string>("reason");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[CaiLite]BOT发送解绑命令...");
                    Console.WriteLine($"[CaiBotLite]原因: {reason}");
                    Console.ResetColor();
                    Config.Settings.Token = string.Empty;
                    Config.Settings.Write();
                    Config.Settings.Token = "";
                    Config.Settings.Write();
                    CaiBotLiteMod.GenCode();
                    break;
                case PackageType.CallCommand:
                    var command = package.Read<string>("command");
                    var userOpenId = package.Read<string>("user_open_id");
                    var groupOpenId = package.Read<string>("group_open_id"); 

                    var caller = new CaiBotCommandCaller();
                    ExecuteCommandHook.StartHook = true;
                    ExecuteCommandHook.Clean();
                    try
                    {
                        Main.ExecuteCommand(command[1..], caller);
                    }
                    catch (Exception e)
                    {
                        ExecuteCommandHook.Reply(e.ToString());
                    }
                    finally
                    {
                        ExecuteCommandHook.StartHook = false;
                    }


                    Console.WriteLine($"[CaiBotLite] \"{userOpenId}\"来自群\"{groupOpenId}\"执行了: {command}");
                    packetWriter
                        .Write("output", ExecuteCommandHook.GetCommandOutput())
                        .Send();
                    break;
                
                case PackageType.PlayerList:
                    var bigBossList = BossCheckList.GetBossList().Where(x => x is { IsBoss: true, IsMiniboss: false }).OrderByDescending(x => x.Progression).ToList();
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
                                    onlineProcess = bigBossList[i - 1].DisplayName + "前";
                                    break;
                                }
                            }
                        }

                    }
                    packetWriter
                        .Write("server_name", string.IsNullOrEmpty(Main.worldName) ? "地图还没加载捏~" : Main.worldName)
                        .Write("player_list",Main.player.Where(x => x is { active: true }).Select(x => x.name))
                        .Write("current_online", Main.player.Count(x => x is { active: true }))
                        .Write("max_online", Main.maxNetPlayers)
                        .Write("process",Config.Settings.ShowProcessInPlayerList?onlineProcess:"")
                        .Send();
                    break;

                case PackageType.Progress:
                    var bossList = BossCheckList.GetBossList().Where(x => x.IsBoss && !x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                    var eventList = BossCheckList.GetBossList().Where(x => x.IsEvent || x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                    if (bossList.Count == 0)
                    {
                        packetWriter
                            .Write("is_text", true)
                            .Write("text", "⚠️需要安装BossChecklist模组才能使用进度查询!")
                            .Send();
                        break;
                    }

                    StringBuilder processResult = new ();
                    processResult.AppendLine("🌙肉前:" + string.Join(',', bossList.Where(x => x.Progression <= 7).Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                    processResult.AppendLine("🔥肉后:" + string.Join(',', bossList.Where(x => x.Progression > 7).Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                    processResult.Append("🚩事件:" + string.Join(',', eventList.Select(x => $"{(x.Downed() ? "\u2714" : "\u2796")}{x.DisplayName}")));
                    packetWriter
                        .Write("is_text", true)
                        .Write("text", processResult.ToString())
                        .Send();
                    break;
                case PackageType.Whitelist:
                    var name = package.Read<string>("player_name");
                    var whitelistResult = package.Read<WhiteListResult>("whitelist_result");
                    Login.CheckWhitelist(name, whitelistResult);
                    break;
                
                case PackageType.SelfKick:
                    var selfKickName = package.Read<string>("name");
                    var kickPlr = CaiBotLiteMod.Players.FirstOrDefault(x => x?.Name == selfKickName);
                    if (kickPlr == null)
                    {
                        return;
                    }

                    kickPlr.Kick("使用BOT自踢命令");
                    break;
                case PackageType.MapImage:
                    var bitmap = MapGenerator.CreateMapImg();
                    using (MemoryStream ms = new ())
                    {
                        await bitmap.SaveAsync(ms, new PngEncoder());
                        var imageBytes = ms.ToArray();
                        var base64 = Convert.ToBase64String(imageBytes);
                        packetWriter
                            .Write("result",Utils.CompressBase64(base64))
                            .Send();
                    }

                    break;
                case PackageType.MapFile:
                    var mapFile = MapGenerator.CreateMapFile();
                    packetWriter
                        .Write("name", mapFile.Item2)
                        .Write("base64", Utils.CompressBase64(mapFile.Item1))
                        .Send();

                    break;
                case PackageType.WorldFile:
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

                    packetWriter
                        .Write("name", zipName)
                        .Write("base64", Utils.CompressBase64(Utils.FileToBase64String(zipName)))
                        .Send();

                    break;

                }
                case PackageType.LookBag:
                    var lookBagName = package.Read<string>("player_name");
                    var playerList3 = TSPlayer.FindByNameOrID("tsn:" + lookBagName);
                    
                    packetWriter
                        .Write("is_text", true)
                        .Write("name", lookBagName);
                    
                    if (playerList3.Count != 0)
                    {
                        var plr = playerList3[0].TPlayer;
                        packetWriter
                            .Write("exist", true)
                            .Write("text", LookBag.LookOnline(plr))
                            .Send();
                    }
                    else
                    {
                        packetWriter
                            .Write("exist", false)
                            .Send();
                    }

                    break;
                case PackageType.RankData:
                    break;
                case PackageType.PluginList:
                    var mods = ModLoader.Mods.Skip(1);
                    var modList = mods.Select(p => new ModInfo(p.DisplayName, p.Version)).ToList();
                    packetWriter
                        .Write("is_mod", true)
                        .Write("plugins", modList)
                        .Send();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CaiBotLite] 处理BOT数据包时出错:\n" +
                                    $"{ex}\n" +
                                    $"源数据包: {receivedData}");
        }
        
    }
}