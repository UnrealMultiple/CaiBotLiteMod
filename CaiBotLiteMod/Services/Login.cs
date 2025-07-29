﻿using CaiBotLiteMod.Enums;
using CaiBotLiteMod.Moudles;
using System;
using System.Linq;

namespace CaiBotLiteMod.Services;

internal static class Login
{
    internal static bool CheckWhitelist(string name, WhiteListResult result)
    {
        var player = CaiBotLiteMod.Players.FirstOrDefault(x => x?.Name == name);

        var groupId = Config.Settings.GroupNumber.ToString();

        if (Config.Settings.GroupNumber == 0)
        {
            groupId = "";
        }

        if (player == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(name))
        {
            Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})版本可能过低...");
            player.Disconnect("你的游戏版本可能过低,\n" +
                              "请使用Terraria1.4.4+游玩");
            return false;
        }

        try
        {
            switch (result)
            {
                case WhiteListResult.Accept:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})已通过白名单验证...");
                    break;
                }
                case WhiteListResult.NotInWhitelist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})没有添加白名单...");
                    player.SilentKickInProgress = true;
                    player.Disconnect($"[Cai白名单]没有添加白名单!\n" +
                                      $"请在群{groupId}内发送\"/添加白名单 角色名字\"");
                    return false;
                }
                case WhiteListResult.InGroupBlacklist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于群黑名单中...");
                    player.SilentKickInProgress = true;
                    player.Disconnect("[Cai白名单]你已被服务器屏蔽\n" +
                                      "你处于本群黑名单中!");
                    return false;
                }
                case WhiteListResult.InBotBlacklist:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})被屏蔽，处于全局黑名单中...");
                    player.SilentKickInProgress = true;
                    player.Disconnect("[Cai白名单]你已被Bot屏蔽\n" +
                                      "你处于全局黑名单中!");
                    return false;
                }
                case WhiteListResult.NeedLogin:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})使用未授权的设备...");
                    player.SilentKickInProgress = true;
                    player.Disconnect($"[Cai白名单]未授权设备!\n" +
                                      $"在群{groupId}内发送'/登录'\n" +
                                      $"以批准此设备登录");

                    return false;
                }
                default:
                {
                    Console.WriteLine($"[Cai白名单]玩家[{name}](IP: {player.IP})无效登录结果[{result}], 可能是适配插件版本过低...");
                    player.SilentKickInProgress = true;
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
            player.SilentKickInProgress = true;
            player.Disconnect($"[Cai白名单]服务器发生错误无法处理该请求!\n" +
                              $"请尝试重新加入游戏或者联系服务器群{groupId}管理员");
            return false;
        }

        return true;
    }
}
