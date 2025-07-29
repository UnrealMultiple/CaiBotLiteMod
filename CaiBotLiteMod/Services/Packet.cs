using CaiBotLiteMod.Enums;
using CaiBotLiteMod.Moudles;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Services;

// ReSharper disable once UnusedType.Global
public class Packet : ModSystem
{
    public override bool HijackGetData(ref byte messageType, ref BinaryReader Reader, int playerNumber)
    {
        if (!Main.dedServ)
        {
            return false;
        }

        BinaryReader reader = new (Reader.BaseStream);
        var player = CaiBotLiteMod.Players[playerNumber];


        switch (messageType)
        {
            case MessageID.Hello:
                CaiBotLiteMod.Players[playerNumber] = new TSPlayer(playerNumber);
                break;
            case MessageID.RequestWorldData:
                if (Config.Settings.WhiteList && !player!.SscLogin)
                {
                    return true;
                }
                break;
            case MessageID.SyncPlayer:

                if (!Config.Settings.WhiteList || !player!.SscLogin || player.IsLoggedIn)
                {
                    return false;
                }

                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();

                var name = reader.ReadString().Trim();
                if (!WebsocketManager.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在CaiBotLite.json中将其关闭。");
                    player.Kick("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }
                new PackageWriter(PackageType.Whitelist, false, null)
                    .Write("player_name", name)
                    .Write("player_ip", player.IP)
                    .Write("player_uuid", player.UUID)
                    .Send();
                break;

            case MessageID.ClientUUID:
                player!.UUID = reader.ReadString();
                if (!Config.Settings.WhiteList)
                {
                    return false;
                }

                if (ModLoader.Mods.Any(x => x.DisplayName == "SSC - 云存档") && player.Name.Length == 17 && long.TryParse(player.Name, out _))
                {
                    Netplay.Clients[player.Index].State = 2;
                    NetMessage.SendData(MessageID.WorldData, player.Index);
                    Main.SyncAnInvasion(player.Index);
                    player.SscLogin = true;
                    player.SendWarningMessage("[CaiBotLite]服务器已开启白名单,请使用已绑定的人物名字！");
                    return false;
                }

                if (string.IsNullOrEmpty(player.Name))
                {
                    player.Kick("[Cai白名单]玩家名获取失败!");
                    return false;
                }
                
                if (!WebsocketManager.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在CaiBotLite.json中将其关闭。");
                    player.Kick("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }
                
                new PackageWriter(PackageType.Whitelist, false, null)
                    .Write("player_name", player.Name)
                    .Write("player_ip", player.IP)
                    .Write("player_uuid", player.UUID)
                    .Send();

                break;
        }

        return false;
    }
}