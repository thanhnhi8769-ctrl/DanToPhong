using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class VehicleCatalog
{
    public class VehicleData
    {
        public string name;
        public int price;
        public string sourceModel;
        public Color color;

        public VehicleData(string name, int price, string sourceModel, Color color)
        {
            this.name = name;
            this.price = price;
            this.sourceModel = sourceModel;
            this.color = color;
        }
    }

    private static readonly VehicleData[] vehicles =
    {
        new VehicleData("Default", 0, "ElectricScooterDefault", Color.white),
        new VehicleData("Classic Black", 100, "ElectricScooterDefaultBlack", new Color32(30,30,35,255)),
        new VehicleData("Ocean Blue", 150, "ElectricScooterDefault", new Color32(20,110,255,255)),
        new VehicleData("Racing Red", 180, "ElectricScooterDefault", new Color32(230,35,45,255)),
        new VehicleData("Neon Green", 180, "ElectricScooterDefault", new Color32(35,220,90,255)),
        new VehicleData("Solar Yellow", 180, "ElectricScooterDefault", new Color32(255,215,20,255)),
        new VehicleData("Royal Purple", 200, "ElectricScooterDefault", new Color32(130,55,220,255)),
        new VehicleData("Pink Blossom", 200, "ElectricScooterDefault", new Color32(255,100,175,255)),
        new VehicleData("Orange Rush", 200, "ElectricScooterDefault", new Color32(255,115,20,255)),
        new VehicleData("Ice Cyan", 220, "ElectricScooterDefault", new Color32(20,220,235,255)),
        new VehicleData("Lime Flash", 220, "ElectricScooterDefault", new Color32(165,230,20,255)),
        new VehicleData("Midnight Blue", 250, "ElectricScooterDefault", new Color32(15,35,110,255)),
        new VehicleData("Rose Gold", 260, "ElectricScooterDefault", new Color32(215,105,120,255)),
        new VehicleData("Sunset Orange", 260, "ElectricScooterDefault", new Color32(255,80,40,255)),
        new VehicleData("Violet Pulse", 280, "ElectricScooterDefault", new Color32(175,45,255,255)),
        new VehicleData("Forest Green", 280, "ElectricScooterDefault", new Color32(20,110,55,255)),
        new VehicleData("Sky Blue", 280, "ElectricScooterDefault", new Color32(60,165,255,255)),
        new VehicleData("Cherry Red", 300, "ElectricScooterDefault", new Color32(165,10,35,255)),
        new VehicleData("Desert Gold", 300, "ElectricScooterDefault", new Color32(225,165,55,255)),
        new VehicleData("Mint Green", 320, "ElectricScooterDefault", new Color32(55,215,160,255)),
        new VehicleData("Lavender", 320, "ElectricScooterDefault", new Color32(165,115,240,255)),
        new VehicleData("Neon Yellow", 340, "ElectricScooterDefault", new Color32(225,255,20,255)),
        new VehicleData("Magenta", 360, "ElectricScooterDefault", new Color32(220,30,165,255)),
        new VehicleData("Copper", 380, "ElectricScooterDefault", new Color32(190,85,35,255)),
        new VehicleData("Emerald", 420, "ElectricScooterDefault", new Color32(0,145,85,255))
    };

    private static readonly Dictionary<int, Texture> cacheTextureGoc = new Dictionary<int, Texture>();
    private const string KEY_EQUIPPED_BACKUP = "VC_Equipped_Scooter";

    public static int Count => vehicles.Length;

    public static VehicleData Get(int index)
    {
        index = Mathf.Clamp(index, 0, Count - 1);
        return vehicles[index];
    }

    public static string GetName(int index) => Get(index).name;
    public static int GetPrice(int index) => Get(index).price;
    public static Color GetColor(int index) => Get(index).color;
    public static string GetSourceModel(int index) => Get(index).sourceModel;

    public static string[] GetAllNames()
    {
        string[] names = new string[Count];
        for (int i = 0; i < Count; i++)
            names[i] = vehicles[i].name;
        return names;
    }

    public static bool IsOwned(int index)
    {
        if (index < 0 || index >= Count) return false;
        if (index == 0) return true;
        return PlayerInventory.IsUnlocked("Scooter", index);
    }

    public static bool TryPurchase(int index)
    {
        if (index < 0 || index >= Count) return false;
        if (IsOwned(index)) return true;
        return PlayerInventory.TryBuyItem("Scooter", index, GetPrice(index));
    }

    public static bool SelectVehicle(int index)
    {
        if (index < 0 || index >= Count) return false;
        if (!IsOwned(index)) return false;

        // Mở khóa miễn phí index 0 trong PlayerInventory nếu chưa có
        if (index == 0 && !PlayerInventory.IsUnlocked("Scooter", 0))
            PlayerInventory.TryBuyItem("Scooter", 0, 0);

        PlayerInventory.SetEquippedIndex("Scooter", index);
        PlayerPrefs.SetInt(KEY_EQUIPPED_BACKUP, index);
        PlayerPrefs.Save();

        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            Hashtable properties = new Hashtable { ["Scooter"] = index };
            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }

        return true;
    }

    public static int GetSelectedIndex()
    {
        int invIndex = PlayerInventory.GetEquippedIndex("Scooter");
        if (PlayerPrefs.HasKey(KEY_EQUIPPED_BACKUP))
        {
            int backupIndex = PlayerPrefs.GetInt(KEY_EQUIPPED_BACKUP, 0);
            if (IsOwned(backupIndex))
                invIndex = backupIndex;
        }
        return Mathf.Clamp(invIndex, 0, Count - 1);
    }

    public static int GetPlayerVehicleIndex(Player player)
    {
        if (player == null) return 0;
        if (player.CustomProperties.TryGetValue("Scooter", out object value) && value is int)
            return Mathf.Clamp((int)value, 0, Count - 1);
        return 0;
    }

    public static GameObject CreatePreviewModel(int index)
    {
        VehicleData data = Get(index);
        GameObject prefab = Resources.Load<GameObject>("Scooter");
        if (prefab == null) return null;

        GameObject temporary = Object.Instantiate(prefab);
        Transform source = FindChild(temporary.transform, data.sourceModel);
        if (source == null)
        {
            Object.Destroy(temporary);
            return null;
        }

        GameObject model = Object.Instantiate(source.gameObject);
        model.name = data.name;
        ApplyPaint(model, data.color);
        Object.Destroy(temporary);
        return model;
    }

    private static Transform FindChild(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChild(parent.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    public static void ApplyPaint(GameObject model, Color color)
    {
        if (model == null) return;

        bool laXeDenGoc = color.r < 0.16f && color.g < 0.16f && color.b < 0.16f;
        Color mauApDung = laXeDenGoc ? Color.white : color;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;

            string tenObj = renderer.gameObject.name.ToLower();
            if (tenObj.Contains("minimap") || tenObj.Contains("vetphanh"))
                continue;

            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                string tenMat = mat.name.ToLower();
                if (tenMat.Contains("black") || tenMat.Contains("tire") || tenMat.Contains("wheel") || tenMat.Contains("rubber"))
                    continue;

                int matId = mat.GetInstanceID();
                if (!cacheTextureGoc.ContainsKey(matId))
                {
                    Texture tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                    if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                    cacheTextureGoc[matId] = tex;
                }

                Texture texGoc = cacheTextureGoc[matId];
                if (texGoc != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texGoc);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texGoc);
                }

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mauApDung);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", mauApDung);
            }

            renderer.materials = materials;
        }
    }
}