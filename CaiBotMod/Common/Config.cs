using Newtonsoft.Json;
using System.IO;

namespace CaiBotMod.Common;

public class Config
{
    public const string Path = "CaiBot.json";

    public static Config config = new ();

    [JsonProperty("白名单拦截提示的群号")] public long GroupNumber = 114514191;

    [JsonProperty("密钥")] public string Token = "";

    [JsonProperty("白名单开关")] public bool WhiteList = true;

    public void Write(string path = Path)
    {
        using (FileStream fileStream = new (path, FileMode.Create, FileAccess.Write, FileShare.Write))
        {
            this.Write(fileStream);
        }
    }

    public void Write(Stream stream)
    {
        var value = JsonConvert.SerializeObject(this, Formatting.Indented);
        using (StreamWriter streamWriter = new (stream))
        {
            streamWriter.Write(value);
        }
    }

    public static Config Read(string path = Path)
    {
        var flag = !File.Exists(path);
        Config result;
        if (flag)
        {
            result = new Config();
            result.Write(path);
        }
        else
        {
            using (FileStream fileStream = new (path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = Read(fileStream);
            }
        }

        config = result;
        return result;
    }

    public static Config Read(Stream stream)
    {
        Config result;
        using (StreamReader streamReader = new (stream))
        {
            result = JsonConvert.DeserializeObject<Config>(streamReader.ReadToEnd())!;
        }

        return result;
    }
}