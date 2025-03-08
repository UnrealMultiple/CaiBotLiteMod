using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

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
            case 1:
                CaiBotLiteMod.Players[playerNumber] = new TSPlayer(playerNumber);
                break;
            case 6:
                if (Config.Settings.WhiteList && !player.SscLogin)
                {
                    return true;
                }
                break;
            case 4:

                if (!Config.Settings.WhiteList || !player.SscLogin || player.IsLoggedIn)
                {
                    return false;
                }

                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();

                var name = reader.ReadString().Trim();
                if (!CaiBotApi.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                    player.Kick("[CaiBot]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }
                PacketWriter packetWriter = new();
                packetWriter.SetType("whitelistV2")
                    .Write("name", name)
                    .Write("uuid", player.UUID)
                    .Write("ip", player.IP)
                    .Send();

                break;

            case 68:
                player.UUID = reader.ReadString();

                if (!Config.Settings.WhiteList)
                {
                    return false;
                }

                if (ModLoader.Mods.Any(x => x.DisplayName == "SSC - 云存档") && player.Name.Length == 17 && long.TryParse(player.Name, out _))
                {
                    Netplay.Clients[player.Index].State = 2;
                    NetMessage.SendData((int) PacketTypes.WorldInfo, player.Index);
                    Main.SyncAnInvasion(player.Index);
                    player.SscLogin = true;
                    player.SendWarningMessage("[CaiBot]服务器已开启白名单,请使用已绑定的人物名字！");
                    return false;
                }

                if (string.IsNullOrEmpty(player.Name))
                {
                    player.Kick("[Cai白名单]玩家名获取失败!");
                    return false;
                }

                RestObject re = new () { { "type", "whitelistV2" }, { "name", player.Name }, { "uuid", player.UUID }, { "ip", player.IP } };
                if (!CaiBotApi.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                    player.Kick("[CaiBot]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }

                PacketWriter packetWriter2 = new();
                packetWriter2.SetType("whitelistV2")
                    .Write("name", player.Name)
                    .Write("uuid", player.UUID)
                    .Write("ip", player.IP)
                    .Send();

                break;
        }

        return false;
    }
}