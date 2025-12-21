using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Utils;

internal static class SearchTool
{
    /// <summary>
    /// Gets an NPC by ID or Name
    /// </summary>
    /// <param name="idOrName"></param>
    /// <returns>List of NPCs</returns>
    public static List<NPC> GetNpcByIdOrName(string idOrName)
    {
        if (int.TryParse(idOrName, out var type))
        {
            if (type >= NPCLoader.NPCCount)
            {
                return [];
            }

            return [GetNpcById(type)];
        }

        return GetNpcByName(idOrName);
    }

    /// <summary>
    /// Gets an NPC by ID
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>NPC</returns>
    public static NPC GetNpcById(int id)
    {
        var npc = new NPC();
        npc.SetDefaults(id);
        return npc;
    }

    /// <summary>
    /// Gets a NPC by name
    /// </summary>
    /// <param name="name">Name</param>
    /// <returns>List of matching NPCs</returns>
    public static List<NPC> GetNpcByName(string name)
    {
        // ReSharper disable once IdentifierTypo
        var startswith = new List<int>();
        var contains = new List<int>();
        for (var i = -17; i < NPCLoader.NPCCount; i++)
        {
            var currentName = Lang.GetNPCNameValue(i);
            if (!string.IsNullOrEmpty(currentName))
            {
                if (currentName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return [GetNpcById(i)];
                }

                if (currentName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    startswith.Add(i);
                    continue;
                }

                if (currentName.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    contains.Add(i);
                }
            }
        }

        if (startswith.Count != 1)
        {
            startswith.AddRange(contains);
        }

        return startswith.Select(GetNpcById).ToList();
    }
}