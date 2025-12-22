using CaiBotLiteMod.Common.Bot;
using CaiBotLiteMod.Common.Model;
using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.Utils;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Hook;

// ReSharper disable once UnusedType.Global
public class PacketHook : ModSystem
{
    public override void Load()
    {
        On_MessageBuffer.GetData += On_MessageBufferOnGetData;
    }

    public override void Unload()
    {
        On_MessageBuffer.GetData -= On_MessageBufferOnGetData;
    }

    private static void On_MessageBufferOnGetData(On_MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        if (!Main.dedServ)
        {
            orig(self, start, length, out messageType);
            return;
        }

        BinaryReader reader = new (self.readerStream);
        reader.BaseStream.Position = start;
        var player = CaiBotLiteMod.Players[self.whoAmI]!;
        messageType = reader.ReadByte();
        switch (messageType)
        {
            case MessageID.Hello:
                CaiBotLiteMod.Players[self.whoAmI] = new TSPlayer(self.whoAmI);
                break;
            case MessageID.RequestWorldData:
                if (ServerConfig.Instance.EnableWhiteList)
                {
                    orig(self, start, length, out messageType);
                    return;
                }

                break;
            case MessageID.ClientUUID:
                player.UUID = reader.ReadString();
                if (!ServerConfig.Instance.EnableWhiteList || player.IsLoggedIn)
                {
                    orig(self, start, length, out messageType);
                    return;
                }

                if (string.IsNullOrEmpty(player.Name))
                {
                    player.Kick("[Cai白名单]玩家名获取失败!");
                    return;
                }

                if (!WebsocketManager.IsWebsocketConnected)
                {
                    Log.WriteLine("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。\n" +
                                  "如果你不想使用Cai白名单，可以在CaiBotLite.json中将其关闭。", ConsoleColor.Yellow);
                    player.Kick("[CaiBotLite]机器人处于未连接状态, 玩家无法加入。");
                    return;
                }

                new PacketWriter(PackageType.Whitelist, false, null)
                    .Write("player_name", player.Name)
                    .Write("player_ip", player.IP)
                    .Write("player_uuid", player.UUID)
                    .Send();

                break;
            default:
                if (player.State < 10 && messageType > 12 && messageType != 93 && messageType != 16 &&
                    messageType != 42 && messageType != 50 && messageType != 38 && messageType != 68 &&
                    messageType != 147 && messageType < 250)
                {
                    Log.WriteLine($"[CaiBotLite]玩家(Index: {player.Index},State: {player.State})发送无效数据包 (Type: {messageType})",
                        ConsoleColor.Yellow);
                    return;
                }

                break;
        }

        orig(self, start, length, out messageType);
    }
}