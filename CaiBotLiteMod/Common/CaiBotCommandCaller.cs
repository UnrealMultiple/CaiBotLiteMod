using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CaiBotLiteMod.Common;

public class CaiBotCommandCaller : CommandCaller
{
    private CommandType _commandType = CommandType.Console;
    private Player _player = null!;

    public void Reply(string text, Color color = new ())
    {
        return;
    }

    public CommandType CommandType => this._commandType;

    public Player Player => this._player;
}