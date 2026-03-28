using Newtonsoft.Json;

namespace ReCoreVery;

public class SpawnKit
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("permission")]
    public string Permission { get; set; }

    [JsonProperty("priority")]
    public int Priority { get; set; }

    [JsonProperty("items")]
    public List<ItemData> Items { get; set; }

    public SpawnKit()
    {
        Name = "default";
        Permission = "";
        Priority = 0;
        Items = new List<ItemData>();
    }

    public SpawnKit(string name, string permission, int priority, List<ItemData> items)
    {
        Name = name;
        Permission = permission;
        Priority = priority;
        Items = items;
    }
}