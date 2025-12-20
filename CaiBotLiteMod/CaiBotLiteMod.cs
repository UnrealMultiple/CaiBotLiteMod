using CaiBotLiteMod.Common;
using CaiBotLiteMod.Hooks;
using CaiBotLiteMod.Moudles;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod;

// ReSharper disable once ClassNeverInstantiated.Global
public class CaiBotLiteMod : Mod
{
    internal static int InitCode = -1;
    internal static bool DebugMode;

    public static readonly Version PluginVersion = ModLoader.GetMod("CaiBotLiteMod").Version;
    public static readonly TSPlayer?[] Players = new TSPlayer[256];


    public override void Load()
    {
        if (!Main.dedServ)
        {
            return;
        }

        DebugMode = Program.LaunchParameters.ContainsKey("-caidebug");
        WebsocketManager.Init();
        ExecuteCommandHook.Apply();
        
        
        if (string.IsNullOrEmpty(ModContent.GetInstance<ClientConfig>().Token))
        {
            GenCode();
        }
    }

    public override void Unload()
    {
        WebsocketManager.StopWebsocket();
        WebsocketManager.WebSocket.Dispose();
        ExecuteCommandHook.Dispose();
    }


    public static void GenCode()
    {
        if (!string.IsNullOrEmpty(ModContent.GetInstance<ClientConfig>().Token))
        {
            Utils.WriteLine("[CaiBotLite]你已经绑定过了!", ConsoleColor.Red);
            return;
        }
        
        InitCode = Main.rand.Next(10000000, 99999999);
        Utils.Write("[CaiBotLite]您的服务器绑定码为: ", ConsoleColor.Green);
        Utils.WriteLine(InitCode.ToString(), ConsoleColor.Red);
        Utils.WriteLine("*你可以在启动服务器后使用'/生成绑定码'重新生成", ConsoleColor.Magenta);
    }
}