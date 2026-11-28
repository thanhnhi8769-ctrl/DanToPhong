using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Boot tạo 1 object chứa 2 component này (DontDestroyOnLoad), chỉ khi chạy từ launcher.

public class ServerHeartbeat : MonoBehaviour
{
    private const float IntervalSeconds = 20f;
    private float nextPing;
    private bool pending;

    private async void Update()
    {
        if (pending || Time.unscaledTime < nextPing || !ServerClient.Ready) return;
        nextPing = Time.unscaledTime + IntervalSeconds;
        pending = true;
        try
        {
            Resp response = await ServerClient.CallAsync(new Req { command = "PING" });
            if (response == null || !response.success || response.message != "PONG")
                Debug.LogWarning("[ServerHeartbeat] Server did not acknowledge PING.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ServerHeartbeat] " + ex.Message);
        }
        finally
        {
            pending = false;
        }
    }
}

// Chủ phòng báo trạng thái phòng lên server (mỗi khi đổi, và 8s/lần để giữ phòng còn sống).
public class RoomReporter : MonoBehaviour
{
    private const float ReportIntervalSeconds = 1f;
    private float nextReportAt;
    private bool reportPending;

    private async void Update()
    {
        if (reportPending || Time.unscaledTime < nextReportAt ||
            !ServerClient.Ready || string.IsNullOrWhiteSpace(LauncherArgs.Room))
            return;

        nextReportAt = Time.unscaledTime + ReportIntervalSeconds;
        reportPending = true;
        try
        {
            Resp response = await ServerClient.CallAsync(new Req
            {
                command = "ROOM_SCENE",
                sceneName = SceneManager.GetActiveScene().name,
                isReady = SelectManager.RoomSelectionConfirmed
            });
            if (response == null || !response.success)
                Debug.LogWarning("[RoomReporter] Scene readiness was not accepted by the server.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[RoomReporter] " + ex.GetType().Name);
        }
        finally
        {
            reportPending = false;
        }
    }
}

// Nhận lời mời + làm mới kho/xu bằng 1 lệnh LAYTRANGTHAI. Tần suất hỏi tùy theo người chơi đang làm gì.
public class InviteListener : MonoBehaviour
{
    public string sceneLobby = SceneNames.Lobby;

    private const float POLL_NGOAI_PHONG = 4f;    // đang ở menu: cần thấy lời mời nhanh
    private const float POLL_TRONG_PHONG = 10f;   // đang chờ trong phòng
    private const float POLL_DANG_DUA = 30f;      // phòng đã đóng (đang đua): không cần gì

    private float nextPoll;
    private bool polling;
    private readonly Queue<InviteDto> queue = new Queue<InviteDto>();
    private InviteDto current;
    private float shownAt;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Vào scene Lobby thì tự dựng ô "mời bạn bằng ID"
    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (s.name == SceneNames.Lobby && FindFirstObjectByType<LobbyInviteUI>() == null)
            new GameObject("LobbyInviteUI").AddComponent<LobbyInviteUI>();

        nextPoll = 0f;   // đổi scene: hỏi lại ngay để xu/kho cập nhật
    }

    private static float KhoangPoll()
    {
        if (string.IsNullOrWhiteSpace(LauncherArgs.Room)) return POLL_NGOAI_PHONG;
        return POLL_TRONG_PHONG;
    }

    private async void PollAsync()
    {
        polling = true;
        try
        {
            // 1 request: kho/xu (tự cập nhật ServerClient.Cache) + lời mời mới (chỉ khi chưa ở trong phòng)
            Resp r = await ServerClient.LayTrangThai(!string.IsNullOrWhiteSpace(LauncherArgs.Room));
            if (r != null && r.success && r.invites != null)
                foreach (InviteDto i in r.invites) queue.Enqueue(i);
        }
        finally { polling = false; }
    }

    private void Update()
    {
        if (!ServerClient.Ready) return;

        if (!polling && Time.unscaledTime >= nextPoll)
        {
            nextPoll = Time.unscaledTime + KhoangPoll();
            PollAsync();
        }

        if (current == null && queue.Count > 0 && string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            current = queue.Dequeue();
            shownAt = Time.unscaledTime;
        }

        if (current != null && (!string.IsNullOrWhiteSpace(LauncherArgs.Room) || Time.unscaledTime - shownAt > 25f))
            current = null;
    }

    private void Join(string code)
    {
        current = null;
        LobbyManager lm = FindFirstObjectByType<LobbyManager>();
        if (lm != null)
        {
            lm.VaoPhongTheoMa(code);
        }
        else
        {
            LauncherArgs.Mode = "multiplayer";
            LauncherArgs.Room = code;
            SceneManager.LoadScene(sceneLobby);
        }
    }

    private void OnGUI()
    {
        if (current == null) return;

        float s = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float w = Screen.width / s;

        Rect box = new Rect(w / 2f - 280f, 40f, 560f, 150f);
        GUI.Box(box, GUIContent.none);

        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        label.normal.textColor = Color.white;
        GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, 70f),
            Txt.InvitedYou(current.fromName, current.roomCode), label);

        GUIStyle btn = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        if (GUI.Button(new Rect(box.x + 40f, box.y + 88f, 220f, 46f), Txt.BtnJoin, btn)) Join(current.roomCode);
        if (GUI.Button(new Rect(box.x + 300f, box.y + 88f, 220f, 46f), Txt.BtnDismiss, btn)) current = null;
    }
}

// Ô nhập ID + 5 người mời gần nhất, nằm phía trên nút START GAME khi đang ở trong phòng (scene Lobby).
public class LobbyInviteUI : MonoBehaviour
{
    // Đáy khối INVITE cách tâm màn hình bao nhiêu (mép trên START GAME ≈ +98). Giảm = sát nút hơn.
    private static readonly Vector2 VI_TRI = new Vector2(0f, 128f);
    private const float RONG = 640f;      // bằng nút SELECT VEHICLE & MAP
    private const float CAO_O = 72f;
    private const float RONG_NUT = 188f;
    private const float KHOANG = 12f;
    private const float CAO_CHIP = 40f;
    private const int SO_CHIP = 5;

    private GameObject root;
    private TMP_InputField input;
    private TMP_Text status;
    private readonly Button[] chips = new Button[SO_CHIP];
    private readonly TMP_Text[] chipLabels = new TMP_Text[SO_CHIP];
    private readonly List<string> recent = new List<string>();
    private bool sending;

    private string KeyRecent => $"{LauncherArgs.User}_RecentInvites";

    private void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        float rongO = RONG - RONG_NUT - KHOANG;
        float yTieuDe = CAO_O + 8f;
        float yChip = yTieuDe + 34f + 6f;

        root = new GameObject("InvitePanel", typeof(RectTransform));
        root.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = VI_TRI;
        rt.sizeDelta = new Vector2(RONG, yChip + CAO_CHIP);

        // Hàng chip: người mời gần nhất (bấm 1 lần là mời lại)
        float rongChip = (RONG - (SO_CHIP - 1) * 8f) / SO_CHIP;
        for (int i = 0; i < SO_CHIP; i++)
        {
            Button b = MakeButton(root.transform, "Recent" + i, "", 22, new Color(0.25f, 0.25f, 0.28f, 0.95f), out chipLabels[i]);
            Place(b.GetComponent<RectTransform>(), i * (rongChip + 8f), yChip, rongChip, CAO_CHIP);
            int idx = i;
            b.onClick.AddListener(() => { if (idx < recent.Count) SendInvite(recent[idx]); });
            chips[i] = b;
        }

        // Hàng tiêu đề: tiêu đề bên trái, kết quả mời bên phải
        TMP_Text title = MakeText(root.transform, "Title", Txt.InviteTitle, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, 0f, yTieuDe, 320f, 34f);

        status = MakeText(root.transform, "Status", "", 20, FontStyles.Normal, new Color(1f, 0.85f, 0.25f), TextAlignmentOptions.MidlineRight);
        Place(status.rectTransform, 330f, yTieuDe, RONG - 330f, 34f);

        // Ô nhập (TMP_InputField dựng bằng code)
        GameObject inp = new GameObject("InviteInput", typeof(RectTransform), typeof(Image));
        inp.transform.SetParent(root.transform, false);
        Place(inp.GetComponent<RectTransform>(), 0f, 0f, rongO, CAO_O);
        Image inpBg = inp.GetComponent<Image>();
        inpBg.color = new Color(0.06f, 0.06f, 0.06f, 0.9f);

        GameObject vp = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(inp.transform, false);
        RectTransform vpRt = vp.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(16f, 0f); vpRt.offsetMax = new Vector2(-16f, 0f);

        TMP_Text txt = MakeText(vp.transform, "Text", "", 34, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
        TMP_Text ph = MakeText(vp.transform, "Placeholder", Txt.InvitePlaceholder, 34, FontStyles.Italic, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.MidlineLeft);
        foreach (TMP_Text t in new[] { txt, ph })
        {
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        }

        input = inp.AddComponent<TMP_InputField>();
        input.textViewport = vpRt;
        input.textComponent = txt;
        input.placeholder = ph;
        input.targetGraphic = inpBg;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 20;
        inp.AddComponent<InviteInputClipboard>().input = input;

        TMP_Text btnLabel;
        Button btn = MakeButton(root.transform, "InviteButton", Txt.BtnInvite, 32, new Color(0.1f, 0.55f, 0.35f, 0.95f), out btnLabel);
        Place(btn.GetComponent<RectTransform>(), rongO + KHOANG, 0f, RONG_NUT, CAO_O);
        btn.onClick.AddListener(() => SendInvite(input.text));

        LoadRecent();
        RefreshChips();
    }

    // Giữ lại cho code cũ gọi (LobbyManager); không còn bắt buộc vì Update tự suy ra từ trạng thái phòng.
    public void HideForStart()
    {
        if (root != null) root.SetActive(false);
    }

    private void Update()
    {
        if (root == null) return;

        bool inRoom = !string.IsNullOrWhiteSpace(LauncherArgs.Room);
        bool inLobby = SceneManager.GetActiveScene().name.IndexOf("lobby", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool shouldShow = inRoom && inLobby;
        if (root.activeSelf != shouldShow)
            root.SetActive(shouldShow);
    }

    private async void SendInvite(string key)
    {
        if (string.IsNullOrWhiteSpace(LauncherArgs.Room) || sending || input == null) return;
        key = (key ?? "").Trim();
        if (key.Length == 0) return;

        sending = true;
        status.text = Txt.Sending;
        Resp r = await ServerClient.CallAsync(new Req { command = "MOIBAN", targetId = key, roomId = LauncherArgs.Room });
        sending = false;
        if (this == null) return;

        status.text = r == null ? Txt.NoServer : r.message;
        if (r != null && r.success)
        {
            input.text = "";
            AddRecent(key);
            RefreshChips();
        }
    }

    // ---------------- người mời gần nhất ----------------
    private void LoadRecent()
    {
        recent.Clear();
        string raw = PlayerPrefs.GetString(KeyRecent, "");
        foreach (string s in raw.Split('|'))
            if (s.Length > 0 && recent.Count < SO_CHIP) recent.Add(s);
    }

    private void AddRecent(string key)
    {
        key = key.Replace("|", "");
        recent.RemoveAll(x => string.Equals(x, key, System.StringComparison.OrdinalIgnoreCase));
        recent.Insert(0, key);
        if (recent.Count > SO_CHIP) recent.RemoveRange(SO_CHIP, recent.Count - SO_CHIP);

        PlayerPrefs.SetString(KeyRecent, string.Join("|", recent));
        PlayerPrefs.Save();
    }

    private void RefreshChips()
    {
        for (int i = 0; i < SO_CHIP; i++)
        {
            bool co = i < recent.Count;
            chips[i].gameObject.SetActive(co);
            if (co) chipLabels[i].text = recent[i];
        }
    }

    // ---------------- helper dựng UI ----------------
    private static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, float size, Color bg, out TMP_Text labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = bg;
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;

        labelText = MakeText(go.transform, "Label", label, size, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(labelText.rectTransform, 8f);
        return b;
    }

    private static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    private static void Stretch(RectTransform r, float pad)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(pad, 0f); r.offsetMax = new Vector2(-pad, 0f);
    }
}

// Ctrl+V hoặc chuột phải = dán ID từ clipboard vào ô (thay toàn bộ nội dung)
public class InviteInputClipboard : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField input;
    private bool canDan;

    private static bool GiuCtrl()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    }

    private void Update()
    {
        if (input == null || !input.isFocused) return;
        if (GiuCtrl() && Input.GetKeyDown(KeyCode.V)) canDan = true;
    }

    // LateUpdate để đè lên kết quả dán mặc định của TMP_InputField
    private void LateUpdate()
    {
        if (!canDan) return;
        canDan = false;
        Dan();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right) Dan();
    }

    private void Dan()
    {
        if (input == null || !input.interactable) return;
        string s = (GUIUtility.systemCopyBuffer ?? "").Replace("\r", "").Replace("\n", "").Trim();
        if (s.Length == 0) return;
        if (s.Length > input.characterLimit) s = s.Substring(0, input.characterLimit);

        input.text = s;
        input.ActivateInputField();
        input.caretPosition = s.Length;
    }
}