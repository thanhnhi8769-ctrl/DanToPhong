using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public int price;
        public GameObject model3D;
    }

    [Header("Shop Items")]
    public ShopItem[] scooters;
    public ShopItem[] helmets;
    public ShopItem[] characters;

    [Header("3D Showcase Stage")]
    public Transform showcaseStage;
    public float rotateSpeed = 45f;

    [Header("Mini Owned Shelf (Top-Left)")]
    public Transform gocMiniPreview;
    public float kichThuocMini = 0.14f;
    public float khoangCachNgang = 0.22f;
    public float khoangCachHang = 0.22f;

    [Header("UI Elements")]
    public TMP_Text coinsText;
    public TMP_Text itemNameText;
    public Button buyButton;
    public TMP_Text buyButtonText;
    public TMP_Text thongBaoMuaText;

    private string currentTab = "Scooter";
    private int currentIndex = 0;
    private Coroutine coroutineThongBao;
    private List<Transform> danhSachMiniXoay = new List<Transform>();

    private const float DO_CAO_SAN_KHAU = 1.45f;
    private const float KICH_THUOC_CHUAN = 1.35f;

    private void Start()
    {
        if (thongBaoMuaText != null) thongBaoMuaText.gameObject.SetActive(false);

        CanChinhCameraVaSanKhau();
        ChuanHoaTatCaModel();

        UpdateCoinsUI();
        CapNhatToanBoDoSoHuuMini();
        SelectTab("Scooter");
    }

    private void CanChinhCameraVaSanKhau()
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 0.8f, 0f);
            Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        if (showcaseStage == null)
        {
            GameObject stageObj = new GameObject("AutoShowcaseStage");
            showcaseStage = stageObj.transform;
        }

        showcaseStage.position = new Vector3(0f, DO_CAO_SAN_KHAU, 4.5f);
        showcaseStage.rotation = Quaternion.identity;
        showcaseStage.localScale = Vector3.one;

        if (gocMiniPreview == null)
        {
            GameObject miniRoot = new GameObject("AutoMiniShelf");
            gocMiniPreview = miniRoot.transform;
        }

        gocMiniPreview.position = new Vector3(-2.55f, 2.02f, 4.5f);
        gocMiniPreview.rotation = Quaternion.identity;
        gocMiniPreview.localScale = Vector3.one;
    }

    private void ChuanHoaTatCaModel()
    {
        CanGiuaDanhSach(scooters);
        CanGiuaDanhSach(helmets);
        CanGiuaDanhSach(characters);
    }

    private void KhoaVatLyVaChuyenDong(GameObject obj)
    {
        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
        {
            Destroy(rb);
        }

        foreach (Animator anim in obj.GetComponentsInChildren<Animator>(true))
        {
            anim.enabled = false;
        }

        foreach (MonoBehaviour script in obj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script != null && script != this)
            {
                script.enabled = false;
            }
        }
    }

    private void CanGiuaDanhSach(ShopItem[] danhSach)
    {
        if (danhSach == null) return;

        foreach (var item in danhSach)
        {
            if (item.model3D == null) continue;

            if (!item.model3D.scene.IsValid())
            {
                item.model3D = Instantiate(item.model3D);
            }

            KhoaVatLyVaChuyenDong(item.model3D);

            item.model3D.transform.SetParent(showcaseStage, false);
            item.model3D.SetActive(true);
            item.model3D.transform.localPosition = Vector3.zero;
            item.model3D.transform.localRotation = Quaternion.identity;
            item.model3D.transform.localScale = Vector3.one;

            Renderer[] renderers = item.model3D.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = new Bounds(showcaseStage.position, Vector3.zero);
                bool hasValidBounds = false;

                foreach (Renderer r in renderers)
                {
                    if (r.bounds.size.sqrMagnitude > 0.0001f)
                    {
                        if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                        else bounds.Encapsulate(r.bounds);
                    }
                }

                if (hasValidBounds)
                {
                    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (maxDim > 0.001f)
                    {
                        float scaleFactor = Mathf.Clamp(KICH_THUOC_CHUAN / maxDim, 0.05f, 20f);
                        item.model3D.transform.localScale = Vector3.one * scaleFactor;
                    }

                    bounds = new Bounds(showcaseStage.position, Vector3.zero);
                    hasValidBounds = false;
                    foreach (Renderer r in renderers)
                    {
                        if (r.bounds.size.sqrMagnitude > 0.0001f)
                        {
                            if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                            else bounds.Encapsulate(r.bounds);
                        }
                    }
                    if (hasValidBounds)
                    {
                        item.model3D.transform.position += (showcaseStage.position - bounds.center);
                    }
                }
            }

            item.model3D.SetActive(false);
        }
    }

    private void Update()
    {
        // Xoay đều 360 độ cho cả Scooter, Helmet và Character
        if (showcaseStage != null)
        {
            showcaseStage.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        }

        for (int i = 0; i < danhSachMiniXoay.Count; i++)
        {
            if (danhSachMiniXoay[i] != null)
            {
                danhSachMiniXoay[i].Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            }
        }
    }

    public void SelectTab(string tabName)
    {
        currentTab = tabName;
        currentIndex = 0;
        UpdateItemUI();
    }

    public void NextItem()
    {
        ShopItem[] currentList = GetCurrentList();
        if (currentList.Length == 0) return;

        currentIndex = (currentIndex + 1) % currentList.Length;
        UpdateItemUI();
    }

    public void PreviousItem()
    {
        ShopItem[] currentList = GetCurrentList();
        if (currentList.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0) currentIndex = currentList.Length - 1;
        UpdateItemUI();
    }

    public void OnBuyButtonClicked()
    {
        ShopItem[] currentList = GetCurrentList();
        if (currentList.Length == 0) return;

        ShopItem selectedItem = currentList[currentIndex];

        if (PlayerInventory.TryBuyItem(currentTab, currentIndex, selectedItem.price))
        {
            UpdateCoinsUI();
            UpdateItemUI();
            CapNhatToanBoDoSoHuuMini();
            HienThongBao($"PURCHASED: {selectedItem.itemName.ToUpper()}!", Color.green);
        }
        else
        {
            HienThongBao("NOT ENOUGH COINS!", Color.red);
        }
    }

    private void HienThongBao(string noiDung, Color mauChu)
    {
        if (thongBaoMuaText == null) return;

        if (coroutineThongBao != null) StopCoroutine(coroutineThongBao);
        coroutineThongBao = StartCoroutine(ChayHienThongBao(noiDung, mauChu));
    }

    private IEnumerator ChayHienThongBao(string noiDung, Color mauChu)
    {
        thongBaoMuaText.gameObject.SetActive(true);
        thongBaoMuaText.color = mauChu;
        thongBaoMuaText.text = noiDung;

        yield return new WaitForSeconds(2.2f);

        if (thongBaoMuaText != null) thongBaoMuaText.gameObject.SetActive(false);
    }

    private void CapNhatToanBoDoSoHuuMini()
    {
        if (gocMiniPreview == null) return;

        danhSachMiniXoay.Clear();

        for (int i = gocMiniPreview.childCount - 1; i >= 0; i--)
        {
            GameObject oldChild = gocMiniPreview.GetChild(i).gameObject;
            oldChild.SetActive(false);
            Destroy(oldChild);
        }

        int soXe = XepHangMiniDaSoHuu("Scooter", scooters, 0);
        int soMu = XepHangMiniDaSoHuu("Helmet", helmets, 1);
        int soNv = XepHangMiniDaSoHuu("Character", characters, 2);

        int maxCot = Mathf.Max(2, Mathf.Max(soXe, Mathf.Max(soMu, soNv)));

        float traiKhung = -0.78f;
        float phaiKhung = (maxCot - 1) * khoangCachNgang + 0.22f;
        float chieuRongKhung = phaiKhung - traiKhung;
        float tamX = (traiKhung + phaiKhung) * 0.5f;

        float trenKhung = 0.28f;
        float duoiKhung = -2 * khoangCachHang - 0.16f;
        float chieuCaoKhung = trenKhung - duoiKhung;
        float tamY = (trenKhung + duoiKhung) * 0.5f;

        TaoChu3D("OWNED ITEMS", new Vector3(tamX, 0.18f, -0.02f), 40, new Color(0.2f, 1f, 0.45f), TextAnchor.MiddleCenter);
        TaoKhungKeMini(tamX, tamY, chieuRongKhung, chieuCaoKhung);
    }

    private void TaoKhungKeMini(float tamX, float tamY, float rong, float cao)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        GameObject vienKhung = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vienKhung.name = "KhungVien";
        Destroy(vienKhung.GetComponent<Collider>());
        vienKhung.transform.SetParent(gocMiniPreview, false);
        vienKhung.transform.localPosition = new Vector3(tamX, tamY, 0.14f);
        vienKhung.transform.localScale = new Vector3(rong + 0.03f, cao + 0.03f, 1f);
        Material matVien = new Material(shader);
        matVien.color = new Color(0.2f, 0.85f, 0.5f);
        vienKhung.GetComponent<MeshRenderer>().material = matVien;

        GameObject nenKhung = GameObject.CreatePrimitive(PrimitiveType.Quad);
        nenKhung.name = "KhungNen";
        Destroy(nenKhung.GetComponent<Collider>());
        nenKhung.transform.SetParent(gocMiniPreview, false);
        nenKhung.transform.localPosition = new Vector3(tamX, tamY, 0.13f);
        nenKhung.transform.localScale = new Vector3(rong, cao, 1f);
        Material matNen = new Material(shader);
        matNen.color = new Color(0.10f, 0.13f, 0.18f);
        nenKhung.GetComponent<MeshRenderer>().material = matNen;
    }

    private void TaoChu3D(string noiDung, Vector3 viTriCucBo, int coChu, Color mauChu, TextAnchor canLe)
    {
        GameObject labelObj = new GameObject($"Label_{noiDung}");
        labelObj.transform.SetParent(gocMiniPreview, false);
        labelObj.transform.localPosition = viTriCucBo;

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = noiDung;
        textMesh.fontSize = coChu;
        textMesh.characterSize = 0.023f;
        textMesh.anchor = canLe;
        textMesh.color = mauChu;
    }

    private int XepHangMiniDaSoHuu(string category, ShopItem[] danhSach, int thuTuHang)
    {
        if (danhSach == null || danhSach.Length == 0) return 0;

        string tenHang = category == "Scooter" ? "SCOOTER:" : (category == "Helmet" ? "HELMET:" : "CHAR:");
        TaoChu3D(tenHang, new Vector3(-0.18f, -thuTuHang * khoangCachHang, -0.02f), 32, Color.yellow, TextAnchor.MiddleRight);

        int cotHienTai = 0;

        for (int i = 0; i < danhSach.Length; i++)
        {
            if (!PlayerInventory.IsUnlocked(category, i)) continue;
            if (danhSach[i].model3D == null) continue;

            GameObject slotObj = new GameObject($"Mini_{category}_{i}");
            slotObj.transform.SetParent(gocMiniPreview, false);
            slotObj.transform.localPosition = new Vector3(
                cotHienTai * khoangCachNgang,
                -thuTuHang * khoangCachHang,
                0f
            );
            slotObj.transform.localRotation = Quaternion.identity;
            slotObj.transform.localScale = Vector3.one;

            GameObject banSao = Instantiate(danhSach[i].model3D, slotObj.transform);
            KhoaVatLyVaChuyenDong(banSao);
            banSao.SetActive(true);
            banSao.transform.localPosition = Vector3.zero;
            banSao.transform.localRotation = Quaternion.identity;
            banSao.transform.localScale = Vector3.one;

            Renderer[] renderers = banSao.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = new Bounds(slotObj.transform.position, Vector3.zero);
                bool hasValidBounds = false;

                foreach (Renderer r in renderers)
                {
                    if (r.bounds.size.sqrMagnitude > 0.0001f)
                    {
                        if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                        else bounds.Encapsulate(r.bounds);
                    }
                }

                if (hasValidBounds)
                {
                    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (maxDim > 0.001f)
                    {
                        float scaleFactor = Mathf.Clamp(kichThuocMini / maxDim, 0.02f, 20f);
                        banSao.transform.localScale = Vector3.one * scaleFactor;
                    }

                    bounds = new Bounds(slotObj.transform.position, Vector3.zero);
                    hasValidBounds = false;
                    foreach (Renderer r in renderers)
                    {
                        if (r.bounds.size.sqrMagnitude > 0.0001f)
                        {
                            if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                            else bounds.Encapsulate(r.bounds);
                        }
                    }
                    if (hasValidBounds)
                    {
                        banSao.transform.position += (slotObj.transform.position - bounds.center);
                    }
                }
            }

            danhSachMiniXoay.Add(slotObj.transform);
            cotHienTai++;
        }

        return cotHienTai;
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("menu");
    }

    private ShopItem[] GetCurrentList()
    {
        if (currentTab == "Helmet") return helmets;
        if (currentTab == "Character") return characters;
        return scooters;
    }

    private void HideAllModels()
    {
        foreach (var item in scooters) if (item.model3D != null) item.model3D.SetActive(false);
        foreach (var item in helmets) if (item.model3D != null) item.model3D.SetActive(false);
        foreach (var item in characters) if (item.model3D != null) item.model3D.SetActive(false);
    }

    private void UpdateItemUI()
    {
        HideAllModels();

        ShopItem[] currentList = GetCurrentList();
        if (currentList.Length == 0) return;

        ShopItem selectedItem = currentList[currentIndex];

        if (selectedItem.model3D != null) selectedItem.model3D.SetActive(true);
        if (itemNameText != null) itemNameText.text = selectedItem.itemName;

        bool isOwned = PlayerInventory.IsUnlocked(currentTab, currentIndex);

        if (buyButton != null && buyButtonText != null)
        {
            if (isOwned)
            {
                buyButton.interactable = false;
                buyButtonText.text = "OWNED";
            }
            else
            {
                buyButton.interactable = true;
                buyButtonText.text = $"BUY ({selectedItem.price} COINS)";
            }
        }
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null)
        {
            coinsText.text = $"COINS: {PlayerInventory.GetCoins()}";
        }
    }
}