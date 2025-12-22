using System;
using Terraria.ModLoader;

namespace CaiBotLiteMod.Common.Utils;

internal static class Log
{
    internal static void WriteLine(string? value, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(value);
        Console.ResetColor();

        var logger = ModContent.GetInstance<CaiBotLiteMod>().Logger;
        switch (color)
        {
            case ConsoleColor.Blue:
            case ConsoleColor.Green:
            default:
                logger.Info(value);
                break;

            case ConsoleColor.Red:
                logger.Error(value);
                break;

            case ConsoleColor.Yellow:
                logger.Warn(value);
                break;
        }
    }

    internal static void Write(string? value, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(value);
        Console.ResetColor();
        var logger = ModContent.GetInstance<CaiBotLiteMod>().Logger;
        switch (color)
        {
            case ConsoleColor.Blue:
            case ConsoleColor.Green:
            default:
                logger.Info(value);
                break;

            case ConsoleColor.Red:
                logger.Error(value);
                break;

            case ConsoleColor.Yellow:
                logger.Warn(value);
                break;
        }
    }
}