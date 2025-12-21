using CaiBotLiteMod.Common.Hook;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Model;

public class CaiBotCommandCaller : CommandCaller
{
    public void Reply(string text, Color color = new ())
    {
        ExecuteCommandHook.Reply(text);
    }

    public CommandType CommandType => CommandType.Console;

    public Player Player => null!;
}