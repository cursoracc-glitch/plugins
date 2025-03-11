// Reference: Facepunch.Sqlite

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.Networking;
using Database = Facepunch.Sqlite.Database;

namespace Oxide.Plugins
{
    [Info("ImageLoader", "Bombardir", "0.1.0")]
    class ImageLoader : RustPlugin
    {
        public class QueueImageCache : IDisposable
        {
            private class DownloadingItem
            {
                public readonly string ImageUrl;
                public UnityWebRequest Www;
                
                public DownloadingItem(string imageUrl)
                {
                    ImageUrl = imageUrl;
                }
            }

            private const int MaxActiveDownloads = 16;
            private readonly List<DownloadingItem> _activeDownloads = new List<DownloadingItem>(MaxActiveDownloads);
            private readonly Queue<DownloadingItem> _itemsToDownload = new Queue<DownloadingItem>();
            
            private readonly Action _onQueueComplete;
            private readonly Action<string> _onError;
            private readonly Dictionary<string, string> _cachedImages;

            public QueueImageCache(Dictionary<string, string> cachedImages, Action onQueueComplete, Action<string> onError)
            {
                _onQueueComplete = onQueueComplete;
                _onError = onError;
                _cachedImages = cachedImages;
            }
            
            public void Dispose()
            {
                foreach (var item in _activeDownloads)
                {
                    if (!item.Www.isDone && !item.Www.isHttpError && !item.Www.isNetworkError)
                    {
                        item.Www.Dispose();
                    }
                }
            }
            
            public bool TryGetCachedOrCache(string imageUrl, out string cachedImageId)
            {
                cachedImageId = null;
                if (string.IsNullOrEmpty(imageUrl))
                    return false;
                
                var isCached = _cachedImages.TryGetValue(imageUrl, out cachedImageId) &&
                               FileStorage.server.Get(uint.Parse(cachedImageId), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID) != null;

                if (isCached)
                    return true;

                var isDownloading = _itemsToDownload.Any(item => item.ImageUrl == imageUrl) || 
                                    _activeDownloads.Any(item => item.ImageUrl == imageUrl);
                
                if (!isDownloading)
                {
                    _itemsToDownload.Enqueue(new DownloadingItem(imageUrl));
                    CacheNextImageIfNeeded();
                }

                return false;
            }
            
            private void CacheNextImageIfNeeded()
            {
                if (_itemsToDownload.Count <= 0 || _activeDownloads.Count >= MaxActiveDownloads)
                {
                    if (_activeDownloads.Count == 0)
                        _onQueueComplete();
                    return;
                }

                var downloadItem = _itemsToDownload.Dequeue();
                Rust.Global.Runner.StartCoroutine(CacheAndStartNextImage(downloadItem));
            }

            private IEnumerator CacheAndStartNextImage(DownloadingItem downloadingItem)
            {
                yield return CacheImage(downloadingItem);
                CacheNextImageIfNeeded();
            }

            private IEnumerator CacheImage(DownloadingItem downloadingItem)
            {
                _activeDownloads.Add(downloadingItem);
                
                var imageUrl = downloadingItem.ImageUrl;
                var www = UnityWebRequest.Get(imageUrl);
                downloadingItem.Www = www;
                yield return www.SendWebRequest();

                _activeDownloads.Remove(downloadingItem);

                if (!string.IsNullOrEmpty(www.error) || www.isHttpError || www.isNetworkError)
                {
                    _onError($"Failed to download image {imageUrl}: {www.error}");
                    yield break;
                }
                
                var tex = new Texture2D(2, 2);
                tex.LoadImage(www.downloadHandler.data);
                if (!tex.LoadImage(www.downloadHandler.data) || tex.height == 8 && tex.width == 8 && tex.name == string.Empty && tex.anisoLevel == 1)
                {
                    _onError($"Failed to cache image {imageUrl}: invalid image format");
                    yield break;
                }
                
                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.Destroy(tex);
                
                var imageId = FileStorage.server.Store(bytes, FileStorage.Type.png, 0);
                if (FileStorage.server.Get(imageId, FileStorage.Type.png, 0) == null)
                {
                    _onError($"Failed to store image {imageUrl} into local database");
                    CacheNextImageIfNeeded();
                    yield break;
                }
                
                _cachedImages[imageUrl] =  imageId.ToString();
            }
        }

        private class ImageLoaderData
        {
            public uint entid;
            public Dictionary<string, string> cachedImages = new Dictionary<string, string>();
            public ImageLoaderData() { }
        }

        private const string DataFileName = "Temporary/ImageLoader/image_cache";
        private const string NoneImageUrl = "https://cdn1.savepice.ru/uploads/2019/8/5/bdd7439a1ca1b8e63a3dc6f99bae60a4-full.png";
        
        private readonly Regex _avatarRegex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>", RegexOptions.Compiled);
        private readonly Dictionary<ulong, string> _avatarUrls = new Dictionary<ulong, string>();
        private readonly Queue<string> _imagesToCache = new Queue<string>();
        
        private QueueImageCache _imageCache;
        private ImageLoaderData _imageLoaderData;

        void Loaded()
        {
            _imageLoaderData = LoadImageLoaderDataFromFile();
            CheckCachedOrCache(NoneImageUrl);
        }
        
        void OnServerInitialized()
        {
            var dbRepaired = RepairImagesDatabaseIfNeeded(_imageLoaderData.entid);
            if (dbRepaired)
            {
                //SaveCachedImages();
            }

            _imageCache = new QueueImageCache(_imageLoaderData.cachedImages, onQueueComplete: SaveCachedImages, onError: LogDownloadError);

            foreach (var image in _imagesToCache)
            {
                CheckCachedOrCache(image);
            }
            
            _imagesToCache.Clear();
            
            foreach (var item in ItemManager.itemList)
            {
                var itemUrl = BuildItemUrl(item.shortname);
                CheckCachedOrCache(itemUrl);
            }
        }

        void Unload()
        {
            _imageCache?.Dispose();
        }
        
        private void OnPlayerInit(BasePlayer player)
        {
            if (_avatarUrls.ContainsKey(player.userID))
                return;
            
            webrequest.Enqueue($"https://steamcommunity.com/profiles/{player.UserIDString}?xml=1", null, 
                (code, response) =>
                {
                    if (response == null || code != 200) 
                        return;
                    
                    var avatarUrl = _avatarRegex.Match(response).Groups[1].ToString();
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        _avatarUrls[player.userID] = avatarUrl;
                    }
                }, this);
        }

        [HookMethod("BuildItemImageComponent")]
        public CuiRawImageComponent BuildItemImageComponent(string shortName)
        {
            var itemUrl = BuildItemUrl(shortName);
            return BuildImageComponent(itemUrl);
        }

        [HookMethod("BuildAvatarImageComponent")]
        public CuiRawImageComponent BuildAvatarImageComponent(ulong userId)
        {
            string steamUrl;
            if (!_avatarUrls.TryGetValue(userId, out steamUrl))
                steamUrl = NoneImageUrl;
            
            return BuildImageComponent(steamUrl);
        }

        [HookMethod("BuildImageComponent")]
        public CuiRawImageComponent BuildImageComponent(string url, string color = "1.0 1.0 1.0 1.0", float FadeIn = 0f)
        {
            if (_imageCache == null)
                throw new Exception("Image cache is not initialized yet");
            
            string imageId;
            return _imageCache.TryGetCachedOrCache(url, out imageId) 
                ? new CuiRawImageComponent {FadeIn = FadeIn, Png = imageId, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"} 
                : new CuiRawImageComponent {FadeIn = FadeIn, Url = url, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
        }

        public bool CheckCachedOrCache(string url)
        {
            if (_imageCache == null)
            {
                _imagesToCache.Enqueue(url);
                return false;
            }
            
            string imageId;
            return _imageCache.TryGetCachedOrCache(url, out imageId);
        }

        private void LogDownloadError(string error)
        {
            // Ignore download errors that always occur 
            if (error.Contains("https://static.moscow.ovh/images/games/rust/icons"))
                return;
            
            Puts(error);
        }

        private void SaveCachedImages()
        {
            Puts("Saving cached images ids");
            _imageLoaderData.entid = CommunityEntity.ServerInstance.net.ID;
            Interface.Oxide.DataFileSystem.WriteObject(DataFileName, _imageLoaderData);
        }
        
        private ImageLoaderData LoadImageLoaderDataFromFile()
        {
            ImageLoaderData imageLoaderData = null;
            
            try
            {
                imageLoaderData = Interface.Oxide.DataFileSystem.ReadObject<ImageLoaderData>(DataFileName);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to read saved cached images: {ex}");
            }
            
            return imageLoaderData ?? new ImageLoaderData();
        }

        private bool RepairImagesDatabaseIfNeeded(uint oldEntId)
        {
            if (oldEntId == 0)
            {
                    Puts("There is no saved cached images, skipping database repair.");
                    return false;
                }
                
                var newEntityId = CommunityEntity.ServerInstance.net.ID;
                if (oldEntId == newEntityId)
                {
                    Puts($"Saved CommunityEntity id equals to database id ({oldEntId}), skipping repair.");
                    return false;
                }
                
                Puts($"Repairing images database. Updating old entid '{oldEntId}' to new '{newEntityId}'");

                var imagesDb = new Database();
                imagesDb.Open($"{ConVar.Server.rootFolder}/sv.files.0.db");
                try
                {
                    imagesDb.Query("UPDATE data SET entid=? WHERE entid=?", newEntityId, oldEntId);
                }
                finally
                {
                    imagesDb.Close();
                }

                return true;
            }

            private static string BuildItemUrl(string shortName)
            {
                return $"https://static.moscow.ovh/images/games/rust/icons/{shortName}.png";
            }
        }
}
