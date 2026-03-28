using Newtonsoft.Json;

namespace ReCoreVery;

public class ItemData
{
    [JsonProperty("itemNameOrId")]
    public string ItemNameOrId { get; set; }

    [JsonProperty("stack")]
    public int Stack { get; set; }

    [JsonProperty("prefix")]
    public byte Prefix { get; set; }

    public ItemData()
    {
        ItemNameOrId = "None";
        Stack = 1;
        Prefix = 0;
    }

    public ItemData(string itemNameOrId, int stack, byte prefix = 0)
    {
        ItemNameOrId = itemNameOrId;
        Stack = stack;
        Prefix = prefix;
    }
}