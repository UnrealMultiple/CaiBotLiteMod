using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CaiBotLiteMod.Common;

[Serializable]
public class RestObject : Dictionary<string, object>
{
    public RestObject()
    {
        this.Status = "200";
    }

    public RestObject(string status = "200")
    {
        this.Status = status;
    }

    public string Status
    {
        get => (this["status"] as string)!;
        set => this["status"] = value;
    }

    public string Error
    {
        get => (this["error"] as string)!;
        set => this["error"] = value;
    }

    public string Response
    {
        get => (this["response"] as string)!;
        set => this["response"] = value;
    }

    //
    // 摘要:
    //     Gets value safely, if it does not exist, return null. Sets/Adds value safely,
    //     if null it will remove.
    //
    // 参数:
    //   key:
    //     the key
    //
    // 返回结果:
    //     Returns null if key does not exist.
    public new object this[string key]
    {
        get
        {
            if (this.TryGetValue(key, out var value))
            {
                return value;
            }

            return null!;
        }
        set
        {
            if (!this.ContainsKey(key))
            {
                if (value != null)
                {
                    this.Add(key, value);
                }
            }
            else if (value != null)
            {
                base[key] = value;
            }
            else
            {
                this.Remove(key);
            }
        }
    }

    internal string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}