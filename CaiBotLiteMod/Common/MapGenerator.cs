using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Terraria;
using Terraria.IO;
using Terraria.Map;
using Terraria.ModLoader;
using Image = SixLabors.ImageSharp.Image;


namespace CaiBotLiteMod.Common;

internal static class MapGenerator
{
    private static bool _inited;
    
    private static void Init()
    {
        try
        {
            
            Main.mapEnabled = true;
            MapHelper.Initialize();
            Main.Map = new WorldMap(Main.tile.Width, Main.tile.Height);
            
            Main.ActivePlayerFileData = new PlayerFileData{ Name = "CaiBot"};
            var playerFileDataType = typeof(PlayerFileData);
            var pathField = playerFileDataType.GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(Main.ActivePlayerFileData, Main.GetPlayerPathFromName("CaiBot", false));
            
            
            Main.MapFileMetadata = FileMetadata.FromCurrentSettings(FileType.Map);
            Lang._mapLegendCache = new MapLegend(MapHelper.LookupCount());
            try
            {
                Main.dedServ = false;
                var mapLoaderType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.MapLoader");
                var finishSetupMethod = mapLoaderType!.GetMethod(
                    "FinishSetup",
                    BindingFlags.NonPublic | BindingFlags.Static
                );
                finishSetupMethod!.Invoke(null, null);
            }
            finally
            {
                Main.dedServ = true;
            }
            
            _inited = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }
    
    

    private static void LightWholeMap()
    {
        if (!_inited)
        {
            Init();
        }
        for (var x = 0; x < Main.tile.Width; x++)
        {
            for (var y = 0; y < Main.tile.Height; y++)
            {
                var tile =MapHelper.CreateMapTile(x, y, byte.MaxValue);
                Main.Map.SetTile(x, y, ref tile);
            }
        }
    }
    
    internal static Image CreateMapImg()
    {
        Image<Rgba32> image = new (Main.tile.Width, Main.tile.Height);
        LightWholeMap();
        for (var x = 0; x < Main.tile.Width; x++)
        {
            for (var y = 0; y < Main.tile.Height; y++)
            {
                var tile = Main.Map[x, y];
                var col = MapHelper.GetMapTileXnaColor(ref tile);
                image[x, y] = new Rgba32(col.R, col.G, col.B, col.A);
            }
        }

        return image;
    }

    internal static (string, string) CreateMapFile()
    {
        LightWholeMap();
        MapHelper.SaveMap();
        var playerPath = Main.playerPathName[..^4];
        var mapFileName = !Main.ActiveWorldFileData.UseGuidAsMapName ? Main.worldID + ".map" : Main.ActiveWorldFileData.UniqueId + ".map";
        var mapFilePath = Path.Combine(playerPath, mapFileName);
        var modMapFileName = !Main.ActiveWorldFileData.UseGuidAsMapName ? Main.worldID + ".tmap" : Main.ActiveWorldFileData.UniqueId + ".tmap";
        var modMapFilePath = Path.Combine(playerPath, modMapFileName);
        var zipName = Main.worldName + ".zip";
        var zipPath = Path.Combine(playerPath, zipName);
        using (var zipToOpen = new FileStream(zipPath, FileMode.Create))
        {
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(mapFilePath, mapFileName);
                if (File.Exists(modMapFilePath))
                {
                    archive.CreateEntryFromFile(modMapFilePath, modMapFileName);
                }
            }
        }

        return (Utils.FileToBase64String(zipPath), zipName);
        
    }
}