using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

// ReSharper disable ClassNeverInstantiated.Global

namespace CaiBotLiteMod.Common;

[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class ServerConfig : ModConfig
{
    [JsonIgnore]
    internal static ServerConfig Instance
    {
        get
        {
            _instance ??= ModContent.GetInstance<ServerConfig>();
            return _instance;
        }
    }

    private static ServerConfig? _instance;
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("General")]
    [DefaultValue(true)]
    public bool EnableWhiteList;

    [Expand(true)]
    public List<string> BanClientMods = [];

    [DefaultValue(true)]
    public bool ShowProcessInPlayerList;

    [DefaultValue("114514")]
    public long GroupNumber;

    [Header("SSC")]
    [DefaultValue(true)]
    public bool EnableSSC;

    [DefaultValue(60)]
    [Range(10, 60 * 60)]
    public int SaveInterval;

    [DefaultValue(false)]
    public bool SendSuccessMessage;

    [Expand(false)]
    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public List<InitItem> StartItems = [];

    [DefaultValue(PlayerDifficultyId.SoftCore)]
    public PlayerDifficultyId DifficultyId;

    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        if (!Instance.EnableWhiteList)
        {
            message = NetworkText.FromLiteral("保存成功，白名单已关闭，无法保护配置文件!");
            return true;
        }
        
        var plr = CaiBotLiteMod.Players[whoAmI];
        
        if (plr is null)
        {
            message = NetworkText.FromLiteral("玩家是棍母");
            return false;
        }
        
        if (!plr.IsAdmin)
        {
            message = NetworkText.FromLiteral("需要BOT管理员才能修改MOD配置");
            return false;
        }

        message = NetworkText.FromLiteral("OKKKKKKKKK!");
        return true;

    }
}

public class ClientConfig : ModConfig
{
    [JsonIgnore]
    internal static ClientConfig Instance
    {
        get
        {
            _instance ??= ModContent.GetInstance<ClientConfig>();
            return _instance;
        }
    }

    private static ClientConfig? _instance;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue("")]
    public string Token = null!;

    [DefaultValue("")]
    public string GroupOpenId = null!;

    public void Save()
    {
        var type = typeof(ConfigManager);

        var method = type.GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, [this]);
    }
}

public enum PlayerDifficultyId : byte
{
    SoftCore = 0,
    MediumCore = 1,
    Hardcore = 2,
    Creative = 3,
}

public class InitItem
{
    public ItemDefinition ItemDefinition = new ();

    [DefaultValue(1)]
    public int Stack;

    public PrefixDefinition Prefix = new ();
}