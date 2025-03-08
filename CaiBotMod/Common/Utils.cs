using Ionic.Zlib;
using System;
using System.IO;
using System.Text;
using Terraria;

namespace CaiBotMod.Common;

public static class Utils
{
    public static string FileToBase64String(string path)
    {
        FileStream fsForRead = new (path, FileMode.Open); //文件路径
        var base64Str = "";
        try
        {
            fsForRead.Seek(0, SeekOrigin.Begin);
            var bs = new byte[fsForRead.Length];
            var log = Convert.ToInt32(fsForRead.Length);
            _ = fsForRead.Read(bs, 0, log);
            base64Str = Convert.ToBase64String(bs);
            return base64Str;
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
            Console.ReadLine();
            return base64Str;
        }
        finally
        {
            fsForRead.Close();
        }
    }

    public static string CompressBase64(string base64String)
    {
        var base64Bytes = Encoding.UTF8.GetBytes(base64String);
        using MemoryStream outputStream = new ();
        using (GZipStream gzipStream = new (outputStream, CompressionMode.Compress))
        {
            gzipStream.Write(base64Bytes, 0, base64Bytes.Length);
        }

        return Convert.ToBase64String(outputStream.ToArray());
    }

    public static string GetItemDesc(Item item, bool isFlag = false)
    {
        if (item.netID == 0)
        {
            return "";
        } 
        return GetItemDesc(item.netID, item.Name, item.stack, item.prefix, isFlag);

    }
    

    public static string GetItemDesc(int id, bool isFlag = false)
    {
        return isFlag ? $"[i:{id}]" : $"[{Lang.GetItemNameValue(id)}]";
    }

    public static string GetItemDesc(int id, string name, int stack, int prefix, bool isFlag = false)
    {
        if (isFlag)
        {
            // https://terraria.fandom.com/wiki/Chat
            // [i:29]   数量
            // [i/s10:29]   数量
            // [i/p57:4]    词缀
            // 控制台显示 物品名称
            // 4.4.0 -1.4.1.2   [i:4444]
            // 4.5.0 -1.4.2.2   [女巫扫帚]
            //ChatItemIsIcon = TShock.VersionNum.CompareTo(new Version(4, 5, 0, 0)) >= 0;
            //Console.WriteLine($"ChatItemIsIcon:");
            if (stack > 1)
            {
                return $"[i/s{stack}:{id}]";
            }

            if (prefix.Equals(0))
            {
                return $"[i:{id}]";
            }

            return $"[i/p{prefix}:{id}]";
        }

        var s = name;
        var prefixName = Lang.prefix[prefix].Value;
        if (prefixName != "")
        {
            s = $"{prefixName}的 {s}";
        }

        if (stack > 1)
        {
            s = $"{s} ({stack})";
        }

        return $"[{s}]";
    }
    
}