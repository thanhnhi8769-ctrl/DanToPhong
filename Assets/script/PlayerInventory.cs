using UnityEngine;

public static class PlayerInventory
{
    private const string COINS_KEY = "PlayerCoins";
    private const string MAP_KEY = "SelectedMap";
    private const string INIT_KEY = "InventoryInitialized_v2"; // Đổi key để reset sạch dữ liệu test cũ

    // Khởi tạo dữ liệu mặc định lần đầu vào game
    private static void EnsureInitialized()
    {
        if (PlayerPrefs.GetInt(INIT_KEY, 0) == 1) return;

        // 1. Tặng đúng 100 xu khởi đầu
        PlayerPrefs.SetInt(COINS_KEY, 100);

        // 2. Mỗi loại chỉ mở khóa duy nhất món đầu tiên (index = 0)
        PlayerPrefs.SetInt("Unlocked_Scooter_0", 1);
        PlayerPrefs.SetInt("Unlocked_Helmet_0", 1);
        PlayerPrefs.SetInt("Unlocked_Character_0", 1);

        // 3. Trang bị mặc định món số 0 và map "game"
        PlayerPrefs.SetInt("Equipped_Scooter", 0);
        PlayerPrefs.SetInt("Equipped_Helmet", 0);
        PlayerPrefs.SetInt("Equipped_Character", 0);
        PlayerPrefs.SetString(MAP_KEY, "game");

        PlayerPrefs.SetInt(INIT_KEY, 1);
        PlayerPrefs.Save();
    }

    public static int GetCoins()
    {
        EnsureInitialized();
        return PlayerPrefs.GetInt(COINS_KEY, 100);
    }

    public static void AddCoins(int amount)
    {
        EnsureInitialized();
        int current = GetCoins();
        PlayerPrefs.SetInt(COINS_KEY, Mathf.Max(0, current + amount));
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(string category, int index)
    {
        EnsureInitialized();
        return PlayerPrefs.GetInt($"Unlocked_{category}_{index}", 0) == 1;
    }

    public static bool TryBuyItem(string category, int index, int price)
    {
        EnsureInitialized();
        if (IsUnlocked(category, index)) return false;

        int currentCoins = GetCoins();
        if (currentCoins < price) return false;

        PlayerPrefs.SetInt(COINS_KEY, currentCoins - price);
        PlayerPrefs.SetInt($"Unlocked_{category}_{index}", 1);
        PlayerPrefs.Save();
        return true;
    }

    public static int GetEquippedIndex(string category)
    {
        EnsureInitialized();
        return PlayerPrefs.GetInt($"Equipped_{category}", 0);
    }

    public static void SetEquippedIndex(string category, int index)
    {
        EnsureInitialized();
        PlayerPrefs.SetInt($"Equipped_{category}", index);
        PlayerPrefs.Save();
    }

    public static string GetSelectedMap()
    {
        EnsureInitialized();
        return PlayerPrefs.GetString(MAP_KEY, "game");
    }

    public static void SetSelectedMap(string sceneName)
    {
        EnsureInitialized();
        PlayerPrefs.SetString(MAP_KEY, sceneName);
        PlayerPrefs.Save();
    }
}