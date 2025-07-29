using CaiBotLiteMod.Hooks;
using CaiBotLiteMod.Moudles;
using CaiBotLiteMod.Services;
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
        Config.Settings.Read();
        Config.Settings.Write();
        WebsocketManager.Init();
        ExecuteCommandHook.Apply();
        if (Config.Settings.Token == "")
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
}