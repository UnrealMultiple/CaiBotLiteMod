using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace CaiBotMod.Common
{
    public class BossChecklistBossInfo
    {
        internal string Key = ""; // unique identifier for an entry
        internal string ModSource = "";
        internal LocalizedText DisplayName = null;

        internal float Progression = 0f;
        internal Func<bool> Downed = () => false;

        internal bool IsBoss = false;
        internal bool IsMiniboss = false;
        internal bool IsEvent = false;

        internal List<int> NpcIDs = new List<int>();
        internal Func<LocalizedText> SpawnInfo = null;
        internal List<int> SpawnItems = new List<int>();
        internal int TreasureBag = 0;
        internal List<DropRateInfo> DropRateInfo = new ();
        internal List<int> Loot = new List<int>();
        internal List<int> Collectibles = new List<int>();

        public override string ToString()
        {
            return this.Progression + ": " + this.Key;
        }
    }
}