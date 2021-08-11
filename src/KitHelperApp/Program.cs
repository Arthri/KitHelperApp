using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;

namespace KitHelperApp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.Write("Specify a locale(Enter for en-US): ");
            var lang = Console.ReadLine();
            if (lang == "")
            {
                lang = "en-US";
            }
            Console.WriteLine($"Fetching Item Names for locale: {lang}...");
            var idMap = typeof(ItemID)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .ToDictionary(f => (short)f.GetValue(null), f => f.Name);
            idMap.Remove(ItemID.None);
            idMap.Remove(ItemID.Count);

            var assembly = typeof(ItemID).Assembly;
            var resource = Utils.ReadEmbeddedResource($"Terraria.Localization.Content.{lang}.Items.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(resource);
            var resDict = new Dictionary<string, string>();
            foreach (var ktp in json["ItemName"])
            {
                resDict.Add(ktp.Key, ktp.Value);
            }

            idMap = idMap
                .Where(kvp => kvp.Key > 0)
                .ToDictionary(i => i.Key, i =>
            {
                if (resDict.TryGetValue(i.Value, out var str))
                {
                    return str;
                }
                else
                {
                    Console.WriteLine($"No key for {i.Value}");
                    return i.Value;
                }
            });
            resDict = null;
            json = null;


            Console.WriteLine("Reading data file...");
            var names = JsonConvert.DeserializeObject<string[]>(File.ReadAllText("name-def.json"));
            var sb = new StringBuilder();
            var sdata = JsonConvert.DeserializeObject<Dictionary<string, Kit[]>>(File.ReadAllText("data.json"));
            var kits = sdata["latecomer-items"];
            for (var i = 0; i < kits.Length; i++)
            {
                var kit = kits[i];
                var kitName = i < names.Length
                    ? names[i]
                    : "Unnamed";
                sb.AppendLine("# " + kitName);
                if (kit.HP != null)
                {
                    sb.AppendLine(string.Format("**HP:** `{0}`", kit.HP));
                }
                if (kit.Mana != null)
                {
                    sb.AppendLine(string.Format("**MP:** `{0}`", kit.Mana));
                }
                if (kit.Items != null)
                {
                    PrintItemCollection(kit.Items, "Items");
                }
                if (kit.CorruptionItems != null)
                {
                    PrintItemCollection(kit.CorruptionItems, "Corruption Items");
                }
                if (kit.CrimsonItems != null)
                {
                    PrintItemCollection(kit.CrimsonItems, "Crimson Items");
                }
                if (kit.GimmickItems != null)
                {
                    foreach (var kvp in kit.GimmickItems)
                    {
                        PrintItemCollection(kvp.Value, $"{kvp.Key} Items");
                    }
                }

                void PrintItem(KitItem item)
                {
                    string itemName;
                    if (item.ID != null)
                    {
                        var itemID = item.ID.Value;
                        itemName = itemID < 0
                            ? idMap[ItemID.FromNetId(itemID)]
                            : idMap[itemID];
                        Console.WriteLine($"{kitName} contains an air item");
                    }
                    else
                    {
                        Console.WriteLine($"{kitName}/ Stack: {item.Stack}, Probability: {item.Probability * 100}% lacks an item ID");
                        itemName = "**BLANK**";
                    }
                    var result = $"- `{item.Probability * 100}%` {item.Stack ?? 0} `{itemName}`.";
                    if (item.GiveFor > 0)
                    {
                        result += $" For {item.GiveFor} kits onward.";
                    }
                    sb.AppendLine(result);
                }

                void PrintItemCollection(KitItem[] items, string name)
                {
                    if (items.Length > 0)
                    {
                        sb.AppendLine($"## {name}");
                        foreach (var item in items)
                        {
                            PrintItem(item);
                        }
                    }
                }
            }

            File.WriteAllText("data.md", sb.ToString());
        }
    }

    public class Kit
    {
        [JsonProperty("hp")]
        public int? HP { get; set; }

        [JsonProperty("mana")]
        public int? Mana { get; set; }

        [JsonProperty("items")]
        public KitItem[] Items { get; set; }

        [JsonProperty("corruption-items")]
        public KitItem[] CorruptionItems { get; set; }

        [JsonProperty("crimson-items")]
        public KitItem[] CrimsonItems { get; set; }

        [JsonProperty("gimmick-items")]
        public Dictionary<string, KitItem[]> GimmickItems { get; set; }
    }

    public class KitItem
    {
        [JsonProperty("id")]
        public short? ID { get; set; }

        [JsonProperty("stack")]
        public int? Stack { get; set; }

        [JsonProperty("give-for")]
        public int? GiveFor { get; set; }

        [JsonProperty("probability")]
        public double? Probability { get; set; }
    }
}
