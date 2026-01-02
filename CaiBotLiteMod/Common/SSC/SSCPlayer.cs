using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace CaiBotLiteMod.Common.SSC;

public class SSCPlayer : ModPlayer
{
    public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
    {
        var items = new List<Item>();
        foreach (var i in ServerConfig.Instance.StartItems.Where(i => !i.ItemDefinition.IsUnloaded))
        {
            if (i.Prefix.IsUnloaded)
            {
                i.Prefix = new PrefixDefinition(0);
            }

            items.Add(new Item(i.ItemDefinition.Type, i.Stack, i.Prefix.Type));
        }

        return items;
    }


    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            return;
        }

        var mp = ModContent.GetInstance<CaiBotLiteMod>().GetPacket();
        mp.Write((byte) CaiBotLiteMod.MessageType.ClientMod);

        var mods = ModLoader.Mods.Where(x => x.Side is ModSide.Client or ModSide.NoSync).Select(x => x.Name).ToArray();
        mp.Write(mods.Length);
        foreach (var mod in mods)
        {
            mp.Write(mod);
        }

        mp.Send();
    }
}