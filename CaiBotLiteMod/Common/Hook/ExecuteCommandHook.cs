using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CaiBotLiteMod.Common.Hook;

public static class ExecuteCommandHook
{
    public static bool StartHook = false;

    private static StringBuilder _outPut = new ();

    private static readonly MonoMod.RuntimeDetour.Hook WriteLineHook = new (
        typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(string)])!,
        typeof(ExecuteCommandHook).GetMethod(nameof(WriteLine), BindingFlags.Static | BindingFlags.NonPublic)!);

    private static readonly MonoMod.RuntimeDetour.Hook WriteHook = new (
        typeof(Console).GetMethod(nameof(Console.Write), [typeof(string)])!,
        typeof(ExecuteCommandHook).GetMethod(nameof(Write), BindingFlags.Static | BindingFlags.NonPublic)!);


    private static void WriteLine(Action<string> orig, string text)
    {
        orig(text);
        if (StartHook)
        {
            _outPut.AppendLine(text);
        }
    }

    private static void Write(Action<string> orig, string text)
    {
        orig(text);
        if (StartHook)
        {
            _outPut.Append(text);
        }
    }

    public static void Reply(string text)
    {
        _outPut.AppendLine(text);
    }

    public static void Clean()
    {
        _outPut = new StringBuilder();
    }

    public static List<string> GetCommandOutput()
    {
        if (_outPut.Length == 0)
        {
            return [];
        }


        if (_outPut.Length > 0 && _outPut[0] == ':')
        {
            _outPut.Remove(0, 2);
        }

        var lines = _outPut.ToString()
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
            .Where(x => x != "")
            .ToList();

        return lines;
    }

    public static void Apply()
    {
        WriteHook.Apply();
        WriteLineHook.Apply();
    }

    public static void Dispose()
    {
        WriteHook.Undo();
        WriteLineHook.Undo();
        WriteHook.Dispose();
        WriteLineHook.Dispose();
    }
}