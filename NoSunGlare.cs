using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Sun Glare", "Tryhard", "2.0.0")]
    [Description("Removes sun or sun glare")]
    public class NoSunGlare : RustPlugin
    {
        private WeatherConfig _config;

        #region Configuration

        public class WeatherConfig
        {
            [JsonProperty(PropertyName = "Clouds")]
            public float Clouds { get; set; } = 1;

            [JsonProperty(PropertyName = "Cloud Opacity")]
            public float CloudOpacity { get; set; } = 0.97f;

            [JsonProperty(PropertyName = "Cloud Brightness")]
            public float CloudBrightness { get; set; } = 1.5f;

            [JsonProperty(PropertyName = "Cloud Coloring")]
            public int CloudColoring { get; set; } = 0;

            [JsonProperty(PropertyName = "Cloud Saturation")]
            public int CloudSaturation { get; set; } = 1;

            [JsonProperty(PropertyName = "Cloud Scattering")]
            public int CloudScattering { get; set; } = 0;

            [JsonProperty(PropertyName = "Cloud Sharpness")]
            public int CloudSharpness { get; set; } = 0;

            [JsonProperty(PropertyName = "Cloud Size")]
            public int CloudSize { get; set; } = 0;

            [JsonProperty(PropertyName = "Cloud Coverage")]
            public int CloudCoverage { get; set; } = 1;

            [JsonProperty(PropertyName = "Cloud Attenuation")]
            public int CloudAttenuation { get; set; } = -1;

            [JsonProperty(PropertyName = "Wind")]
            public float Wind { get; set; } = 0;

            [JsonProperty(PropertyName = "Rain")]
            public float Rain { get; set; } = 0;

            [JsonProperty(PropertyName = "Fog")]
            public float Fog { get; set; } = 0;

            [JsonProperty(PropertyName = "Fogginess")]
            public float Fogginess { get; set; } = 0;

            [JsonProperty(PropertyName = "Dust Chance")]
            public float DustChance { get; set; } = 0;

            [JsonProperty(PropertyName = "Fog Chance")]
            public float FogChance { get; set; } = 0;

            [JsonProperty(PropertyName = "Overcast Chance")]
            public float OvercastChance { get; set; } = 0;

            [JsonProperty(PropertyName = "Storm Chance")]
            public float StormChance { get; set; } = 0;

            [JsonProperty(PropertyName = "Clear Chance")]
            public float ClearChance { get; set; } = 1;

            [JsonProperty(PropertyName = "Atmosphere Contrast")]
            public float AtmosphereContrast { get; set; } = 1.2f;

            [JsonProperty(PropertyName = "Atmosphere Directionality")]
            public float AtmosphereDirectionality { get; set; } = 0;

            [JsonProperty(PropertyName = "Atmosphere Mie")]
            public float AtmosphereMie { get; set; } = 0;

            [JsonProperty(PropertyName = "Atmosphere Rayleigh")]
            public float AtmosphereRayleigh { get; set; } = 1.3f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<WeatherConfig>();

                if (_config == null) LoadDefaultConfig();
            }

            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new WeatherConfig();

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        private void OnServerInitialized()
        {
            SetWeatherParameters(
                _config.Clouds,
                _config.CloudOpacity,
                _config.CloudBrightness,
                _config.CloudColoring,
                _config.CloudSaturation,
                _config.CloudScattering,
                _config.CloudSharpness,
                _config.CloudSize,
                _config.CloudCoverage,
                _config.CloudAttenuation,
                _config.Wind,
                _config.Rain,
                _config.Fog,
                _config.Fogginess,
                _config.DustChance,
                _config.FogChance,
                _config.OvercastChance,
                _config.StormChance,
                _config.ClearChance,
                _config.AtmosphereContrast,
                _config.AtmosphereDirectionality,
                _config.AtmosphereMie,
                _config.AtmosphereRayleigh
            );
        }

        private void SetWeatherParameters(
            float clouds,
            float cloudOpacity,
            float cloudBrightness,
            int cloudColoring,
            int cloudSaturation,
            int cloudScattering,
            int cloudSharpness,
            int cloudSize,
            int cloudCoverage,
            int cloudAttenuation,
            float wind,
            float rain,
            float fog,
            float fogginess,
            float dustChance,
            float fogChance,
            float overcastChance,
            float stormChance,
            float clearChance,
            float atmosphereContrast,
            float atmosphereDirectionality,
            float atmosphereMie,
            float atmosphereRayleigh)
        {
            var climate = SingletonComponent<Climate>.Instance;

            climate.Overrides.Clouds = clouds;
            climate.WeatherOverrides.Clouds.Opacity = cloudOpacity;
            climate.WeatherOverrides.Clouds.Brightness = cloudBrightness;
            climate.WeatherOverrides.Clouds.Coloring = cloudColoring;
            climate.WeatherOverrides.Clouds.Saturation = cloudSaturation;
            climate.WeatherOverrides.Clouds.Scattering = cloudScattering;
            climate.WeatherOverrides.Clouds.Sharpness = cloudSharpness;
            climate.WeatherOverrides.Clouds.Size = cloudSize;
            climate.WeatherOverrides.Clouds.Coverage = cloudCoverage;
            climate.WeatherOverrides.Clouds.Attenuation = cloudAttenuation;
            climate.Overrides.Wind = wind;
            climate.Overrides.Rain = rain;
            climate.Overrides.Fog = fog;
            climate.WeatherOverrides.Atmosphere.Fogginess = fogginess;
            climate.Weather.DustChance = dustChance;
            climate.Weather.FogChance = fogChance;
            climate.Weather.OvercastChance = overcastChance;
            climate.Weather.StormChance = stormChance;
            climate.Weather.ClearChance = clearChance;
            climate.WeatherOverrides.Atmosphere.Contrast = atmosphereContrast;
            climate.WeatherOverrides.Atmosphere.Directionality = atmosphereDirectionality;
            climate.WeatherOverrides.Atmosphere.MieMultiplier = atmosphereMie;
            climate.WeatherOverrides.Atmosphere.RayleighMultiplier = atmosphereRayleigh;

            ServerMgr.SendReplicatedVars("weather.");
        }
    }
}