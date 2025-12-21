using System.Collections.Generic;
using System.Text;
using Terraria;

#pragma warning disable CS0414 // 字段已被赋值，但它的值从未被使用

namespace CaiBotLiteMod.Common.Bot;

internal class LookBag
{
    public string Name = "";
    public int Health = 0;
    public int MaxHealth = 0;
    public int Mana = 0;
    public int MaxMana = 0;
    public int QuestsCompleted = 0;

    public readonly List<List<int>> ItemList = [];
    public readonly List<int> Enhances = [];
    public List<int> Buffs = [];

    // public static LookBag LookOffline(UserAccount acc, PlayerData data)
    // {
    //     var lookBagData = new LookBag
    //     {
    //         Name = acc.Name,
    //         Health = data.health,
    //         MaxHealth = data.maxHealth,
    //         Mana = data.mana,
    //         MaxMana = data.maxMana,
    //         QuestsCompleted = data.questsCompleted
    //     };
    //     if (data.extraSlot == 1)
    //     {
    //         lookBagData.Enhances.Add(3335); // 3335 恶魔之心
    //     }
    //
    //     if (data.unlockedBiomeTorches == 1)
    //     {
    //         lookBagData.Enhances.Add(5043); // 5043 火把神徽章
    //     }
    //
    //     if (data.ateArtisanBread == 1)
    //     {
    //         lookBagData.Enhances.Add(5326); // 5326	工匠面包
    //     }
    //
    //     if (data.usedAegisCrystal == 1)
    //     {
    //         lookBagData.Enhances.Add(5337); // 5337 生命水晶	永久强化生命再生 
    //     }
    //
    //     if (data.usedAegisFruit == 1)
    //     {
    //         lookBagData.Enhances.Add(5338); // 5338 埃癸斯果	永久提高防御力 
    //     }
    //
    //     if (data.usedArcaneCrystal == 1)
    //     {
    //         lookBagData.Enhances.Add(5339); // 5339 奥术水晶	永久提高魔力再生 
    //     }
    //
    //     if (data.usedGalaxyPearl == 1)
    //     {
    //         lookBagData.Enhances.Add(5340); // 5340	银河珍珠	永久增加运气 
    //     }
    //
    //     if (data.usedGummyWorm == 1)
    //     {
    //         lookBagData.Enhances.Add(5341); // 5341	黏性蠕虫	永久提高钓鱼技能  
    //     }
    //
    //     if (data.usedAmbrosia == 1)
    //     {
    //         lookBagData.Enhances.Add(5342); // 5342	珍馐	永久提高采矿和建造速度 
    //     }
    //
    //     if (data.unlockedSuperCart == 1)
    //     {
    //         lookBagData.Enhances.Add(5289); // 5289	矿车升级包
    //     }
    //
    //     foreach (var i in data.inventory)
    //     {
    //         lookBagData.ItemList.Add(new List<int> { i.NetId, i.Stack });
    //     }
    //
    //     lookBagData.Buffs = Utils.GetActiveBuffs(TShock.DB, acc.ID, acc.Name);
    //     return lookBagData;
    // }


    public static string LookOnline(Player plr)
    {
        var msgs = new StringBuilder();
        msgs.AppendLine($"玩家: {plr.name}");
        msgs.AppendLine($"生命: {plr.statLife}/{plr.statLifeMax}");
        msgs.AppendLine($"魔力: {plr.statMana}/{plr.statManaMax}");
        msgs.AppendLine($"渔夫任务: {plr.anglerQuestsFinished} 次");

        List<string> enhance = [];
        if (plr.extraAccessory)
        {
            enhance.Add(GetItemDesc(3335)); // 3335 恶魔之心
        }

        if (plr.unlockedBiomeTorches)
        {
            enhance.Add(GetItemDesc(5043)); // 5043 火把神徽章
        }

        if (plr.ateArtisanBread)
        {
            enhance.Add(GetItemDesc(5326)); // 5326	工匠面包
        }

        if (plr.usedAegisCrystal)
        {
            enhance.Add(GetItemDesc(5337)); // 5337 生命水晶	永久强化生命再生 
        }

        if (plr.usedAegisFruit)
        {
            enhance.Add(GetItemDesc(5338)); // 5338 埃癸斯果	永久提高防御力 
        }

        if (plr.usedArcaneCrystal)
        {
            enhance.Add(GetItemDesc(5339)); // 5339 奥术水晶	永久提高魔力再生 
        }

        if (plr.usedGalaxyPearl)
        {
            enhance.Add(GetItemDesc(5340)); // 5340	银河珍珠	永久增加运气 
        }

        if (plr.usedGummyWorm)
        {
            enhance.Add(GetItemDesc(5341)); // 5341	黏性蠕虫	永久提高钓鱼技能  
        }

        if (plr.usedAmbrosia)
        {
            enhance.Add(GetItemDesc(5342)); // 5342	珍馐	 永久提高采矿和建造速度 
        }

        if (plr.unlockedSuperCart)
        {
            enhance.Add(GetItemDesc(5289)); // 5289	矿车升级包
        }

        if (enhance.Count != 0)
        {
            msgs.AppendLine("永久增强: " + string.Join(",", enhance));
        }
        else
        {
            msgs.AppendLine("永久增强: " + "啥都没有...");
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
            msgs.AppendLine("背包：" + string.Join(",", inventory));
        }
        else
        {
            msgs.AppendLine("背包：啥都没有...");
        }

        if (trash.Count != 0)
        {
            msgs.AppendLine("垃圾桶：" + string.Join(",", trash));
        }
        else
        {
            msgs.AppendLine("垃圾桶：啥都没有...");
        }

        if (assist.Count != 0)
        {
            msgs.AppendLine("钱币弹药：" + string.Join(",", assist));
        }


        var num = plr.CurrentLoadoutIndex + 1;

        if (armor.Count != 0)
        {
            msgs.AppendLine($">装备{num}：" + string.Join(",", armor));
        }


        if (vanity.Count != 0)
        {
            msgs.AppendLine($">时装{num}：" + string.Join(",", vanity));
        }


        if (dye.Count != 0)
        {
            msgs.AppendLine($">染料{num}：" + string.Join(",", dye));
        }


        if (armor1.Count != 0)
        {
            msgs.AppendLine("装备1：" + string.Join(",", armor1));
        }


        if (vanity1.Count != 0)
        {
            msgs.AppendLine("时装1：" + string.Join(",", vanity1));
        }


        if (dye1.Count != 0)
        {
            msgs.AppendLine("染料1：" + string.Join(",", dye1));
        }


        if (armor2.Count != 0)
        {
            msgs.AppendLine("装备2：" + string.Join(",", armor2));
        }


        if (vanity2.Count != 0)
        {
            msgs.AppendLine("时装2：" + string.Join(",", vanity2));
        }


        if (dye2.Count != 0)
        {
            msgs.AppendLine("染料2：" + string.Join(",", dye2));
        }


        if (armor3.Count != 0)
        {
            msgs.AppendLine("装备3：" + string.Join(",", armor3));
        }


        if (vanity3.Count != 0)
        {
            msgs.AppendLine("时装3：" + string.Join(",", vanity3));
        }


        if (dye3.Count != 0)
        {
            msgs.AppendLine("染料3：" + string.Join(",", dye3));
        }


        if (miscEquips.Count != 0)
        {
            msgs.AppendLine("工具栏：" + string.Join(",", miscEquips));
        }


        if (miscDyes.Count != 0)
        {
            msgs.AppendLine("染料2：" + string.Join(",", miscDyes));
        }


        if (bank.Count != 0)
        {
            msgs.AppendLine("储蓄罐：" + string.Join(",", bank));
        }


        if (bank2.Count != 0)
        {
            msgs.AppendLine("保险箱：" + string.Join(",", bank2));
        }


        if (bank3.Count != 0)
        {
            msgs.AppendLine("护卫熔炉：" + string.Join(",", bank3));
        }


        if (bank4.Count != 0)
        {
            msgs.AppendLine("虚空保险箱：" + string.Join(",", bank4));
        }

        return string.Join("\n", msgs);
    }

    public static string GetItemDesc(Item item, bool isFlag = false)
    {
        if (item.netID == 0)
        {
            return "";
        }

        return GetItemDesc(item.netID, item.Name, item.stack, item.prefix, isFlag);
    }

    public static string GetItemDesc(int id, bool isFlag = false)
    {
        return isFlag ? $"[i:{id}]" : $"[{Lang.GetItemNameValue(id)}]";
    }

    public static string GetItemDesc(int id, string name, int stack, int prefix, bool isFlag = false)
    {
        if (isFlag)
        {
            // https://terraria.fandom.com/wiki/Chat
            // [i:29]   数量
            // [i/s10:29]   数量
            // [i/p57:4]    词缀
            // 控制台显示 物品名称
            // 4.4.0 -1.4.1.2   [i:4444]
            // 4.5.0 -1.4.2.2   [女巫扫帚]
            //ChatItemIsIcon = TShock.VersionNum.CompareTo(new Version(4, 5, 0, 0)) >= 0;
            //Console.WriteLine($"ChatItemIsIcon:");
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