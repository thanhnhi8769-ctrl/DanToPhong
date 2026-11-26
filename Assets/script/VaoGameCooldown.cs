using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bộ đếm chờ giữa 2 lần vào game. Biến static nên không mất khi đổi scene.
public static class VaoGameCooldown
{
    // Chỉnh trong Inspector của LobbyCountdownUI (script đó gán vào đây khi chạy)
    public static float ThoiGianChoSauKhiThoat = 3f;

    // true trong lúc đếm ngược TRƯỚC KHI vào scene game
    public static bool DangDemNguoc = false;

    private static float hetHan = 0f;

    public static void BatDau()
    {
        BatDau(ThoiGianChoSauKhiThoat);
    }

    public static void BatDau(float giay)
    {
        hetHan = Mathf.Max(hetHan, Time.unscaledTime + giay);
    }

    public static float ConLai
    {
        get
        {
            float c = hetHan - Time.unscaledTime;
            return c > 0f ? c : 0f;
        }
    }

    public static bool DangCho
    {
        get { return DangDemNguoc || ConLai > 0f; }
    }
}

// Gắn vào 1 object trong scene lobby: kéo các nút vào game (Start / Join / Create...) và 1 TMP_Text hiển thị số giây.
public class LobbyCountdownUI : MonoBehaviour
{
    public Button[] cacNutVaoGame;
    public TMP_Text chuDemNguoc;
    public string mauChu = "Please wait {0}s for the game to reload...";
    public string chuDangKetNoi = "Reconnecting...";

    [Header("Thời gian (giây)")]
    [Tooltip("Đếm ngược TRƯỚC KHI vào scene game (gọi DemNguocRoiVao từ nút Start / Join)")]
    public float thoiGianDemNguocTruocKhiVao = 3f;
    [Tooltip("Chờ ở lobby SAU KHI thoát khỏi game, để game kịp dọn và tải lại")]
    public float thoiGianChoSauKhiThoat = 3f;
    public string mauChuVao = "Entering game in {0}s...";

    private bool dangKhoa = false;
    private bool dangDem = false;
    private int giayCuoi = -1;

    private void Awake()
    {
        VaoGameCooldown.ThoiGianChoSauKhiThoat = Mathf.Max(0f, thoiGianChoSauKhiThoat);
    }

    private void OnEnable()
    {
        VaoGameCooldown.DangDemNguoc = false;
        CapNhat(true);
    }

    private void OnDestroy()
    {
        VaoGameCooldown.DangDemNguoc = false;
    }

    // Gọi từ nút vào game: countdown local trước khi tải scene đã chọn.
    public void DemNguocRoiVao(System.Action vaoGame)
    {
        if (dangDem || VaoGameCooldown.DangCho) return;
        StartCoroutine(ChayDemNguoc(vaoGame));
    }

    private IEnumerator ChayDemNguoc(System.Action vaoGame)
    {
        dangDem = true;
        VaoGameCooldown.DangDemNguoc = true;
        CapNhat(true);

        if (chuDemNguoc != null) chuDemNguoc.gameObject.SetActive(true);

        int giayHienThi = -1;
        for (float t = thoiGianDemNguocTruocKhiVao; t > 0f; t -= Time.unscaledDeltaTime)
        {
            int g = Mathf.CeilToInt(t);
            if (g != giayHienThi && chuDemNguoc != null)
            {
                giayHienThi = g;
                chuDemNguoc.text = string.Format(mauChuVao, g);
            }
            yield return null;
        }

        // Giữ nút khóa tới khi scene đổi (DangDemNguoc chỉ được reset khi lobby mở lại)
        try { vaoGame?.Invoke(); }
        catch (System.Exception ex)
        {
            Debug.LogError("[LobbyCountdownUI] Could not enter the selected scene: " + ex.Message);
            dangDem = false;
            VaoGameCooldown.DangDemNguoc = false;
            if (chuDemNguoc != null) chuDemNguoc.text = "Could not load the scene. Please try again.";
            CapNhat(true);
            yield break;
        }

        // Phòng khi nạp scene thất bại: sau 8 giây mở khóa lại để không kẹt nút
        yield return new WaitForSecondsRealtime(8f);
        dangDem = false;
        VaoGameCooldown.DangDemNguoc = false;
        CapNhat(true);
    }

    private void Update()
    {
        CapNhat(false);
    }

    private void CapNhat(bool ep)
    {
        bool cho = VaoGameCooldown.DangCho;

        if (cho != dangKhoa || ep)
        {
            dangKhoa = cho;
            if (cacNutVaoGame != null)
            {
                for (int i = 0; i < cacNutVaoGame.Length; i++)
                    if (cacNutVaoGame[i] != null) cacNutVaoGame[i].interactable = !cho;
            }

            if (chuDemNguoc != null && chuDemNguoc.gameObject.activeSelf != cho)
                chuDemNguoc.gameObject.SetActive(cho);

            giayCuoi = -1;
        }

        if (dangDem) return;
        if (!cho || chuDemNguoc == null) return;

        float conLai = VaoGameCooldown.ConLai;
        int giay = Mathf.CeilToInt(conLai);
        if (giay == giayCuoi) return;
        giayCuoi = giay;

        chuDemNguoc.text = giay > 0 ? string.Format(mauChu, giay) : chuDangKetNoi;
    }
}
