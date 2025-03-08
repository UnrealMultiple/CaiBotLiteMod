using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

public static class BossCheckList
{
    private static readonly Version BossChecklistAPIVersion = new (2, 0);

    public static List<BossChecklistBossInfo> GetBossList()
    {
        if (ModLoader.TryGetMod("BossChecklist", out var bossChecklist) && bossChecklist.Version >= BossChecklistAPIVersion)
        {
            var currentBossInfoResponse = bossChecklist.Call("GetBossInfoDictionary", ModLoader.GetMod("CaiBotLiteMod"), BossChecklistAPIVersion.ToString());
            if (currentBossInfoResponse is Dictionary<string, Dictionary<string, object>> bossInfoList)
            {
                return bossInfoList.ToDictionary(boss => boss.Key, boss => new BossChecklistBossInfo()
                {
                    Key = (boss.Value.ContainsKey("key") ? boss.Value["key"] as string : "")!,
                    ModSource = (boss.Value.ContainsKey("modSource") ? boss.Value["modSource"] as string : "")!,
                    DisplayName = (boss.Value.ContainsKey("displayName") ? boss.Value["displayName"] as LocalizedText : null)!,
                    Progression = boss.Value.ContainsKey("progression") ? Convert.ToSingle(boss.Value["progression"]) : 0f,
                    Downed = (boss.Value.ContainsKey("downed") ? boss.Value["downed"] as Func<bool> : () => false)!,
                    IsBoss = boss.Value.ContainsKey("isBoss") ? Convert.ToBoolean(boss.Value["isBoss"]) : false,
                    IsMiniboss = boss.Value.ContainsKey("isMiniboss") ? Convert.ToBoolean(boss.Value["isMiniboss"]) : false,
                    IsEvent = boss.Value.ContainsKey("isEvent") ? Convert.ToBoolean(boss.Value["isEvent"]) : false,
                    NpcIDs = (boss.Value.ContainsKey("npcIDs") ? boss.Value["npcIDs"] as List<int> : new List<int>())!,
                    SpawnInfo = (boss.Value.ContainsKey("spawnInfo") ? boss.Value["spawnInfo"] as Func<LocalizedText> : null)!,
                    SpawnItems = (boss.Value.ContainsKey("spawnItems") ? boss.Value["spawnItems"] as List<int> : new List<int>())!,
                    TreasureBag = boss.Value.ContainsKey("treasureBag") ? Convert.ToInt32(boss.Value["treasureBag"]) : 0,
                    DropRateInfo = (boss.Value.ContainsKey("dropRateInfo") ? boss.Value["dropRateInfo"] as List<DropRateInfo> : new List<DropRateInfo>())!,
                    Loot = (boss.Value.ContainsKey("loot") ? boss.Value["loot"] as List<int> : new List<int>())!,
                    Collectibles = (boss.Value.ContainsKey("collectibles") ? boss.Value["collectibles"] as List<int> : new List<int>())!
                }).Values.ToList();
            }
        }

        return new List<BossChecklistBossInfo>();
    }
}