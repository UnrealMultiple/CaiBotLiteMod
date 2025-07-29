using Newtonsoft.Json;
using System;
using Terraria;

namespace CaiBotLiteMod.Moudles;

[JsonObject(MemberSerialization.OptIn)]
public struct NetItem
{
    //
    // 摘要:
    //     40 - The number of slots in a piggy bank
    public static readonly int PiggySlots = 40;

    //
    // 摘要:
    //     40 - The number of slots in a safe
    public static readonly int SafeSlots = PiggySlots;

    //
    // 摘要:
    //     40 - The number of slots in a forge
    public static readonly int ForgeSlots = SafeSlots;

    //
    // 摘要:
    //     40 - The number of slots in a void vault
    public static readonly int VoidSlots = ForgeSlots;

    //
    // 摘要:
    //     59 - The size of the player's inventory (inventory, coins, ammo, held item)
    public static readonly int InventorySlots = 59;

    //
    // 摘要:
    //     20 - The number of armor slots.
    public static readonly int ArmorSlots = 20;

    //
    // 摘要:
    //     5 - The number of other equippable items
    public static readonly int MiscEquipSlots = 5;

    //
    // 摘要:
    //     10 - The number of dye slots.
    public static readonly int DyeSlots = 10;

    //
    // 摘要:
    //     5 - The number of other dye slots (for TShockAPI.NetItem.MiscEquipSlots)
    public static readonly int MiscDyeSlots = MiscEquipSlots;

    //
    // 摘要:
    //     1 - The number of trash can slots.
    public static readonly int TrashSlots = 1;

    //
    // 摘要:
    //     The number of armor slots in a loadout.
    public static readonly int LoadoutArmorSlots = ArmorSlots;

    //
    // 摘要:
    //     The number of dye slots in a loadout.
    public static readonly int LoadoutDyeSlots = DyeSlots;

    //
    // 摘要:
    //     180 - The inventory size (inventory, held item, armour, dies, coins, ammo, piggy,
    //     safe, and trash)
    public static readonly int MaxInventory = InventorySlots + ArmorSlots + DyeSlots + MiscEquipSlots + MiscDyeSlots +
                                              PiggySlots + SafeSlots + ForgeSlots + VoidSlots + TrashSlots +
                                              (LoadoutArmorSlots * 3) + (LoadoutDyeSlots * 3);

    public static readonly Tuple<int, int> InventoryIndex = new (0, InventorySlots);

    public static readonly Tuple<int, int> ArmorIndex = new (InventoryIndex.Item2, InventoryIndex.Item2 + ArmorSlots);

    public static readonly Tuple<int, int> DyeIndex = new (ArmorIndex.Item2, ArmorIndex.Item2 + DyeSlots);

    public static readonly Tuple<int, int> MiscEquipIndex = new (DyeIndex.Item2, DyeIndex.Item2 + MiscEquipSlots);

    public static readonly Tuple<int, int>
        MiscDyeIndex = new (MiscEquipIndex.Item2, MiscEquipIndex.Item2 + MiscDyeSlots);

    public static readonly Tuple<int, int> PiggyIndex = new (MiscDyeIndex.Item2, MiscDyeIndex.Item2 + PiggySlots);

    public static readonly Tuple<int, int> SafeIndex = new (PiggyIndex.Item2, PiggyIndex.Item2 + SafeSlots);

    public static readonly Tuple<int, int> TrashIndex = new (SafeIndex.Item2, SafeIndex.Item2 + TrashSlots);

    public static readonly Tuple<int, int> ForgeIndex = new (TrashIndex.Item2, TrashIndex.Item2 + ForgeSlots);

    public static readonly Tuple<int, int> VoidIndex = new (ForgeIndex.Item2, ForgeIndex.Item2 + VoidSlots);

    public static readonly Tuple<int, int> Loadout1Armor = new (VoidIndex.Item2, VoidIndex.Item2 + LoadoutArmorSlots);

    public static readonly Tuple<int, int>
        Loadout1Dye = new (Loadout1Armor.Item2, Loadout1Armor.Item2 + LoadoutDyeSlots);

    public static readonly Tuple<int, int>
        Loadout2Armor = new (Loadout1Dye.Item2, Loadout1Dye.Item2 + LoadoutArmorSlots);

    public static readonly Tuple<int, int>
        Loadout2Dye = new (Loadout2Armor.Item2, Loadout2Armor.Item2 + LoadoutDyeSlots);

    public static readonly Tuple<int, int>
        Loadout3Armor = new (Loadout2Dye.Item2, Loadout2Dye.Item2 + LoadoutArmorSlots);

    public static readonly Tuple<int, int>
        Loadout3Dye = new (Loadout3Armor.Item2, Loadout3Armor.Item2 + LoadoutDyeSlots);

    [JsonProperty("netID")] private int _netId;

    [JsonProperty("prefix")] private byte _prefixId;

    [JsonProperty("stack")] private int _stack;

    //
    // 摘要:
    //     Gets the net ID.
    public int NetId => this._netId;

    //
    // 摘要:
    //     Gets the prefix.
    public byte PrefixId => this._prefixId;

    //
    // 摘要:
    //     Gets the stack.
    public int Stack => this._stack;

    //
    // 摘要:
    //     Creates a new TShockAPI.NetItem.
    //
    // 参数:
    //   netId:
    //     The net ID.
    //
    //   stack:
    //     The stack.
    //
    //   prefixId:
    //     The prefix ID.
    public NetItem(int netId, int stack, byte prefixId)
    {
        this._netId = netId;
        this._stack = stack;
        this._prefixId = prefixId;
    }

    //
    // 摘要:
    //     Converts the TShockAPI.NetItem to a string.
    public override string ToString()
    {
        return $"{this._netId},{this._stack},{this._prefixId}";
    }

    //
    // 摘要:
    //     Converts a string into a TShockAPI.NetItem.
    //
    // 参数:
    //   str:
    //     The string.
    //
    // 异常:
    //   T:System.ArgumentNullException:
    //
    //   T:System.FormatException:
    public static NetItem Parse(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException("str");
        }

        var array = str.Split(',');
        if (array.Length != 3)
        {
            throw new FormatException("String does not contain three sections.");
        }

        var netId = int.Parse(array[0]);
        var stack = int.Parse(array[1]);
        var prefixId = byte.Parse(array[2]);
        return new NetItem(netId, stack, prefixId);
    }

    //
    // 摘要:
    //     Converts an Terraria.Item into a TShockAPI.NetItem.
    //
    // 参数:
    //   item:
    //     The Terraria.Item.
    public static explicit operator NetItem(Item item)
    {
        if (item != null)
        {
            return new NetItem(item.netID, item.stack, (byte) item.prefix);
        }

        return default;
    }
}