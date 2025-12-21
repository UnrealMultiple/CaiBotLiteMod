using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

namespace CaiBotLiteMod.Common.SSC;

public static class SSCComposer
{
    public static async Task<byte[]> Decompose(byte[] data)
    {
        var gZipStream = new MemoryStream(data);
        await using var archive = new GZipStream(gZipStream, CompressionMode.Decompress);

        var stream = new MemoryStream();
        await archive.CopyToAsync(stream);

        return stream.ToArray();
    }

    public static async Task<byte[]> Compose(byte[] playerData, byte[] modPlayerData)
    {
        var stream = new MemoryStream();
        await using var archive = new GZipStream(stream, CompressionLevel.SmallestSize);
        await archive.WriteAsync(playerData);
        await archive.WriteAsync(modPlayerData);
        await archive.FlushAsync();

        return stream.ToArray();
    }

    public static async Task<byte[]> Compose(byte[] playerData, TagCompound tagCompound)
    {
        var modPlayerStream = new MemoryStream();
        TagIO.ToStream(tagCompound, modPlayerStream);
        var modPlayerData = modPlayerStream.ToArray();
        return await Compose(playerData, modPlayerData);
    }
}