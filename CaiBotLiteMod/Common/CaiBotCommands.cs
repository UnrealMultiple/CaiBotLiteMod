using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

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