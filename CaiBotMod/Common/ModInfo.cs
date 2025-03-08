using System;

namespace CaiBotMod.Common;

public class ModInfo
{
    public string Author = null!;
    public string Description = null!;

    public string Name;
    public Version Version;

    public ModInfo(string name, Version version)
    {
        this.Name = name;
        this.Version = version;
    }
}