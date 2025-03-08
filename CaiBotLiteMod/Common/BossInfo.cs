using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace CaiBotLiteMod.Common;

public class BossChecklistBossInfo
{
    internal string Key = ""; // unique identifier for an entry
    internal string ModSource = "";
    internal LocalizedText DisplayName = null!;

    internal float Progression = 0f;
    internal Func<bool> Downed = () => false;

    internal bool IsBoss = false;
    internal bool IsMiniboss = false;
    internal bool IsEvent = false;

    internal List<int> NpcIDs = new ();
    internal Func<LocalizedText> SpawnInfo = null!;
    internal List<int> SpawnItems = new ();
    internal int TreasureBag = 0;
    internal List<DropRateInfo> DropRateInfo = new ();
    internal List<int> Loot = new ();
    internal List<int> Collectibles = new ();

    public override string ToString()
    {
        return this.Progression + ": " + this.Key;
    }
}