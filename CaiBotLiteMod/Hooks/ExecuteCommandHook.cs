﻿using CaiBotLiteMod.Common;
using Terraria.ModLoader;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CaiBotLiteMod.Hooks;

public static class ExecuteCommandHook
{
    public static bool StartHook = false;

    private static StringBuilder _outPut = new ();

    private static readonly Hook WriteLineHook = new (
        typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(string)])!,
        typeof(ExecuteCommandHook).GetMethod(nameof(WriteLine), BindingFlags.Static | BindingFlags.NonPublic)!);

    private static readonly Hook WriteHook = new (
        typeof(Console).GetMethod(nameof(Console.Write), [typeof(string)])!,
        typeof(ExecuteCommandHook).GetMethod(nameof(Write), BindingFlags.Static| BindingFlags.NonPublic)!);

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

    public static void Clean()
    {
        _outPut = new StringBuilder();
    }

    public static string GetCommandOutput()
    {
        return _outPut.ToString();
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