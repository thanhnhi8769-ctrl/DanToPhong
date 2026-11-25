using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// Gắn script này vào 1 GameObject bất kỳ trong scene "menu" (ví dụ object MenuUI).
// Tự tạo thẻ thông tin người chơi ở góc trên bên trái: avatar tròn, tên, @username, ID, xu.
// Xu và xe tự cập nhật khi ServerClient.Cache đổi (đồng bộ với launcher).
public class MenuPlayerInfo : MonoBehaviour
{
    public enum ViTriThe { TrenTrai, TrenPhaiCanhLoa }

    [Header("Vị trí")]
    public ViTriThe viTri = ViTriThe.TrenTrai;
    public float cachMep = 28f;            // cách mép trên (và mép trái nếu đặt bên trái)
    public float cachMepPhaiCanhLoa = 170f; // khi đặt cạnh loa: cách mép phải bao nhiêu để không đè nút loa

    [Header("Kích thước")]
    public float kichThuocAvatar = 110f;
    public bool vienAvatar = true;          // viền vàng mảnh quanh avatar tròn
    public int coChuTen = 32;
    public int coChuUser = 18;
    public int coChuXu = 24;

    // Avatar tải từ server chỉ cần 1 lần mỗi phiên chơi (đổi scene không tải lại)
    private static byte[] avatarCache;
    private static string avatarCacheUser;

    private Font font;
    private Sprite hinhTron;
    private Texture2D texAvatar;            // texture do script này tạo: phải tự hủy khi rời scene
    private RawImage rawAvatar;
    private Text chuChuCai, txtTen, txtUser, txtXu, txtXe;

    private void OnEnable() { ServerClient.DaCapNhat += CapNhatXuXe; }
    private void OnDisable() { ServerClient.DaCapNhat -= CapNhatXuXe; }

    private void OnDestroy()
    {
        if (texAvatar != null) Destroy(texAvatar);
        if (hinhTron != null)
        {
            if (hinhTron.texture != null) Destroy(hinhTron.texture);
            Destroy(hinhTron);
        }
    }

    private void CapNhatXuXe()
    {
        if (txtXu == null || txtXe == null) return;
        txtXu.text = $"COINS: {PlayerInventory.GetCoins()}";
        try { txtXe.text = "SCOOTER: " + VehicleCatalog.Get(VehicleCatalog.GetSelectedIndex()).name.ToUpper(); }
        catch { }
    }

    private async void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hinhTron = TaoSpriteTron();
        DungUI();

        // Hiện ngay dữ liệu có sẵn, rồi cập nhật khi server trả về
        string user = LauncherArgs.User;
        HienThongTin(user, user, "");

        // Avatar: ưu tiên bản đã tải trong phiên này, rồi tới file local do launcher lưu
        if (avatarCache != null && avatarCacheUser == user)
            ApDungBytes(avatarCache);
        else
            NapAvatarLocal(user);

        if (ServerClient.Ready)
        {
            Resp r = await ServerClient.CallAsync(new Req { command = "LAYTHONGTIN" });
            if (this == null) return;
            if (r != null && r.success && r.user != null)
            {
                string ten = string.IsNullOrEmpty(r.user.fullName) ? r.user.username : r.user.fullName;
                HienThongTin(r.user.username, ten, r.user.racerId);
            }

            // Avatar lưu trong DB: chỉ tải khi chưa có trong phiên này
            if (avatarCache == null || avatarCacheUser != user)
            {
                Resp a = await ServerClient.CallAsync(new Req { command = "LAYAVATAR" });
                if (this == null) return;
                if (a != null && a.success && !string.IsNullOrEmpty(a.avatarBase64))
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(a.avatarBase64);
                        avatarCache = bytes;
                        avatarCacheUser = user;
                        ApDungBytes(bytes);
                    }
                    catch (Exception e) { Debug.LogWarning("[MenuPlayerInfo] " + e.Message); }
                }
            }
        }
    }

    private void HienThongTin(string username, string tenHienThi, string racerId)
    {
        txtTen.text = tenHienThi;
        txtUser.text = string.IsNullOrEmpty(racerId) ? $"@{username}" : $"@{username}  •  ID {racerId}";
        txtXu.text = $"COINS: {PlayerInventory.GetCoins()}";
        try { txtXe.text = "SCOOTER: " + VehicleCatalog.Get(VehicleCatalog.GetSelectedIndex()).name.ToUpper(); }
        catch { txtXe.text = ""; }
        chuChuCai.text = string.IsNullOrEmpty(tenHienThi) ? "P" : tenHienThi.Trim()[0].ToString().ToUpperInvariant();
    }

    // Avatar do launcher lưu ở máy: %LocalAppData%\DanToPhong\Avatars\<username>.png
    private void NapAvatarLocal(string username)
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DanToPhong", "Avatars", username + ".png");

            if (!File.Exists(path)) return;
            ApDungBytes(File.ReadAllBytes(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MenuPlayerInfo] " + e.Message);
        }
    }

    private void ApDungBytes(byte[] bytes)
    {
        if (rawAvatar == null) return;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) { Destroy(tex); return; }

        if (texAvatar != null) Destroy(texAvatar);
        texAvatar = tex;

        rawAvatar.texture = tex;
        rawAvatar.gameObject.SetActive(true);
        chuChuCai.gameObject.SetActive(false);
    }

    // ---------------- dựng UI: avatar 1 bên, chữ 1 bên, nằm ở góc, không khung ----------------
    private void DungUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[MenuPlayerInfo] Không thấy Canvas."); return; }
        Transform goc = canvas.rootCanvas.transform;

        Color vang = new Color(0.96f, 0.82f, 0.47f);
        bool phai = viTri == ViTriThe.TrenPhaiCanhLoa;   // đặt bên phải: avatar sát loa, chữ canh phải
        float av = kichThuocAvatar;
        float gap = 16f;
        float rongChu = 360f;

        GameObject root = new GameObject("PlayerInfo", typeof(RectTransform));
        root.transform.SetParent(goc, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        float cx = phai ? 1f : 0f;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(cx, 1f);
        rt.anchoredPosition = new Vector2(phai ? -cachMepPhaiCanhLoa : cachMep, -cachMep);
        rt.sizeDelta = new Vector2(av + gap + rongChu, av + 8f);

        // Avatar
        float ringPad = vienAvatar ? 4f : 0f;
        if (vienAvatar)
        {
            GameObject ring = TaoImage(root.transform, "AvatarRing", hinhTron, vang, false);
            DatGoc(ring.GetComponent<RectTransform>(), phai, 0f, 0f, av + ringPad * 2f, av + ringPad * 2f);
        }
        GameObject khung = TaoImage(root.transform, "AvatarMask", hinhTron, new Color(0.36f, 0.18f, 0.57f), false);
        DatGoc(khung.GetComponent<RectTransform>(), phai, ringPad, -ringPad, av, av);
        khung.AddComponent<Mask>().showMaskGraphic = true;

        GameObject raw = new GameObject("AvatarImage", typeof(RectTransform), typeof(RawImage));
        raw.transform.SetParent(khung.transform, false);
        RectTransform rr = raw.GetComponent<RectTransform>();
        rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one;
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        rawAvatar = raw.GetComponent<RawImage>();
        rawAvatar.raycastTarget = false;
        raw.SetActive(false);

        chuChuCai = TaoText(khung.transform, "Initial", Mathf.RoundToInt(av * 0.45f), FontStyle.Bold, vang, TextAnchor.MiddleCenter);
        Stretch(chuChuCai.rectTransform);

        // Khối chữ: canh giữa theo chiều dọc của avatar
        TextAnchor can = phai ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        float xChu = av + ringPad * 2f + gap;
        float hTen = coChuTen + 10f, hUser = coChuUser + 8f, hXu = coChuXu + 8f, hXe = coChuUser + 8f;
        float y = -((av + ringPad * 2f) - (hTen + hUser + hXu + hXe)) / 2f;

        txtTen = TaoText(root.transform, "Name", coChuTen, FontStyle.Bold, Color.white, can);
        DatGoc(txtTen.rectTransform, phai, xChu, y, rongChu, hTen);
        ThemBong(txtTen);
        y -= hTen;

        txtUser = TaoText(root.transform, "User", coChuUser, FontStyle.Normal, new Color(0.85f, 0.82f, 0.93f), can);
        DatGoc(txtUser.rectTransform, phai, xChu, y, rongChu, hUser);
        ThemBong(txtUser);
        y -= hUser;

        txtXu = TaoText(root.transform, "Coins", coChuXu, FontStyle.Bold, new Color(1f, 0.85f, 0.25f), can);
        DatGoc(txtXu.rectTransform, phai, xChu, y, rongChu, hXu);
        ThemBong(txtXu);
        y -= hXu;

        txtXe = TaoText(root.transform, "Scooter", coChuUser, FontStyle.Normal, new Color(0.85f, 0.82f, 0.93f), can);
        DatGoc(txtXe.rectTransform, phai, xChu, y, rongChu, hXe);
        ThemBong(txtXe);

        root.transform.SetAsLastSibling();
    }

    // Đặt theo mép trái (phai=false) hoặc mép phải (phai=true) của khối; y âm = đi xuống
    private static void DatGoc(RectTransform r, bool phai, float x, float y, float w, float h)
    {
        float c = phai ? 1f : 0f;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(c, 1f);
        r.anchoredPosition = new Vector2(phai ? -x : x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    // đổ bóng chữ để đọc rõ trên nền hình
    private static void ThemBong(Text t)
    {
        Shadow sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(2f, -2f);
    }

    private GameObject TaoImage(Transform cha, string ten, Sprite sp, Color mau, bool sliced)
    {
        GameObject go = new GameObject(ten, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(cha, false);
        Image im = go.GetComponent<Image>();
        im.sprite = sp; im.color = mau; im.raycastTarget = false;
        if (sliced) im.type = Image.Type.Sliced;
        return go;
    }

    private Text TaoText(Transform cha, string ten, int co, FontStyle st, Color mau, TextAnchor can)
    {
        GameObject go = new GameObject(ten, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(cha, false);
        Text t = go.GetComponent<Text>();
        t.font = font; t.fontSize = co; t.fontStyle = st; t.color = mau; t.alignment = can;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static Sprite TaoSpriteTron()
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float c = (n - 1) / 2f, rad = n / 2f - 1f;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rad - d + 1f));
            }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}