using CaiBotLiteMod.Enums;
using System;
using System.Linq;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

internal static class Login
{
    internal static bool CheckWhitelist(string name, WhiteListResult result)
    {
        var player = CaiBotLiteMod.Players.FirstOrDefault(x => x?.Name == name);
        
        if (player == null)
        {
            return false;
        }

        var groupId = ModContent.GetInstance<ServerConfig>().GroupNumber.ToString();
        groupId = groupId == "0" ? "" : groupId;

        try
        {
            switch (result)
            {
                case WhiteListResult.Accept:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})已通过白名单验证...");
                    player.IsLoggedIn = true;
                    break;
                }
                case WhiteListResult.NotInWhitelist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})没有添加白名单...");
                    player.Disconnect($"[Cai白名单]没有添加白名单!\n" +
                                      $"请在群{groupId}内发送\"/添加白名单 角色名字\"");
                    return false;
                }
                case WhiteListResult.InGroupBlacklist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于群黑名单中...");
                    player.Disconnect("[Cai白名单]你已被服务器屏蔽\n" +
                                      "你处于本群黑名单中!");
                    return false;
                }
                case WhiteListResult.InBotBlacklist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于全局黑名单中...");
                    player.Disconnect("[Cai白名单]你已被Bot屏蔽\n" +
                                      "你处于全局黑名单中!");
                    return false;
                }
                case WhiteListResult.NeedLogin:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})使用未授权的设备...");
                    player.Disconnect($"[Cai白名单]未授权设备!\n" +
                                      $"在群{groupId}内发送'/登录'\n" +
                                      $"以批准此设备登录");

                    return false;
                }
                default:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})无效登录结果[{result}], 可能是适配插件版本过低...");
                    player.Disconnect($"[Cai白名单]登录出错!" +
                                      $"无法处理登录结果: {result}");

                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})验证白名单时出现错误...\n" +
                              $"{ex}");

            player.Disconnect($"[Cai白名单]服务器发生错误无法处理该请求!\n" +
                              $"请尝试重新加入游戏或者联系服务器群{groupId}管理员");
            return false;
        }

        return true;
    }
}