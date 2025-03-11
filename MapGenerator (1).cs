using System.IO;
using Oxide.Core;
using UnityEngine;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("MapGenerator", "Dids/SkiTles", "0.1")]
    [Description("Simple map generator")]
    class MapGenerator : RustPlugin
    {
        //Original code - SeederyIoMapGen by Pauli 'Dids' Jokela
        //url - https://github.com/Dids/seedery.io-mapgen

        //Данный плагин (модификация SeederyIoMapGen) принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Config
        private static ConfigFile config;
        private class ConfigFile
        {
            [JsonProperty(PropertyName = "Имя генерируемого файла карты, без расширения (.jpg), если папки указаной в имени не существует, плагин будет выдавать ошибку")]
            public string filename { get; set; }

            [JsonProperty(PropertyName = "Автоматическая генерация новой карты после вайпа")]
            public bool AutoMap { get; set; }

            [JsonProperty(PropertyName = "Размер изображения для автоматической генерации карты (0 - размер по дефолту)")]
            public int autosize { get; set; }

            [JsonProperty(PropertyName = "Расширение файла (автоматическая генерация, jpg или png)")]
            public string type { get; set; }

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    filename = "map",
                    AutoMap = false,
                    autosize = 0,
                    type = "jpg"
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = ConfigFile.DefaultConfig();
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch { Regenerate(); }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private void Regenerate()
        {
            PrintWarning($"Конфигурационный файл 'oxide/config/{Name}.json' поврежден, создается новый...");
            LoadDefaultConfig();
        }
        #endregion

        #region Properties
        private int _mapWidth;
        private int _mapHeight;
        private Terrain _terrain;
        private bool NewWipe = false;
        #endregion

        #region Commands
        [ConsoleCommand("savemap")]
        private void SaveMapCMD(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            int size = 0;
            string filetype = ".jpg";
            if (arg.HasArgs())
            {
                if (!Int32.TryParse(arg.Args[0], out size)) PrintWarning("Неправильно указан размер изображения. При генерации будет установлено дефолтное значение (размер карты / 2)");
                if (arg.Args.Length > 1 && arg.Args[1] == "png") filetype = ".png";
            }
            GenerateMap(size, filetype);
        }
        #endregion

        #region MapGenerator
        private void GenerateMap(int size = 0, string filetype = ".jpg")
        {
            PrintWarning("Создается изображение карты. Сервер может подвиснуть на 5- 10 секунд!");
            TerrainMeta.HeightMap.GenerateTextures();
            Texture2D rawmap = TerrainMeta.HeightMap.NormalTexture;
            int width = TerrainMeta.Terrain.terrainData.heightmapWidth - 1;
            int height = TerrainMeta.Terrain.terrainData.heightmapHeight - 1;
            Texture2D finalTexture = new Texture2D(width, height);
            _terrain = TerrainMeta.Terrain;
            Color waterColor = Color.white;
            float lowestTerrainheight = GetLowestTerrainHeight();
            float highestTerrainHeight = GetHighestTerrainHeight();
            for (int y = 0; y < finalTexture.height; y++)
            {
                for (int x = 0; x < finalTexture.width; x++)
                {
                    bool water = false;
                    int mask = -1;
                    float terrainStartX = TerrainMeta.Terrain.GetPosition().x;
                    float terrainStartY = TerrainMeta.Terrain.GetPosition().z;
                    float terrainSizeX = TerrainMeta.Size.x;
                    float terrainSizeY = TerrainMeta.Size.z;
                    float terrainScaleUpX = TerrainMeta.Size.x / width;
                    float terrainScaleUpY = TerrainMeta.Size.z / height;
                    float terrainScaleDownX = width / TerrainMeta.Size.x;
                    float terrainScaleDownY = height / TerrainMeta.Size.z;
                    float startX = x * terrainScaleUpX;
                    float startY = y * terrainScaleUpY;
                    float calculatedTerrainX = startX + terrainStartX;
                    float calculatedTerrainY = startY + terrainStartY;
                    Vector3 terrainWorldPosition = new Vector3(calculatedTerrainX, 0, calculatedTerrainY);
                    float waterDepth = TerrainMeta.WaterMap.GetDepth(terrainWorldPosition);
                    float terrainHeight = _terrain.terrainData.GetHeight(x, y) - lowestTerrainheight;
                    float currentHeight = terrainHeight / (highestTerrainHeight - lowestTerrainheight);
                    if (currentHeight > waterDepth)
                    {
                        Color pixelColor = TerrainMeta.Colors.GetColor(terrainWorldPosition, mask);
                        pixelColor.a = TerrainMeta.AlphaMap.GetAlpha(x, y);
                        finalTexture.SetPixel(x, y, pixelColor);
                    }
                    else
                    {
                        Color blueColor = new Color(0f, (1f / 255f) * 150f, 1f, 1f);
                        finalTexture.SetPixel(x, y, blueColor);
                        water = true;
                    }
                    if (rawmap != null)
                    {
                        var finalTextureColor = finalTexture.GetPixel(x, y);
                        var heightmapNormalTextureColor = rawmap.GetPixel(x, y);
                        var alphaBlendedColor = AlphaBlend(heightmapNormalTextureColor, finalTextureColor);
                        finalTexture.SetPixel(x, y, alphaBlendedColor);
                        if (x == 0 && y == 0 && water)
                            waterColor = alphaBlendedColor;
                        else
                        {
                            if (water)
                            {
                                if (waterDepth > 4 && waterColor != Color.white) finalTexture.SetPixel(x, y, waterColor);
                                if (waterDepth <= 4 && waterColor != Color.white)
                                {
                                    var waterColor2 = new Color(waterColor.r, waterColor.g, waterColor.b, (waterDepth / 255f) * 63f);
                                    var alphaBlendedColor2 = AlphaBlend(waterColor2, finalTextureColor);
                                    finalTexture.SetPixel(x, y, alphaBlendedColor2);
                                }
                            }
                        }
                    }
                }
            }
            finalTexture.Apply();
            SaveIMG(finalTexture, size, filetype);
            UnityEngine.Object.Destroy(finalTexture);
        }

        private float GetLowestTerrainHeight()
        {
            float lowestHeight = TerrainMeta.Size.y;
            for (var x = 0; x < TerrainMeta.Size.x; x++)
            {
                for (var y = 0; y < TerrainMeta.Size.z; y++)
                {
                    var h = _terrain.terrainData.GetHeight(x, y);
                    if (h < lowestHeight) lowestHeight = h;
                }
            }
            return lowestHeight;
        }

        private float GetHighestTerrainHeight()
        {
            float highestHeight = 0;
            for (var x = 0; x < TerrainMeta.Size.x; x++)
            {
                for (var y = 0; y < TerrainMeta.Size.z; y++)
                {
                    var h = _terrain.terrainData.GetHeight(x, y);
                    if (h > highestHeight) highestHeight = h;
                }
            }
            return highestHeight;
        }

        private static Color AlphaBlend(Color top, Color bottom)
        {
            return new Color(BlendSubpixel(top.r, bottom.r, top.a, bottom.a),
                             BlendSubpixel(top.g, bottom.g, top.a, bottom.a),
                             BlendSubpixel(top.b, bottom.b, top.a, bottom.a),
                             top.a + bottom.a);
        }

        private static float BlendSubpixel(float top, float bottom, float alphaTop, float alphaBottom)
        {
            return (top * alphaTop) + ((bottom - 1f) * (alphaBottom - alphaTop));
        }

        private void SaveIMG(Texture2D texture, int size = 0, string filetype =".jpg")
        {
            if (filetype != ".jpg" && filetype != ".png") return;
            byte[] bytes = null;
            if (filetype == ".jpg") bytes = texture.EncodeToJPG();
            if (filetype == ".png") bytes = texture.EncodeToPNG();
            if (bytes == null) return;    
            Stream stream = new MemoryStream(bytes);
            System.Drawing.Image mapimage = System.Drawing.Image.FromStream(stream);
            if (size != 0 && size != mapimage.Height)
            {
                System.Drawing.Image.GetThumbnailImageAbort myCallback = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
                mapimage = mapimage.GetThumbnailImage(size, size, myCallback, IntPtr.Zero);
            }
            if (filetype == ".jpg") mapimage.Save(Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + config.filename + filetype, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filetype == ".png") mapimage.Save(Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + config.filename + filetype, System.Drawing.Imaging.ImageFormat.Png);
            PrintWarning($"Изображение карты размером {mapimage.Height}px сохранено: {Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + config.filename + filetype}");
        }

        public bool ThumbnailCallback()
        {
            return false;
        }
        #endregion

        #region OxideHooks
        void OnNewSave(string filename)
        {
            if (config.AutoMap) NewWipe = true;
        }
        void OnServerInitialized()
        {
            if (config.AutoMap && NewWipe) GenerateMap(config.autosize, config.type);
        }
        #endregion
    }
}