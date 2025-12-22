using CaiBotLiteMod.Common.Bot;
using CaiBotLiteMod.Common.Model;
using CaiBotLiteMod.Common.Model.Enum;
using CaiBotLiteMod.Common.Utils;
using MonoMod.Cil;
using System;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Hook;

// ReSharper disable once UnusedType.Global
public class PacketHook : ModSystem
{
    public override void Load()
    {
        if (!Main.dedServ)
        {
            return;
        }
        On_MessageBuffer.GetData += On_MessageBufferOnGetData;
        IL_MessageBuffer.GetData += IL_MessageBufferOnGetData;
    }
    
    public override void Unload()
    {
        if (!Main.dedServ)
        {
            return;
        }
        On_MessageBuffer.GetData -= On_MessageBufferOnGetData;
        IL_MessageBuffer.GetData -= IL_MessageBufferOnGetData;
    }

    private static void IL_MessageBufferOnGetData(ILContext il)
    {
        var cursor = new ILCursor(il);
        // ReSharper disable once InvertIf
        if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out _),
                x => x.MatchLdsfld(out _),
                x => x.MatchLdcI4(2),
                x => x.MatchLdelemRef(),
                x => x.MatchCallvirt(out _),
                x => x.MatchCall(out _)))
        {
            for (var i = 0; i < 7; i++)
            {
                cursor.Remove();
            }
        }
    }

    

    private static void On_MessageBufferOnGetData(On_MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
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
                    return;
                }

                break;
            case MessageID.SyncPlayer:
                if (string.IsNullOrEmpty(player.Name))
                {
                    break;
                }

                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();
                
                var name = reader.ReadString();
                if (player.Name != name)
                {
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
        }
        orig(self, start, length, out messageType);
    }
}