// Reference: System.Drawing

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Plugins.SignArtistClasses;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Sign Artist", "Mughisi", 1.0, ResourceId = 992)]
    [Description("Allows players with the appropriate permission to import images from the internet on paintable objects")]

    /*********************************************************************************
     * This plugin was originally created by Bombardir and then maintained by Nogrod.
     * It was rewritten from scratch by Mughisi on January 12th, 2018.
     *********************************************************************************/

    internal class SignArtist : RustPlugin
    {
        private Dictionary<ulong, float> cooldowns = new Dictionary<ulong, float>();

        private GameObject imageDownloaderGameObject;

        private ImageDownloader imageDownloader;

        public SignArtistConfig Settings { get; private set; }

        public Dictionary<string, ImageSize> ImageSizePerAsset { get; private set; }

        /// <summary>
        /// Plugin configuration
        /// </summary>
        public class SignArtistConfig
        {
            [JsonProperty(PropertyName = "Time in seconds between download requests (0 to disable)")]
            public int Cooldown { get; set; }

            [JsonProperty(PropertyName = "Maximum concurrent downloads")]
            public int MaxActiveDownloads { get; set; }

            [JsonProperty(PropertyName = "Maximum distance from the sign")]
            public int MaxDistance { get; set; }

            [JsonProperty(PropertyName = "Maximum filesize in MB")]
            public float MaxSize { get; set; }

            [JsonProperty("Enable logging file")]
            public bool FileLogging { get; set; }

            [JsonProperty("Enable logging console")]
            public bool ConsoleLogging { get; set; }

            [JsonIgnore]
            public float MaxFileSizeInBytes
            {
                get
                {
                    return MaxSize * 1024 * 1024;
                }
            }

            /// <summary>
            /// Creates a default configuration file
            /// </summary>
            /// <returns>Default config</returns>
            public static SignArtistConfig DefaultConfig()
            {
                return new SignArtistConfig
                {
                    Cooldown = 0,
                    MaxSize = 1,
                    MaxDistance = 3,
                    MaxActiveDownloads = 5,
                    FileLogging = false,
                    ConsoleLogging = false
                };
            }
        }

        /// <summary>
        /// A type used to request new images to download.
        /// </summary>
        private class DownloadRequest
        {
            public BasePlayer Sender { get; }

            public Signage Sign { get; }

            public string Url { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DownloadRequest" /> class.
            /// </summary>
            /// <param name="url">The URL to download the image from. </param>
            /// <param name="player">The player that requested the download. </param>
            /// <param name="sign">The sign to add the image to. </param>
            public DownloadRequest(string url, BasePlayer player, Signage sign)
            {
                Url = url;
                Sender = player;
                Sign = sign;
            }
        }

        /// <summary>
        /// A type used to request new images to be restored.
        /// </summary>
        private class RestoreRequest
        {
            public BasePlayer Sender { get; }

            public Signage Sign { get; }
            
            /// <summary>
            /// Initializes a new instance of the <see cref="RestoreRequest" /> class.
            /// </summary>
            /// <param name="player">The player that requested the restore. </param>
            /// <param name="sign">The sign to restore the image from. </param>
            public RestoreRequest(BasePlayer player, Signage sign)
            {
                Sender = player;
                Sign = sign;
            }
        }

        /// <summary>
        /// A type used to determine the size of the image for a sign
        /// </summary>
        public class ImageSize
        {
            public int Width { get; }

            public int Height { get; }

            public int ImageWidth { get; }

            public int ImageHeight { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageSize" /> class.
            /// </summary>
            /// <param name="width">The width of the canvas and the image. </param>
            /// <param name="height">The height of the canvas and the image. </param>
            public ImageSize(int width, int height) : this(width, height, width, height)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageSize" /> class.
            /// </summary>
            /// <param name="width">The width of the canvas. </param>
            /// <param name="height">The height of the canvas. </param>
            /// <param name="imageWidth">The width of the image. </param>
            /// <param name="imageHeight">The height of the image. </param>
            public ImageSize(int width, int height, int imageWidth, int imageHeight)
            {
                Width = width;
                Height = height;
                ImageWidth = imageWidth;
                ImageHeight = imageHeight;
            }
        }

        /// <summary>
        /// UnityEngine script to be attached to a GameObject to download images and apply them to signs.
        /// </summary>
        private class ImageDownloader : MonoBehaviour
        {
            private byte activeDownloads;

            private byte activeRestores;

            private readonly SignArtist signArtist = (SignArtist)Interface.Oxide.RootPluginManager.GetPlugin(nameof(SignArtist));

            private readonly Queue<DownloadRequest> downloadQueue = new Queue<DownloadRequest>();

            private readonly Queue<RestoreRequest> restoreQueue = new Queue<RestoreRequest>();

            /// <summary>
            /// Queue a new image to download and add to a sign
            /// </summary>
            /// <param name="url">The URL to download the image from. </param>
            /// <param name="player">The player that requested the download. </param>
            /// <param name="sign">The sign to add the image to. </param>
            public void Queue(string url, BasePlayer player, Signage sign)
            {
                // Instantiate a new DownloadRequest and add it to the queue.
                downloadQueue.Enqueue(new DownloadRequest(url, player, sign));

                // Attempt to start the next download.
                StartNextDownload();
            }

            /// <summary>
            /// Attempts to restore a sign.
            /// </summary>
            /// <param name="player">The player that requested the restore. </param>
            /// <param name="sign">The sign to restore the image from. </param>
            public void QueueRestore(BasePlayer player, Signage sign)
            {
                // Instantiate a new RestoreRequest and add it to the queue.
                restoreQueue.Enqueue(new RestoreRequest(player, sign));

                // Attempt to start the next restore.
                StartNextRestore();
            }

            /// <summary>
            /// Starts the next download if available.
            /// </summary>
            /// <param name="reduceCount"></param>
            private void StartNextDownload(bool reduceCount = false)
            {
                // Check if we need to reduce the active downloads counter after a succesful or failed download.
                if (reduceCount)
                {
                    activeDownloads--;
                }

                // Check if we don't have the maximum configured amount of downloads running already.
                if (activeDownloads >= signArtist.Settings.MaxActiveDownloads)
                {
                    return;
                }

                // Check if there is still an image in the queue.
                if (downloadQueue.Count <= 0)
                {
                    return;
                }

                // Increment the active downloads by 1 and start the download process.
                activeDownloads++;
                StartCoroutine(DownloadImage(downloadQueue.Dequeue()));
            }

            /// <summary>
            /// Starts the next restore if available.
            /// </summary>
            /// <param name="reduceCount"></param>
            private void StartNextRestore(bool reduceCount = false)
            {
                // Check if we need to reduce the active restores counter after a succesful or failed restore.
                if (reduceCount)
                {
                    activeRestores--;
                }

                // Check if we don't have the maximum configured amount of restores running already.
                if (activeRestores >= signArtist.Settings.MaxActiveDownloads)
                {
                    return;
                }

                // Check if there is still an image in the queue.
                if (restoreQueue.Count <= 0)
                {
                    return;
                }

                // Increment the active restores by 1 and start the restore process.
                activeRestores++;
                StartCoroutine(RestoreImage(restoreQueue.Dequeue()));
            }

            /// <summary>
            /// Downloads the image and adds it to the sign.
            /// </summary>
            /// <param name="request">The requested <see cref="DownloadRequest"/> instance. </param>
            private IEnumerator DownloadImage(DownloadRequest request)
            {
                using (WWW www = new WWW(request.Url))
                {
                    // Wait for the webrequest to complete
                    yield return www;

                    // Verify that there is a valid reference to the plugin from this class.
                    if (signArtist == null)
                    {
                        throw new NullReferenceException("signArtist");
                    }

                    // Verify that the webrequest was succesful.
                    if (www.error != null)
                    {
                        // The webrequest wasn't succesful, show a message to the player and attempt to start the next download.
                        signArtist.SendMessage(request.Sender, "WebErrorOccurred", www.error);
                        StartNextDownload(true);

                        yield break;
                    }

                    // Verify that the file doesn't exceed the maximum configured filesize.
                    if (www.bytesDownloaded > signArtist.Settings.MaxFileSizeInBytes)
                    {
                        signArtist.Puts($"1st check, {www.bytesDownloaded}");
                        // The file is too large, show a message to the player and attempt to start the next download.
                        signArtist.SendMessage(request.Sender, "FileTooLarge", signArtist.Settings.MaxSize);
                        StartNextDownload(true);

                        yield break;
                    }

                    // Get the bytes array for the image from the webrequest and lookup the target image size for the targetted sign.
                    byte[] imageBytes = GetImageBytes(www);
                    ImageSize size = GetImageSizeFor(request.Sign);

                    // Verify that we have image size data for the targetted sign.
                    if (size == null)
                    {
                        // No data was found, show a message to the player and print a detailed message to the server console and attempt to start the next download.
                        signArtist.SendMessage(request.Sender, "ErrorOccurred");
                        signArtist.PrintWarning($"Couldn't find the required image size for {request.Sign.PrefabName}, please report this in the plugin's thread.");
                        StartNextDownload(true);

                        yield break;
                    }

                    // Get the bytes array for the resized image for the targetted sign.
                    byte[] resizedImageBytes = imageBytes.ResizeImage(size.Width, size.Height, size.ImageWidth, size.ImageHeight);

                    // Verify that the resized file doesn't exceed the maximum configured filesize.
                    if (resizedImageBytes.Length > signArtist.Settings.MaxFileSizeInBytes)
                    {
                        signArtist.Puts($"2nd check, {resizedImageBytes.Length}");
                        // The file is too large, show a message to the player and attempt to start the next download.
                        signArtist.SendMessage(request.Sender, "FileTooLarge", signArtist.Settings.MaxSize);
                        StartNextDownload(true);

                        yield break;
                    }

                    // Check if the sign already has a texture assigned to it.
                    if (request.Sign.textureID > 0)
                    {
                        // A texture was already assigned, remove this file to make room for the new one.
                        FileStorage.server.Remove(request.Sign.textureID, FileStorage.Type.png, request.Sign.net.ID);
                    }

                    // Create the image on the filestorage and send out a network update for the sign.
                    request.Sign.textureID = FileStorage.server.Store(resizedImageBytes, FileStorage.Type.png, request.Sign.net.ID);
                    request.Sign.SendNetworkUpdate();

                    // Notify the player that the image was loaded.
                    signArtist.SendMessage(request.Sender, "ImageLoaded");

                    // Call the Oxide hook 'OnSignUpdated' to notify other plugins of the update event.
                    Interface.Oxide.CallHook("OnSignUpdated", request.Sign, request.Sender);

                    // Check if logging to console is enabled.
                    if (signArtist.Settings.ConsoleLogging)
                    {
                        // Console logging is enabled, show a message in the server console.
                        signArtist.Puts(signArtist.GetTranslation("LogEntry"), request.Sender.displayName,
                            request.Sender.userID, request.Sign.textureID, request.Sign.ShortPrefabName, request.Url);
                    }

                    // Check if logging to file is enabled.
                    if (signArtist.Settings.FileLogging)
                    {
                        // File logging is enabled, add an entry to the logfile.
                        signArtist.LogToFile("log",
                            string.Format(signArtist.GetTranslation("LogEntry"), request.Sender.displayName,
                                request.Sender.userID, request.Sign.textureID, request.Sign.ShortPrefabName,
                                request.Url), signArtist);
                    }

                    // Attempt to start the next download.
                    StartNextDownload(true);
                }
            }

            /// <summary>
            /// Restores the image and adds it to the sign again.
            /// </summary>
            /// <param name="request">The requested <see cref="RestoreRequest"/> instance. </param>
            /// <returns></returns>
            private IEnumerator RestoreImage(RestoreRequest request)
            {
                // Verify that there is a valid reference to the plugin from this class.
                if (signArtist == null)
                {
                    throw new NullReferenceException("signArtist");
                }

                byte[] imageBytes;

                // Check if the sign already has a texture assigned to it.
                if (request.Sign.textureID == 0)
                {
                    // No texture was previously assigned, show a message to the player.
                    signArtist.SendMessage(request.Sender, "RestoreErrorOccurred");

                    yield break;
                }

                // Cache the byte array of the currently stored file.
                imageBytes = FileStorage.server.Get(request.Sign.textureID, FileStorage.Type.png, request.Sign.net.ID);
                ImageSize size = GetImageSizeFor(request.Sign);

                // Verify that we have image size data for the targetted sign.
                if (size == null)
                {
                    // No data was found, show a message to the player and print a detailed message to the server console and attempt to start the next download.
                    signArtist.SendMessage(request.Sender, "ErrorOccurred");
                    signArtist.PrintWarning($"Couldn't find the required image size for {request.Sign.PrefabName}, please report this in the plugin's thread.");
                    StartNextRestore(true);

                    yield break;
                }

                // Remove the texture from the FileStorage.
                FileStorage.server.Remove(request.Sign.textureID, FileStorage.Type.png, request.Sign.net.ID);

                // Get the bytes array for the resized image for the targetted sign.
                byte[] resizedImageBytes = imageBytes.ResizeImage(size.Width, size.Height, size.ImageWidth, size.ImageHeight);

                // Create the image on the filestorage and send out a network update for the sign.
                request.Sign.textureID = FileStorage.server.Store(resizedImageBytes, FileStorage.Type.png, request.Sign.net.ID);
                request.Sign.SendNetworkUpdate();

                // Notify the player that the image was loaded.
                signArtist.SendMessage(request.Sender, "ImageRestored");

                // Call the Oxide hook 'OnSignUpdated' to notify other plugins of the update event.
                Interface.Oxide.CallHook("OnSignUpdated", request.Sign, request.Sender);

                // Attempt to start the next download.
                StartNextRestore(true);
            }

            /// <summary>
            /// Gets the target image size for a <see cref="Signage"/>.
            /// </summary>
            /// <param name="signage"></param>
            private ImageSize GetImageSizeFor(Signage signage)
            {
                if (signArtist.ImageSizePerAsset.ContainsKey(signage.PrefabName))
                {
                    return signArtist.ImageSizePerAsset[signage.PrefabName];
                }

                return null;
            }

            /// <summary>
            /// Converts the <see cref="Texture2D"/> from the webrequest to a <see cref="byte"/> array.
            /// </summary>
            /// <param name="www">The completed webrequest. </param>
            private byte[] GetImageBytes(WWW www)
            {
                Texture2D texture = www.texture;
                byte[] image = texture.EncodeToPNG();

                DestroyImmediate(texture);

                return image;
            }
        }

        /// <summary>
        /// Oxide hook that is triggered when the plugin is loaded.
        /// </summary>
        private void Init()
        {
            // Register all the permissions used by the plugin
            permission.RegisterPermission("signartist.ignorecd", this);
            permission.RegisterPermission("signartist.ignoreowner", this);
            permission.RegisterPermission("signartist.url", this);
            permission.RegisterPermission("signartist.restore", this);

            // Initialize the dictionary with all paintable object assets and their target sizes
            ImageSizePerAsset = new Dictionary<string, ImageSize>()
            {
                // Picture Frames
                ["assets/prefabs/deployable/signs/sign.pictureframe.landscape.prefab"] = new ImageSize(256, 128), // Landscape Picture Frame
                ["assets/prefabs/deployable/signs/sign.pictureframe.portrait.prefab"] = new ImageSize(128, 256),  // Portrait Picture Frame
                ["assets/prefabs/deployable/signs/sign.pictureframe.tall.prefab"] = new ImageSize(128, 512),      // Tall Picture Frame
                ["assets/prefabs/deployable/signs/sign.pictureframe.xl.prefab"] = new ImageSize(512, 512),        // XL Picture Frame
                ["assets/prefabs/deployable/signs/sign.pictureframe.xxl.prefab"] = new ImageSize(1024, 512),      // XXL Picture Frame

                // Wooden Signs
                ["assets/prefabs/deployable/signs/sign.small.wood.prefab"] = new ImageSize(128, 64),              // Small Wooden Sign
                ["assets/prefabs/deployable/signs/sign.medium.wood.prefab"] = new ImageSize(256, 128),            // Wooden Sign
                ["assets/prefabs/deployable/signs/sign.large.wood.prefab"] = new ImageSize(256, 128),             // Large Wooden Sign
                ["assets/prefabs/deployable/signs/sign.huge.wood.prefab"] = new ImageSize(512, 128),              // Huge Wooden Sign

                // Banners
                ["assets/prefabs/deployable/signs/sign.hanging.banner.large.prefab"] = new ImageSize(64, 256),    // Large Banner Hanging
                ["assets/prefabs/deployable/signs/sign.pole.banner.large.prefab"] = new ImageSize(64, 256),       // Large Banner on Pole

                // Hanging Signs
                ["assets/prefabs/deployable/signs/sign.hanging.prefab"] = new ImageSize(128, 256),                // Two Sided Hanging Sign
                ["assets/prefabs/deployable/signs/sign.hanging.ornate.prefab"] = new ImageSize(256, 128),         // Two Sided Ornate Hanging Sign

                // Town Signs
                ["assets/prefabs/deployable/signs/sign.post.single.prefab"] = new ImageSize(128, 64),             // Single Sign Post
                ["assets/prefabs/deployable/signs/sign.post.double.prefab"] = new ImageSize(256, 256),            // Double Sign Post
                ["assets/prefabs/deployable/signs/sign.post.town.prefab"] = new ImageSize(256, 128),              // One Sided Town Sign Post
                ["assets/prefabs/deployable/signs/sign.post.town.roof.prefab"] = new ImageSize(256, 128),         // Two Sided Town Sign Post

                // Other paintable assets
                ["assets/prefabs/deployable/spinner_wheel/spinner.wheel.deployed.prefab"] = new ImageSize(512, 512, 285, 285), // Spinning Wheel
            };
        }

        /// <summary>
        /// Oxide hook that is triggered automatically after it has been loaded to initialize the messages for the Lang API.
        /// </summary>
        protected override void LoadDefaultMessages()
        {
            // Register all messages used by the plugin in the Lang API.
            lang.RegisterMessages(new Dictionary<string, string>
            {
                // Messages used throughout the plugin.
                ["WebErrorOccurred"] = "Failed to download the image! Error {0}.",
                ["FileTooLarge"] = "The file exceeds the maximum file size of {0}Mb.",
                ["ErrorOccurred"] = "An unknown error has occured, if this error keeps occuring please notify the server admin.",
                ["RestoreErrorOccurred"] = "Can't restore the sign because no texture is assigned to it.",
                ["DownloadQueued"] = "Your image was added to the download queue!",
                ["RestoreQueued"] = "Your sign was added to the restore queue!",
                ["ImageLoaded"] = "The image was succesfully loaded to the sign!",
                ["ImageRestored"] = "The image was succesfully restored for the sign!",
                ["LogEntry"] = "Player `{0}` (SteamId: {1}) loaded {2} into {3} from {4}",
                ["NoSignFound"] = "Unable to find a sign! Make sure you are looking at one and that you are not too far away from it.",
                ["Cooldown"] = "You can't use the command yet! Remaining cooldown: {0}.",
                ["SignNotOwned"] = "You can't change this sign as it is protected by a tool cupboard.",
                ["SyntaxError"] = "Syntax error!\nSyntax: /sil <url>",
                ["NoPermission"] = "You don't have permission to use this command.",

                // Cooldown formatting 'translations'.
                ["day"] = "day",
                ["days"] = "days",
                ["hour"] = "hour",
                ["hours"] = "hours",
                ["minute"] = "minute",
                ["minutes"] = "minutes",
                ["second"] = "second",
                ["seconds"] = "seconds",
                ["and"] = "and"
            }, this);
        }

        /// <summary>
        /// Oxide hook that is triggered to automatically load the configuration file.
        /// </summary>
        protected override void LoadConfig()
        {
            base.LoadConfig();
            Settings = Config.ReadObject<SignArtistConfig>();
        }

        /// <summary>
        /// Oxide hook that is triggered to automatically load the default configuration file when no file exists.
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Settings = SignArtistConfig.DefaultConfig();
        }

        /// <summary>
        /// Oxide hook that is triggered to save the configuration file.
        /// </summary>
        protected override void SaveConfig()
        {
            Config.WriteObject(Settings);
        }

        /// <summary>
        /// Oxide hook that is triggered when the server has fully initialized.
        /// </summary>
        private void OnServerInitialized()
        {
            // Create a new GameObject and attach the UnityEngine script to it for handling the image downloads.
            imageDownloaderGameObject = new GameObject("ImageDownloader");
            imageDownloader = imageDownloaderGameObject.AddComponent<ImageDownloader>();
        }

        /// <summary>
        /// Oxide hook that is triggered when the plugin is unloaded.
        /// </summary>
        private void Unload()
        {
            // Destroy the created GameObject and cleanup.
            UnityEngine.Object.Destroy(imageDownloaderGameObject);
            imageDownloader = null;
            cooldowns = null;
        }

        /// <summary>
        /// Handles the /sil chat command.
        /// </summary>
        /// <param name="player">The player that has executed the command. </param>
        /// <param name="command">The name of the command that was executed. </param>
        /// <param name="args">All arguments that were passed with the command. </param>
        [ChatCommand("sil")]
        private void SilChatCommand(BasePlayer player, string command, string[] args)
        {
            // Verify if the correct syntax is used.
            if (args.Length != 1)
            {
                // Invalid syntax was used, show an error message to the player.
                SendMessage(player, "SyntaxError");

                return;
            }

            // Verify if the player has permission to use this command.
            if (!HasPermission(player, "signartist.url"))
            {
                // The player doesn't have permission to use this command, show an error message.
                SendMessage(player, "NoPermission");

                return;
            }

            // Verify that the command isn't on cooldown for the user.
            if (HasCooldown(player))
            {
                // The command is still on cooldown for the player, show an error message.
                SendMessage(player, "Cooldown", FormatCooldown(GetCooldown(player)));

                return;
            }

            // Check if the player is looking at a sign.
            Signage sign;
            if (!IsLookingAtSign(player, out sign))
            {
                // The player isn't looking at a sign or is too far away from it, show an error message.
                SendMessage(player, "NoSignFound");

                return;
            }

            // Check if the player is able to update the sign.
            if (!CanChangeSign(player, sign))
            {
                // The player isn't able to update the sign, show an error message.
                SendMessage(player, "SignNotOwned");

                return;
            }

            // Notify the player that it is added to the queue.
            SendMessage(player, "DownloadQueued");

            // Queue the download of the specified image.
            imageDownloader.Queue(args[0], player, sign);

            // Set the cooldown on the command for the player if the cooldown setting is enabled.
            SetCooldown(player);
        }

        /// <summary>
        /// Handles the sil console command
        /// </summary>
        /// <param name="arg"><see cref="ConsoleSystem.Arg"/> running the command. </param>
        [ConsoleCommand("sil")]
        private void SilConsoleCommand(ConsoleSystem.Arg arg)
        {
            // Verify that the command was run from an ingame console.
            if (arg.Player() == null)
            {
                // It wasn't run from an ingame console, do nothing.
                return;
            }

            // Manually trigger the chat command with the console command args.
            SilChatCommand(arg.Player(), "sil", arg.Args ?? new string[0]);
        }

        [ChatCommand("silrestore")]
        private void RestoreCommand(BasePlayer player, string command, string[] args)
        {
            // Verify if the player has permission to use this command.
            if (!HasPermission(player, "signartist.restore"))
            {
                // The player doesn't have permission to use this command, show an error message.
                SendMessage(player, "NoPermission");

                return;
            }

            // Check if the player is looking at a sign.
            Signage sign;
            if (!IsLookingAtSign(player, out sign))
            {
                // The player isn't looking at a sign or is too far away from it, show an error message.
                SendMessage(player, "NoSignFound");

                return;
            }

            // Notify the player that it is added to the queue.
            SendMessage(player, "RestoreQueued");

            // Queue the restore of the image on the specified sign.
            imageDownloader.QueueRestore(player, sign);
        }

        /// <summary>
        /// Check if the given <see cref="BasePlayer"/> is able to use the command.
        /// </summary>
        /// <param name="player">The player to check. </param>
        private bool HasCooldown(BasePlayer player)
        {
            // Check if cooldown is enabled.
            if (Settings.Cooldown <= 0)
            {
                return false;
            }

            // Check if cooldown is ignored for the player.
            if (HasPermission(player, "signartist.ignorecd"))
            {
                return false;
            }

            // Make sure there is an entry for the player in the dictionary.
            if (!cooldowns.ContainsKey(player.userID))
            {
                cooldowns.Add(player.userID, 0);
            }

            // Check if the command is on cooldown or not.
            return Time.realtimeSinceStartup - cooldowns[player.userID] < Settings.Cooldown;
        }

        /// <summary>
        /// Returns the cooldown in seconds for the given <see cref="BasePlayer"/>.
        /// </summary>
        /// <param name="player">The player to obtain the cooldown of. </param>
        private float GetCooldown(BasePlayer player)
        {
            return Time.realtimeSinceStartup - cooldowns[player.userID];
        }

        /// <summary>
        /// Sets the last use for the cooldown handling of the command for the given <see cref="BasePlayer"/>.
        /// </summary>
        /// <param name="player">The player to put the command on cooldown for. </param>
        private void SetCooldown(BasePlayer player)
        {
            // Check if cooldown is enabled.
            if (Settings.Cooldown <= 0)
            {
                return;
            }

            // Check if cooldown is ignored for the player.
            if (HasPermission(player, "signartist.ignorecd"))
            {
                return;
            }

            // Make sure there is an entry for the player in the dictionary.
            if (!cooldowns.ContainsKey(player.userID))
            {
                cooldowns.Add(player.userID, 0);
            }

            // Set the last use
            cooldowns[player.userID] = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Returns a formatted string for the given cooldown.
        /// </summary>
        /// <param name="seconds">The cooldown in seconds. </param>
        private string FormatCooldown(float seconds)
        {
            // Create a new TimeSpan from the remaining cooldown.
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            List<string> output = new List<string>();

            // Check if it is more than a single day and add it to the result.
            if (t.Days >= 1)
            {
                output.Add($"{t.Days} {(t.Days > 1 ? "days" : "day")}");
            }

            // Check if it is more than an hour and add it to the result.
            if (t.Hours >= 1)
            {
                output.Add($"{t.Hours} {(t.Hours > 1 ? "hours" : "hour")}");
            }

            // Check if it is more than a minute and add it to the result.
            if (t.Minutes >= 1)
            {
                output.Add($"{t.Minutes} {(t.Minutes > 1 ? "minutes" : "minute")}");
            }

            // Check if there is more than a second and add it to the result.
            if (t.Seconds >= 1)
            {
                output.Add($"{t.Seconds} {(t.Seconds > 1 ? "seconds" : "second")}");
            }

            // Format the result and return it.
            return output.Count >= 3 ? output.ToSentence().Replace(" and", ", and") : output.ToSentence();
        }

        /// <summary>
        /// Checks if the <see cref="BasePlayer"/> is looking at a valid <see cref="Signage"/> object.
        /// </summary>
        /// <param name="player">The player to check. </param>
        /// <param name="sign">When this method returns, contains the <see cref="Signage"/> the player contained in <paramref name="player" /> is looking at, or null if the player isn't looking at a sign. </param>
        private bool IsLookingAtSign(BasePlayer player, out Signage sign)
        {
            RaycastHit hit;
            sign = null;

            // Get the object that is in front of the player within the maximum distance set in the config.
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Settings.MaxDistance))
            {
                // Attempt to grab the Signage component, if there is none this will set the sign to null, 
                // otherwise this will set it to the sign the player is looking at.
                sign = hit.transform.GetComponentInParent<Signage>();
            }

            // Return true or false depending on if we found a sign.
            return sign != null;
        }

        /// <summary>
        /// Checks if the <see cref="BasePlayer"/> is allowed to change the drawing on the <see cref="Signage"/> object.
        /// </summary>
        /// <param name="player">The player to check. </param>
        /// <param name="sign">The sign to check. </param>
        /// <returns></returns>
        private bool CanChangeSign(BasePlayer player, Signage sign)
        {
            return sign.CanUpdateSign(player) || HasPermission(player, "signartist.ignoreowner");
        }

        /// <summary>
        /// Checks if the given <see cref="BasePlayer"/> has the specified permission.
        /// </summary>
        /// <param name="player">The player to check a permission on. </param>
        /// <param name="perm">The permission to check for. </param>
        private bool HasPermission(BasePlayer player, string perm)
        {
            return permission.UserHasPermission(player.UserIDString, perm);
        }

        /// <summary>
        /// Send a formatted message to a single player.
        /// </summary>
        /// <param name="player">The player to send the message to. </param>
        /// <param name="key">The key of the message from the Lang API to get the message for. </param>
        /// <param name="args">Any amount of arguments to add to the message. </param>
        private void SendMessage(BasePlayer player, string key, params object[] args)
        {
            player.ChatMessage(string.Format(GetTranslation(key, player), args));
        }

        /// <summary>
        /// Gets the message for a specific player from the Lang API.
        /// </summary>
        /// <param name="key">The key of the message from the Lang API to get the message for. </param>
        /// <param name="player">The player to get the message for. </param>
        /// <returns></returns>
        private string GetTranslation(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player?.UserIDString);
        }
    }

    namespace SignArtistClasses
    {
        /// <summary>
        /// Extension class with extension methods used by the <see cref="SignArtist"/> plugin.
        /// </summary>
        public static class Extensions
        {
            /// <summary>
            /// Resizes an image from the <see cref="byte"/> array to a new image with a specific width and height.
            /// </summary>
            /// <param name="bytes">Source image. </param>
            /// <param name="width">New image canvas width. </param>
            /// <param name="height">New image canvas height. </param>
            /// <param name="targetWidth">New image width. </param>
            /// <param name="targetHeight">New image height. </param>
            public static byte[] ResizeImage(this byte[] bytes, int width, int height, int targetWidth, int targetHeight)
            {
                byte[] resizedImageBytes;

                using (MemoryStream originalBytesStream = new MemoryStream(), resizedBytesStream = new MemoryStream())
                {
                    originalBytesStream.Write(bytes, 0, bytes.Length);
                    Bitmap image = new Bitmap(originalBytesStream);

                    if (image.Width != width || image.Height != height)
                    {
                        Bitmap resizedImage = new Bitmap(width, height);

                        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
                        {
                            graphics.DrawImage(image, new Rectangle(0, 0, targetWidth, targetHeight));
                        }

                        resizedImage.Save(resizedBytesStream, ImageFormat.Png);
                        resizedImageBytes = resizedBytesStream.ToArray();
                    }
                    else
                    {
                        resizedImageBytes = bytes;
                    }
                }

                return resizedImageBytes;
            }
        }
    }
}
