﻿using Maple2Storage.Types;
using Newtonsoft.Json;

namespace MapleServer2.Data.Static;

public static class FishingRewardsMetadataStorage
{
    private static readonly Dictionary<int, FishingRewardItem> FishItems = new();

    public static void Init()
    {
        string json = File.ReadAllText($"{Paths.JSON_DIR}/FishingRewards.json");
        List<FishingRewardItem> items = JsonConvert.DeserializeObject<List<FishingRewardItem>>(json);
        foreach (FishingRewardItem item in items)
        {
            FishItems[item.Id] = item;
        }
    }

    public static FishingRewardItem GetFishingRewardItem(FishingItemType type)
    {
        Random random = new();
        List<FishingRewardItem> items = FishItems.Values.Where(x => x.Type == type).ToList();
        int index = random.Next(items.Count);
        return items[index];
    }
}
public class FishingRewardItem
{
    public int Id;
    public FishingItemType Type;
    public int Amount;
    public int Rarity;
}
public enum FishingItemType
{
    Trash = 0,
    LightBox = 1,
    HeavyBox = 2,
    Skin = 3,
    GoldFish = 4
}
