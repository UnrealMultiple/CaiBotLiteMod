using CaiBotLiteMod.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.SSC;

internal static class SSCFragmenter
{
    private static readonly ConcurrentDictionary<int, byte[]> PacketCache = new ();

    private enum ReadStatus : byte
    {
        Start,
        Reading,
        End,
    }

    public static async Task ReadAsync(BinaryReader reader, int whoami)
    {
        var status = (ReadStatus) reader.ReadByte();
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (status)
        {
            case ReadStatus.Start:
                PacketCache.Remove(whoami, out _);
                break;
            case ReadStatus.Reading:
                var bytesRead = reader.ReadInt32();
                var data = reader.ReadBytes(bytesRead);
                PacketCache.AddOrUpdate(whoami,
                    _ => data,
                    (_, value) => value.Concat(data).ToArray());
                break;
            case ReadStatus.End:
            {
                PacketCache.Remove(whoami, out var composedData);

                if (composedData is null)
                {
                    Log.WriteLine("[CaiBotLite]接收云存档发生错误，数据为null!", ConsoleColor.Red);
                    return;
                }

                var playerDataLength = reader.ReadInt32();
                var md5 = reader.ReadString();
                var currentMd5 = Convert.ToHexString(MD5.HashData(composedData));
                
                if (md5 != currentMd5)
                {
                    Log.WriteLine("[CaiBotLite]接收云存档发生错误，MD5校验失败!", ConsoleColor.Red);
                    return;
                }

                var decomposeData = await SSCComposer.Decompose(composedData);

                if (Main.dedServ)
                {
                    SSCManager.SaveSSC(whoami, decomposeData, playerDataLength);
                }
                else
                {
                    SSCManager.Restore(decomposeData, playerDataLength);
                }

                return;
            }
        }
    }

    public static async Task SendAsync(byte[] data, int playerDataLength, int toClient = -1)
    {
        var stream = new MemoryStream(data);
        var buffer = new byte[16384];
        int bytesRead;

        var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.SSCSegment);
        mp.Write((byte) ReadStatus.Start);
        mp.Send(toClient);
        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
            mp.Write((byte) CaiBotLiteMod.MessageType.SSCSegment);
            mp.Write((byte) ReadStatus.Reading);
            mp.Write(bytesRead);
            mp.Write(buffer, 0, bytesRead);
            mp.Send(toClient);
        }

        var md5 = Convert.ToHexString(MD5.HashData(data));
        mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.SSCSegment);
        mp.Write((byte) ReadStatus.End);
        mp.Write(playerDataLength);
        mp.Write(md5);
        mp.Send(toClient);
    }
}