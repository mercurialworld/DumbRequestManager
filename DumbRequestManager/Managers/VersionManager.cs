using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DumbRequestManager.UI;
using IPA.Utilities.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DumbRequestManager.Managers
{
    public class ManifestData
    {
        [JsonProperty("gameVersion")]
        public string? GameVersion { get; private set; } = string.Empty;
    }
    
    internal class VersionManager
    {
        private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("https://theblackparrot.me") };

        private async Task<Version?> GetRemoteVersionData()
        {
            Plugin.Log.Info($"Getting version data from {HttpClient.BaseAddress} ...");
            
            HttpResponseMessage response = await HttpClient.GetAsync("DumbRequestManager/version.json");
            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.Warn("Failed to get version data");
                return null;
            }
            
            Plugin.Log.Info($"Got version data from {response.RequestMessage.RequestUri}");

            string rawVersionData = await response.Content.ReadAsStringAsync();
            
            JObject json = JObject.Parse(rawVersionData);
            string? rawVersionString = json[_gameVersion.ToString(3)]?.ToString();
            
            return rawVersionString != null ? new Version(rawVersionString) : null;
        }
        
        internal readonly Version ModVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private readonly Version _gameVersion;
        internal static Version? LatestVersion;

        public VersionManager()
        {
            byte[] manifestData = SiraUtil.Extras.Utilities.GetResource(Assembly.GetExecutingAssembly(), "DumbRequestManager.manifest.json");
            ManifestData? manifest = JsonConvert.DeserializeObject<ManifestData>(System.Text.Encoding.UTF8.GetString(manifestData));
            
            _gameVersion = new Version(manifest?.GameVersion!);

            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                LatestVersion = await GetRemoteVersionData();
                Plugin.Log.Info("Latest version is " + LatestVersion?.ToString(3));

#if DEBUG
                SideSettingsViewController.Instance.UpdateObject.gameObject.SetActive(true);
#else
                if (LatestVersion > ModVersion)
                {
                    SideSettingsViewController.Instance.UpdateObject.gameObject.SetActive(true);
                }
#endif
            });
        }
    }
}