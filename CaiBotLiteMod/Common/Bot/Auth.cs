using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.SSC;
using CaiBotLiteMod.Common.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Bot;

internal static class Auth
{
    internal static async Task Login(string name, WhiteListResult result, bool isAdmin)
    {
        try
        {
            var player = CaiBotLiteMod.Players.FirstOrDefault(x => x?.Name == name);

            if (player == null)
            {
                return;
            }

            var groupId = ServerConfig.Instance.GroupNumber.ToString();
            groupId = groupId == "0" ? "" : groupId;

            switch (result)
            {
                case WhiteListResult.Accept:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})已通过白名单验证...", ConsoleColor.Green);

                    player.IsAdmin = isAdmin;
                    player.IsLoggedIn = true;
                    
                    if (ServerConfig.Instance.EnableSSC)
                    {
                        if (SSCManager.ExistSSC(name))
                        {
                            var dataTuple = await SSCManager.LoadSSC(name);
                            await SSCFragmenter.SendAsync(dataTuple.Item1, dataTuple.Item2, player.Index);
                        }
                        else
                        {
                            var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
                            mp.Write((byte) CaiBotLiteMod.MessageType.ResetSSC);
                            mp.Send(player.Index);
                        }
                    }

                    player.State = 2;
                    NetMessage.SendData(MessageID.WorldData, player.Index);
                    break;
                }
                case WhiteListResult.NotInWhitelist:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})没有添加白名单...", ConsoleColor.Yellow);
                    player.Disconnect($"[Cai白名单]没有添加白名单!\n" +
                                      $"请在群{groupId}内发送\"/添加白名单 角色名字\"");
                    return;
                }
                case WhiteListResult.InGroupBlacklist:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于群黑名单中...", ConsoleColor.Yellow);
                    player.Disconnect("[Cai白名单]你已被服务器屏蔽\n" +
                                      "你处于本群黑名单中!");
                    return;
                }
                case WhiteListResult.InBotBlacklist:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于全局黑名单中...", ConsoleColor.Yellow);
                    player.Disconnect("[Cai白名单]你已被Bot屏蔽\n" +
                                      "你处于全局黑名单中!");
                    return;
                }
                case WhiteListResult.NeedLogin:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})使用未授权的设备...", ConsoleColor.Yellow);
                    player.Disconnect($"[Cai白名单]未授权设备!\n" +
                                      $"在群{groupId}内发送'/登录'\n" +
                                      $"以批准此设备登录");

                    return;
                }
                default:
                {
                    Log.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})无效登录结果[{result}], 可能是适配插件版本过低...", ConsoleColor.Yellow);
                    player.Disconnect($"[Cai白名单]登录出错!" +
                                      $"无法处理登录结果: {result}");

                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine($"[Cai白名单]玩家[{name}]验证白名单时出现错误...\n" +
                          $"{ex}", ConsoleColor.Red);
        }
    }
}