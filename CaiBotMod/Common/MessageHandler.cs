using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotMod.Common;

public static class MessageHandle
{
    public static bool IsWebsocketConnected =>
        CaiBotMod.WebSocket.State == WebSocketState.Open;

    public static async Task SendDateAsync(string message)
    {
        if (Program.LaunchParameters.ContainsKey("-caidebug"))
        {
            Console.WriteLine($"[CaiAPI]发送BOT数据包：{message}");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);
        await CaiBotMod.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
            CancellationToken.None);
    }

    public static async Task HandleMessageAsync(string receivedData)
    {
        var jsonObject = JObject.Parse(receivedData);
        var type = (string) jsonObject["type"]!;
        RestObject result;
        switch (type)
        {
            case "delserver":
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[CaiAPI]BOT发送解绑命令...");
                Console.ResetColor();
                Config.config.Token = "";
                Config.config.Write();
                CaiBotMod.GenCode();
                break;
            case "hello":
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CaiAPI]CaiBOT连接成功...");
                Console.ResetColor();
                //发送服务器信息
                result = new RestObject
                {
                    { "type", "hello" },
                    { "tshock_version", "TModLoader" },
                    { "plugin_version", CaiBotMod.PluginVersion! },
                    { "terraria_version", ModLoader.versionedName },
                    { "cai_whitelist", Config.config.WhiteList },
                    { "os", RuntimeInformation.RuntimeIdentifier },
                    { "world", Main.worldName },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "groupid":
                var groupId = (long) jsonObject["groupid"]!;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[CaiAPI]群号获取成功: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(groupId);
                Console.ResetColor();
                if (Config.config.GroupNumber != 0L)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[CaiAPI]检测到你在配置文件中已设置群号: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(Config.config.GroupNumber);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(",BOT自动获取的群号将被忽略！");
                    Console.ResetColor();
                }
                else
                {
                    Config.config.GroupNumber = groupId;
                }

                break;
            case "cmd":
                var cmd = (string) jsonObject["cmd"]!;
                CaiBotPlayer tr = new ();
                Commands.HandleCommand(tr, cmd);
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[CaiBot] `{(string) jsonObject["at"]!}`来自群`{(long) jsonObject["group"]!}`执行了: {(string) jsonObject["cmd"]!}");
                Console.ResetColor();
                RestObject dictionary = new () { { "type", "cmd" }, { "result", string.Join('\n', tr.GetCommandOutput()) }, { "at", (string) jsonObject["at"]! }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(dictionary.ToJson());
                break;
            case "online":
                var onlineResult = "";
                List<string> players = [];
                if (!Main.player.Any(p => p is { active: true }))
                {
                    onlineResult = "当前没有玩家在线捏";
                }
                else
                {
                    onlineResult +=
                        $"在线的玩家({Main.player.Count(p => null != p && p.active)}/{Main.maxNetPlayers})\n";

                    for (var k = 0; k < 255; k++)
                    {
                        if (Main.player[k] != null && Main.player[k].active)
                        {
                            players.Add(Main.player[k].name);
                        }
                    }

                    onlineResult += string.Join(',', players).TrimEnd(',');
                }
                var bigBossList = BossCheckList.GetBossList().Where(x=>x.IsBoss && !x.IsMiniboss).OrderByDescending(x=>x.Progression).ToList();
                var onlineProcess = "不可用";
                if (bigBossList.Any())
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
                

                result = new RestObject
                {
                    { "type", "online" },
                    { "result", onlineResult },
                    { "worldname", Main.worldName },
                    { "process", onlineProcess },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "process":
                var bossList = BossCheckList.GetBossList().Where(x => x.IsBoss && !x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                var eventList = BossCheckList.GetBossList().Where(x => x.IsEvent || x.IsMiniboss).OrderBy(x => x.Progression).ToList();
                if (!bossList.Any())
                {
                    result = new RestObject
                    {
                        { "type", "process_text" },
                        { "process", "需要安装BossChecklist模组才能使用进度查询!" },
                        { "group", (long) jsonObject["group"]! }
                    };
                    await SendDateAsync(JsonConvert.SerializeObject(result));
                    break;
                }
                StringBuilder processResult = new ();
                processResult.AppendLine("🖼️肉前:"+string.Join(',',bossList.Where(x => x.Progression<=7).Select(x=>$"{(x.Downed()?"\u2714":"\u2796")}{x.DisplayName}")));
                processResult.AppendLine("🔥肉后:"+string.Join(',',bossList.Where(x => x.Progression>7).Select(x=>$"{(x.Downed()?"\u2714":"\u2796")}{x.DisplayName}")));
                processResult.AppendLine("🚩事件:"+string.Join(',',eventList.Select(x=>$"{(x.Downed()?"\u2714":"\u2796")}{x.DisplayName}")));
                result = new RestObject
                {
                    { "type", "process_text" },
                    { "process", processResult.ToString() },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
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
                break; //无法实现, 太Cai了5555
            case "lookbag":
                name = (string) jsonObject["name"]!;
                var playerList3 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList3.Count != 0)
                {
                    var plr = playerList3[0].TPlayer;
                    var msgs = new List<string>();
                    // 在线
                    msgs.Add($"玩家: {plr.name}");
                    msgs.Add($"生命: {plr.statLife}/{plr.statLifeMax}");
                    msgs.Add($"魔力: {plr.statMana}/{plr.statManaMax}");
                    msgs.Add($"渔夫任务: {plr.anglerQuestsFinished} 次");

                    List<string> enhance = [];
                    if (plr.extraAccessory)
                    {
                        enhance.Add(Utils.GetItemDesc(3335)); // 3335 恶魔之心
                    }

                    if (plr.unlockedBiomeTorches)
                    {
                        enhance.Add(Utils.GetItemDesc(5043)); // 5043 火把神徽章
                    }

                    if (plr.ateArtisanBread)
                    {
                        enhance.Add(Utils.GetItemDesc(5326)); // 5326	工匠面包
                    }

                    if (plr.usedAegisCrystal)
                    {
                        enhance.Add(Utils.GetItemDesc(5337)); // 5337 生命水晶	永久强化生命再生 
                    }

                    if (plr.usedAegisFruit)
                    {
                        enhance.Add(Utils.GetItemDesc(5338)); // 5338 埃癸斯果	永久提高防御力 
                    }

                    if (plr.usedArcaneCrystal)
                    {
                        enhance.Add(Utils.GetItemDesc(5339)); // 5339 奥术水晶	永久提高魔力再生 
                    }

                    if (plr.usedGalaxyPearl)
                    {
                        enhance.Add(Utils.GetItemDesc(5340)); // 5340	银河珍珠	永久增加运气 
                    }

                    if (plr.usedGummyWorm)
                    {
                        enhance.Add(Utils.GetItemDesc(5341)); // 5341	黏性蠕虫	永久提高钓鱼技能  
                    }

                    if (plr.usedAmbrosia)
                    {
                        enhance.Add(Utils.GetItemDesc(5342)); // 5342	珍馐	 永久提高采矿和建造速度 
                    }

                    if (plr.unlockedSuperCart)
                    {
                        enhance.Add(Utils.GetItemDesc(5289)); // 5289	矿车升级包
                    }

                    if (enhance.Count != 0)
                    {
                        msgs.Add("永久增强: " + string.Join(",", enhance));
                    }
                    else
                    {
                        msgs.Add("永久增强: " + "啥都没有...");
                    }

                    List<string> inventory = [];
                    List<string> assist = [];
                    List<string> armor = [];
                    List<string> vanity = [];
                    List<string> dye = [];
                    List<string> miscEquips = [];
                    List<string> miscDyes = [];
                    List<string> bank = [];
                    List<string> bank2 = [];
                    List<string> bank3 = [];
                    List<string> bank4 = [];
                    List<string> armor1 = [];
                    List<string> armor2 = [];
                    List<string> armor3 = [];
                    List<string> vanity1 = [];
                    List<string> vanity2 = [];
                    List<string> vanity3 = [];
                    List<string> dye1 = [];
                    List<string> dye2 = [];
                    List<string> dye3 = [];

                    string s;
                    for (var i = 0; i < 59; i++)
                    {
                        s = Utils.GetItemDesc(plr.inventory[i].Clone());
                        if (i < 50)
                        {
                            if (s != "")
                            {
                                inventory.Add(s);
                            }
                        }
                        else if (i >= 50 && i < 59)
                        {
                            if (s != "")
                            {
                                assist.Add(s);
                            }
                        }
                    }

                    for (var i = 0; i < plr.armor.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.armor[i]);
                        if (i < 10)
                        {
                            if (s != "")
                            {
                                armor.Add(s);
                            }
                        }
                        else
                        {
                            if (s != "")
                            {
                                vanity.Add(s);
                            }
                        }
                    }

                    for (var i = 0; i < plr.dye.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.dye[i]);
                        if (s != "")
                        {
                            dye.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.miscEquips.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.miscEquips[i]);
                        if (s != "")
                        {
                            miscEquips.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.miscDyes.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.miscDyes[i]);
                        if (s != "")
                        {
                            miscDyes.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank.item.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.bank.item[i]);
                        if (s != "")
                        {
                            bank.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank2.item.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.bank2.item[i]);
                        if (s != "")
                        {
                            bank2.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank3.item.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.bank3.item[i]);
                        if (s != "")
                        {
                            bank3.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank4.item.Length; i++)
                    {
                        s = Utils.GetItemDesc(plr.bank4.item[i]);
                        if (s != "")
                        {
                            bank4.Add(s);
                        }
                    }

                    // 装备（loadout）
                    for (var i = 0; i < plr.Loadouts.Length; i++)
                    {
                        Item[] items = plr.Loadouts[i].Armor;
                        // 装备 和 时装
                        for (var j = 0; j < items.Length; j++)
                        {
                            s = Utils.GetItemDesc(items[j]);
                            if (!string.IsNullOrEmpty(s))
                            {
                                if (i == 0)
                                {
                                    if (j < 10)
                                    {
                                        armor1.Add(s);
                                    }
                                    else
                                    {
                                        vanity1.Add(s);
                                    }
                                }
                                else if (i == 1)
                                {
                                    if (j < 10)
                                    {
                                        armor2.Add(s);
                                    }
                                    else
                                    {
                                        vanity2.Add(s);
                                    }
                                }
                                else if (i == 2)
                                {
                                    if (j < 10)
                                    {
                                        armor3.Add(s);
                                    }
                                    else
                                    {
                                        vanity3.Add(s);
                                    }
                                }
                            }
                        }

                        // 染料
                        items = plr.Loadouts[i].Dye;
                        for (var j = 0; j < items.Length; j++)
                        {
                            s = Utils.GetItemDesc(items[j]);
                            if (!string.IsNullOrEmpty(s))
                            {
                                if (i == 0)
                                {
                                    dye1.Add(s);
                                }
                                else if (i == 1)
                                {
                                    dye2.Add(s);
                                }
                                else if (i == 2)
                                {
                                    dye3.Add(s);
                                }
                            }
                        }
                    }

                    List<string> trash = [];
                    s = Utils.GetItemDesc(plr.trashItem);
                    if (s != "")
                    {
                        trash.Add(s);
                    }

                    if (inventory.Count != 0)
                    {
                        msgs.Add("背包：" + string.Join(",", inventory));
                    }
                    else
                    {
                        msgs.Add("背包：啥都没有...");
                    }

                    if (trash.Count != 0)
                    {
                        msgs.Add("垃圾桶：" + string.Join(",", trash));
                    }
                    else
                    {
                        msgs.Add("垃圾桶：啥都没有...");
                    }

                    if (assist.Count != 0)
                    {
                        msgs.Add("钱币弹药：" + string.Join(",", assist));
                    }


                    var num = plr.CurrentLoadoutIndex + 1;

                    if (armor.Count != 0)
                    {
                        msgs.Add($">装备{num}：" + string.Join(",", armor));
                    }


                    if (vanity.Count != 0)
                    {
                        msgs.Add($">时装{num}：" + string.Join(",", vanity));
                    }


                    if (dye.Count != 0)
                    {
                        msgs.Add($">染料{num}：" + string.Join(",", dye));
                    }


                    if (armor1.Count != 0)
                    {
                        msgs.Add("装备1：" + string.Join(",", armor1));
                    }


                    if (vanity1.Count != 0)
                    {
                        msgs.Add("时装1：" + string.Join(",", vanity1));
                    }


                    if (dye1.Count != 0)
                    {
                        msgs.Add("染料1：" + string.Join(",", dye1));
                    }


                    if (armor2.Count != 0)
                    {
                        msgs.Add("装备2：" + string.Join(",", armor2));
                    }


                    if (vanity2.Count != 0)
                    {
                        msgs.Add("时装2：" + string.Join(",", vanity2));
                    }


                    if (dye2.Count != 0)
                    {
                        msgs.Add("染料2：" + string.Join(",", dye2));
                    }


                    if (armor3.Count != 0)
                    {
                        msgs.Add("装备3：" + string.Join(",", armor3));
                    }


                    if (vanity3.Count != 0)
                    {
                        msgs.Add("时装3：" + string.Join(",", vanity3));
                    }


                    if (dye3.Count != 0)
                    {
                        msgs.Add("染料3：" + string.Join(",", dye3));
                    }


                    if (miscEquips.Count != 0)
                    {
                        msgs.Add("工具栏：" + string.Join(",", miscEquips));
                    }


                    if (miscDyes.Count != 0)
                    {
                        msgs.Add("染料2：" + string.Join(",", miscDyes));
                    }


                    if (bank.Count != 0)
                    {
                        msgs.Add("储蓄罐：" + string.Join(",", bank));
                    }


                    if (bank2.Count != 0)
                    {
                        msgs.Add("保险箱：" + string.Join(",", bank2));
                    }


                    if (bank3.Count != 0)
                    {
                        msgs.Add("护卫熔炉：" + string.Join(",", bank3));
                    }


                    if (bank4.Count != 0)
                    {
                        msgs.Add("虚空保险箱：" + string.Join(",", bank4));
                    }

                    result = new RestObject
                    {
                        { "type", "lookbag_text" },
                        { "name", name },
                        { "exist", 1 },
                        { "inventory", string.Join("\n", msgs) },
                        //{ "inventory", string.Join("暂时无法使用") },
                        { "group", (long) jsonObject["group"]! }
                    };
                    //Console.WriteLine(string.Join("\n", msgs));
                }
                else
                {
                    result = new RestObject { { "type", "lookbag" }, { "exist", 0 }, { "group", (long) jsonObject["group"]! } };
                }

                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "pluginlist":
                var mods = ModLoader.Mods.Skip(1);
                var modList = mods.Select(p => new ModInfo(p.DisplayName, p.Version)).ToList();
                result = new RestObject { { "type", "modlist" }, { "mods", modList }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
        }
    }
}