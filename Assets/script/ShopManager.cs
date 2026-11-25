using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NutHieuUng : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float phongHover = 1.06f;
    public float nenBam = 0.94f;
    public float bienDoNhip = 0f;
    public float tocDoNhip = 2.2f;

    private Selectable sel;
    private Outline vien;
    private TMP_Text chu;
    private Vector3 scaleGoc;
    private Color mauChuGoc = Color.white;

    private bool hover, bam;
    private float tHover;
    private float scaleHienTai = 1f;

    private void Awake()
    {
        sel = GetComponent<Selectable>();
        chu = GetComponentInChildren<TMP_Text>(true);
        scaleGoc = transform.localScale;

        if (chu != null)
            mauChuGoc = chu.color;

        Graphic nen = GetComponent<Graphic>();
        if (nen != null)
        {
            vien = GetComponent<Outline>();
            if (vien == null)
                vien = gameObject.AddComponent<Outline>();

            vien.effectDistance = new Vector2(2f, -2f);
            vien.effectColor = new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; bam = false; }
    public void OnPointerDown(PointerEventData e) { bam = true; }
    public void OnPointerUp(PointerEventData e) { bam = false; }

    private void OnDisable()
    {
        hover = false;
        bam = false;
        tHover = 0f;
        scaleHienTai = 1f;
        transform.localScale = scaleGoc;

        if (chu != null)
            chu.color = mauChuGoc;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool ok = sel == null || sel.interactable;

        tHover = Mathf.MoveTowards(tHover, (ok && hover) ? 1f : 0f, dt * 10f);

        float s = 1f + (phongHover - 1f) * tHover;
        if (ok && bam) s *= nenBam;

        scaleHienTai = Mathf.Lerp(scaleHienTai, s, 1f - Mathf.Exp(-20f * dt));
        transform.localScale = scaleGoc * scaleHienTai;

        if (vien != null)
        {
            float a = ok ? Mathf.Lerp(0.35f, 1f, tHover) : 0.15f;
            vien.effectColor = new Color(1f, 1f, 1f, a);
        }

        if (chu != null)
        {
            chu.color = Color.Lerp(mauChuGoc, Color.white, tHover);
        }
    }
}

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public int price;
        public GameObject model3D;
        [HideInInspector] public string sourceModel;

        [Header("Custom Vehicle Paint")]
        public bool applyCustomPaint = false;
        public Color paintColor = Color.white;
        public string[] rendererNamesToPaint;
    }

    [Header("Shop Items (Tu dong dong bo tu VehicleCatalog)")]
    public bool dongBoTuVehicleCatalog = true;
    public ShopItem[] scooters;

    [Header("3D Showcase Stage")]
    public Transform showcaseStage;
    public float rotateSpeed = 40f;

    [Header("Mini Owned Shelf")]
    public Transform gocMiniPreview;
    public float kichThuocMini = 0.14f;
    public float khoangCachNgang = 0.22f;
    public float khoangCachHang = 0.22f;
    public float tocDoXoayMini = 35f;

    [Header("UI Elements")]
    public TMP_Text coinsText;
    public TMP_Text itemNameText;
    public Button buyButton;
    public TMP_Text buyButtonText;
    public TMP_Text thongBaoMuaText;

    [Header("Item Price Display")]
    public TMP_Text itemPriceText;
    public Color mauGia = new Color(1f, 0.82f, 0.18f, 1f);
    public float khoangCachGiaDuoiTen = 18f;
    public float chieuRongTenXe = 210f;
    public float chieuCaoTenXe = 44f;
    public float chieuRongTheGia = 185f;
    public float chieuCaoTheGia = 40f;

    [Header("Hiệu ứng tối giản (Màu trắng)")]
    public bool batVongSan = true;
    public float doCaoSan = 0.85f;
    public bool hieuUngNut = true;
    public float tocDoDemXu = 5f;

    [Header("Navigation")]
    public string tenSceneMenu = "menu";

    private const float DO_CAO_SAN_KHAU = 1.45f;
    private const float KICH_THUOC_CHUAN = 1.35f;

    private int currentIndex = 0;
    private bool dangThoat = false;

    private Coroutine coroutineThongBao;
    private readonly List<Transform> danhSachMiniXoay = new List<Transform>();
    private readonly Dictionary<int, GameObject> miniModelsCache = new Dictionary<int, GameObject>();
    private readonly Dictionary<string, GameObject> modelTheoLoai = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, int> daToMau = new Dictionary<string, int>();   // loại model -> index xe đã tô gần nhất

    private GameObject prefabScooterGoc;
    private GameObject holderNguon;                // object ẩn chứa 1 bản prefab dùng chung
    private GameObject goiNguon;                   // bản prefab đó (nằm dưới holder ẩn nên script không chạy)
    private readonly Dictionary<string, Transform> nodeNguon = new Dictionary<string, Transform>();
    private GameObject keMiniRoot;
    private Vector2 viTriThongBao;

    private float xuHienThi;
    private int xuMucTieu;
    private int xuDaHien;
    private bool xuKhoiTao = false;

    private Camera cam;
    private Material matVong;
    private LineRenderer vongSan;
    private TextMesh chuTieuDeMini;

    // Đồng bộ với launcher: ServerClient chỉ báo khi kho thật sự đổi
    private void OnEnable() { ServerClient.DaCapNhat += KhiKhoDoi; }
    private void OnDisable() { ServerClient.DaCapNhat -= KhiKhoDoi; }

    private void KhiKhoDoi()
    {
        if (scooters == null || scooters.Length == 0) return;

        UpdateCoinsUI();
        CapNhatToanBoDoSoHuuMini();
        HienThiXeTaiIndex(currentIndex);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (thongBaoMuaText != null)
        {
            viTriThongBao = thongBaoMuaText.rectTransform.anchoredPosition;
            thongBaoMuaText.gameObject.SetActive(false);
        }

        cam = Camera.main;

        CanChinhCameraVaSanKhau();

        if (dongBoTuVehicleCatalog)
            KhoiTaoMetadataTuCatalog();

        currentIndex = VehicleCatalog.GetSelectedIndex();
        if (scooters != null && scooters.Length > 0)
            currentIndex = Mathf.Clamp(currentIndex, 0, scooters.Length - 1);

        if (batVongSan)
            TaoVongSanDonGian();

        if (hieuUngNut)
            GanHieuUngNut();

        ChuanHoaTenXeUI();
        DamBaoPriceUI();
        TuDongKetNoiNutUI();

        UpdateCoinsUI();
        CapNhatToanBoDoSoHuuMini();
        HienThiXeTaiIndex(currentIndex);
    }

    private void KhoiTaoMetadataTuCatalog()
    {
        int total = VehicleCatalog.Count;
        scooters = new ShopItem[total];

        prefabScooterGoc = Resources.Load<GameObject>("Scooter");
        if (prefabScooterGoc == null)
            Debug.LogError("[ShopManager] Không tìm thấy Prefab 'Resources/Scooter'!");

        for (int i = 0; i < total; i++)
        {
            VehicleCatalog.VehicleData data = VehicleCatalog.Get(i);
            scooters[i] = new ShopItem
            {
                itemName = data.name,
                price = data.price,
                sourceModel = data.sourceModel,
                applyCustomPaint = true,
                paintColor = data.color
            };
        }
    }

    // Trả node xe (vd ElectricScooterDefault) trong 1 bản prefab dùng chung. Bản này nằm dưới holder
    // bị tắt nên Awake/OnEnable của các script trong prefab không chạy; chỉ dùng để nhân bản.
    private Transform LayNodeNguon(string sourceModel)
    {
        if (prefabScooterGoc == null) return null;

        if (goiNguon == null)
        {
            holderNguon = new GameObject("ShopSourceHolder");
            holderNguon.SetActive(false);
            goiNguon = Instantiate(prefabScooterGoc, holderNguon.transform);
            nodeNguon.Clear();
        }

        string key = sourceModel ?? "";
        if (!nodeNguon.TryGetValue(key, out Transform node) || node == null)
        {
            node = TimNodeXeThongMinh(goiNguon.transform, sourceModel);
            nodeNguon[key] = node;
        }
        return node;
    }

    private GameObject LayHoacTaoModelXe(int index)
    {
        if (scooters == null || index < 0 || index >= scooters.Length) return null;
        ShopItem item = scooters[index];
        if (item == null || prefabScooterGoc == null) return null;

        string loai = string.IsNullOrEmpty(item.sourceModel) ? "Default" : item.sourceModel;

        if (!modelTheoLoai.TryGetValue(loai, out GameObject model) || model == null)
        {
            Transform nguon = LayNodeNguon(item.sourceModel);

            if (nguon != null)
            {
                model = Instantiate(nguon.gameObject);
                model.name = loai;
                model.SetActive(true);

                foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.SetActive(true);
                    if (t.name.Contains("Minimap") || t.name.Contains("vetPhanh"))
                        t.gameObject.SetActive(false);
                }

                ChuanHoaModelSanKhau(model);
                modelTheoLoai[loai] = model;
                daToMau.Remove(loai);
            }
        }

        foreach (var kv in modelTheoLoai)
        {
            if (kv.Value != null)
                kv.Value.SetActive(kv.Key == loai);
        }

        // Chỉ tô lại khi đổi sang màu khác (làm mới UI không cần tô lại)
        if (model != null && (!daToMau.TryGetValue(loai, out int daTo) || daTo != index))
        {
            VehicleCatalog.ApplyPaint(model, item.paintColor);
            daToMau[loai] = index;
        }

        return model;
    }

    private void ChuanHoaModelSanKhau(GameObject model)
    {
        if (model == null) return;
        KhoaVatLyVaChuyenDong(model, this);

        Transform t = model.transform;
        t.SetParent(showcaseStage, false);

        model.SetActive(true);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        FitVaCanGiua(model, showcaseStage, KICH_THUOC_CHUAN, 0.05f);
        model.SetActive(false);
    }

    private static Transform TimNodeXeThongMinh(Transform root, string tenUuTien)
    {
        if (!string.IsNullOrEmpty(tenUuTien))
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == tenUuTien && t.GetComponentsInChildren<Renderer>(true).Length > 0)
                    return t;
            }
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            string n = t.name.ToLower();
            if ((n.Contains("scooter") || n.Contains("default")) && !n.Contains("black") && t.GetComponentsInChildren<Renderer>(true).Length > 0)
                return t;
        }

        foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!mr.name.Contains("Minimap") && !mr.name.Contains("vetPhanh"))
                return mr.transform.parent != null && mr.transform.parent != root ? mr.transform.parent : mr.transform;
        }

        return null;
    }

    private void TuDongKetNoiNutUI()
    {
        if (buyButton == null)
        {
            foreach (Button btn in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (btn == null) continue;
                string n = btn.gameObject.name.ToLower();
                if (n.Contains("buy") || n.Contains("equip"))
                {
                    buyButton = btn;
                    break;
                }
            }
        }

        if (buyButton != null)
        {
            if (buyButtonText == null)
                buyButtonText = buyButton.GetComponentInChildren<TMP_Text>(true);

            buyButton.onClick.RemoveListener(BuyItem);
            buyButton.onClick.AddListener(BuyItem);
        }

        foreach (Button btn in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn == null || btn == buyButton || btn.onClick.GetPersistentEventCount() != 0)
                continue;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>(true);
            if (txt == null) continue;

            string label = txt.text.Trim();
            if (label == ">")
            {
                btn.onClick.RemoveListener(NextItem);
                btn.onClick.AddListener(NextItem);
            }
            else if (label == "<")
            {
                btn.onClick.RemoveListener(PreviousItem);
                btn.onClick.AddListener(PreviousItem);
            }
        }
    }

    private void Update()
    {
        if (dangThoat) return;

        if (!MultiplayerAllInOne.DangGoChat)
        {
            if (Nhan(Phim.Esc))
            {
                BackToMenu();
                return;
            }

            if (Nhan(Phim.Trai))
                DoiXe(-1);
            else if (Nhan(Phim.Phai))
                DoiXe(1);
        }

        float dt = Time.deltaTime;

        if (showcaseStage != null)
            showcaseStage.Rotate(0f, rotateSpeed * dt, 0f, Space.World);

        float gocXoay = tocDoXoayMini * dt;
        for (int i = 0; i < danhSachMiniXoay.Count; i++)
        {
            Transform t = danhSachMiniXoay[i];
            if (t != null)
                t.Rotate(0f, gocXoay, 0f, Space.World);
        }

        CapNhatDemXu(dt);
    }

    private void OnDestroy()
    {
        if (matVong != null) Destroy(matVong);
        if (holderNguon != null) Destroy(holderNguon);
    }

    private enum Phim { Esc, Trai, Phai }

    private static bool Nhan(Phim p)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return false;
        switch (p)
        {
            case Phim.Esc: return kb.escapeKey.wasPressedThisFrame;
            case Phim.Trai: return kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
            case Phim.Phai: return kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
        }
        return false;
#else
        switch (p)
        {
            case Phim.Esc: return Input.GetKeyDown(KeyCode.Escape);
            case Phim.Trai: return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            case Phim.Phai: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        }
        return false;
#endif
    }

    private void GanHieuUngNut()
    {
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b != null && b.GetComponent<NutHieuUng>() == null)
                b.gameObject.AddComponent<NutHieuUng>();
        }
    }

    private void CapNhatDemXu(float dt)
    {
        if (coinsText == null || xuDaHien == xuMucTieu)
            return;

        float khoang = Mathf.Abs(xuMucTieu - xuHienThi);
        float buoc = Mathf.Max(30f, khoang * tocDoDemXu) * dt;

        xuHienThi = Mathf.MoveTowards(xuHienThi, xuMucTieu, buoc);
        int n = Mathf.RoundToInt(xuHienThi);

        if (n != xuDaHien)
        {
            xuDaHien = n;
            coinsText.text = $"COINS: {n}";
        }
    }

    private void CanChinhCameraVaSanKhau()
    {
        if (cam != null)
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0.8f, 0f), Quaternion.identity);

        if (showcaseStage == null)
            showcaseStage = new GameObject("AutoShowcaseStage").transform;

        showcaseStage.SetPositionAndRotation(new Vector3(0f, DO_CAO_SAN_KHAU, 4.5f), Quaternion.identity);
        showcaseStage.localScale = Vector3.one;

        if (gocMiniPreview == null)
            gocMiniPreview = new GameObject("AutoMiniShelf").transform;

        gocMiniPreview.SetPositionAndRotation(new Vector3(-2.55f, 2.02f, 4.5f), Quaternion.identity);
        gocMiniPreview.localScale = Vector3.one;
    }

    private void TaoVongSanDonGian()
    {
        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite == null) sprite = Shader.Find("Unlit/Color");
        matVong = new Material(sprite) { color = Color.white };

        GameObject go = new GameObject("VongSanTrang");
        go.transform.position = new Vector3(0f, doCaoSan, showcaseStage.position.z);

        vongSan = go.AddComponent<LineRenderer>();
        vongSan.useWorldSpace = false;
        vongSan.loop = true;
        vongSan.widthMultiplier = 0.03f;
        vongSan.sharedMaterial = matVong;
        vongSan.shadowCastingMode = ShadowCastingMode.Off;
        vongSan.receiveShadows = false;
        vongSan.startColor = Color.white;
        vongSan.endColor = Color.white;

        const int n = 48;
        vongSan.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            vongSan.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.95f, 0f, Mathf.Sin(a) * 0.95f));
        }
    }

    private static bool TinhBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool coBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy)
                continue;

            Bounds b = renderers[i].bounds;
            if (b.size.sqrMagnitude <= 0.0001f) continue;

            if (!coBounds) { bounds = b; coBounds = true; }
            else bounds.Encapsulate(b);
        }

        return coBounds;
    }

    private static void FitVaCanGiua(GameObject obj, Transform dich, float kichThuoc, float scaleMin, float scaleMax = 20f)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        if (!TinhBounds(renderers, out Bounds bounds)) return;

        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDim > 0.001f)
        {
            float scaleFactor = Mathf.Clamp(kichThuoc / maxDim, scaleMin, scaleMax);
            obj.transform.localScale = Vector3.one * scaleFactor;
        }

        if (TinhBounds(renderers, out bounds))
            obj.transform.position += dich.position - bounds.center;
    }

    private static void KhoaVatLyVaChuyenDong(GameObject obj, MonoBehaviour boQua)
    {
        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
            Destroy(rb);

        foreach (Collider col in obj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (Animator anim in obj.GetComponentsInChildren<Animator>(true))
            anim.enabled = false;

        foreach (MonoBehaviour script in obj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script != null && script != boQua)
                script.enabled = false;
        }
    }

    private static void TatBongDo(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.lightProbeUsage = LightProbeUsage.Off;
        }
    }

    private void ChuanHoaTenXeUI()
    {
        if (itemNameText == null) return;

        RectTransform nameRect = itemNameText.rectTransform;
        Vector2 size = nameRect.sizeDelta;

        size.x = Mathf.Max(size.x, chieuRongTenXe);
        size.y = Mathf.Max(size.y, chieuCaoTenXe);
        nameRect.sizeDelta = size;

        itemNameText.color = Color.white;
        itemNameText.enableAutoSizing = true;
        itemNameText.fontSizeMin = 16f;
        itemNameText.fontSizeMax = Mathf.Max(24f, itemNameText.fontSize);
        itemNameText.textWrappingMode = TextWrappingModes.NoWrap;
        itemNameText.overflowMode = TextOverflowModes.Ellipsis;
        itemNameText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void DatViTriTheGia(RectTransform cardRect, RectTransform nameRect)
    {
        float nameBottom = nameRect.anchoredPosition.y - nameRect.rect.height * nameRect.pivot.y;

        cardRect.anchorMin = nameRect.anchorMin;
        cardRect.anchorMax = nameRect.anchorMax;
        cardRect.pivot = new Vector2(nameRect.pivot.x, 1f);

        cardRect.anchoredPosition = new Vector2(
            nameRect.anchoredPosition.x,
            nameBottom - Mathf.Max(0f, khoangCachGiaDuoiTen)
        );

        cardRect.sizeDelta = new Vector2(chieuRongTheGia, chieuCaoTheGia);
        cardRect.localScale = Vector3.one;
    }

    private void DamBaoPriceUI()
    {
        if (itemNameText == null) return;

        ChuanHoaTenXeUI();
        RectTransform nameRect = itemNameText.rectTransform;

        if (itemPriceText != null)
        {
            DatViTriTheGia(itemPriceText.rectTransform, nameRect);

            itemPriceText.textWrappingMode = TextWrappingModes.NoWrap;
            itemPriceText.enableAutoSizing = true;
            itemPriceText.fontSizeMin = 14f;
            itemPriceText.fontSizeMax = 21f;
            itemPriceText.alignment = TextAlignmentOptions.Center;
            itemPriceText.raycastTarget = false;
            return;
        }

        Transform parent = nameRect.parent;
        if (parent == null) return;

        GameObject priceCard = new GameObject(
            "AutoItemPriceCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement)
        );
        priceCard.transform.SetParent(parent, false);

        RectTransform cardRect = priceCard.GetComponent<RectTransform>();
        DatViTriTheGia(cardRect, nameRect);

        LayoutElement layout = priceCard.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;

        Image background = priceCard.GetComponent<Image>();
        background.color = new Color(0.025f, 0.045f, 0.065f, 0.88f);
        background.raycastTarget = false;

        GameObject labelObject = new GameObject("PriceText", typeof(RectTransform));
        labelObject.transform.SetParent(priceCard.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 2f);
        labelRect.offsetMax = new Vector2(-10f, -2f);

        TextMeshProUGUI tmp = labelObject.AddComponent<TextMeshProUGUI>();
        tmp.font = itemNameText.font;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 21f;
        tmp.raycastTarget = false;

        itemPriceText = tmp;
    }

    private GameObject TaoModelMiniDocLap(int index)
    {
        if (scooters == null || index < 0 || index >= scooters.Length) return null;
        ShopItem item = scooters[index];
        if (item == null || prefabScooterGoc == null) return null;

        Transform nguon = LayNodeNguon(item.sourceModel);
        if (nguon == null) return null;

        GameObject miniXe = Instantiate(nguon.gameObject);
        miniXe.name = "MiniModel";
        miniXe.SetActive(true);

        foreach (Transform t in miniXe.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.SetActive(true);
            if (t.name.Contains("Minimap") || t.name.Contains("vetPhanh"))
                t.gameObject.SetActive(false);
        }

        KhoaVatLyVaChuyenDong(miniXe, this);
        TatBongDo(miniXe);

        // ApplyPaint tự tạo bản sao Material riêng cho model này
        VehicleCatalog.ApplyPaint(miniXe, item.paintColor);
        return miniXe;
    }

    private void CapNhatToanBoDoSoHuuMini()
    {
        if (gocMiniPreview == null || scooters == null) return;

        if (keMiniRoot == null)
        {
            keMiniRoot = new GameObject("KeMiniRoot");
            keMiniRoot.transform.SetParent(gocMiniPreview, false);

            GameObject titleObj = new GameObject("MiniShelfTitle");
            titleObj.transform.SetParent(keMiniRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0.45f, 0.35f, 0f);
            chuTieuDeMini = titleObj.AddComponent<TextMesh>();
            chuTieuDeMini.text = "OWNED SCOOTERS";
            chuTieuDeMini.characterSize = 0.035f;
            chuTieuDeMini.fontSize = 36;
            chuTieuDeMini.anchor = TextAnchor.MiddleCenter;
            chuTieuDeMini.color = Color.white;
        }

        danhSachMiniXoay.Clear();
        const int soCot = 5;
        int viTriSlot = 0;

        for (int i = 0; i < scooters.Length; i++)
        {
            bool daMua = VehicleCatalog.IsOwned(i);

            if (!daMua)
            {
                if (miniModelsCache.TryGetValue(i, out GameObject cu) && cu != null)
                    cu.SetActive(false);
                continue;
            }

            if (!miniModelsCache.TryGetValue(i, out GameObject slotObj) || slotObj == null)
            {
                slotObj = new GameObject($"MiniSlot_{i}");
                slotObj.transform.SetParent(keMiniRoot.transform, false);

                GameObject miniXe = TaoModelMiniDocLap(i);
                if (miniXe != null)
                {
                    miniXe.transform.SetParent(slotObj.transform, false);
                    miniXe.transform.localPosition = Vector3.zero;
                    miniXe.transform.localRotation = Quaternion.identity;
                    miniXe.transform.localScale = Vector3.one;
                    FitVaCanGiua(miniXe, slotObj.transform, kichThuocMini, 0.01f);
                }

                miniModelsCache[i] = slotObj;
            }

            int hang = viTriSlot / soCot;
            int cot = viTriSlot % soCot;
            slotObj.transform.localPosition = new Vector3(cot * khoangCachNgang, -hang * khoangCachHang, 0f);
            slotObj.SetActive(true);

            danhSachMiniXoay.Add(slotObj.transform);
            viTriSlot++;
        }
    }

    public void SelectTab(string tab)
    {
        HienThiXeTaiIndex(currentIndex);
    }

    public void NextItem() => DoiXe(1);
    public void PreviousItem() => DoiXe(-1);

    private void DoiXe(int huong)
    {
        if (scooters == null || scooters.Length == 0) return;

        currentIndex = (currentIndex + huong + scooters.Length) % scooters.Length;
        HienThiXeTaiIndex(currentIndex);
    }

    private void HienThiXeTaiIndex(int index)
    {
        if (scooters == null || scooters.Length == 0) return;
        currentIndex = Mathf.Clamp(index, 0, scooters.Length - 1);

        GameObject xeCanHien = LayHoacTaoModelXe(currentIndex);
        if (xeCanHien != null) xeCanHien.SetActive(true);

        ShopItem item = scooters[currentIndex];
        bool daSoHuu = VehicleCatalog.IsOwned(currentIndex);
        bool dangChon = VehicleCatalog.GetSelectedIndex() == currentIndex;

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName.ToUpper();
            itemNameText.color = Color.white;
        }

        if (itemPriceText != null)
        {
            if (daSoHuu)
            {
                itemPriceText.text = "OWNED";
                itemPriceText.color = Color.white;
            }
            else
            {
                itemPriceText.text = $"{item.price} COINS";
                itemPriceText.color = mauGia;
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable = !dangChon;
            if (buyButtonText != null)
            {
                if (dangChon) buyButtonText.text = "EQUIPPED";
                else if (daSoHuu) buyButtonText.text = "EQUIP";
                else buyButtonText.text = "BUY";
            }
        }
    }

    public void BuyItem()
    {
        if (scooters == null || currentIndex < 0 || currentIndex >= scooters.Length) return;

        ShopItem item = scooters[currentIndex];
        bool daSoHuu = VehicleCatalog.IsOwned(currentIndex);

        if (daSoHuu)
        {
            VehicleCatalog.SelectVehicle(currentIndex);
            HienThongBao($"EQUIPPED {item.itemName.ToUpper()}!", Color.white);
            HienThiXeTaiIndex(currentIndex);
            return;
        }

        if (!VehicleCatalog.TryPurchase(currentIndex))
        {
            HienThongBao("NOT ENOUGH COINS!", new Color(1f, 0.35f, 0.35f));
            return;
        }

        VehicleCatalog.SelectVehicle(currentIndex);

        UpdateCoinsUI();
        CapNhatToanBoDoSoHuuMini();
        HienThiXeTaiIndex(currentIndex);
        HienThongBao($"PURCHASED {item.itemName.ToUpper()}!", new Color(1f, 0.85f, 0.2f));
    }

    private void UpdateCoinsUI()
    {
        xuMucTieu = PlayerInventory.GetCoins();
        if (!xuKhoiTao)
        {
            xuKhoiTao = true;
            xuHienThi = xuMucTieu;
            xuDaHien = xuMucTieu;
            if (coinsText != null)
                coinsText.text = $"COINS: {xuMucTieu}";
        }
    }

    private void HienThongBao(string noiDung, Color mau)
    {
        if (thongBaoMuaText == null) return;

        if (coroutineThongBao != null) StopCoroutine(coroutineThongBao);
        coroutineThongBao = StartCoroutine(ChayThongBao(noiDung, mau));
    }

    private IEnumerator ChayThongBao(string noiDung, Color mau)
    {
        thongBaoMuaText.gameObject.SetActive(true);
        thongBaoMuaText.text = noiDung;
        thongBaoMuaText.color = mau;
        thongBaoMuaText.rectTransform.anchoredPosition = viTriThongBao;

        yield return new WaitForSecondsRealtime(1.8f);

        thongBaoMuaText.gameObject.SetActive(false);
        coroutineThongBao = null;
    }

    public void BackToMenu()
    {
        if (dangThoat) return;
        dangThoat = true;
        DieuHuong.VeMenu(tenSceneMenu);
    }
}