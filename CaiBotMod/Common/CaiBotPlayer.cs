using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.UI.Chat;

namespace CaiBotMod.Common;

public class CaiBotPlayer : TSPlayer
{
    internal List<string> CommandOutput = new ();

    public CaiBotPlayer()
        : base("CaiBot")
    {
        this.AwaitingResponse = new Dictionary<string, Action<object>>();
    }

    public override void SendMessage(string? msg, byte red, byte green, byte blue)
    {
        var result1 = "";
        foreach (var item in ChatManager.ParseMessage(msg, new Color(red, green, blue)))
        {
            result1 += item.Text;
        }

        Regex regex = new (@"\[i(tem)?(?:\/s(?<Stack>\d{1,4}))?(?:\/p(?<Prefix>\d{1,3}))?:(?<NetID>-?\d{1,4})\]");

        var result = regex.Replace(result1, m =>
        {
            var netID = m.Groups["NetID"].Value;
            var prefix = m.Groups["Prefix"].Success ? m.Groups["Prefix"].Value : "0";
            var stack = m.Groups["Stack"].Success ? m.Groups["Stack"].Value : "0";
            if (stack == "0")
            {
                return "";
            }

            if (stack == "1")
            {
                if (prefix == "0")
                {
                    return $"[{Lang.GetItemName(int.Parse(netID))}]";
                }

                return
                    $"[{Lang.prefix[int.Parse(prefix)]} {Lang.GetItemName(int.Parse(netID))}]"; //return $"[{Terraria.Lang.prefix[int.Parse(netID)]}]";
            }

            return $"[{Lang.prefix[int.Parse(prefix)]} {Lang.GetItemName(int.Parse(netID))} ({stack})]";
        });
        this.CommandOutput.Add(result);
    }

    public override void SendInfoMessage(string? msg)
    {
        this.SendMessage(msg, System.Drawing.Color.Yellow);
    }

    public override void SendSuccessMessage(string? msg)
    {
        this.SendMessage(msg, System.Drawing.Color.Green);
    }

    public override void SendWarningMessage(string? msg)
    {
        this.SendMessage(msg, System.Drawing.Color.OrangeRed);
    }

    public override void SendErrorMessage(string? msg)
    {
        this.SendMessage(msg, System.Drawing.Color.Red);
    }

    public List<string> GetCommandOutput()
    {
        return this.CommandOutput;
    }
}