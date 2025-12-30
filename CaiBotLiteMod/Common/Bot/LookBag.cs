using CaiBotLiteMod.Common.SSC;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

#pragma warning disable CS0414 // 字段已被赋值，但它的值从未被使用

namespace CaiBotLiteMod.Common.Bot;

internal static class LookBag
{
    public static async Task<string> Look(string name)
    {
        var player = Main.player.FirstOrDefault(p => p != null && p.name == name);
        if (player != null)
        {
            return LookPlayer(player);
        }

        if (!SSCManager.ExistSSC(name))
        {
            return string.Empty;
        }

        var sscPlayer = await SSCManager.LoadSSCPlayer(name);
        return sscPlayer != null ? LookPlayer(sscPlayer) : string.Empty;
    }


    private static string LookPlayer(Player plr)
    {
        var result = new StringBuilder();
        result.AppendLine($"玩家: {plr.name}");
        result.AppendLine($"生命: {plr.statLife}/{plr.statLifeMax}");
        result.AppendLine($"魔力: {plr.statMana}/{plr.statManaMax}");
        result.AppendLine($"渔夫任务: {plr.anglerQuestsFinished}次");

        List<string> enhance = [];
        if (plr.extraAccessory)
        {
            enhance.Add(GetItemDesc(ItemID.DemonHeart));
        }

        if (plr.unlockedBiomeTorches)
        {
            enhance.Add(GetItemDesc(ItemID.TorchGodsFavor));
        }

        if (plr.ateArtisanBread)
        {
            enhance.Add(GetItemDesc(ItemID.ArtisanLoaf));
        }

        if (plr.usedAegisCrystal)
        {
            enhance.Add(GetItemDesc(ItemID.AegisCrystal));
        }

        if (plr.usedAegisFruit)
        {
            enhance.Add(GetItemDesc(ItemID.AegisFruit));
        }

        if (plr.usedArcaneCrystal)
        {
            enhance.Add(GetItemDesc(ItemID.ArcaneCrystal));
        }

        if (plr.usedGalaxyPearl)
        {
            enhance.Add(GetItemDesc(ItemID.GalaxyPearl));
        }

        if (plr.usedGummyWorm)
        {
            enhance.Add(GetItemDesc(ItemID.GummyWorm));
        }

        if (plr.usedAmbrosia)
        {
            enhance.Add(GetItemDesc(ItemID.Ambrosia));
        }

        if (plr.unlockedSuperCart)
        {
            enhance.Add(GetItemDesc(ItemID.PeddlersSatchel));
        }

        if (enhance.Count != 0)
        {
            result.AppendLine("永久增强: " + string.Join(",", enhance));
        }
        else
        {
            result.AppendLine("永久增强: " + "啥都没有...");
        }

        List<string> inventory = [];
        List<string> assist = [];
        List<string> armor = [];
        List<string> vanity = [];
        List<string> dye = [];
        List<string> miscEquips = [];
        List<string> miscDyes = [];
        List<string> bank = [];
        List<string> bank2 = [];
        List<string> bank3 = [];
        List<string> bank4 = [];
        List<string> armor1 = [];
        List<string> armor2 = [];
        List<string> armor3 = [];
        List<string> vanity1 = [];
        List<string> vanity2 = [];
        List<string> vanity3 = [];
        List<string> dye1 = [];
        List<string> dye2 = [];
        List<string> dye3 = [];

        string s;
        for (var i = 0; i < 59; i++)
        {
            s = GetItemDesc(plr.inventory[i].Clone());
            if (i < 50)
            {
                if (s != "")
                {
                    inventory.Add(s);
                }
            }
            else if (i < 59)
            {
                if (s != "")
                {
                    assist.Add(s);
                }
            }
        }

        for (var i = 0; i < plr.armor.Length; i++)
        {
            s = GetItemDesc(plr.armor[i]);
            if (i < 10)
            {
                if (s != "")
                {
                    armor.Add(s);
                }
            }
            else
            {
                if (s != "")
                {
                    vanity.Add(s);
                }
            }
        }

        foreach (var t in plr.dye)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                dye.Add(s);
            }
        }

        foreach (var t in plr.miscEquips)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                miscEquips.Add(s);
            }
        }

        foreach (var t in plr.miscDyes)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                miscDyes.Add(s);
            }
        }

        foreach (var t in plr.bank.item)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                bank.Add(s);
            }
        }

        foreach (var t in plr.bank2.item)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                bank2.Add(s);
            }
        }

        foreach (var t in plr.bank3.item)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                bank3.Add(s);
            }
        }

        foreach (var t in plr.bank4.item)
        {
            s = GetItemDesc(t);
            if (s != "")
            {
                bank4.Add(s);
            }
        }

        // 装备（loadout）
        for (var i = 0; i < plr.Loadouts.Length; i++)
        {
            Item[] items = plr.Loadouts[i].Armor;
            // 装备 和 时装
            for (var j = 0; j < items.Length; j++)
            {
                s = GetItemDesc(items[j]);
                if (!string.IsNullOrEmpty(s))
                {
                    if (i == 0)
                    {
                        if (j < 10)
                        {
                            armor1.Add(s);
                        }
                        else
                        {
                            vanity1.Add(s);
                        }
                    }
                    else if (i == 1)
                    {
                        if (j < 10)
                        {
                            armor2.Add(s);
                        }
                        else
                        {
                            vanity2.Add(s);
                        }
                    }
                    else if (i == 2)
                    {
                        if (j < 10)
                        {
                            armor3.Add(s);
                        }
                        else
                        {
                            vanity3.Add(s);
                        }
                    }
                }
            }

            // 染料
            items = plr.Loadouts[i].Dye;
            foreach (var t in items)
            {
                s = GetItemDesc(t);
                if (!string.IsNullOrEmpty(s))
                {
                    if (i == 0)
                    {
                        dye1.Add(s);
                    }
                    else if (i == 1)
                    {
                        dye2.Add(s);
                    }
                    else if (i == 2)
                    {
                        dye3.Add(s);
                    }
                }
            }
        }

        List<string> trash = [];
        s = GetItemDesc(plr.trashItem);
        if (s != "")
        {
            trash.Add(s);
        }

        if (inventory.Count != 0)
        {
            result.AppendLine("背包: " + string.Join(",", inventory));
        }
        else
        {
            result.AppendLine("背包: 啥都没有...");
        }

        if (trash.Count != 0)
        {
            result.AppendLine("垃圾桶: " + string.Join(",", trash));
        }
        else
        {
            result.AppendLine("垃圾桶: 啥都没有...");
        }

        if (assist.Count != 0)
        {
            result.AppendLine("钱币弹药: " + string.Join(",", assist));
        }


        var num = plr.CurrentLoadoutIndex + 1;

        if (armor.Count != 0)
        {
            result.AppendLine($">装备{num}: " + string.Join(",", armor));
        }


        if (vanity.Count != 0)
        {
            result.AppendLine($">时装{num}: " + string.Join(",", vanity));
        }


        if (dye.Count != 0)
        {
            result.AppendLine($">染料{num}: " + string.Join(",", dye));
        }


        if (armor1.Count != 0)
        {
            result.AppendLine("装备1: " + string.Join(",", armor1));
        }


        if (vanity1.Count != 0)
        {
            result.AppendLine("时装1: " + string.Join(",", vanity1));
        }


        if (dye1.Count != 0)
        {
            result.AppendLine("染料1: " + string.Join(",", dye1));
        }


        if (armor2.Count != 0)
        {
            result.AppendLine("装备2: " + string.Join(",", armor2));
        }


        if (vanity2.Count != 0)
        {
            result.AppendLine("时装2: " + string.Join(",", vanity2));
        }


        if (dye2.Count != 0)
        {
            result.AppendLine("染料2: " + string.Join(",", dye2));
        }


        if (armor3.Count != 0)
        {
            result.AppendLine("装备3: " + string.Join(",", armor3));
        }


        if (vanity3.Count != 0)
        {
            result.AppendLine("时装3: " + string.Join(",", vanity3));
        }


        if (dye3.Count != 0)
        {
            result.AppendLine("染料3: " + string.Join(",", dye3));
        }


        if (miscEquips.Count != 0)
        {
            result.AppendLine("工具栏: " + string.Join(",", miscEquips));
        }


        if (miscDyes.Count != 0)
        {
            result.AppendLine("染料2: " + string.Join(",", miscDyes));
        }


        if (bank.Count != 0)
        {
            result.AppendLine("储蓄罐: " + string.Join(",", bank));
        }


        if (bank2.Count != 0)
        {
            result.AppendLine("保险箱: " + string.Join(",", bank2));
        }


        if (bank3.Count != 0)
        {
            result.AppendLine("护卫熔炉: " + string.Join(",", bank3));
        }


        if (bank4.Count != 0)
        {
            result.AppendLine("虚空保险箱: " + string.Join(",", bank4));
        }

        return string.Join("\n", result);
    }

    private static string GetItemDesc(Item item, bool isFlag = false)
    {
        if (item.netID == 0)
        {
            return "";
        }

        return GetItemDesc(item.netID, item.Name, item.stack, item.prefix, isFlag);
    }

    private static string GetItemDesc(int id, bool isFlag = false)
    {
        return isFlag ? $"[i:{id}]" : $"[{Lang.GetItemNameValue(id)}]";
    }

    private static string GetItemDesc(int id, string name, int stack, int prefix, bool isFlag = false)
    {
        if (isFlag)
        {
            if (stack > 1)
            {
                return $"[i/s{stack}:{id}]";
            }

            if (prefix.Equals(0))
            {
                return $"[i:{id}]";
            }

            return $"[i/p{prefix}:{id}]";
        }

        var s = name;
        var prefixName = Lang.prefix[prefix].Value;
        if (prefixName != "")
        {
            s = $"{prefixName}的 {s}";
        }

        if (stack > 1)
        {
            s = $"{s} ({stack})";
        }

        return $"[{s}]";
    }
}