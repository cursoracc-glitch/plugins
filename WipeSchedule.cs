using System;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("WipeSchedule", "Sempai#3239", "1.0.0")]
    class WipeSchedule : RustPlugin
    {
        #region Вар
        string Layer = "Wipe_UI";

        DateTime NextWipeDate;
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            public string NameServer = "EU 10X NO BPS";
            public string Description = "Server updates @ <color=#db8c5a>store.хуита.ru</color>";
            public string Day = "SATURDAY\n<color=#db8c5a>12 PM CET</color>";
            public string Day2 = "WEDNESDAY\n<color=#db8c5a>12 PM CET</color>";
            public string LastWipe = DateTime.Now.ToString("dd/MM/yyyy");
            public string NextWipe = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy");
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                };
            }
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.NextWipe == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region 
        void OnNewSave()
        {
            config.LastWipe = DateTime.Now.Date.ToString("dd/MM/yyyy");
            config.NextWipe = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy");
            SaveConfig();
        }
        void OnServerInitialized()
        {
            LoadWipeDates();
        }
        #endregion

        #region Интерфейс
        void WipeUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.284 0", AnchorMax = "0.952 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.6" },
            }, "Menu", Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.032 0.893", AnchorMax = $"0.347 0.954", OffsetMax = "0 0" },
                Image = { Color = "0.86 0.55 0.35 1" }
            }, Layer, "Title");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"WIPE SCHEDULE", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-bold.ttf" }
            }, "Title");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.36 0.893", AnchorMax = $"0.97 0.954", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = config.Description, Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Description");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.75", AnchorMax = $"0.97 0.86", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Name");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = config.NameServer, Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Name");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.5", AnchorMax = $"0.495 0.73", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Day");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = config.Day, Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-bold.ttf" }
            }, "Day");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.505 0.5", AnchorMax = $"0.97 0.73", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Day2");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = config.Day2, Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-bold.ttf" }
            }, "Day2");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.35", AnchorMax = $"0.97 0.48", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "WipeDay");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"THIS SERVER WILL WIPE IN:\n{NextWipeDays(NextWipeDate)}", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-regular.ttf" }
            }, "WipeDay");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        void LoadWipeDates()
        {
            NextWipeDate = ParseTime(config.NextWipe);
        }

        string NextWipeDays(DateTime WipeDate)
        {
            TimeSpan time = WipeDate.Subtract(DateTime.Now);
            return string.Format(FormatTime(TimeSpan.FromDays(time.TotalDays)));
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "DAYS", "OF THE DAY", "DAY")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "HOURS", "HORS'S", "HOUR")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "MINUTES", "MINUTES", "MINUTE")} ";
            
            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        DateTime ParseTime(string time) => DateTime.ParseExact(time, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        #endregion
    }
}