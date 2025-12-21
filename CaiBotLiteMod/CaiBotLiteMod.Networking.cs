using CaiBotLiteMod.Common;
using CaiBotLiteMod.Common.SSC;
using System;
using System.IO;
using Terraria;

namespace CaiBotLiteMod;

// ReSharper disable once ClassNeverInstantiated.Global
partial class CaiBotLiteMod
{
    internal enum MessageType : byte
    {
        ClientMod,
        SSCSegment,
        ResetSSC,
        DeleteSSC,
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var type = reader.ReadByte();
        switch ((MessageType) type)
        {
            case MessageType.ClientMod:
                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    if (ServerConfig.Instance.BanClientMods.Contains(name))
                    {
                        Players[whoAmI]?.Kick("[CaiBotLite]使用被禁用的客户端模组: \n" +
                                              $"{name}");
                        return;
                    }
                }

                break;
            case MessageType.SSCSegment:
                _ = SSCFragmenter.ReadAsync(reader, whoAmI);
                break;
            case MessageType.ResetSSC:
                SSCManager.Reset();
                break;
            case MessageType.DeleteSSC:
                SSCManager.DeleteSSC(Main.player[whoAmI].name);
                break;
        }
    }
}