using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public static Color MauChung = Color.white;
    public float phongHover = 1.06f;
    public float nenBam = 0.94f;

    private Selectable sel;
    private Outline vien;
    private Vector3 scaleGoc;
    private bool hover, bam;
    private bool namTrongLayout;
    private float tHover;
    private float scaleHienTai = 1f;

    private void Awake()
    {
        sel = GetComponent<Selectable>();
        scaleGoc = transform.localScale;
        namTrongLayout = GetComponentInParent<LayoutGroup>() != null;

        Graphic nen = GetComponent<Graphic>();
        if (nen != null)
        {
            // Use a dedicated effect so authored outline/material settings remain intact.
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
        if (!namTrongLayout) transform.localScale = scaleGoc;
    }

    private void OnDestroy()
    {
        if (vien != null) Destroy(vien);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool ok = sel == null || sel.interactable;

        tHover = Mathf.MoveTowards(tHover, (ok && hover) ? 1f : 0f, dt * 10f);
        float s = 1f + (phongHover - 1f) * tHover;
        if (ok && bam) s *= nenBam;

        scaleHienTai = Mathf.Lerp(scaleHienTai, s, 1f - Mathf.Exp(-20f * dt));
        if (!namTrongLayout) transform.localScale = scaleGoc * scaleHienTai;

        if (vien != null)
        {
            float a = ok ? Mathf.Lerp(0.35f, 1f, tHover) : 0.15f;
            vien.effectColor = new Color(1f, 1f, 1f, a);
        }
    }
}

public class SelectManager : MonoBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        public string displayName;
        public string sceneName;
        public Sprite previewImage;
    }

    [Header("Vehicle Catalog")]
    public string[] tenCacMauXe;

    [Header("Danh sach Map")]
    public MapOption[] maps;

    [Header("San khau trung bay 3D")]
    public Transform showcaseStage;
    public float rotateSpeed = 40f;

    [Header("Hop kinh xe - HOST")]
    public Vector3 hopViTri = new Vector3(0f, 1.3f, 3f);
    public float hopKichThuoc = 1f;

    [Header("Hop kinh xe - GUEST")]
    public Vector3 hopViTriMotVat = new Vector3(0f, 1.2f, 3f);
    public float hopKichThuocMotVat = 1.4f;

    [Header("Khung chon Map")]
    public float mapPickerY = 0.14f;
    public Vector2 mapPreviewSize = new Vector2(400f, 225f);

    [Header("Giao dien UI")]
    public TMPro.TMP_Text itemNameText;
    public TMPro.TMP_Text warningText;
    public Button confirmButton;

    public float doHaChuTenXe = 40f;
    public RectTransform[] cacUICanHa;

    [Header("Hieu ung nut")]
    public bool hieuUngNut = true;

    [Header("Navigation")]
    public string tenSceneLobby = SceneNames.Lobby;
    public string tenSceneMenu = SceneNames.Menu;

    private const string TAB_SCOOTER = "Scooter";

    private int scooterIndex;
    private int mapIndex;
    private bool dangChuyenScene;
    private bool laChuPhong = true;

    private Vector3 hopViTriHT;
    private float hopKichThuocHT;

    private Camera cam;
    private Font fontMacDinh;
    private Transform trucXoayXe;

    private readonly Dictionary<string, GameObject> modelTheoLoai = new Dictionary<string, GameObject>();
    private GameObject prefabScooterGoc;

    private GameObject mapPickerGO;
    private Text lblMap;
    private Image imgMap;
    private Outline mapOutline;

    private Material matKinh;
    private Material matDe;
    private Material matLine;

    private LineRenderer lrVienHop;
    private float thoiDiemKiemTraVaiTro;
    private Canvas cachedCanvas;

    private void DamBaoEventSystemHoatDong()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }
        es.enabled = true;
        if (es.gameObject != null && !es.gameObject.activeSelf)
            es.gameObject.SetActive(true);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        DamBaoEventSystemHoatDong();

        if (VehicleCatalog.Count <= 0)
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "NO VEHICLES ARE AVAILABLE. PLEASE RETURN TO THE LOBBY.";
            }
            if (confirmButton != null) confirmButton.interactable = false;
            Debug.LogError("[SelectManager] VehicleCatalog is empty; selection controls are disabled.");
            return;
        }

        cam = Camera.main;
        cachedCanvas = FindFirstObjectByType<Canvas>();

        ButtonFx.MauChung = Color.white;
        tenCacMauXe = VehicleCatalog.GetAllNames();

        if (itemNameText != null)
        {
            itemNameText.color = Color.white;
            itemNameText.rectTransform.anchoredPosition += new Vector2(0f, -doHaChuTenXe);
        }

        if (cacUICanHa != null)
        {
            foreach (RectTransform r in cacUICanHa)
            {
                if (r != null && (itemNameText == null || r != itemNameText.rectTransform))
                    r.anchoredPosition += new Vector2(0f, -doHaChuTenXe);
            }
        }

        laChuPhong = string.IsNullOrWhiteSpace(LauncherArgs.Room) || LauncherArgs.IsRoomHost;

        bool coMap = laChuPhong && maps != null && maps.Length > 0;
        hopViTriHT = coMap ? hopViTri : hopViTriMotVat;
        hopKichThuocHT = coMap ? hopKichThuoc : hopKichThuocMotVat;

        scooterIndex = VehicleCatalog.GetSelectedIndex();
        if (!VehicleCatalog.IsOwned(scooterIndex))
            scooterIndex = TimXeDauTienDaSoHuu();

        scooterIndex = Mathf.Clamp(scooterIndex, 0, VehicleCatalog.Count - 1);

        string savedMap = PlayerInventory.GetSelectedMap();
        if (maps != null)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] != null && maps[i].sceneName == savedMap)
                {
                    mapIndex = i;
                    break;
                }
            }
        }

        TaoTaiNguyenDungChung();
        TuDongNoiNutUI();
        CanChinhCameraVaSanKhau();

        prefabScooterGoc = Resources.Load<GameObject>("Scooter");
        if (prefabScooterGoc == null)
            Debug.LogError("[SelectManager] Không tìm thấy Prefab 'Resources/Scooter'!");

        if (laChuPhong) TaoUIChonMap();
        DongBoLopHienThiUI();

        SelectTab(TAB_SCOOTER);
        CapNhatGiaoDien();
    }

    private float thoiDiemDongBoUI;

    private void LateUpdate()
    {
        if (Time.unscaledTime < thoiDiemDongBoUI) return;
        thoiDiemDongBoUI = Time.unscaledTime + 0.25f;
        DongBoLopHienThiUI();
    }

    private void DongBoLopHienThiUI()
    {
        if (cachedCanvas == null) cachedCanvas = FindFirstObjectByType<Canvas>();
        if (cachedCanvas == null) return;

        Transform root = cachedCanvas.rootCanvas.transform;
        bool bangHuongDanDangMo = false;

        foreach (Image img in root.GetComponentsInChildren<Image>(false))
        {
            if (img == null || img.GetComponent<Button>() != null) continue;
            if (mapPickerGO != null && img.transform.IsChildOf(mapPickerGO.transform)) continue;

            RectTransform rt = img.rectTransform;
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            if (rt.rect.width > 500f && rt.rect.height > 300f)
            {
                bangHuongDanDangMo = true;

                Color c = img.color;
                if (c.r > 0.55f && c.g > 0.55f && c.b > 0.55f)
                    img.color = new Color(0.10f, 0.12f, 0.16f, 0.96f);
            }
        }

        if (mapPickerGO != null)
        {
            bool nenHienMap = laChuPhong && maps != null && maps.Length > 0 && !bangHuongDanDangMo;
            if (mapPickerGO.activeSelf != nenHienMap)
                mapPickerGO.SetActive(nenHienMap);

            if (nenHienMap && mapPickerGO.transform.GetSiblingIndex() != root.childCount - 1)
                mapPickerGO.transform.SetAsLastSibling();
        }
    }

    private GameObject LayHoacTaoXe(int index)
    {
        if (prefabScooterGoc == null || index < 0 || index >= VehicleCatalog.Count) return null;

        VehicleCatalog.VehicleData data = VehicleCatalog.Get(index);
        string loai = string.IsNullOrEmpty(data.sourceModel) ? "Default" : data.sourceModel;

        if (!modelTheoLoai.TryGetValue(loai, out GameObject holder) || holder == null)
        {
            holder = new GameObject($"XeModel_{loai}");
            holder.transform.SetParent(trucXoayXe, false);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = Vector3.one;

            GameObject tempRoot = Instantiate(prefabScooterGoc);
            Transform nguon = TimNodeThongMinh(tempRoot.transform, data.sourceModel);

            if (nguon != null)
            {
                GameObject banSao = Instantiate(nguon.gameObject, holder.transform);
                banSao.name = loai;
                banSao.SetActive(true);

                foreach (Rigidbody rb in banSao.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
                foreach (Collider col in banSao.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                foreach (MonoBehaviour mb in banSao.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;

                foreach (Transform t in banSao.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.SetActive(true);
                    t.gameObject.layer = 0;
                }

                Renderer[] renderers = banSao.GetComponentsInChildren<Renderer>(true);
                Bounds b = new Bounds();
                bool coMesh = false;

                foreach (Renderer r in renderers)
                {
                    if (r.gameObject.name.Contains("Minimap") || r.gameObject.name.Contains("vetPhanh"))
                    {
                        r.enabled = false;
                        continue;
                    }
                    r.enabled = true;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    if (!coMesh) { b = r.bounds; coMesh = true; }
                    else b.Encapsulate(r.bounds);
                }

                if (coMesh)
                {
                    float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (maxDim > 0.0001f)
                    {
                        float factor = 0.72f / maxDim;
                        banSao.transform.localScale *= factor;

                        coMesh = false;
                        foreach (Renderer r in renderers)
                        {
                            if (!r.enabled) continue;
                            if (!coMesh) { b = r.bounds; coMesh = true; }
                            else b.Encapsulate(r.bounds);
                        }
                    }

                    banSao.transform.position += holder.transform.position - b.center;
                }
            }

            Destroy(tempRoot);
            modelTheoLoai[loai] = holder;
        }

        foreach (var kv in modelTheoLoai)
        {
            if (kv.Value != null)
                kv.Value.SetActive(kv.Key == loai);
        }

        VehicleCatalog.ApplyPaint(holder, data.color);
        return holder;
    }

    private int TimXeDauTienDaSoHuu()
    {
        for (int i = 0; i < VehicleCatalog.Count; i++)
        {
            if (VehicleCatalog.IsOwned(i)) return i;
        }
        return 0;
    }

    private void CapNhatVaiTro()
    {
        if (Time.unscaledTime < thoiDiemKiemTraVaiTro) return;
        thoiDiemKiemTraVaiTro = Time.unscaledTime + 0.5f;

        bool moi = string.IsNullOrWhiteSpace(LauncherArgs.Room) || LauncherArgs.IsRoomHost;
        if (moi == laChuPhong) return;

        laChuPhong = moi;

        if (laChuPhong)
        {
            if (mapPickerGO == null) TaoUIChonMap();
            else mapPickerGO.SetActive(true);
            DongBoLopHienThiUI();
        }
        else
        {
            if (mapPickerGO != null) mapPickerGO.SetActive(false);
        }

        CanChinhCameraTheoVaiTro();
        CapNhatGiaoDien();
    }

    private void CanChinhCameraTheoVaiTro()
    {
        bool coMap = laChuPhong && maps != null && maps.Length > 0;
        hopViTriHT = coMap ? hopViTri : hopViTriMotVat;
        hopKichThuocHT = coMap ? hopKichThuoc : hopKichThuocMotVat;

        if (trucXoayXe != null && trucXoayXe.parent != null)
        {
            Transform hop = trucXoayXe.parent;
            hop.position = hopViTriHT;
            hop.localScale = Vector3.one * hopKichThuocHT;
        }
    }

    private void Update()
    {
        if (dangChuyenScene) return;

        CapNhatVaiTro();

        if (!MultiplayerAllInOne.DangGoChat)
        {
            if (InputEscapeUtility.TryConsume())
            {
                if (HelpSystem.DongNeuDangMo() || MenuLeaderboard.DongNeuDangMo()) return;
                BackToMenu();
                return;
            }
            if (Nhan(Phim.Trai)) PreviousItem();
            else if (Nhan(Phim.Phai)) NextItem();
            else if (Nhan(Phim.Enter)) ConfirmSelection();
        }

        if (trucXoayXe != null)
            trucXoayXe.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnDestroy()
    {
        if (matKinh != null) Destroy(matKinh);
        if (matDe != null) Destroy(matDe);
        if (matLine != null) Destroy(matLine);
    }

    private enum Phim { Esc, Trai, Phai, Enter }

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
            case Phim.Enter: return kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
        }
        return false;
#else
        switch (p)
        {
            case Phim.Esc: return Input.GetKeyDown(KeyCode.Escape);
            case Phim.Trai: return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            case Phim.Phai: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            case Phim.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }
        return false;
#endif
    }

    private void TaoTaiNguyenDungChung()
    {
        fontMacDinh = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Shader unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Standard");

        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite == null) sprite = unlit;

        matKinh = TaoMaterialKinh(new Color(1f, 1f, 1f, 0.12f));
        matDe = new Material(unlit) { color = new Color(0.16f, 0.18f, 0.20f) };
        matLine = new Material(sprite) { color = Color.white };
    }

    private Material TaoMaterialKinh(Color mauKinh)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            Material mat = new Material(urpLit);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", mauKinh);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }

        Shader standard = Shader.Find("Standard");
        if (standard == null) standard = Shader.Find("Unlit/Color");

        Material stdMat = new Material(standard);
        if (stdMat.HasProperty("_Mode")) stdMat.SetFloat("_Mode", 3f);
        stdMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        stdMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        stdMat.SetInt("_ZWrite", 0);
        stdMat.renderQueue = 3000;
        stdMat.color = mauKinh;
        return stdMat;
    }

    private void GanHieuUngNut(Button b)
    {
        if (!hieuUngNut || b == null) return;
        if (b.GetComponent<ButtonFx>() == null)
            b.gameObject.AddComponent<ButtonFx>();
    }

    private void TuDongNoiNutUI()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            if (btn == null) continue;
            GanHieuUngNut(btn);

            string tenBtn = btn.gameObject.name.ToLower();
            TMPro.TMP_Text tmp = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            Text uguiTxt = btn.GetComponentInChildren<Text>(true);
            string label = (tmp != null ? tmp.text : (uguiTxt != null ? uguiTxt.text : "")).Trim().ToUpper();

            bool laNutConfirm = btn == confirmButton
                || tenBtn.Contains("confirm")
                || tenBtn.Contains("xacnhan")
                || tenBtn.Contains("xac_nhan")
                || tenBtn.Contains("select")
                || tenBtn.Contains("chon")
                || label == "CONFIRM"
                || label == "XÁC NHẬN"
                || label == "XAC NHAN"
                || label == "SELECT"
                || label == "CHỌN"
                || label == "OK";

            if (laNutConfirm)
            {
                if (confirmButton == null) confirmButton = btn;
                btn.onClick.RemoveListener(ConfirmSelection);
                btn.onClick.AddListener(ConfirmSelection);
                continue;
            }

            if (tenBtn.Contains("back") || label == "BACK" || label == "QUAY LẠI")
            {
                btn.onClick.RemoveListener(BackToMenu);
                btn.onClick.AddListener(BackToMenu);
                continue;
            }

            if (btn.onClick.GetPersistentEventCount() != 0) continue;

            if (label == ">" || tenBtn.Contains("next"))
            {
                btn.onClick.RemoveListener(NextItem);
                btn.onClick.AddListener(NextItem);
            }
            else if (label == "<" || tenBtn.Contains("pre") || tenBtn.Contains("prev"))
            {
                btn.onClick.RemoveListener(PreviousItem);
                btn.onClick.AddListener(PreviousItem);
            }
        }
    }

    private void TaoUIChonMap()
    {
        if (!laChuPhong || maps == null || maps.Length == 0) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform goc = canvas.rootCanvas.transform;
        Transform cu = goc.Find("MapPicker");
        if (cu != null) Destroy(cu.gameObject);

        mapPickerGO = new GameObject("MapPicker", typeof(RectTransform));
        mapPickerGO.transform.SetParent(goc, false);

        RectTransform rt = mapPickerGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, mapPickerY);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 34f);

        lblMap = TaoText(mapPickerGO.transform, "LblMap", Vector2.zero, new Vector2(160f, 30f), 20);
        lblMap.color = Color.white;

        TaoNut(mapPickerGO.transform, "<", new Vector2(-100f, 0f), () => DoiMap(-1));
        TaoNut(mapPickerGO.transform, ">", new Vector2(100f, 0f), () => DoiMap(1));

        Vector2 viTriAnh = new Vector2(0f, 17f + 8f + mapPreviewSize.y / 2f);

        GameObject h = new GameObject("MapPreview", typeof(RectTransform), typeof(Image));
        h.transform.SetParent(mapPickerGO.transform, false);

        RectTransform hr = h.GetComponent<RectTransform>();
        hr.anchoredPosition = viTriAnh;
        hr.sizeDelta = mapPreviewSize;

        imgMap = h.GetComponent<Image>();
        imgMap.preserveAspect = true;
        imgMap.raycastTarget = false;

        mapOutline = h.AddComponent<Outline>();
        mapOutline.effectDistance = new Vector2(3f, -3f);
        mapOutline.effectColor = Color.white;

        CapNhatMap();
    }

    private void DoiMap(int huong)
    {
        if (!laChuPhong || maps == null || maps.Length == 0) return;

        mapIndex = (mapIndex + huong + maps.Length) % maps.Length;
        CapNhatMap();

        PlayerInventory.SetSelectedMap(maps[mapIndex].sceneName);
    }

    private Text TaoText(Transform cha, string ten, Vector2 viTri, Vector2 kichThuoc, int coChu = 20)
    {
        GameObject go = new GameObject(ten, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(cha, false);

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchoredPosition = viTri;
        r.sizeDelta = kichThuoc;

        Text t = go.GetComponent<Text>();
        t.font = fontMacDinh;
        t.fontSize = coChu;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private void TaoNut(Transform cha, string nhan, Vector2 viTri, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + nhan, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(cha, false);

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchoredPosition = viTri;
        r.sizeDelta = new Vector2(34f, 30f);

        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        GanHieuUngNut(btn);

        Text t = TaoText(go.transform, "Text", Vector2.zero, r.sizeDelta, 18);
        t.text = nhan;
        t.color = Color.white;
    }

    private void CapNhatMap()
    {
        if (maps == null || maps.Length == 0 || mapIndex >= maps.Length || maps[mapIndex] == null)
            return;

        MapOption m = maps[mapIndex];

        if (lblMap != null)
            lblMap.text = string.IsNullOrEmpty(m.displayName) ? m.sceneName : m.displayName;

        if (imgMap != null)
        {
            imgMap.sprite = m.previewImage;
            imgMap.color = m.previewImage != null ? Color.white : new Color(0f, 0f, 0f, 0.25f);
        }
    }

    private void CanChinhCameraVaSanKhau()
    {
        if (cam != null)
        {
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0.9f, 0f), Quaternion.identity);
            cam.cullingMask = -1;
            cam.nearClipPlane = 0.05f;
        }

        if (showcaseStage == null)
            showcaseStage = new GameObject("AutoSelectStage").transform;

        for (int i = showcaseStage.childCount - 1; i >= 0; i--)
            Destroy(showcaseStage.GetChild(i).gameObject);

        showcaseStage.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        showcaseStage.localScale = Vector3.one;

        trucXoayXe = TaoHopKinh("HopKinh_Xe", hopViTriHT, hopKichThuocHT);
    }

    private Transform TaoHopKinh(string ten, Vector3 viTri, float kichThuoc)
    {
        GameObject hop = new GameObject(ten);
        hop.transform.SetParent(showcaseStage, false);
        hop.transform.position = viTri;
        hop.transform.localScale = Vector3.one * kichThuoc;

        GameObject de = GameObject.CreatePrimitive(PrimitiveType.Cube);
        de.name = "DeHop";
        Destroy(de.GetComponent<Collider>());
        de.transform.SetParent(hop.transform, false);
        de.transform.localPosition = new Vector3(0f, -0.52f, 0f);
        de.transform.localScale = new Vector3(1.06f, 0.06f, 1.06f);
        de.GetComponent<MeshRenderer>().sharedMaterial = matDe;

        GameObject kinh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kinh.name = "MatKinh";
        Destroy(kinh.GetComponent<Collider>());
        kinh.transform.SetParent(hop.transform, false);
        kinh.transform.localPosition = Vector3.zero;
        kinh.transform.localScale = Vector3.one;
        MeshRenderer mrKinh = kinh.GetComponent<MeshRenderer>();
        mrKinh.sharedMaterial = matKinh;
        mrKinh.shadowCastingMode = ShadowCastingMode.Off;

        GameObject vienObj = new GameObject("VienHop");
        vienObj.transform.SetParent(hop.transform, false);
        lrVienHop = vienObj.AddComponent<LineRenderer>();
        lrVienHop.useWorldSpace = false;
        lrVienHop.loop = false;
        lrVienHop.widthMultiplier = 0.015f;
        lrVienHop.sharedMaterial = matLine;
        lrVienHop.startColor = Color.white;
        lrVienHop.endColor = Color.white;

        Vector3[] pts = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f)
        };
        lrVienHop.positionCount = pts.Length;
        lrVienHop.SetPositions(pts);

        GameObject truc = new GameObject("TrucXoay");
        truc.transform.SetParent(hop.transform, false);
        truc.transform.localPosition = Vector3.zero;
        return truc.transform;
    }

    private static Transform TimNodeThongMinh(Transform root, string tenUuTien)
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

    public void SelectTab(string tab)
    {
        CapNhatGiaoDien();
    }

    public void NextItem() => DoiXe(1);
    public void PreviousItem() => DoiXe(-1);

    private void DoiXe(int huong)
    {
        int total = VehicleCatalog.Count;
        if (total <= 0) return;

        scooterIndex = (scooterIndex + huong + total) % total;
        CapNhatGiaoDien();
    }

    private void CapNhatGiaoDien()
    {
        if (VehicleCatalog.Count <= 0)
        {
            if (confirmButton != null) confirmButton.interactable = false;
            if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "NO VEHICLES ARE AVAILABLE.";
            }
            return;
        }

        LayHoacTaoXe(scooterIndex);

        VehicleCatalog.VehicleData data = VehicleCatalog.Get(scooterIndex);
        bool daSoHuu = VehicleCatalog.IsOwned(scooterIndex);

        if (itemNameText != null)
        {
            itemNameText.text = data.name.ToUpper();
            itemNameText.color = Color.white;
        }

        if (warningText != null)
        {
            warningText.gameObject.SetActive(!daSoHuu);
            if (!daSoHuu)
                warningText.text = $"LOCKED ({data.price} COINS IN SHOP)";
        }

        if (confirmButton != null)
            confirmButton.interactable = daSoHuu;
    }

    private string TimTenSceneLobbyHopLe()
    {
        if (!string.IsNullOrEmpty(tenSceneLobby) && Application.CanStreamedLevelBeLoaded(tenSceneLobby))
            return tenSceneLobby;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(name) && name.ToLower().Contains("lobby"))
                return name;
        }

        return null;
    }

    public void ConfirmSelection()
    {
        if (VehicleCatalog.Count <= 0)
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "NO VEHICLES ARE AVAILABLE. PLEASE RETURN TO THE LOBBY.";
            }
            return;
        }
        bool inRoom = !string.IsNullOrWhiteSpace(LauncherArgs.Room);
        Debug.Log($"[SelectManager] Confirm index={scooterIndex}, owned={VehicleCatalog.IsOwned(scooterIndex)}, inRoom={inRoom}, lobby={tenSceneLobby}");
        if (dangChuyenScene) return;

        if (!VehicleCatalog.IsOwned(scooterIndex))
        {
            int xeSoHuu = TimXeDauTienDaSoHuu();
            if (VehicleCatalog.IsOwned(xeSoHuu))
            {
                scooterIndex = xeSoHuu;
                CapNhatGiaoDien();
            }
            else
            {
                if (warningText != null)
                {
                    warningText.gameObject.SetActive(true);
                    warningText.text = "YOU DO NOT OWN THIS SCOOTER!";
                }
                return;
            }
        }

        if (!VehicleCatalog.SelectVehicle(scooterIndex))
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "COULD NOT SAVE THIS SCOOTER. PLEASE TRY AGAIN.";
            }
            return;
        }
        dangChuyenScene = true;

        bool laChuPhongHienTai = inRoom && LauncherArgs.IsRoomHost;

        if (laChuPhongHienTai && maps != null && maps.Length > 0 && mapIndex < maps.Length && maps[mapIndex] != null)
        {
            string selectedMap = maps[mapIndex].sceneName;
            if (!string.IsNullOrWhiteSpace(selectedMap) && Application.CanStreamedLevelBeLoaded(selectedMap))
                PlayerInventory.SetSelectedMap(selectedMap);
            else if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = $"MAP '{selectedMap}' IS NOT INCLUDED IN THIS BUILD.";
            }
        }

        if (inRoom)
        {
            LobbyManager.DanhDauCoSanPhongTuVanTruoc();

            string sceneDich = TimTenSceneLobbyHopLe();
            if (!string.IsNullOrEmpty(sceneDich))
            {
                SceneManager.LoadScene(sceneDich);
            }
            else
            {
                Debug.LogError($"[SelectManager] Không tìm thấy scene Lobby '{tenSceneLobby}' trong Build Settings!");
                dangChuyenScene = false;
            }
        }
        else
        {
            string mapToLoad = (maps != null && maps.Length > 0 && mapIndex < maps.Length && maps[mapIndex] != null)
                ? maps[mapIndex].sceneName
                : tenSceneMenu;
            if (string.IsNullOrWhiteSpace(mapToLoad) || !Application.CanStreamedLevelBeLoaded(mapToLoad))
            {
                Debug.LogError($"[SelectManager] Cannot load selected scene '{mapToLoad}'.");
                if (warningText != null)
                {
                    warningText.gameObject.SetActive(true);
                    warningText.text = "THE SELECTED MAP IS NOT AVAILABLE IN THIS BUILD.";
                }
                dangChuyenScene = false;
                return;
            }
            SceneManager.LoadScene(mapToLoad);
        }
    }

    // BACK / ESC = quay về PHÒNG CHỜ (Lobby), giữ nguyên phòng, KHÔNG lưu lựa chọn đang xem.
    // (CONFIRM = lưu lựa chọn rồi về phòng chờ). Chỉ khi không ở trong phòng mới về menu.
    // Giữ tên BackToMenu để các nút / script đang gọi hàm này vẫn chạy.
    public void BackToMenu()
    {
        if (dangChuyenScene) return;
        dangChuyenScene = true;

        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            LobbyManager.DanhDauCoSanPhongTuVanTruoc();

            string sceneDich = TimTenSceneLobbyHopLe();
            if (!string.IsNullOrEmpty(sceneDich))
            {
                SceneManager.LoadScene(sceneDich);
                return;
            }

            // Không tìm thấy scene Lobby: ở lại màn này, TUYỆT ĐỐI không rời phòng
            Debug.LogError($"[SelectManager] Không tìm thấy scene Lobby '{tenSceneLobby}' trong Build Settings!");
            dangChuyenScene = false;
            return;
        }

        DieuHuong.VeMenu(tenSceneMenu);
    }
}

