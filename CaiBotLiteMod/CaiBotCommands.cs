using Newtonsoft.Json;
using System;
using System.Linq;
using Terraria.ModLoader;

namespace CaiBotLiteMod;

public class BindCodeCommand : ModCommand
{
    public override CommandType Type
        => CommandType.Console;

    public override string Command
        => "生成绑定码";

    public override string Description
        => "生成一个CaiBot绑定码";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        CaiBotLiteMod.GenCode();
    }
}

public class TestCommand : ModCommand
{
    public override CommandType Type
        => CommandType.Console;

    public override string Command
        => "t";

    public override string Description
        => "Test";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Console.WriteLine(JsonConvert.SerializeObject(ModLoader.Mods.Where(i => i != null).Select(i => i.Name)));
        var mod = ModLoader.GetMod("MagicStorage");
        var asset = mod.Assets.GetLoadedAssets();

        Console.WriteLine(asset.Length);
    }
}