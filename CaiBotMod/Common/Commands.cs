/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Terraria;

namespace CaiBotMod.Common;

public delegate void CommandDelegate(CommandArgs args);

public class CommandArgs : EventArgs
{
    public CommandArgs(string message, TSPlayer ply, List<string> args)
    {
        this.Message = message;
        this.Player = ply;
        this.Parameters = args;
        this.Silent = false;
    }

    public CommandArgs(string message, bool silent, TSPlayer ply, List<string> args)
    {
        this.Message = message;
        this.Player = ply;
        this.Parameters = args;
        this.Silent = silent;
    }

    public string Message { get; private set; }
    public TSPlayer Player { get; }
    public bool Silent { get; private set; }

    /// <summary>
    ///     Parameters passed to the argument. Does not include the command name.
    ///     IE '/kick "jerk face"' will only have 1 argument
    /// </summary>
    public List<string> Parameters { get; private set; }

    public Player TPlayer => this.Player.TPlayer;
}

public class Command
{
    private CommandDelegate commandDelegate = null!;

    public Command(List<string> permissions, CommandDelegate cmd, params string[] names)
        : this(cmd, names)
    {
        this.Permissions = permissions;
    }

    public Command(string permissions, CommandDelegate cmd, params string[] names)
        : this(cmd, names)
    {
        this.Permissions = new List<string> { permissions };
    }

    public Command(CommandDelegate cmd, params string[] names)
    {
        if (cmd == null)
        {
            throw new ArgumentNullException("cmd");
        }

        if (names == null || names.Length < 1)
        {
            throw new ArgumentException("names");
        }

        this.AllowServer = true;
        this.CommandDelegate = cmd;
        this.DoLog = true;
        this.HelpText = "没有可用的帮助.";
        this.HelpDesc = null!;
        this.Names = new List<string>(names);
        this.Permissions = new List<string>();
    }

    /// <summary>
    ///     Gets or sets whether to allow non-players to use this command.
    /// </summary>
    public bool AllowServer { get; set; }

    /// <summary>
    ///     Gets or sets whether to do logging of this command.
    /// </summary>
    public bool DoLog { get; set; }

    /// <summary>
    ///     Gets or sets the help text of this command.
    /// </summary>
    public string HelpText { get; set; }

    /// <summary>
    ///     Gets or sets an extended description of this command.
    /// </summary>
    public string?[] HelpDesc { get; set; }

    /// <summary>
    ///     Gets the name of the command.
    /// </summary>
    public string Name => this.Names[0];

    /// <summary>
    ///     Gets the names of the command.
    /// </summary>
    public List<string> Names { get; protected set; }

    /// <summary>
    ///     Gets the permissions of the command.
    /// </summary>
    public List<string> Permissions { get; protected set; }

    public CommandDelegate CommandDelegate
    {
        get => this.commandDelegate;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            this.commandDelegate = value;
        }
    }

    public bool Run(string msg, bool silent, TSPlayer ply, List<string> parms)
    {
        if (!this.CanRun(ply))
        {
            return false;
        }

        try
        {
            this.CommandDelegate(new CommandArgs(msg, silent, ply, parms));
        }
        catch
        {
            ply.SendErrorMessage("运行命令时出现错误!");
        }

        return true;
    }

    public bool Run(string msg, TSPlayer ply, List<string> parms)
    {
        return this.Run(msg, false, ply, parms);
    }

    public bool HasAlias(string name)
    {
        return this.Names.Contains(name);
    }

    public bool CanRun(TSPlayer ply)
    {
        if (this.Permissions == null || this.Permissions.Count < 1)
        {
            return true;
        }

        foreach (var Permission in this.Permissions)
        {
            return true;
        }

        return false;
    }
}

public static class Commands
{
    public static List<Command> ChatCommands = new ();
    public static ReadOnlyCollection<Command> TShockCommands = new (new List<Command>());

    /// <summary>
    ///     The command specifier, defaults to "/"
    /// </summary>
    public static string Specifier => "/";

    /// <summary>
    ///     The silent command specifier, defaults to "."
    /// </summary>
    public static string SilentSpecifier => "#";

    public static void InitCommands()
    {
        List<Command> tshockCommands = new (100);
        Action<Command> add = cmd =>
        {
            tshockCommands.Add(cmd);
            ChatCommands.Add(cmd);
        };

        TShockCommands = new ReadOnlyCollection<Command>(tshockCommands);
    }

    public static bool HandleCommand(TSPlayer player, string text)
    {
        var cmdText = text.Remove(0, 1);
        var cmdPrefix = text[0].ToString();
        var silent = false;

        if (cmdPrefix == SilentSpecifier)
        {
            silent = true;
        }
        else if (cmdPrefix == Specifier)
        {
            silent = false;
        }
        else
        {
            return false;
        }

        var index = -1;
        for (var i = 0; i < cmdText.Length; i++)
        {
            if (IsWhiteSpace(cmdText[i]))
            {
                index = i;
                break;
            }
        }

        string cmdName;
        if (index == 0) // Space after the command specifier should not be supported
        {
            player.SendErrorMessage("您在{0}后输入了空格，而不是命令。键入{0}帮助以获取有效命令的列表.", Specifier);
            return true;
        }

        if (index < 0)
        {
            cmdName = cmdText.ToLower();
        }
        else
        {
            cmdName = cmdText.Substring(0, index).ToLower();
        }

        List<string> args;
        if (index < 0)
        {
            args = new List<string>();
        }
        else
        {
            args = ParseParameters(cmdText.Substring(index));
        }

        IEnumerable<Command> cmds = ChatCommands.FindAll(c => c.HasAlias(cmdName));


        if (cmds.Count() == 0)
        {
            if (player.AwaitingResponse.ContainsKey(cmdName))
            {
                Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                player.AwaitingResponse.Remove(cmdName);
                call(new CommandArgs(cmdText, player, args));
                return true;
            }

            //player.SendErrorMessage("无效的命令,输入/help获取命令列表.", Specifier);
            return false;
        }

        foreach (var cmd in cmds)
        {
            if (!cmd.AllowServer && !player.RealPlayer)
            {
                player.SendErrorMessage("你必须在游戏中执行这个命令.");
            }
            else
            {
                Console.WriteLine("{0}执行了{1}{2}.", player.Name, silent ? SilentSpecifier : Specifier, cmdText);
                cmd.Run(cmdText, silent, player, args);
            }
        }

        return true;
    }

    /// <summary>
    ///     Parses a string of parameters into a list. Handles quotes.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static List<string> ParseParameters(string str)
    {
        List<string> ret = new ();
        StringBuilder sb = new ();
        var instr = false;
        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];

            if (c == '\\' && ++i < str.Length)
            {
                if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
                {
                    sb.Append('\\');
                }

                sb.Append(str[i]);
            }
            else if (c == '"')
            {
                instr = !instr;
                if (!instr)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
                else if (sb.Length > 0)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (IsWhiteSpace(c) && !instr)
            {
                if (sb.Length > 0)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            ret.Add(sb.ToString());
        }

        return ret;
    }

    private static bool IsWhiteSpace(char c)
    {
        return c == ' ' || c == '\t' || c == '\n';
    }

    private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);
}