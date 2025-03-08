using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.NetModules;
using Terraria.ModLoader;
using Terraria.Net;

namespace CaiBotMod.Common;

public class Packet : ModSystem
{
    public override bool HijackGetData(ref byte messageType, ref BinaryReader Reader, int playerNumber)
    {
        if (!Main.dedServ)
        {
            return false;
        }

        BinaryReader reader = new (Reader.BaseStream);
        var player = CaiBotMod.Players[playerNumber];


        switch (messageType)
        {
            case 1:
                CaiBotMod.Players[playerNumber] = new TSPlayer(playerNumber);
                break;
            case 6:
                if (!Config.config.WhiteList)
                {
                    return false;
                }

                break;
            case 4:

                if (!Config.config.WhiteList || !player.SscLogin || player.IsLoggedIn)
                {
                    return false;
                }
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();

                var name = reader.ReadString().Trim();

                RestObject re2 = new () { { "type", "whitelistV2" }, { "name", name }, { "uuid", player.UUID }, { "ip", player.IP } };
                if (!MessageHandle.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                    player.Kick("[CaiBot]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }

                _ = MessageHandle.SendDateAsync(re2.ToJson());

                break;

            case 68:
                player.UUID = reader.ReadString();

                if (!Config.config.WhiteList)
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
                if (!MessageHandle.IsWebsocketConnected)
                {
                    Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                      "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                    player.Kick("[CaiBot]机器人处于未连接状态, 玩家无法加入。");

                    return false;
                }

                _ = MessageHandle.SendDateAsync(re.ToJson());

                break;

            case 82:
                var moduleId = reader.ReadUInt16();
                if (moduleId == NetManager.Instance.GetId<NetTextModule>())
                {
                    var msg = ChatMessage.Deserialize(reader);
                    if (Commands.HandleCommand(player, msg.Text))
                    {
                        return true;
                    }
                }

                break;
            case 118:
            {
                var id = reader.ReadByte();
                var playerDeathReason = PlayerDeathReason.FromReader(new BinaryReader(reader.BaseStream));
                var dmg = reader.ReadInt16();
                var direction = (byte) (reader.ReadByte() - 1);
                BitsByte bits = reader.ReadByte();
                var pvp = bits[0];
                if (CaiBotMod.PlayerDeath.ContainsKey(Main.player[playerNumber].name))
                {
                    CaiBotMod.PlayerDeath[Main.player[playerNumber].name] = new Point(
                        (int) Main.player[playerNumber].position.X, (int) Main.player[playerNumber].position.Y);
                }
                else
                {
                    CaiBotMod.PlayerDeath.Add(Main.player[playerNumber].name,
                        new Point((int) Main.player[playerNumber].position.X,
                            (int) Main.player[playerNumber].position.Y));
                }

                break;
            }
            default:
                if (messageType is (byte) PacketTypes.PlayerSlot or (byte) PacketTypes.TileGetSection or (byte) PacketTypes.PlayerSpawn or (byte)PacketTypes.PlayerBuff)
                {
                    break;
                }

                if (messageType == 250)
                {
                    var id =   ModNet.NetModCount < 256 ? reader.ReadByte() : reader.ReadInt16();
                    if (ModNet.GetMod(id)?.DisplayName == "SSC - 云存档" || ModNet.GetMod(id)?.DisplayName == "HERO's Mod")
                    {
                        break;
                    }
                }
                if (player is { SscLogin: true, IsLoggedIn: false })
                {
                    //Console.WriteLine($"[CaiBot]处理数据包{messageType}(来自{player.Name})");
                    return true;
                }
                break;
        }
        return false;
    }
}