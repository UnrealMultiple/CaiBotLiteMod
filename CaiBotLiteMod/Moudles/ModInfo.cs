using System;

namespace CaiBotLiteMod.Moudles;

public class ModInfo(string name, Version version)
{
    public string Author = null!;
    public string Description = null!;

    public string Name = name;
    public Version Version = version;
}