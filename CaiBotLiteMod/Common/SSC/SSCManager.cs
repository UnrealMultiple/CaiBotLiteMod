using CaiBotLiteMod.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.UI.States;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace CaiBotLiteMod.Common.SSC;

public static class SSCManager
{
    private static readonly string SaveDirPath = Path.Combine(Main.SavePath, nameof(CaiBotLiteMod), Main.ActiveWorldFileData.UniqueId.ToString());

    public static async void SaveSSC(int index, byte[] data, int playerDataLength)
    {
        try
        {
            Directory.CreateDirectory(SaveDirPath);
            var playerName = Main.player[index].name;
            await using var plrFile = File.Open(Path.Combine(SaveDirPath, $"{playerName}.plr"), FileMode.Create);
            await using var tplrFile = File.Open(Path.Combine(SaveDirPath, $"{playerName}.tplr"), FileMode.Create);

            await plrFile.WriteAsync(data.AsMemory(0, playerDataLength));
            await tplrFile.WriteAsync(data.AsMemory(playerDataLength, data.Length - playerDataLength));

            if (ServerConfig.Instance.SendSuccessMessage)
            {
                ChatHelper.SendChatMessageToClient(
                    NetworkText.FromLiteral(
                        $"[CaiBotLite]云存档保存成功! " +
                        $"([c/00FF00:{Math.Round(plrFile.Length / 1024.0, 2)}KB+{Math.Round(tplrFile.Length / 1024.0, 2)}KB])"),
                    Color.Green, index);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine($"[CaiBotLite]保存玩家存档时出错: {e}", ConsoleColor.Red);
        }
    }

    public static bool ExistSSC(string playerName)
    {
        return File.Exists(Path.Combine(SaveDirPath, $"{playerName}.plr")) && File.Exists(Path.Combine(SaveDirPath, $"{playerName}.tplr"));
    }

    public static void DeleteSSC(string playerName)
    {
        try
        {
            File.Delete(Path.Combine(SaveDirPath, $"{playerName}.plr"));
            File.Delete(Path.Combine(SaveDirPath, $"{playerName}.tplr"));
        }
        catch (Exception e)
        {
            Log.WriteLine($"[CaiBotLite]删除玩家存档时出错: {e}", ConsoleColor.Red);
        }
    }

    public static async Task<(byte[], int)> LoadSSC(string playerName)
    {
        try
        {
            await using var plrFile = File.Open(Path.Combine(SaveDirPath, $"{playerName}.plr"), FileMode.Open);
            await using var tplrFile = File.Open(Path.Combine(SaveDirPath, $"{playerName}.tplr"), FileMode.Open);

            var plrData = new byte[plrFile.Length];
            var tplrData = new byte[tplrFile.Length];
            _ = await plrFile.ReadAsync(plrData);
            _ = await tplrFile.ReadAsync(tplrData);

            var data = await SSCComposer.Compose(plrData, tplrData);
            return (data, (int) plrFile.Length);
        }
        catch (Exception e)
        {
            Log.WriteLine($"[CaiBotLite]加载玩家存档时出错: {e}", ConsoleColor.Red);
            return ([], 0);
        }
    }

    public static void Restore(byte[] data, int playerDataLength, bool spawn = false)
    {
        var fileData = new PlayerFileData(Path.GetTempFileName(), false) { Metadata = FileMetadata.FromCurrentSettings(FileType.Player) };
        fileData = Player.LoadPlayerFromStream(fileData, data[..playerDataLength], data[playerDataLength..]);
        fileData.Player.whoAmI = Main.LocalPlayer.whoAmI;
        fileData.SetAsActive();
        if (spawn)
        {
            fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        }
        
        var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.ConnectContinue);
        mp.Send();
    }

    public static void Reset(bool spawn = false)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var player = new Player();
        var characterCreation = new UICharacterCreation(player);

        player.whoAmI = Main.LocalPlayer.whoAmI;
        player.name = Main.LocalPlayer.name;
        player.difficulty = (byte) ServerConfig.Instance.DifficultyId;
        player.skinVariant = Main.LocalPlayer.skinVariant;
        player.skinColor = Main.LocalPlayer.skinColor;
        player.eyeColor = Main.LocalPlayer.eyeColor;
        player.hair = Main.LocalPlayer.hair;
        player.hairColor = Main.LocalPlayer.hairColor;
        player.shirtColor = Main.LocalPlayer.shirtColor;
        player.underShirtColor = Main.LocalPlayer.underShirtColor;
        player.pantsColor = Main.LocalPlayer.pantsColor;
        player.shoeColor = Main.LocalPlayer.shoeColor;

        var setupPlayerStatsAndInventoryBasedOnDifficultyMethod = typeof(UICharacterCreation).GetMethod("SetupPlayerStatsAndInventoryBasedOnDifficulty", BindingFlags.Instance | BindingFlags.NonPublic);
        setupPlayerStatsAndInventoryBasedOnDifficultyMethod!.Invoke(characterCreation, []);


        var fileData = new PlayerFileData(Path.GetTempFileName(), false) { Metadata = FileMetadata.FromCurrentSettings(FileType.Player), Player = player };

        var playerData = Player.SavePlayerFile_Vanilla(fileData);

        var saveDataMethod = Type.GetType("Terraria.ModLoader.IO.PlayerIO, tModLoader")!.GetMethod("SaveData",
            BindingFlags.Static | BindingFlags.NonPublic);

        var tagCompound = (TagCompound) saveDataMethod!.Invoke(null, [player])!;
        var stream = new MemoryStream();
        TagIO.ToStream(tagCompound, stream);

        var modPlayerData = stream.ToArray();
        fileData = Player.LoadPlayerFromStream(fileData, playerData, modPlayerData);
        fileData.SetAsActive();
        if (spawn)
        {
            player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        }
        
        var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.ConnectContinue);
        mp.Send();
    }
}