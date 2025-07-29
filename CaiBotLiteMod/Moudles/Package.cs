using CaiBotLiteMod.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaiBotLiteMod.Moudles;

[Serializable]
public class Package(Direction direction, PackageType type, bool isRequest, string? requestId)
{
    // {
    //     "version": "0.1.0",       // 数据包版本
    //     "direction": "to_server", // 数据包方向
    //     "type": "self_kick",      // 数据包类型
    //     "is_request": true,       // 是否为请求
    //     "request_id": "...",      // 请求ID
    //     "payload": {              // 数据 Dict[str, Any]
    //         ...
    //     }
    // }

    [JsonProperty("version")] public Version Version => this.Type.GetVersion();

    [JsonProperty("direction")] public Direction Direction = direction;

    [JsonProperty("type")] public PackageType Type = type;

    [JsonProperty("is_request")] public bool IsRequest = isRequest;

    [JsonProperty("request_id")] public string? RequestId = requestId;

    [JsonProperty("payload")] public Payload Payload = new ();

    public T Read<T>(string key)
    {
        if (!this.Payload.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
        }

        if (typeof(T).IsEnum && value is string stringValue)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>($"\"{stringValue}\"", Converter)!;
            }
            catch (JsonException ex)
            {
                if (Enum.GetNames(typeof(T)).Any(name => string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase)))
                {
                    return (T)Enum.Parse(typeof(T), "Unknown", true);
                }
                
                throw new InvalidCastException($"Cannot convert string '{stringValue}' to enum {typeof(T).Name}", ex);
            }
        }


        if (value is not T targetValue)
        {
            throw new InvalidCastException($"Expected type {typeof(T).Name}, but got {value.GetType().Name}");
        }

        return targetValue;
    }

    private static readonly StringEnumConverter Converter = new (new SnakeCaseNamingStrategy());

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.None, Converter);
    }

    public static Package Parse(string json)
    {
        return JsonConvert.DeserializeObject<Package>(json, Converter)!;
    }
}

public enum Direction
{
    [JsonProperty("to_server")] ToServer,
    [JsonProperty("to_bot")] ToBot
}

public class Payload : Dictionary<string, object>
{
    public new object this[string key]
    {
        get => this.TryGetValue(key, out var obj);
        set
        {
            if (!this.ContainsKey(key))
            {
                this.Add(key, value);
            }
            else
            {
                base[key] = value;
            }
        }
    }
}