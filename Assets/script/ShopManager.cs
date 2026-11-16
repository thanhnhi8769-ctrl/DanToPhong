using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Hiệu ứng cho Button: phóng to khi rê chuột, co khi bấm, viền sáng, chữ đổi màu, nhấp nhô (tùy chọn).
/// Được ShopManager tự gắn vào các Button lúc chạy.
public class NutHieuUng : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float phongHover = 1.08f;
    public float nenBam = 0.93f;
    public float bienDoNhip = 0f;      // > 0 thì nút nhấp nhô nhẹ khi bấm được
    public float tocDoNhip = 2.2f;

    private Selectable sel;
    private Outline vien;
    private TMP_Text chu;
    private Vector3 scaleGoc;
    private Color mauChuGoc = Color.white;
    private Color mau = new Color(0.15f, 0.95f, 0.5f);

    private bool hover, bam;
    private float tHover;
    private float scaleHienTai = 1f;
    private float vienAlphaCuoi = -1f;
    private float tChuCuoi = -1f;

    private void Awake()
    {
        sel = GetComponent<Selectable>();
        chu = GetComponentInChildren<TMP_Text>(true);
        scaleGoc = transform.localScale;
        if (chu != null) mauChuGoc = chu.color;

        Graphic nen = GetComponent<Graphic>();
        if (nen != null)
        {
            vien = GetComponent<Outline>();
            if (vien == null) vien = gameObject.AddComponent<Outline>();
            vien.effectDistance = new Vector2(3f, -3f);
            vien.effectColor = new Color(mau.r, mau.g, mau.b, 0f);
        }
    }

    public void DatMau(Color c)
    {
        mau = c;
        vienAlphaCuoi = -1f;   // ép ghi lại màu viền
        tChuCuoi = -1f;        // ép ghi lại màu chữ
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
        if (chu != null) chu.color = mauChuGoc;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool ok = sel == null || sel.interactable;

        tHover = Mathf.MoveTowards(tHover, (ok && hover) ? 1f : 0f, dt * 8f);

        float s = 1f + (phongHover - 1f) * tHover;
        if (ok && bam) s *= nenBam;

        float nhip = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * tocDoNhip);
        if (ok && bienDoNhip > 0f) s *= 1f + bienDoNhip * nhip;

        scaleHienTai = Mathf.Lerp(scaleHienTai, s, 1f - Mathf.Exp(-18f * dt));
        transform.localScale = scaleGoc * scaleHienTai;

        // Viền sáng
        if (vien != null)
        {
            float a = ok ? Mathf.Lerp(0.25f, 1f, tHover) : 0f;
            if (ok && bienDoNhip > 0f) a = Mathf.Max(a, Mathf.Lerp(0.2f, 0.7f, nhip));

            if (Mathf.Abs(a - vienAlphaCuoi) > 0.003f)
            {
                vien.effectColor = new Color(mau.r, mau.g, mau.b, a);
                vienAlphaCuoi = a;
            }
        }

        // Chữ chuyển dần sang màu xe khi rê chuột
        if (chu != null && Mathf.Abs(tHover - tChuCuoi) > 0.003f)
        {
            chu.color = Color.Lerp(mauChuGoc, mau, tHover * 0.85f);
            tChuCuoi = tHover;
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
    }

    [Header("Shop Items")]
    public ShopItem[] scooters;

    [Header("3D Showcase Stage")]
    public Transform showcaseStage;
    public float rotateSpeed = 45f;

    [Header("Mini Owned Shelf (Top-Left)")]
    public Transform gocMiniPreview;
    public float kichThuocMini = 0.14f;
    public float khoangCachNgang = 0.22f;
    public float khoangCachHang = 0.22f;
    public float tocDoXoayMini = 45f;

    [Header("UI Elements")]
    public TMP_Text coinsText;
    public TMP_Text itemNameText;
    public Button buyButton;
    public TMP_Text buyButtonText;
    public TMP_Text thongBaoMuaText;

    [Header("Màu chủ đạo")]
    public Color mauMacDinh = new Color(0.15f, 0.95f, 0.5f);
    public bool doiMauTheoXe = true;
    public bool doiMauChuTenXe = true;
    public float tocDoDoiMau = 6f;

    [Header("Hiệu ứng")]
    public bool batHieuUng = true;
    public float doCaoSan = 0.85f;
    public float tocDoNhip = 2.2f;
    public bool tintNenCamera = true;

    [Header("Hiệu ứng Text / Button")]
    public bool hieuUngNut = true;
    public bool hieuUngChu = true;
    public float tocDoDemXu = 4f;

    [Header("Navigation")]
    public string tenSceneMenu = "menu";

    private const string TAB_SCOOTER = "Scooter";
    private const float DO_CAO_SAN_KHAU = 1.45f;
    private const float KICH_THUOC_CHUAN = 1.35f;
    private static readonly Color MAU_NEN_CAMERA = new Color(0.02f, 0.03f, 0.04f);

    private int currentIndex = 0;
    private bool dangThoat = false;
    private bool daKhoiTao = false;

    private Coroutine coroutineThongBao;
    private readonly List<Transform> danhSachMiniXoay = new List<Transform>();

    // Hiệu ứng text / button
    private readonly List<NutHieuUng> cacNut = new List<NutHieuUng>();
    private readonly Dictionary<TMP_Text, Coroutine> popDangChay = new Dictionary<TMP_Text, Coroutine>();
    private Vector2 viTriThongBao;
    private float xuHienThi;
    private int xuMucTieu;
    private int xuDaHien;
    private bool xuKhoiTao = false;

    // Màu
    private Color[] mauXe;
    private Color mauHienTai;
    private Color mauMucTieu;
    private Color mauDaApDung = new Color(-1f, -1f, -1f, -1f);
    private bool canApMauTinh = true;   // true = phải áp lại màu cho chữ / hạt / camera / nút
    private Camera cam;

    // Material / texture dùng chung (tạo 1 lần, hủy ở OnDestroy)
    private Material matVien;
    private Material matNen;
    private Material matVong;
    private Material matSan;
    private Material matHat;
    private Texture2D texGlow;

    // Hiệu ứng
    private Transform fxRoot;
    private LineRenderer vongA, vongB;
    private Light denMau;
    private ParticleSystem hatSang;
    private TextMesh chuTieuDeMini;

    // ------------------------------------------------------------------
    // LIFECYCLE
    // ------------------------------------------------------------------
    private void Start()
    {
        Time.timeScale = 1f;

        if (thongBaoMuaText != null)
        {
            viTriThongBao = thongBaoMuaText.rectTransform.anchoredPosition;
            thongBaoMuaText.gameObject.SetActive(false);
        }

        cam = Camera.main;

        TaoMaterialVaTexture();
        CanChinhCameraVaSanKhau();
        CanGiuaDanhSach(scooters);
        TinhMauCacXe();

        mauHienTai = mauMucTieu = LayMauMucTieu();

        if (batHieuUng) TaoHieuUng();
        if (hieuUngNut) GanHieuUngNut();
        if (hieuUngChu) ChuanBiChu();

        UpdateCoinsUI();
        CapNhatToanBoDoSoHuuMini();
        SelectTab(TAB_SCOOTER);

        mauHienTai = mauMucTieu;
        canApMauTinh = true;
        ApDungMau(1f);

        daKhoiTao = true;
    }

    private void Update()
    {
        if (dangThoat) return;

        if (Nhan(Phim.Esc))
        {
            BackToMenu();
            return;
        }

        if (Nhan(Phim.Trai)) DoiXe(-1);
        else if (Nhan(Phim.Phai)) DoiXe(1);

        float dt = Time.deltaTime;

        if (showcaseStage != null)
            showcaseStage.Rotate(0f, rotateSpeed * dt, 0f, Space.World);

        float gocXoay = tocDoXoayMini * dt;
        for (int i = 0; i < danhSachMiniXoay.Count; i++)
        {
            Transform t = danhSachMiniXoay[i];
            if (t != null) t.Rotate(0f, gocXoay, 0f, Space.World);
        }

        // Đổi màu mượt, tới nơi thì chốt luôn
        if (!GanBang(mauHienTai, mauMucTieu))
        {
            Color moi = Color.Lerp(mauHienTai, mauMucTieu, 1f - Mathf.Exp(-tocDoDoiMau * dt));
            mauHienTai = GanBang(moi, mauMucTieu) ? mauMucTieu : moi;
        }

        CapNhatDemXu(dt);

        float nhip = 0.5f + 0.5f * Mathf.Sin(Time.time * tocDoNhip);
        ApDungMau(nhip);

        if (vongA != null) vongA.transform.localScale = Vector3.one * (1f + 0.04f * nhip);
        if (vongB != null) vongB.transform.localScale = Vector3.one * (1f + 0.08f * (1f - nhip));
    }

    private void OnDestroy()
    {
        if (matVien != null) Destroy(matVien);
        if (matNen != null) Destroy(matNen);
        if (matVong != null) Destroy(matVong);
        if (matSan != null) Destroy(matSan);
        if (matHat != null) Destroy(matHat);
        if (texGlow != null) Destroy(texGlow);
    }

    // ------------------------------------------------------------------
    // INPUT
    // ------------------------------------------------------------------
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

    private static bool GanBang(Color a, Color b)
    {
        return ((Vector4)a - (Vector4)b).sqrMagnitude < 0.00001f;
    }

    // ------------------------------------------------------------------
    // HIỆU ỨNG TEXT / BUTTON
    // ------------------------------------------------------------------
    private void GanHieuUngNut()
    {
        cacNut.Clear();

        Button[] tatCa = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in tatCa)
        {
            NutHieuUng h = b.GetComponent<NutHieuUng>();
            if (h == null) h = b.gameObject.AddComponent<NutHieuUng>();

            // Nút BUY nhấp nhô nhẹ để gây chú ý
            if (b == buyButton) h.bienDoNhip = 0.035f;

            h.tocDoNhip = tocDoNhip;
            cacNut.Add(h);
        }
    }

    private void ChuanBiChu()
    {
        // Viền chữ cho tên xe (màu viền cập nhật trong ApMauTinh)
        if (itemNameText != null)
            itemNameText.outlineWidth = 0.18f;
    }

    /// Nảy chữ lên rồi về cỡ gốc.
    private void Pop(TMP_Text t, float dinh = 1.15f)
    {
        if (!hieuUngChu || t == null) return;

        Coroutine c;
        if (popDangChay.TryGetValue(t, out c) && c != null) StopCoroutine(c);
        popDangChay[t] = StartCoroutine(ChayPop(t, dinh));
    }

    private IEnumerator ChayPop(TMP_Text t, float dinh)
    {
        const float thoiGian = 0.28f;
        float troi = 0f;

        while (troi < thoiGian)
        {
            if (t == null) yield break;

            troi += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(troi / thoiGian);
            float s = k < 0.4f
                ? Mathf.Lerp(0.85f, dinh, k / 0.4f)
                : Mathf.Lerp(dinh, 1f, (k - 0.4f) / 0.6f);

            t.transform.localScale = Vector3.one * s;
            yield return null;
        }

        if (t != null) t.transform.localScale = Vector3.one;
        popDangChay[t] = null;
    }

    /// Số coin chạy lên/xuống mượt. Chỉ cập nhật text khi số nguyên đổi.
    private void CapNhatDemXu(float dt)
    {
        if (coinsText == null || xuDaHien == xuMucTieu) return;

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

    // ------------------------------------------------------------------
    // SETUP
    // ------------------------------------------------------------------
    private void TaoMaterialVaTexture()
    {
        Shader unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Standard");

        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite == null) sprite = unlit;

        matVien = new Material(unlit) { color = mauMacDinh };
        matNen = new Material(unlit) { color = new Color(0.07f, 0.10f, 0.13f) };

        // Texture glow tròn mềm, ghi 1 lần bằng SetPixels32
        const int size = 64;
        var pixels = new Color32[size * size];
        float tam = (size - 1) * 0.5f;
        float banKinh = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - tam, dy = y - tam;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / banKinh;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        texGlow = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texGlow.SetPixels32(pixels);
        texGlow.Apply(false, true);

        matVong = new Material(sprite);
        matSan = new Material(sprite) { mainTexture = texGlow };
        matHat = new Material(sprite) { mainTexture = texGlow };
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

    // ------------------------------------------------------------------
    // MÀU CHỦ ĐẠO TỪ XE
    // ------------------------------------------------------------------
    private void TinhMauCacXe()
    {
        int n = scooters != null ? scooters.Length : 0;
        mauXe = new Color[n];
        for (int i = 0; i < n; i++)
            mauXe[i] = LayMauChuDao(scooters[i].model3D);
    }

    /// Lấy màu bão hòa nhất trong các material của model rồi làm sáng lên cho hợp glow.
    private Color LayMauChuDao(GameObject model)
    {
        if (model == null) return mauMacDinh;

        float diemTotNhat = 0f;
        Color tot = mauMacDinh;
        bool tim = false;

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;

                Color c;
                if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) c = m.GetColor("_Color");
                else continue;

                Color.RGBToHSV(c, out float h, out float s, out float v);
                if (s < 0.35f || v < 0.3f) continue; // bỏ xám / đen / trắng

                float diem = s * v;
                if (diem > diemTotNhat)
                {
                    diemTotNhat = diem;
                    tot = Color.HSVToRGB(h, Mathf.Clamp(s, 0.55f, 1f), 1f);
                    tim = true;
                }
            }
        }

        return tim ? tot : mauMacDinh;
    }

    private Color LayMauMucTieu()
    {
        if (doiMauTheoXe && mauXe != null && currentIndex >= 0 && currentIndex < mauXe.Length)
            return mauXe[currentIndex];
        return mauMacDinh;
    }

    // ------------------------------------------------------------------
    // HIỆU ỨNG 3D
    // ------------------------------------------------------------------
    private void TaoHieuUng()
    {
        fxRoot = new GameObject("AutoStageFX").transform;
        fxRoot.position = new Vector3(0f, doCaoSan, showcaseStage.position.z);

        // Vệt sáng trên sàn
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pad.name = "FX_SanSang";
        Destroy(pad.GetComponent<Collider>());
        pad.transform.SetParent(fxRoot, false);
        pad.transform.localPosition = new Vector3(0f, -0.005f, 0f);
        pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        pad.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
        MeshRenderer mrPad = pad.GetComponent<MeshRenderer>();
        mrPad.sharedMaterial = matSan;
        mrPad.shadowCastingMode = ShadowCastingMode.Off;
        mrPad.receiveShadows = false;

        // Hai vòng sáng
        vongA = TaoVong("FX_VongTrong", 0.95f, 0.040f);
        vongB = TaoVong("FX_VongNgoai", 1.30f, 0.015f);

        // Đèn màu
        GameObject denObj = new GameObject("FX_DenMau");
        denObj.transform.SetParent(fxRoot, false);
        denObj.transform.localPosition = new Vector3(0f, 1.2f, -1.5f);
        denMau = denObj.AddComponent<Light>();
        denMau.type = LightType.Point;
        denMau.range = 5f;
        denMau.shadows = LightShadows.None;

        // Hạt sáng bay lên
        GameObject hatObj = new GameObject("FX_HatSang");
        hatObj.transform.SetParent(fxRoot, false);
        hatObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Z chỉ lên trên

        hatSang = hatObj.AddComponent<ParticleSystem>();
        hatSang.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = hatSang.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = mauHienTai;

        var em = hatSang.emission;
        em.rateOverTime = 14f;

        var shape = hatSang.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 1.5f;
        shape.radiusThickness = 1f;

        var col = hatSang.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.25f),
                new GradientAlphaKey(0.6f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var pr = hatObj.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = matHat;
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = ShadowCastingMode.Off;
        pr.receiveShadows = false;

        hatSang.Play();
    }

    private LineRenderer TaoVong(string ten, float banKinh, float doDay)
    {
        GameObject go = new GameObject(ten);
        go.transform.SetParent(fxRoot, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = doDay;
        lr.sharedMaterial = matVong;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;

        const int n = 72;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * banKinh, 0f, Mathf.Sin(a) * banKinh));
        }
        return lr;
    }

    /// Áp màu hiện tại. nhip: 0..1
    private void ApDungMau(float nhip)
    {
        Color c = mauHienTai;

        // Phần tĩnh: chỉ áp khi màu đổi (hoặc vừa tạo lại chữ / UI)
        if (canApMauTinh || !GanBang(c, mauDaApDung))
        {
            ApMauTinh(c);
            mauDaApDung = c;
            canApMauTinh = false;
        }

        // Phần nhịp thở: đổi mỗi frame
        if (matVien != null)
        {
            float k = Mathf.Lerp(0.6f, 1f, nhip);
            matVien.color = new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        if (vongA != null)
            vongA.startColor = vongA.endColor = new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, nhip));
        if (vongB != null)
            vongB.startColor = vongB.endColor = new Color(c.r, c.g, c.b, Mathf.Lerp(0.2f, 0.5f, nhip));

        if (matSan != null)
            matSan.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.35f, 0.6f, nhip));

        if (denMau != null)
        {
            denMau.color = c;
            denMau.intensity = Mathf.Lerp(1.2f, 2.2f, nhip);
        }
    }

    private void ApMauTinh(Color c)
    {
        if (chuTieuDeMini != null) chuTieuDeMini.color = c;

        if (doiMauChuTenXe && itemNameText != null)
        {
            itemNameText.color = c;
            if (hieuUngChu)
                itemNameText.outlineColor = (Color32)Color.Lerp(c, Color.black, 0.55f);
        }

        if (hatSang != null)
        {
            var main = hatSang.main;
            main.startColor = c;
        }

        if (tintNenCamera && cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
            cam.backgroundColor = Color.Lerp(MAU_NEN_CAMERA, c, 0.10f);

        // Viền + chữ của các nút theo màu xe
        for (int i = 0; i < cacNut.Count; i++)
            if (cacNut[i] != null) cacNut[i].DatMau(c);
    }

    // ------------------------------------------------------------------
    // HELPER: BOUNDS / FIT / CĂN GIỮA
    // ------------------------------------------------------------------
    private static bool TinhBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool coBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
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

        foreach (Animator anim in obj.GetComponentsInChildren<Animator>(true))
            anim.enabled = false;

        foreach (MonoBehaviour script in obj.GetComponentsInChildren<MonoBehaviour>(true))
            if (script != null && script != boQua) script.enabled = false;
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

    // ------------------------------------------------------------------
    // MODEL CHÍNH
    // ------------------------------------------------------------------
    private void CanGiuaDanhSach(ShopItem[] danhSach)
    {
        if (danhSach == null) return;

        foreach (var item in danhSach)
        {
            if (item.model3D == null) continue;

            if (!item.model3D.scene.IsValid())
                item.model3D = Instantiate(item.model3D);

            GameObject model = item.model3D;
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
    }

    // ------------------------------------------------------------------
    // UI / SHOP LOGIC
    // ------------------------------------------------------------------
    public void SelectTab(string tabName)
    {
        currentIndex = 0;
        UpdateItemUI();
    }

    public void NextItem() { DoiXe(1); }
    public void PreviousItem() { DoiXe(-1); }

    private void DoiXe(int huong)
    {
        if (scooters == null || scooters.Length == 0) return;
        currentIndex = (currentIndex + huong + scooters.Length) % scooters.Length;
        UpdateItemUI();

        if (daKhoiTao) Pop(itemNameText, 1.12f);
    }

    public void OnBuyButtonClicked()
    {
        if (scooters == null || scooters.Length == 0) return;

        ShopItem selectedItem = scooters[currentIndex];

        if (PlayerInventory.TryBuyItem(TAB_SCOOTER, currentIndex, selectedItem.price))
        {
            UpdateCoinsUI();
            UpdateItemUI();
            CapNhatToanBoDoSoHuuMini();
            HienThongBao($"PURCHASED: {selectedItem.itemName.ToUpper()}!", mauHienTai, false);
            Pop(buyButtonText, 1.25f);
        }
        else
        {
            HienThongBao("NOT ENOUGH COINS!", Color.red, true);
            Pop(coinsText, 1.2f);
        }
    }

    private void UpdateItemUI()
    {
        if (scooters == null || scooters.Length == 0) return;

        for (int i = 0; i < scooters.Length; i++)
        {
            GameObject m = scooters[i].model3D;
            if (m != null) m.SetActive(i == currentIndex);
        }

        ShopItem selectedItem = scooters[currentIndex];

        if (itemNameText != null) itemNameText.text = selectedItem.itemName;

        if (buyButton != null && buyButtonText != null)
        {
            bool isOwned = PlayerInventory.IsUnlocked(TAB_SCOOTER, currentIndex);
            buyButton.interactable = !isOwned;
            buyButtonText.text = isOwned ? "OWNED" : $"BUY ({selectedItem.price} COINS)";
        }

        mauMucTieu = LayMauMucTieu();
    }

    private void UpdateCoinsUI()
    {
        if (coinsText == null) return;

        int xu = PlayerInventory.GetCoins();

        if (!xuKhoiTao)
        {
            // Lần đầu: hiện thẳng, không đếm
            xuKhoiTao = true;
            xuHienThi = xu;
            xuDaHien = xu;
            xuMucTieu = xu;
            coinsText.text = $"COINS: {xu}";
            return;
        }

        if (xu != xuMucTieu)
        {
            xuMucTieu = xu;
            Pop(coinsText, 1.2f);
        }
    }

    // ------------------------------------------------------------------
    // THÔNG BÁO
    // ------------------------------------------------------------------
    private void HienThongBao(string noiDung, Color mauChu, bool rung)
    {
        if (thongBaoMuaText == null) return;

        if (coroutineThongBao != null) StopCoroutine(coroutineThongBao);
        coroutineThongBao = StartCoroutine(ChayHienThongBao(noiDung, mauChu, rung));
    }

    private IEnumerator ChayHienThongBao(string noiDung, Color mauChu, bool rung)
    {
        RectTransform rt = thongBaoMuaText.rectTransform;

        thongBaoMuaText.gameObject.SetActive(true);
        thongBaoMuaText.color = mauChu;
        thongBaoMuaText.text = noiDung;
        rt.anchoredPosition = viTriThongBao;

        if (!hieuUngChu)
        {
            yield return new WaitForSeconds(2.2f);
            if (thongBaoMuaText != null) thongBaoMuaText.gameObject.SetActive(false);
            coroutineThongBao = null;
            yield break;
        }

        // 1) Hiện ra: nảy + rõ dần (rung ngang nếu là lỗi)
        const float thoiGianHien = 0.30f;
        float troi = 0f;
        while (troi < thoiGianHien)
        {
            if (thongBaoMuaText == null) yield break;

            troi += Time.deltaTime;
            float k = Mathf.Clamp01(troi / thoiGianHien);

            float s = k < 0.6f
                ? Mathf.Lerp(0.5f, 1.15f, k / 0.6f)
                : Mathf.Lerp(1.15f, 1f, (k - 0.6f) / 0.4f);

            rt.localScale = Vector3.one * s;
            thongBaoMuaText.alpha = k;

            if (rung)
                rt.anchoredPosition = viTriThongBao + new Vector2(Mathf.Sin(k * 40f) * 14f * (1f - k), 0f);

            yield return null;
        }

        rt.localScale = Vector3.one;
        rt.anchoredPosition = viTriThongBao;
        thongBaoMuaText.alpha = 1f;

        // 2) Giữ
        yield return new WaitForSeconds(1.5f);

        // 3) Mờ dần
        const float thoiGianMo = 0.4f;
        troi = 0f;
        while (troi < thoiGianMo)
        {
            if (thongBaoMuaText == null) yield break;

            troi += Time.deltaTime;
            thongBaoMuaText.alpha = 1f - Mathf.Clamp01(troi / thoiGianMo);
            yield return null;
        }

        if (thongBaoMuaText != null)
        {
            thongBaoMuaText.alpha = 1f;
            thongBaoMuaText.gameObject.SetActive(false);
        }
        coroutineThongBao = null;
    }

    // ------------------------------------------------------------------
    // KỆ MINI
    // ------------------------------------------------------------------
    private void CapNhatToanBoDoSoHuuMini()
    {
        if (gocMiniPreview == null) return;

        danhSachMiniXoay.Clear();
        chuTieuDeMini = null;

        for (int i = gocMiniPreview.childCount - 1; i >= 0; i--)
        {
            GameObject oldChild = gocMiniPreview.GetChild(i).gameObject;
            oldChild.SetActive(false);
            Destroy(oldChild);
        }

        int soXe = XepHangMiniDaSoHuu(TAB_SCOOTER, scooters, 0);
        int maxCot = Mathf.Max(2, soXe);

        float traiKhung = -0.78f;
        float phaiKhung = (maxCot - 1) * khoangCachNgang + 0.22f;
        float tamX = (traiKhung + phaiKhung) * 0.5f;

        float trenKhung = 0.28f;
        float duoiKhung = -0.16f;
        float tamY = (trenKhung + duoiKhung) * 0.5f;

        chuTieuDeMini = TaoChu3D("OWNED ITEMS", new Vector3(tamX, 0.18f, -0.02f), 40, mauHienTai, TextAnchor.MiddleCenter);
        TaoKhungKeMini(tamX, tamY, phaiKhung - traiKhung, trenKhung - duoiKhung);

        canApMauTinh = true; // chữ mới tạo, cần áp màu lại
    }

    private void TaoQuad(string ten, Material mat, Vector3 viTri, Vector3 scale)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = ten;
        Destroy(quad.GetComponent<Collider>());

        Transform t = quad.transform;
        t.SetParent(gocMiniPreview, false);
        t.localPosition = viTri;
        t.localScale = scale;

        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void TaoKhungKeMini(float tamX, float tamY, float rong, float cao)
    {
        TaoQuad("KhungVien", matVien, new Vector3(tamX, tamY, 0.14f), new Vector3(rong + 0.03f, cao + 0.03f, 1f));
        TaoQuad("KhungNen", matNen, new Vector3(tamX, tamY, 0.13f), new Vector3(rong, cao, 1f));
    }

    private TextMesh TaoChu3D(string noiDung, Vector3 viTriCucBo, int coChu, Color mauChu, TextAnchor canLe)
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

        MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        return textMesh;
    }

    private int XepHangMiniDaSoHuu(string category, ShopItem[] danhSach, int thuTuHang)
    {
        if (danhSach == null || danhSach.Length == 0) return 0;

        TaoChu3D("SCOOTER:", new Vector3(-0.18f, -thuTuHang * khoangCachHang, -0.02f), 32, Color.white, TextAnchor.MiddleRight);

        int cotHienTai = 0;

        for (int i = 0; i < danhSach.Length; i++)
        {
            if (danhSach[i].model3D == null) continue;
            if (!PlayerInventory.IsUnlocked(category, i)) continue;

            GameObject slotObj = new GameObject($"Mini_{category}_{i}");
            Transform slot = slotObj.transform;
            slot.SetParent(gocMiniPreview, false);
            slot.localPosition = new Vector3(cotHienTai * khoangCachNgang, -thuTuHang * khoangCachHang, 0f);

            GameObject banSao = Instantiate(danhSach[i].model3D, slot);
            KhoaVatLyVaChuyenDong(banSao, this);
            TatBongDo(banSao);

            banSao.SetActive(true);
            Transform bt = banSao.transform;
            bt.localPosition = Vector3.zero;
            bt.localRotation = Quaternion.identity;
            bt.localScale = Vector3.one;

            FitVaCanGiua(banSao, slot, kichThuocMini, 0.02f);

            danhSachMiniXoay.Add(slot);
            cotHienTai++;
        }

        return cotHienTai;
    }

    // ------------------------------------------------------------------
    // NAVIGATION
    // ------------------------------------------------------------------
    public void BackToMenu()
    {
        if (dangThoat) return;
        dangThoat = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(tenSceneMenu);
    }
}