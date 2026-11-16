using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SelectManager : MonoBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        public string displayName;
        public string sceneName;
        public Sprite previewImage;   // KÉO ẢNH MAP VÀO ĐÂY
    }

    [Header("Tên các mẫu xe")]
    public string[] tenCacMauXe = { "ElectricScooterDefault", "ElectricScooterDefaultBlack" };

    [Header("Danh sách Map đua (thêm map = tăng Size, điền tên scene + kéo ảnh)")]
    public MapOption[] maps;

    [Header("Sân khấu trưng bày 3D")]
    public Transform showcaseStage;
    public float rotateSpeed = 45f;

    [Header("Hộp kính xe")]
    public Vector3 hopViTri = new Vector3(0f, 1.3f, 3.0f);
    public float hopKichThuoc = 1.0f;

    [Header("Khung chọn Map")]
    public float mapPickerY = 0.14f;
    public Vector2 mapPreviewSize = new Vector2(400f, 225f);

    [Header("Giao diện UI")]
    public TMP_Text itemNameText;
    public TMP_Text warningText;
    public Button confirmButton;

    [Header("Màu chủ đạo")]
    public Color mauMacDinh = new Color(0.15f, 0.95f, 0.5f);
    [Tooltip("Ép màu cho từng xe theo thứ tự. Để Alpha = 0 thì tự lấy từ model.")]
    public Color[] mauXeTuyChinh;
    public bool doiMauTheoXe = true;
    public bool doiMauChuTenXe = true;
    public float tocDoDoiMau = 6f;

    [Header("Hiệu ứng")]
    public bool batHieuUng = true;
    [Tooltip("Dịch hiệu ứng sàn lên/xuống so với đáy hộp kính.")]
    public float doLechSan = -0.07f;
    public float tocDoNhip = 2.2f;
    public bool tintNenCamera = true;
    public bool hieuUngKhungMap = true;

    [Header("Navigation")]
    public string tenSceneLobby = "LobbyScene";
    public string tenSceneMenu = "menu";

    private const string TAB_SCOOTER = "Scooter";
    private const string TAB_MAP = "Map";
    private static readonly Color MAU_NEN_CAMERA = new Color(0.02f, 0.03f, 0.04f);

    private string currentTab = TAB_SCOOTER;
    private int scooterIndex = 0;
    private int mapIndex = 0;
    private bool dangChuyenScene = false;

    private Camera cam;
    private Font fontMacDinh;

    private Transform trucXoayXe;
    private readonly List<GameObject> danhSachXeTrungBay = new List<GameObject>();

    // UI chọn map
    private GameObject mapPickerGO;
    private Text lblMap;
    private Image imgMap;
    private Coroutine coroutineHieuUngMap;
    private Outline mapOutline1;
    private Outline mapOutline2;
    private readonly List<Text> chuTheoMau = new List<Text>();

    // Hiệu ứng khung map
    private Image haloMap;
    private RectTransform scanRT;
    private Image scanBand;
    private Image scanLine;
    private Image flashMap;
    private float xungMap = 0f;      // 1 = vừa đổi map, giảm dần về 0

    // Màu
    private Color[] mauXe;
    private Color mauHienTai;
    private Color mauMucTieu;
    private Color mauDaApDung = new Color(-1f, -1f, -1f, -1f);
    private bool canApMauTinh = true;
    private float xungSang = 0f;     // 1 = vừa đổi xe, giảm dần về 0

    // Material / texture dùng chung (tạo 1 lần, hủy ở OnDestroy)
    private Material matKinh;
    private Material matDe;
    private Material matLine;
    private Material matSan;
    private Material matHat;
    private Texture2D texGlow;
    private Sprite spriteGlow;

    // Hiệu ứng 3D
    private LineRenderer lrVienHop;
    private Transform fxRoot;
    private LineRenderer vongTrong, vongNgoai, vongSong;
    private ParticleSystem hatSang;

    // ------------------------------------------------------------------
    // LIFECYCLE
    // ------------------------------------------------------------------
    private void Start()
    {
        Time.timeScale = 1f;
        cam = Camera.main;

        tenCacMauXe = new string[] { "ElectricScooterDefault", "ElectricScooterDefaultBlack" };
        scooterIndex = Mathf.Clamp(PlayerInventory.GetEquippedIndex(TAB_SCOOTER), 0, tenCacMauXe.Length - 1);

        // Khớp map đã chọn từ trước khi vẽ UI
        string savedMap = PlayerInventory.GetSelectedMap();
        if (maps != null)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i].sceneName == savedMap) { mapIndex = i; break; }
            }
        }

        TaoTaiNguyenDungChung();
        TuDongNoiNutUI();
        CanChinhCameraVaSanKhau();
        TaoHaiModelXeDocLap();

        mauHienTai = mauMucTieu = LayMauMucTieu();

        if (batHieuUng) TaoHieuUng();

        TaoUIChonMap();
        SelectTab(TAB_SCOOTER);

        // Vào cảnh là đúng màu ngay, không chuyển màu từ đầu
        mauHienTai = mauMucTieu;
        xungSang = 0f;
        xungMap = 0f;
        canApMauTinh = true;
        ApDungMau(1f);
    }

    private void Update()
    {
        if (dangChuyenScene) return;

        if (Nhan(Phim.Esc))
        {
            BackToMenu();
            return;
        }

        if (Nhan(Phim.Trai)) PreviousItem();
        else if (Nhan(Phim.Phai)) NextItem();

        float dt = Time.deltaTime;

        if (trucXoayXe != null)
            trucXoayXe.Rotate(0f, rotateSpeed * dt, 0f, Space.World);

        // Chuyển màu mượt, tới nơi thì chốt luôn
        if (!GanBang(mauHienTai, mauMucTieu))
        {
            Color moi = Color.Lerp(mauHienTai, mauMucTieu, 1f - Mathf.Exp(-tocDoDoiMau * dt));
            mauHienTai = GanBang(moi, mauMucTieu) ? mauMucTieu : moi;
        }

        xungSang = Mathf.MoveTowards(xungSang, 0f, dt * 2.5f);
        xungMap = Mathf.MoveTowards(xungMap, 0f, dt * 3f);

        float nhip = 0.5f + 0.5f * Mathf.Sin(Time.time * tocDoNhip);
        ApDungMau(nhip);
    }

    private void OnDestroy()
    {
        if (matKinh != null) Destroy(matKinh);
        if (matDe != null) Destroy(matDe);
        if (matLine != null) Destroy(matLine);
        if (matSan != null) Destroy(matSan);
        if (matHat != null) Destroy(matHat);
        if (spriteGlow != null) Destroy(spriteGlow);
        if (texGlow != null) Destroy(texGlow);

        // Không để UI MapPicker lọt sang scene tiếp theo
        if (mapPickerGO != null) Destroy(mapPickerGO);
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
    // TÀI NGUYÊN DÙNG CHUNG
    // ------------------------------------------------------------------
    private void TaoTaiNguyenDungChung()
    {
        fontMacDinh = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Shader unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Standard");

        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite == null) sprite = unlit;

        matKinh = TaoMaterialKinh(new Color(0.28f, 0.45f, 0.32f, 0.24f));
        matDe = new Material(unlit) { color = new Color(0.18f, 0.32f, 0.22f) };
        matLine = new Material(sprite);

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

        // Sprite dùng cho quầng sáng của khung map (UI)
        spriteGlow = Sprite.Create(texGlow, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        matSan = new Material(sprite) { mainTexture = texGlow };
        matHat = new Material(sprite) { mainTexture = texGlow };
    }

    private Material TaoMaterialKinh(Color mauKinh)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            Material mat = new Material(urpLit);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", mauKinh);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }

        Material stdMat = new Material(Shader.Find("Standard"));
        stdMat.SetFloat("_Mode", 3f);
        stdMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        stdMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        stdMat.SetInt("_ZWrite", 0);
        stdMat.renderQueue = 3000;
        stdMat.color = mauKinh;
        return stdMat;
    }

    // ------------------------------------------------------------------
    // MÀU CHỦ ĐẠO TỪ XE
    // ------------------------------------------------------------------
    private Color LayMauXe(int index, GameObject model)
    {
        if (mauXeTuyChinh != null && index < mauXeTuyChinh.Length && mauXeTuyChinh[index].a > 0.01f)
        {
            Color c = mauXeTuyChinh[index];
            c.a = 1f;
            return c;
        }
        return LayMauChuDao(model);
    }

    /// Lấy màu chiếm nhiều nhất trên xe (tính theo số đỉnh mesh), kể cả màu đen.
    private Color LayMauChuDao(GameObject model)
    {
        if (model == null) return mauMacDinh;

        var trongSo = new Dictionary<int, float>();
        var bangMau = new Dictionary<int, Color>();

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("Minimap") || r.name.Contains("vetPhanh")) continue;

            Mesh mesh = null;
            SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
            if (smr != null) mesh = smr.sharedMesh;
            else
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }

            Material[] mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            float w = (mesh != null ? mesh.vertexCount : 1f) / mats.Length;

            foreach (Material m in mats)
            {
                if (m == null) continue;

                Color c;
                if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) c = m.GetColor("_Color");
                else continue;

                Color32 k = c;
                int key = k.r | (k.g << 8) | (k.b << 16);

                float cu;
                trongSo.TryGetValue(key, out cu);
                trongSo[key] = cu + w;
                bangMau[key] = c;
            }
        }

        if (trongSo.Count == 0) return mauMacDinh;

        int tot = 0;
        float max = -1f;
        foreach (var kv in trongSo)
        {
            if (kv.Value > max) { max = kv.Value; tot = kv.Key; }
        }

        Color chon = bangMau[tot];
        Color.RGBToHSV(chon, out float h, out float s, out float v);

        // Màu rực thì làm sáng cho đẹp, màu đen/xám/trắng thì giữ nguyên
        if (s >= 0.35f && v >= 0.3f)
            return Color.HSVToRGB(h, Mathf.Clamp(s, 0.55f, 1f), 1f);

        return Color.HSVToRGB(h, s, Mathf.Max(v, 0.06f));
    }

    private Color LayMauMucTieu()
    {
        if (doiMauTheoXe && mauXe != null && scooterIndex >= 0 && scooterIndex < mauXe.Length)
            return mauXe[scooterIndex];
        return mauMacDinh;
    }

    /// Áp màu hiện tại lên toàn bộ UI + 3D + hiệu ứng. nhip: 0..1
    private void ApDungMau(float nhip)
    {
        Color c = mauHienTai;

        // Phần tĩnh: chỉ áp khi màu đổi (hoặc vừa tạo lại UI)
        if (canApMauTinh || !GanBang(c, mauDaApDung))
        {
            ApMauTinh(c);
            mauDaApDung = c;
            canApMauTinh = false;
        }

        // Kính + đế hộp
        if (matKinh != null)
        {
            Color tint = Color.Lerp(new Color(0.15f, 0.20f, 0.18f), c, 0.35f);
            tint.a = 0.24f + xungSang * 0.10f;
            matKinh.color = tint;
        }
        if (matDe != null)
            matDe.color = Color.Lerp(new Color(0.05f, 0.08f, 0.07f), c, 0.30f);

        if (lrVienHop != null)
        {
            Color e = new Color(c.r, c.g, c.b, Mathf.Lerp(0.65f, 1f, nhip));
            lrVienHop.startColor = e;
            lrVienHop.endColor = e;
        }

        // Khung map: cùng màu với xe
        if (mapOutline1 != null)
            mapOutline1.effectColor = new Color(c.r, c.g, c.b, Mathf.Lerp(0.55f, 1f, nhip));
        if (mapOutline2 != null)
        {
            Color sang = Color.Lerp(c, Color.white, 0.25f);
            mapOutline2.effectColor = new Color(sang.r, sang.g, sang.b, Mathf.Lerp(0.45f, 0.85f, nhip));
        }

        CapNhatHieuUngKhungMap(c, nhip);
        CapNhatHieuUng(c, nhip);
    }

    private void ApMauTinh(Color c)
    {
        for (int i = 0; i < chuTheoMau.Count; i++)
            if (chuTheoMau[i] != null) chuTheoMau[i].color = c;
        if (doiMauChuTenXe && itemNameText != null) itemNameText.color = c;

        if (hatSang != null)
        {
            var main = hatSang.main;
            main.startColor = c;
        }
    }

    // ------------------------------------------------------------------
    // HIỆU ỨNG 3D (quanh hộp kính)
    // ------------------------------------------------------------------
    private void TaoHieuUng()
    {
        fxRoot = new GameObject("AutoStageFX").transform;
        fxRoot.position = new Vector3(
            hopViTri.x,
            hopViTri.y - hopKichThuoc * 0.5f + doLechSan,
            hopViTri.z);

        // Vệt sáng trên sàn
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pad.name = "FX_SanSang";
        Destroy(pad.GetComponent<Collider>());
        Transform padSan = pad.transform;
        padSan.SetParent(fxRoot, false);
        padSan.localPosition = new Vector3(0f, -0.005f, 0f);
        padSan.localRotation = Quaternion.Euler(90f, 0f, 0f);
        padSan.localScale = new Vector3(hopKichThuoc * 3.6f, hopKichThuoc * 3.6f, 1f);
        MeshRenderer mrPad = pad.GetComponent<MeshRenderer>();
        mrPad.sharedMaterial = matSan;
        mrPad.shadowCastingMode = ShadowCastingMode.Off;
        mrPad.receiveShadows = false;

        // 2 vòng cố định + 1 vòng sóng lan ra
        vongTrong = TaoVong("FX_VongTrong", 0.040f);
        vongNgoai = TaoVong("FX_VongNgoai", 0.015f);
        vongSong = TaoVong("FX_VongSong", 0.020f);

        // Hạt sáng bay lên, chỉ sinh ra quanh hộp (không xuyên qua xe)
        GameObject hatObj = new GameObject("FX_HatSang");
        hatObj.transform.SetParent(fxRoot, false);
        hatObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // trục Z chỉ lên

        hatSang = hatObj.AddComponent<ParticleSystem>();
        hatSang.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = hatSang.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = mauHienTai;

        var em = hatSang.emission;
        em.rateOverTime = 14f;

        var shape = hatSang.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = hopKichThuoc * 1.35f;
        shape.radiusThickness = 0.35f;   // chỉ sinh ở vành ngoài, bên ngoài hộp

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

    /// Vòng tròn bán kính 1, chỉnh kích thước bằng localScale.
    private LineRenderer TaoVong(string ten, float doDay)
    {
        GameObject go = new GameObject(ten);
        go.transform.SetParent(fxRoot, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = doDay;
        lr.sharedMaterial = matLine;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;

        const int n = 72;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
        }
        return lr;
    }

    private void CapNhatHieuUng(Color c, float nhip)
    {
        if (fxRoot == null) return;

        if (vongTrong != null)
        {
            float r = hopKichThuoc * 0.85f * (1f + 0.04f * nhip + 0.12f * xungSang);
            vongTrong.transform.localScale = new Vector3(r, 1f, r);
            Color v = new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, nhip));
            vongTrong.startColor = v; vongTrong.endColor = v;
        }

        if (vongNgoai != null)
        {
            float r = hopKichThuoc * 1.20f * (1f + 0.08f * (1f - nhip) + 0.18f * xungSang);
            vongNgoai.transform.localScale = new Vector3(r, 1f, r);
            Color v = new Color(c.r, c.g, c.b, Mathf.Lerp(0.2f, 0.5f, nhip));
            vongNgoai.startColor = v; vongNgoai.endColor = v;
        }

        if (vongSong != null)
        {
            // Sóng lan ra ngoài rồi mờ dần, lặp mỗi 2 giây
            float t = (Time.time * 0.5f) % 1f;
            float r = hopKichThuoc * Mathf.Lerp(0.8f, 2.0f, t);
            vongSong.transform.localScale = new Vector3(r, 1f, r);
            Color v = new Color(c.r, c.g, c.b, (1f - t) * 0.55f);
            vongSong.startColor = v; vongSong.endColor = v;
        }

        if (matSan != null)
            matSan.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.35f, 0.6f, nhip) + xungSang * 0.25f);

        if (tintNenCamera && cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
            cam.backgroundColor = Color.Lerp(MAU_NEN_CAMERA, c, 0.10f + xungSang * 0.05f);
    }

    private void BungSangKhiDoiXe()
    {
        xungSang = 1f;
        if (hatSang != null) hatSang.Emit(24);
    }

    // ------------------------------------------------------------------
    // HIỆU ỨNG KHUNG MAP (UI)
    // ------------------------------------------------------------------
    private void CapNhatHieuUngKhungMap(Color c, float nhip)
    {
        if (!hieuUngKhungMap || imgMap == null) return;

        // Quầng sáng phía sau
        if (haloMap != null)
        {
            haloMap.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.25f, 0.5f, nhip) + xungMap * 0.3f);
            float s = 1f + 0.03f * nhip + 0.10f * xungMap;
            haloMap.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        // Vệt quét chạy từ dưới lên trên
        if (scanRT != null)
        {
            float t = (Time.time * 0.35f) % 1f;
            float h = mapPreviewSize.y;
            scanRT.anchoredPosition = new Vector2(0f, Mathf.Lerp(-h * 0.5f, h * 0.5f, t));

            float fade = Mathf.Sin(t * Mathf.PI);
            Color sang = Color.Lerp(c, Color.white, 0.35f);
            if (scanBand != null) scanBand.color = new Color(sang.r, sang.g, sang.b, 0.14f * fade);
            if (scanLine != null) scanLine.color = new Color(sang.r, sang.g, sang.b, 0.75f * fade);
        }

        // Chớp sáng khi đổi map
        if (flashMap != null)
            flashMap.color = new Color(1f, 1f, 1f, xungMap * 0.35f);
    }

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------
    private void TuDongNoiNutUI()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button btn in allButtons)
        {
            if (btn.onClick.GetPersistentEventCount() != 0) continue;

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

    private void TaoUIChonMap()
    {
        if (maps == null || maps.Length == 0) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform goc = canvas.rootCanvas.transform;

        // Xóa bản cũ nếu còn sót (tránh bị nhân đôi)
        Transform cu = goc.Find("MapPicker");
        if (cu != null) Destroy(cu.gameObject);

        mapPickerGO = new GameObject("MapPicker", typeof(RectTransform));
        mapPickerGO.transform.SetParent(goc, false);
        RectTransform rt = mapPickerGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, mapPickerY);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 34f);

        chuTheoMau.Clear();
        lblMap = TaoText(mapPickerGO.transform, "LblMap", Vector2.zero, new Vector2(160f, 30f), 20);
        chuTheoMau.Add(lblMap);

        TaoNut(mapPickerGO.transform, "<", new Vector2(-100f, 0f), () => DoiMap(-1));
        TaoNut(mapPickerGO.transform, ">", new Vector2(100f, 0f), () => DoiMap(1));

        Vector2 viTriAnh = new Vector2(0f, 17f + 8f + mapPreviewSize.y / 2f);

        // Quầng sáng phía sau ảnh map
        if (hieuUngKhungMap)
        {
            GameObject halo = new GameObject("MapHalo", typeof(RectTransform), typeof(Image));
            halo.transform.SetParent(mapPickerGO.transform, false);
            halo.transform.SetAsFirstSibling();
            RectTransform hrt = halo.GetComponent<RectTransform>();
            hrt.anchoredPosition = viTriAnh;
            hrt.sizeDelta = mapPreviewSize * 1.35f;
            haloMap = halo.GetComponent<Image>();
            haloMap.sprite = spriteGlow;
            haloMap.raycastTarget = false;
        }

        GameObject h = new GameObject("MapPreview", typeof(RectTransform), typeof(Image));
        h.transform.SetParent(mapPickerGO.transform, false);
        RectTransform hr = h.GetComponent<RectTransform>();
        hr.anchoredPosition = viTriAnh;
        hr.sizeDelta = mapPreviewSize;

        imgMap = h.GetComponent<Image>();
        imgMap.preserveAspect = true;
        imgMap.raycastTarget = false;

        // Màu viền được ApDungMau cập nhật theo xe
        mapOutline1 = h.AddComponent<Outline>();
        mapOutline1.effectDistance = new Vector2(4f, -4f);

        mapOutline2 = h.AddComponent<Outline>();
        mapOutline2.effectDistance = new Vector2(-4f, 4f);

        // Lớp cắt riêng cho vệt quét + chớp sáng (không cắt viền Outline)
        if (hieuUngKhungMap)
        {
            GameObject clip = new GameObject("FxClip", typeof(RectTransform), typeof(RectMask2D));
            clip.transform.SetParent(h.transform, false);
            RectTransform crt = clip.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            // Chớp sáng
            GameObject fl = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            fl.transform.SetParent(clip.transform, false);
            RectTransform frt = fl.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            flashMap = fl.GetComponent<Image>();
            flashMap.raycastTarget = false;
            flashMap.color = new Color(1f, 1f, 1f, 0f);

            // Vệt quét
            GameObject sc = new GameObject("Scan", typeof(RectTransform), typeof(Image));
            sc.transform.SetParent(clip.transform, false);
            scanRT = sc.GetComponent<RectTransform>();
            scanRT.anchorMin = new Vector2(0f, 0.5f);
            scanRT.anchorMax = new Vector2(1f, 0.5f);
            scanRT.pivot = new Vector2(0.5f, 0.5f);
            scanRT.sizeDelta = new Vector2(0f, 26f);
            scanBand = sc.GetComponent<Image>();
            scanBand.raycastTarget = false;

            GameObject ln = new GameObject("Line", typeof(RectTransform), typeof(Image));
            ln.transform.SetParent(sc.transform, false);
            RectTransform lrt = ln.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.offsetMin = new Vector2(0f, 0f);
            lrt.offsetMax = new Vector2(0f, 3f);
            scanLine = ln.GetComponent<Image>();
            scanLine.raycastTarget = false;
        }

        canApMauTinh = true;
        CapNhatMap();
    }

    private void DoiMap(int huong)
    {
        if (maps == null || maps.Length == 0) return;
        mapIndex = (mapIndex + huong + maps.Length) % maps.Length;
        CapNhatMap();
        xungMap = 1f;

        if (imgMap != null)
        {
            if (coroutineHieuUngMap != null) StopCoroutine(coroutineHieuUngMap);
            coroutineHieuUngMap = StartCoroutine(HieuUngNhayAnhMap(imgMap.rectTransform));
        }

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "Map", maps[mapIndex].sceneName } });
    }

    private IEnumerator HieuUngNhayAnhMap(RectTransform rectTransform)
    {
        Vector3 scaleGoc = Vector3.one;
        Vector3 scaleTo = Vector3.one * 1.06f;
        const float thoiGian = 0.1f;

        float elapsed = 0f;
        while (elapsed < thoiGian)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(scaleGoc, scaleTo, elapsed / thoiGian);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < thoiGian)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(scaleTo, scaleGoc, elapsed / thoiGian);
            yield return null;
        }

        rectTransform.localScale = scaleGoc;
        coroutineHieuUngMap = null;
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

        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        // Chữ nút luôn trắng (nền nút tối, chữ đen sẽ không đọc được)
        Text t = TaoText(go.transform, "Text", Vector2.zero, r.sizeDelta, 18);
        t.text = nhan;
    }

    private void CapNhatMap()
    {
        if (maps == null || maps.Length == 0) return;
        MapOption m = maps[mapIndex];

        if (lblMap != null) lblMap.text = string.IsNullOrEmpty(m.displayName) ? m.sceneName : m.displayName;

        if (imgMap != null)
        {
            imgMap.sprite = m.previewImage;
            imgMap.color = m.previewImage != null ? Color.white : new Color(0f, 0f, 0f, 0.25f);
        }
    }

    // ------------------------------------------------------------------
    // SÂN KHẤU + HỘP KÍNH
    // ------------------------------------------------------------------
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

        trucXoayXe = TaoHopKinh("HopKinh_Xe", hopViTri, Vector3.one * hopKichThuoc);
    }

    private Transform TaoHopKinh(string tenHop, Vector3 viTri, Vector3 kichThuocHop)
    {
        GameObject hopCha = new GameObject(tenHop);
        hopCha.transform.SetParent(showcaseStage, false);
        hopCha.transform.position = viTri;

        TaoKhoi("KinhTrongSuot", hopCha.transform, matKinh, Vector3.zero, kichThuocHop);
        TaoKhoi("DeHop", hopCha.transform, matDe,
            new Vector3(0f, -kichThuocHop.y * 0.5f - 0.03f, 0f),
            new Vector3(kichThuocHop.x + 0.06f, 0.06f, kichThuocHop.z + 0.06f));

        VeVienHop(hopCha.transform, kichThuocHop);

        GameObject truc = new GameObject("TrucXoayBenTrong");
        truc.transform.SetParent(hopCha.transform, false);
        truc.transform.localPosition = Vector3.zero;
        return truc.transform;
    }

    private static void TaoKhoi(string ten, Transform cha, Material mat, Vector3 viTri, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = ten;
        Destroy(cube.GetComponent<Collider>());

        Transform t = cube.transform;
        t.SetParent(cha, false);
        t.localPosition = viTri;
        t.localScale = scale;

        MeshRenderer mr = cube.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void VeVienHop(Transform parent, Vector3 size)
    {
        GameObject lineObj = new GameObject("VienHopKinh");
        lineObj.transform.SetParent(parent, false);

        lrVienHop = lineObj.AddComponent<LineRenderer>();
        lrVienHop.useWorldSpace = false;
        lrVienHop.startWidth = 0.018f;
        lrVienHop.endWidth = 0.018f;
        lrVienHop.sharedMaterial = matLine;
        lrVienHop.shadowCastingMode = ShadowCastingMode.Off;
        lrVienHop.receiveShadows = false;

        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;

        Vector3[] pts = new Vector3[]
        {
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, -y, z), new Vector3(-x, -y, z), new Vector3(-x, -y, -z),
            new Vector3(-x,  y, -z), new Vector3(x,  y, -z), new Vector3(x, -y, -z), new Vector3(x,  y, -z),
            new Vector3( x,  y,  z), new Vector3(x, -y,  z), new Vector3(x,  y,  z),
            new Vector3(-x,  y,  z), new Vector3(-x, -y,  z), new Vector3(-x,  y,  z),
            new Vector3(-x,  y, -z)
        };
        lrVienHop.positionCount = pts.Length;
        lrVienHop.SetPositions(pts);
    }

    // ------------------------------------------------------------------
    // MODEL XE
    // ------------------------------------------------------------------
    private void TaoHaiModelXeDocLap()
    {
        danhSachXeTrungBay.Clear();

        mauXe = new Color[tenCacMauXe.Length];
        for (int i = 0; i < mauXe.Length; i++) mauXe[i] = mauMacDinh;

        GameObject prefabGoc = Resources.Load<GameObject>("Scooter");
        if (prefabGoc == null) return;

        GameObject tempRoot = Instantiate(prefabGoc);
        tempRoot.SetActive(false);

        for (int i = 0; i < tenCacMauXe.Length; i++)
        {
            Transform timThay = TimNodeTheoTen(tempRoot.transform, tenCacMauXe[i]);

            if (timThay == null)
            {
                Transform slot = TimNodeTheoTen(tempRoot.transform, i == 0 ? "Scooter_1" : "Scooter_2");
                if (slot != null && slot.childCount > 0) timThay = slot.GetChild(0);
            }

            if (timThay == null)
            {
                danhSachXeTrungBay.Add(null); // giữ đúng index
                continue;
            }

            GameObject banSao = Instantiate(timThay.gameObject);
            banSao.name = tenCacMauXe[i];

            mauXe[i] = LayMauXe(i, banSao);   // lấy màu trước khi xử lý model
            danhSachXeTrungBay.Add(TaoHolderVaCanGiuaHop(banSao));
        }

        Destroy(tempRoot);
    }

    private static Transform TimNodeTheoTen(Transform root, string ten)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == ten) return t;
        }
        return null;
    }

    private static bool TinhBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool co = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;

            if (!co) { bounds = r.bounds; co = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return co;
    }

    private GameObject TaoHolderVaCanGiuaHop(GameObject model)
    {
        foreach (MonoBehaviour mb in model.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) Destroy(mb);
        foreach (Collider col in model.GetComponentsInChildren<Collider>(true))
            if (col != null) Destroy(col);
        foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) Destroy(rb);

        GameObject khungChuan = new GameObject(model.name + "_Holder");
        Transform kt = khungChuan.transform;
        kt.SetParent(trucXoayXe, false);
        kt.localPosition = Vector3.zero;
        kt.localRotation = Quaternion.identity;
        kt.localScale = Vector3.one;

        Transform mt = model.transform;
        mt.SetParent(kt, false);
        mt.localPosition = Vector3.zero;
        mt.localRotation = Quaternion.identity;
        mt.localScale = Vector3.one;

        khungChuan.SetActive(true);
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.SetActive(true);
            t.gameObject.layer = 0;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.name.Contains("Minimap") || r.name.Contains("vetPhanh"))
            {
                r.gameObject.SetActive(false);
                continue;
            }
            r.enabled = true;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        if (TinhBounds(renderers, out Bounds b))
        {
            float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxDim > 0.000001f)
            {
                mt.localScale = Vector3.one * ((hopKichThuoc * 0.72f) / maxDim);

                if (TinhBounds(renderers, out b))
                    mt.position += kt.position - b.center;
            }
        }

        khungChuan.SetActive(false);
        return khungChuan;
    }

    // ------------------------------------------------------------------
    // CHỌN XE / MAP
    // ------------------------------------------------------------------
    public void SelectTab(string tabName)
    {
        currentTab = tabName;
        CapNhatGiaoDien();
    }

    public void NextItem() { DoiItem(1); }
    public void PreviousItem() { DoiItem(-1); }

    private void DoiItem(int huong)
    {
        if (currentTab == TAB_SCOOTER && tenCacMauXe.Length > 0)
        {
            scooterIndex = (scooterIndex + huong + tenCacMauXe.Length) % tenCacMauXe.Length;
            BungSangKhiDoiXe();
            CapNhatGiaoDien();
        }
        else if (currentTab == TAB_MAP)
        {
            DoiMap(huong);
            CapNhatGiaoDien();
        }
    }

    private void CapNhatGiaoDien()
    {
        int soXe = Mathf.Max(1, tenCacMauXe.Length);
        if (scooterIndex < 0 || scooterIndex >= soXe) scooterIndex = 0;

        for (int i = 0; i < danhSachXeTrungBay.Count; i++)
        {
            if (danhSachXeTrungBay[i] != null)
                danhSachXeTrungBay[i].SetActive(i == scooterIndex);
        }

        if (itemNameText != null)
        {
            if (currentTab == TAB_SCOOTER && tenCacMauXe.Length > 0)
                itemNameText.text = $"XE: {tenCacMauXe[scooterIndex]}";
            else if (currentTab == TAB_MAP && maps != null && maps.Length > 0)
                itemNameText.text = $"MAP: {maps[mapIndex].displayName}";
        }

        if (confirmButton != null) confirmButton.interactable = true;

        mauMucTieu = LayMauMucTieu();

        if (warningText != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(mauMucTieu);
            warningText.text = $"<color=#{hex}>SẴN SÀNG RA ĐƯỜNG ĐUA!</color>";
        }
    }

    // ------------------------------------------------------------------
    // NAVIGATION
    // ------------------------------------------------------------------
    public void ConfirmAndGoToLobby()
    {
        if (dangChuyenScene) return;
        dangChuyenScene = true;

        PlayerInventory.SetEquippedIndex(TAB_SCOOTER, scooterIndex);
        if (maps != null && maps.Length > 0)
            PlayerInventory.SetSelectedMap(maps[mapIndex].sceneName);

        if (mapPickerGO != null) Destroy(mapPickerGO);

        Time.timeScale = 1f;
        SceneManager.LoadScene(tenSceneLobby);
    }

    public void BackToMenu()
    {
        if (dangChuyenScene) return;
        dangChuyenScene = true;

        if (mapPickerGO != null) Destroy(mapPickerGO);

        Time.timeScale = 1f;
        SceneManager.LoadScene(tenSceneMenu);
    }
}