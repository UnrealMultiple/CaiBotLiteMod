using CaiBotLiteMod.Common.Bot;
using CaiBotLiteMod.Common.Model;
using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.Utils;
using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace CaiBotLiteMod.Common.System;

// ReSharper disable once UnusedType.Global
public class PacketSystem : Terraria.ModLoader.ModSystem
{
    // ReSharper disable once InconsistentNaming
    public override bool HijackGetData(ref byte messageType, ref BinaryReader Reader, int playerNumber)
    {
        if (!Main.dedServ)
        {
            return false;
        }

        BinaryReader reader = new (Reader.BaseStream);
        var player = CaiBotLiteMod.Players[playerNumber]!;


        switch (messageType)
        {
            case MessageID.Hello:
                CaiBotLiteMod.Players[playerNumber] = new TSPlayer(playerNumber);
                break;
            case MessageID.RequestWorldData:
                if (ServerConfig.Instance.EnableWhiteList)
                {
                    return true;
                }

                break;
            case MessageID.ClientUUID:
                player.UUID = reader.ReadString();
                if (!ServerConfig.Instance.EnableWhiteList)
                {
                    return false;
                }

                if (player.IsLoggedIn)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(player.Name))
                {
                    player.Kick("[Cai白名单]玩家名获取失败!");
                    return false;
                }

                if (!WebsocketManager.IsWebsocketConnected)
                {
                    Log.WriteLine("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。\n" +
                                  "如果你不想使用Cai白名单，可以在CaiBotLite.json中将其关闭。", ConsoleColor.Yellow);
                    player.Kick("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }

                new PacketWriter(PackageType.Whitelist, false, null)
                    .Write("player_name", player.Name)
                    .Write("player_ip", player.IP)
                    .Write("player_uuid", player.UUID)
                    .Send();

                break;
        }

        return false;
    }
}