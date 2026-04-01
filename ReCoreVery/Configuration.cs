using Newtonsoft.Json;
using TShockAPI;

namespace ReCoreVery;

public class Configuration
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("mediumcoreOnly")]
    public bool MediumcoreOnly { get; set; }

    [JsonProperty("hardcoreIncluded")]
    public bool HardcoreIncluded { get; set; }

    [JsonProperty("respawnDelaySeconds")]
    public int RespawnDelaySeconds { get; set; }

    [JsonProperty("clearInventoryOnRespawn")]
    public bool ClearInventoryOnRespawn { get; set; }

    [JsonProperty("announceToPlayer")]
    public bool AnnounceToPlayer { get; set; }

    [JsonProperty("announceMessage")]
    public string AnnounceMessage { get; set; }

    [JsonProperty("spawnKits")]
    public List<SpawnKit> SpawnKits { get; set; }

    private static string ConfigPath => Path.Combine(TShock.SavePath, "ReCoreVery.json");

    public Configuration()
    {
        Enabled = true;
        MediumcoreOnly = true;
        HardcoreIncluded = false;
        RespawnDelaySeconds = 0;
        ClearInventoryOnRespawn = true;
        AnnounceToPlayer = true;
        AnnounceMessage = "[ReCoreVery] You have received your starter kit!";
        SpawnKits = new List<SpawnKit>
        {
            new SpawnKit(
                "default",
                "",
                0,
                new List<ItemData>
                {
                    new ItemData("Copper Shortsword", 1, 0),
                    new ItemData("Copper Pickaxe", 1, 0),
                    new ItemData("Copper Axe", 1, 0),
                    new ItemData("Torch", 50, 0),
                    new ItemData("Rope", 50, 0),
                    new ItemData("Lesser Healing Potion", 5, 0)
                }
            ),
            new SpawnKit(
                "vip",
                "recorevery.vip",
                10,
                new List<ItemData>
                {
                    new ItemData("Iron Shortsword", 1, 0),
                    new ItemData("Iron Pickaxe", 1, 0),
                    new ItemData("Iron Axe", 1, 0),
                    new ItemData("Torch", 100, 0),
                    new ItemData("Rope", 100, 0),
                    new ItemData("Healing Potion", 10, 0),
                    new ItemData("Ironskin Potion", 3, 0),
                    new ItemData("Swiftness Potion", 3, 0)
                }
            )
        };
    }

    public static Configuration Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var config = new Configuration();
            config.Save();
            TShock.Log.ConsoleInfo("[ReCoreVery] Created default configuration file.");
            return config;
        }

        try
        {
            string json = File.ReadAllText(ConfigPath);
            var config = JsonConvert.DeserializeObject<Configuration>(json);
            if (config == null)
            {
                TShock.Log.ConsoleError("[ReCoreVery] Config deserialized as null. Using defaults.");
                config = new Configuration();
                config.Save();
            }
            else if (config.SpawnKits == null || config.SpawnKits.Count == 0)
            {
                TShock.Log.ConsoleError("[ReCoreVery] No spawn kits found in config. Adding defaults.");
                config.SpawnKits = new Configuration().SpawnKits;
                config.Save();
            }

            foreach (var kit in config.SpawnKits)
            {
                foreach (var item in kit.Items)
                {
                    int id = GetItemIdStatic(item.ItemNameOrId);
                    if (id <= 0)
                    {
                        TShock.Log.ConsoleError(
                            $"[ReCoreVery] Warning: Item '{item.ItemNameOrId}' in kit '{kit.Name}' could not be resolved.");
                    }
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[ReCoreVery] Failed to load config: {ex.Message}");
            var config = new Configuration();
            config.Save();
            return config;
        }
    }

    private static int GetItemIdStatic(string itemNameOrId)
    {
        if (int.TryParse(itemNameOrId, out int itemId))
            return itemId;

        try
        {
            var items = TShock.Utils.GetItemByIdOrName(itemNameOrId);
            if (items.Count == 1)
                return items[0].type;
        }
        catch
        {
        }

        return 0;
    }

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    public SpawnKit? GetKitForPlayer(TSPlayer player)
    {
        SpawnKit? selectedKit = null;
        int highestPriority = int.MinValue;

        foreach (var kit in SpawnKits)
        {
            bool hasPermission = string.IsNullOrEmpty(kit.Permission) || player.HasPermission(kit.Permission);
            if (hasPermission && kit.Priority > highestPriority)
            {
                highestPriority = kit.Priority;
                selectedKit = kit;
            }
        }

        return selectedKit;
    }
}