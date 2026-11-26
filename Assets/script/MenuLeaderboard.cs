using UnityEngine;
using UnityEngine.UI;

// Gắn vào 1 GameObject trong scene "menu" (cạnh MenuUI). Tự tạo nút LEADERBOARD ở giữa phía dưới
// và bảng xếp hạng top 10 (ID + tên + xu).
public class MenuLeaderboard : MonoBehaviour
{
    public float yNut = 80f;
    public Vector2 kichThuocNut = new Vector2(420f, 72f);

    private Font font;
    private GameObject overlay;
    private Text status;
    private Text[,] cells;        // [hàng, cột]
    private const int Rows = 10;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject btn = new GameObject("LeaderboardButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, yNut);
        rt.sizeDelta = kichThuocNut;
        btn.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.16f, 0.95f);
        btn.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.8f);
        btn.GetComponent<Button>().onClick.AddListener(Mo);

        Text t = MakeText(btn.transform, "Label", "LEADERBOARD", 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform);
        if (btn.GetComponent<ButtonFx>() == null) btn.AddComponent<ButtonFx>();   // hiệu ứng hover giống các nút khác
    }

    private void Update()
    {
        if (overlay != null && InputEscapeUtility.TryConsume()) Dong();
    }

    public static bool DongNeuDangMo()
    {
        foreach (MenuLeaderboard leaderboard in FindObjectsByType<MenuLeaderboard>(FindObjectsSortMode.None))
        {
            if (leaderboard == null || leaderboard.overlay == null) continue;
            leaderboard.Dong();
            return true;
        }
        return false;
    }

    private void Dong()
    {
        if (overlay != null) Destroy(overlay);
        overlay = null;
    }

    private async void Mo()
    {
        if (overlay != null) return;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Color vang = new Color(0.96f, 0.82f, 0.47f);

        overlay = new GameObject("LeaderboardOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.rootCanvas.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);   // chặn click xuống nền

        GameObject vien = Box(overlay.transform, "Border", vang, 780f, 760f);
        GameObject panel = Box(vien.transform, "Panel", new Color(0.07f, 0.04f, 0.12f, 1f), 774f, 754f);

        Text title = MakeText(panel.transform, "Title", "LEADERBOARD", 42, FontStyle.Bold, vang, TextAnchor.MiddleCenter);
        Top(title.rectTransform, 18f, 700f, 56f);

        string[] heads = { "#", "ID", "PLAYER", "COINS" };
        float[] xs = { 30f, 110f, 250f, 610f };
        float[] ws = { 70f, 130f, 340f, 140f };
        TextAnchor[] al = { TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, TextAnchor.MiddleLeft, TextAnchor.MiddleRight };

        for (int c = 0; c < 4; c++)
        {
            Text h = MakeText(panel.transform, "H" + c, heads[c], 20, FontStyle.Bold, new Color(0.72f, 0.68f, 0.82f), al[c]);
            Left(h.rectTransform, xs[c], 88f, ws[c], 30f);
        }

        cells = new Text[Rows, 4];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < 4; c++)
            {
                Text cell = MakeText(panel.transform, "C" + r + "_" + c, "", 24, FontStyle.Normal, Color.white, al[c]);
                cell.horizontalOverflow = HorizontalWrapMode.Wrap;
                cell.verticalOverflow = VerticalWrapMode.Truncate;
                Left(cell.rectTransform, xs[c], 124f + r * 50f, ws[c], 44f);
                cells[r, c] = cell;
            }

        status = MakeText(panel.transform, "Status", "Loading...", 20, FontStyle.Normal, new Color(0.85f, 0.82f, 0.93f), TextAnchor.MiddleCenter);
        Top(status.rectTransform, 632f, 700f, 30f);

        GameObject close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        RectTransform cr = close.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 1f);
        cr.anchoredPosition = new Vector2(0f, -672f);
        cr.sizeDelta = new Vector2(240f, 56f);
        close.GetComponent<Image>().color = new Color(0.55f, 0.07f, 0.1f, 1f);
        close.GetComponent<Button>().onClick.AddListener(Dong);
        Text ct = MakeText(close.transform, "L", "CLOSE", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Stretch(ct.rectTransform);

        if (!ServerClient.Ready)
        {
            status.text = "Open the game from the launcher to see rankings.";
            return;
        }

        Resp res = await ServerClient.CallAsync(new Req { command = "BANGXEPHANG" });
        if (overlay == null) return;   // đã đóng trong lúc chờ

        if (res == null || !res.success || res.leaderboard == null)
        {
            status.text = res == null ? "Cannot reach server." : res.message;
            return;
        }

        for (int r = 0; r < Rows && r < res.leaderboard.Count; r++)
        {
            LbEntry e = res.leaderboard[r];
            bool me = e.username == LauncherArgs.User;
            string name = string.IsNullOrEmpty(e.fullName) || e.fullName == e.username ? e.username : e.fullName + " (@" + e.username + ")";
            string[] vals = { e.rank.ToString(), string.IsNullOrEmpty(e.racerId) ? "-" : e.racerId, name, e.coins.ToString("N0") };
            for (int c = 0; c < 4; c++)
            {
                cells[r, c].text = vals[c];
                cells[r, c].color = me ? new Color(1f, 0.85f, 0.25f) : Color.white;
                cells[r, c].fontStyle = me ? FontStyle.Bold : FontStyle.Normal;
            }
        }
        status.text = res.leaderboard.Count == 0 ? "No players yet." : "Top by coins";
    }

    // ---- helpers ----
    private GameObject Box(Transform parent, string name, Color c, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = c;
        return go;
    }

    private Text MakeText(Transform parent, string name, string text, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = anchor;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    private static void Top(RectTransform r, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -y);
        r.sizeDelta = new Vector2(w, h);
    }

    private static void Left(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
