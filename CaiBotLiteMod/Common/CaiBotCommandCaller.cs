using CaiBotLiteMod.Hooks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common;

public class CaiBotCommandCaller : CommandCaller
{
    public void Reply(string text, Color color = new ())
    {
        ExecuteCommandHook.Reply(text);
    }

    public CommandType CommandType => CommandType.Console;

    public Player Player => null!;
}