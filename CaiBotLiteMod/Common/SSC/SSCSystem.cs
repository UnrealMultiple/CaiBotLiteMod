using CaiBotLiteMod.Common.Utils;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CaiBotLiteMod.Common.SSC;

public class SSCSystem : ModSystem
{
    public override void Load()
    {
        On_Player.InternalSavePlayerFile += On_PlayerOnInternalSavePlayerFile;
        On_Player.KillMeForGood += On_PlayerOnKillMeForGood;
    }
    
    public override void Unload()
    {
        On_Player.InternalSavePlayerFile -= On_PlayerOnInternalSavePlayerFile;
        On_Player.KillMeForGood -= On_PlayerOnKillMeForGood;
    }

    private static void On_PlayerOnKillMeForGood(On_Player.orig_KillMeForGood orig, Player self)
    {
        if (!ServerConfig.Instance.EnableWhiteList || !ServerConfig.Instance.EnableSSC)
        {
            orig(self);
            return;
        }

        var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.DeleteSSC);
        mp.Send();
    }

    private int _timer;

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
        {
            return;
        }

        this._timer++;
        if (this._timer >= ServerConfig.Instance.SaveInterval * 60)
        {
            this._timer = 0;
            Terraria.Utilities.FileUtilities.ProtectedInvoke(() =>
            {
                Player.SavePlayer(Main.ActivePlayerFileData);
            });
        }
    }

    public override void PreSaveAndQuit()
    {
        Terraria.Utilities.FileUtilities.ProtectedInvoke(() =>
        {
            Player.SavePlayer(Main.ActivePlayerFileData);
        });
    }

    private static async void On_PlayerOnInternalSavePlayerFile(On_Player.orig_InternalSavePlayerFile orig, PlayerFileData playerFile)
    {
        try
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.SinglePlayer)
            {
                orig(playerFile);
                return;
            }

            if (!ServerConfig.Instance.EnableWhiteList || !ServerConfig.Instance.EnableSSC)
            {
                orig(playerFile);
                return;
            }

            if (Main.LocalPlayer.difficulty == (int) PlayerDifficultyId.Hardcore && (Main.LocalPlayer.dead || Main.LocalPlayer.ghost))
            {
                return;
            }

            var playerData = Player.SavePlayerFile_Vanilla(playerFile);

            var method = Type.GetType("Terraria.ModLoader.IO.PlayerIO, tModLoader")!.GetMethod("SaveData",
                BindingFlags.Static | BindingFlags.NonPublic);


            var tagCompound = (TagCompound) method!.Invoke(null, [playerFile.Player])!;

            var data = await SSCComposer.Compose(playerData, tagCompound);


            await SSCFragmenter.SendAsync(data, playerData.Length);
        }
        catch (Exception e)
        {
            Log.WriteLine($"[CaiBotLite]压缩SSC数据时出错: {e}", ConsoleColor.Red);
        }
    }
}