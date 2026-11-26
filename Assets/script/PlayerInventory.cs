using System;
using UnityEngine;

// API giữ nguyên (đồng bộ) nên ShopManager / SelectManager / VehicleCatalog KHÔNG cần sửa.
// Chạy từ launcher: đọc từ ServerClient.Cache, ghi thì cập nhật cache ngay rồi gửi lên server.
// Chạy trong Editor (không launcher): dùng PlayerPrefs như cũ.
public static class PlayerInventory
{
    private const string COINS_KEY = "PlayerCoins";
    private const string MAP_KEY = "SelectedMap";
    private const string INIT_KEY = "InventoryInitialized_v3";

    public static event Action OnChanged;

    private static bool UseServer => LauncherArgs.HasLauncher && ServerClient.Ready;

    private static string K(string key) => $"{LauncherArgs.User}_{key}";

    private static void NotifyChanged()
    {
        try { OnChanged?.Invoke(); }
        catch (Exception e) { Debug.LogWarning("[PlayerInventory] " + e.Message); }
    }

    private static void EnsureInitialized()
    {
        if (PlayerPrefs.GetInt(K(INIT_KEY), 0) == 1) return;

        PlayerPrefs.SetInt(K(COINS_KEY), 100);
        PlayerPrefs.SetInt(K("Unlocked_Scooter_0"), 1);
        PlayerPrefs.SetInt(K("Equipped_Scooter"), 0);
        PlayerPrefs.SetString(K(MAP_KEY), "game");

        PlayerPrefs.SetInt(K(INIT_KEY), 1);
        PlayerPrefs.Save();
    }

    // ---------- helpers cho cache ----------
    private static ItemDto Find(string category, int index)
    {
        if (ServerClient.Cache == null || ServerClient.Cache.items == null) return null;
        foreach (ItemDto it in ServerClient.Cache.items)
            if (it.category == category && it.itemIndex == index) return it;
        return null;
    }

    // ---------- COINS ----------
    public static int GetCoins()
    {
        if (UseServer) return ServerClient.Cache.coins;
        EnsureInitialized();
        return PlayerPrefs.GetInt(K(COINS_KEY), 100);
    }

    public static void SetCoins(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (UseServer)
        {
            int delta = amount - ServerClient.Cache.coins;
            if (delta == 0) return;
            ServerClient.Cache.coins = amount;
            NotifyChanged();
            ServerClient.Fire(new Req { command = "CAPNHATXU", coinsDelta = delta, requestId = Guid.NewGuid().ToString("N") });
            return;
        }

        EnsureInitialized();
        PlayerPrefs.SetInt(K(COINS_KEY), amount);
        PlayerPrefs.Save();
        NotifyChanged();
    }

    public static void AddCoins(int amount)
    {
        if (amount == 0) return;
        if (UseServer)
        {
            ServerClient.Cache.coins = Mathf.Max(0, ServerClient.Cache.coins + amount);
            NotifyChanged();
            ServerClient.Fire(new Req { command = "CAPNHATXU", coinsDelta = amount, requestId = Guid.NewGuid().ToString("N") });
            return;
        }

        EnsureInitialized();
        PlayerPrefs.SetInt(K(COINS_KEY), Mathf.Max(0, GetCoins() + amount));
        PlayerPrefs.Save();
        NotifyChanged();
    }

    // ---------- ITEMS ----------
    public static bool IsUnlocked(string category, int index)
    {
        if (UseServer) return Find(category, index) != null;
        EnsureInitialized();
        return PlayerPrefs.GetInt(K($"Unlocked_{category}_{index}"), 0) == 1;
    }

    public static bool TryBuyItem(string category, int index, int price)
    {
        if (UseServer)
        {
            if (IsUnlocked(category, index)) return false;
            if (ServerClient.Cache.coins < price) return false;

            ServerClient.Cache.coins -= price;
            ServerClient.Cache.items.Add(new ItemDto { category = category, itemIndex = index, isEquipped = false });
            NotifyChanged();
            ServerClient.Fire(new Req { command = "MUAHANG", category = category, itemIndex = index, price = price, requestId = Guid.NewGuid().ToString("N") });
            return true;
        }

        EnsureInitialized();
        if (IsUnlocked(category, index)) return false;

        int currentCoins = GetCoins();
        if (currentCoins < price) return false;

        PlayerPrefs.SetInt(K(COINS_KEY), currentCoins - price);
        PlayerPrefs.SetInt(K($"Unlocked_{category}_{index}"), 1);
        PlayerPrefs.Save();
        NotifyChanged();
        return true;
    }

    public static int GetEquippedIndex(string category)
    {
        if (UseServer)
        {
            if (ServerClient.Cache == null || ServerClient.Cache.items == null) return 0;
            foreach (ItemDto it in ServerClient.Cache.items)
                if (it.category == category && it.isEquipped) return it.itemIndex;
            return 0;
        }

        EnsureInitialized();
        return PlayerPrefs.GetInt(K($"Equipped_{category}"), 0);
    }

    public static void SetEquippedIndex(string category, int index)
    {
        if (UseServer)
        {
            if (Find(category, index) == null) return;
            foreach (ItemDto it in ServerClient.Cache.items)
                if (it.category == category) it.isEquipped = it.itemIndex == index;
            NotifyChanged();
            ServerClient.Fire(new Req { command = "TRANGBI", category = category, itemIndex = index, requestId = Guid.NewGuid().ToString("N") });
            return;
        }

        EnsureInitialized();
        PlayerPrefs.SetInt(K($"Equipped_{category}"), index);
        PlayerPrefs.Save();
        NotifyChanged();
    }

    // ---------- MAP ----------
    public static string GetSelectedMap()
    {
        if (UseServer)
            return string.IsNullOrEmpty(ServerClient.Cache.selectedMap) ? "game" : ServerClient.Cache.selectedMap;

        EnsureInitialized();
        return PlayerPrefs.GetString(K(MAP_KEY), "game");
    }

    public static void SetSelectedMap(string sceneName)
    {
        if (UseServer)
        {
            ServerClient.Cache.selectedMap = sceneName;
            NotifyChanged();
            ServerClient.Fire(new Req { command = "CHONMAP", mapName = sceneName, requestId = Guid.NewGuid().ToString("N") });
            return;
        }

        EnsureInitialized();
        PlayerPrefs.SetString(K(MAP_KEY), sceneName);
        PlayerPrefs.Save();
        NotifyChanged();
    }
}
