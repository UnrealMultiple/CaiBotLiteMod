using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;
using Terraria.ModLoader.Config;

namespace CaiBotLiteMod.Common;


public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    
    [DefaultValue(true)]
    [LabelKey("$Mods.CaiBotLiteMod.Config.EnableWhiteList.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.EnableWhiteList.LocalizedTooltip")]
    public bool EnableWhiteList;
    
    [DefaultValue(true)]
    [LabelKey("$Mods.CaiBotLiteMod.Config.EnableSsc.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.EnableSsc.LocalizedTooltip")]
    public bool EnableSsc;

    [DefaultValue(true)]
    [LabelKey("$Mods.CaiBotLiteMod.Config.ShowProcessInPlayerList.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.ShowProcessInPlayerList.LocalizedTooltip")]
    public bool ShowProcessInPlayerList;

    [LabelKey("$Mods.CaiBotLiteMod.Config.GroupNumber.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.GroupNumber.LocalizedTooltip")]
    [DefaultValue("114514")]
    public long GroupNumber;
    
}

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    
    [LabelKey("$Mods.CaiBotLiteMod.Config.Token.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.Token.LocalizedTooltip")]
    [DefaultValue("")]
    public string Token = null!;
    
    [DefaultValue("")]
    [LabelKey("$Mods.CaiBotLiteMod.Config.GroupOpenId.LocalizedLabel")]
    [TooltipKey("$Mods.CaiBotLiteMod.Config.GroupOpenId.LocalizedTooltip")]
    public string GroupOpenId = null!;

    public void Save()
    {
        var type = typeof(ConfigManager);
                    
        var method = type.GetMethod("Save", BindingFlags.NonPublic|BindingFlags.Static);
        method?.Invoke(null, [this]);
    }
}