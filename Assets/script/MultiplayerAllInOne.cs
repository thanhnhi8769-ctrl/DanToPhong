using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public class MultiplayerAllInOne : MonoBehaviour
{
    private const int MAX_MESSAGES = 40;
    private const float CHAT_POLL_INTERVAL = 1f;

    public static bool DangGoChat { get; private set; }

    [Header("Cài đặt UI")]
    public Font customFont;
    [Tooltip("Bật: vừa vào scene game là mở sẵn khung chat luôn.")]
    public bool moSanKhungChatKhiVaoGame = true;

    [Header("UI - Runtime")]
    private Canvas canvas;
    private GameObject chatPanel;
    private Button chatButton;
    private Button micButton;
    private Text chatButtonText;
    private Text micText;
    private Text historyText;
    private InputField inputField;
    private ScrollRect scrollRect;
    private Text roomStatusText;

    private int unreadCount = 0;
    private GameObject notifBadge;
    private Text notifText;

    private readonly List<string> messages = new List<string>();
    private Font defaultFont;
    private Sprite roundedSprite;
    private Texture2D roundedTexture;

    private string trangThaiCuoi;
    private bool scrollDirty = false;
    private bool historyDirty = false;
    private float nextChatPoll;
    private bool pollingChat;
    private long lastChatMessageId;

    private readonly Color hudGray = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    private readonly Color hudPanel = new Color(0.08f, 0.08f, 0.10f, 0.94f);
    private readonly Color hudHeader = new Color(0.18f, 0.18f, 0.22f, 1f);
    private readonly Color hudInput = new Color(0.04f, 0.04f, 0.05f, 0.95f);
    private readonly Color white = new Color(0.95f, 0.95f, 0.95f, 1f);
    private readonly Color softText = new Color(0.7f, 0.75f, 0.8f, 1f);

    private const string TEXT_CHAT = "CHAT";
    private const string TEXT_MIC_ON = "MIC ON";
    private const string TEXT_MIC_OFF = "MIC OFF";

    private Sprite TaoSpriteBoGoc(int size, int bk)
    {
        roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        roundedTexture.wrapMode = TextureWrapMode.Clamp;
        roundedTexture.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[size * size];
        float h = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - h) - (h - bk);
                float qy = Mathf.Abs(y + 0.5f - h) - (h - bk);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - bk;
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        roundedTexture.SetPixels32(px);
        roundedTexture.Apply(false, true);

        return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(bk, bk, bk, bk));
    }

    private void Start()
    {
        if (customFont != null) defaultFont = customFont;
        else
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        roundedSprite = TaoSpriteBoGoc(64, 14);

        TaoEventSystem();
        TaoCanvas();
        CapNhatTrangThaiPhong();
        CapNhatNutMic();

        ThemTinNhanHeThong("Hệ thống Chat đã sẵn sàng!");
    }

    private void OnDisable()
    {
        DangGoChat = false;
    }

    private static bool NhanPhimEnter()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private void Update()
    {
        if (!pollingChat && Time.unscaledTime >= nextChatPoll && !string.IsNullOrWhiteSpace(LauncherArgs.Room))
            PollChatAsync();

        bool dangMo = chatPanel != null && chatPanel.activeSelf;
        bool dangGo = dangMo && inputField != null && inputField.isFocused;
        DangGoChat = dangGo;

        // ESC khi đang gõ chat: thoát ô nhập để các phím game / menu dùng lại được
        if (dangGo && InputEscapeUtility.TryConsume())
        {
            ThoatKhoiFocusChat();
            return;
        }

        if (dangMo && NhanPhimEnter())
        {
            if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
            {
                GuiTinNhan();
            }
            else if (inputField != null && !inputField.isFocused)
            {
                FocusOChat();
            }
        }
    }

    public static bool DongChatNeuDangGo()
    {
        MultiplayerAllInOne chat = FindFirstObjectByType<MultiplayerAllInOne>();
        if (chat == null || chat.chatPanel == null || !chat.chatPanel.activeSelf ||
            chat.inputField == null || !chat.inputField.isFocused)
        {
            DangGoChat = false;
            return false;
        }
        chat.ThoatKhoiFocusChat();
        return true;
    }

    private void ThoatKhoiFocusChat()
    {
        if (inputField != null) inputField.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        DangGoChat = false;
    }

    // Gom mọi yêu cầu cuộn/đo layout trong frame thành 1 lần duy nhất
    private void LateUpdate()
    {
        if (!scrollDirty || scrollRect == null || chatPanel == null || !chatPanel.activeInHierarchy) return;

        scrollDirty = false;
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void FocusOChat()
    {
        if (inputField == null) return;
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void TaoEventSystem()
    {
        EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all != null && all.Length > 0)
        {
            all[0].gameObject.SetActive(true);
            all[0].enabled = true;
            for (int i = 1; i < all.Length; i++)
                Destroy(all[i].gameObject);
            return;
        }

        GameObject obj = new GameObject("MultiplayerEventSystem");
        obj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        obj.AddComponent<InputSystemUIInputModule>();
#else
        obj.AddComponent<StandaloneInputModule>();
#endif
    }

    private void ToggleMic()
    {
        ThemTinNhanHeThong("Microphone voice chat is disabled in this build. Audio is not captured or sent; use text chat instead.");
    }

    private void CapNhatNutMic()
    {
        if (micText != null) micText.text = TEXT_MIC_OFF;
        if (micButton != null)
        {
            Image image = micButton.GetComponent<Image>();
            if (image != null) image.color = hudGray;
        }
    }

    // Chỉ gọi khi có sự kiện phòng (vào / ra / có người vào / ra), không gọi mỗi frame
    private void CapNhatTrangThaiPhong()
    {
        if (roomStatusText == null) return;

        string trangThai = !string.IsNullOrWhiteSpace(LauncherArgs.Room)
            ? "TCP ROOM: " + LauncherArgs.Room
            : "SOLO";

        if (trangThai == trangThaiCuoi) return;

        trangThaiCuoi = trangThai;
        roomStatusText.text = trangThai;
    }

    private void TaoCanvas()
    {
        GameObject obj = new GameObject("MultiplayerOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dời sang phải 76px để chừa góc trái dưới cho nút "?" (HelpSystem) cố định ở mọi scene
        TaoNutMic(new Vector2(96, 20));
        TaoNutChat(new Vector2(176, 20));

        TaoKhungChat();
    }

    private void TaoNutChat(Vector2 position)
    {
        chatButton = TaoIconButton(TEXT_CHAT, position, new Vector2(70, 40), hudGray, canvas.transform, 15, Vector2.zero);
        chatButtonText = chatButton.GetComponentInChildren<Text>();

        notifBadge = TaoPanelCon("NotifBadge", chatButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-5, -5), new Vector2(22, 22), new Color(0.9f, 0.2f, 0.2f, 1f));
        notifBadge.GetComponent<Image>().raycastTarget = false;
        notifText = TaoText(notifBadge.transform, "0", 12, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Stretch(notifText.rectTransform, 0, 0, 0, 0);
        notifText.fontStyle = FontStyle.Bold;
        notifBadge.SetActive(false);

        chatButton.onClick.AddListener(() =>
        {
            if (chatPanel == null) return;
            bool show = !chatPanel.activeSelf;
            chatPanel.SetActive(show);

            if (show)
            {
                if (historyDirty) LamMoiLichSu();
                else CapNhatScroll();

                FocusOChat();

                unreadCount = 0;
                if (notifBadge != null) notifBadge.SetActive(false);
            }
            else
            {
                DangGoChat = false;
            }
        });
    }

    private void TaoNutMic(Vector2 position)
    {
        micButton = TaoIconButton(TEXT_MIC_OFF, position, new Vector2(70, 40), hudGray, canvas.transform, 14, Vector2.zero);
        micText = micButton.GetComponentInChildren<Text>();
        micButton.onClick.AddListener(ToggleMic);
    }

    private void TaoKhungChat()
    {
        chatPanel = TaoPanel("ChatPanel", new Vector2(96, 70), new Vector2(360, 280), hudPanel, Vector2.zero);

        GameObject header = TaoPanelCon("Header", chatPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 38), hudHeader);

        Text title = TaoText(header.transform, "PHÒNG CHAT", 14, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Stretch(title.rectTransform, 0, 0, 0, 10);
        title.fontStyle = FontStyle.Bold;

        roomStatusText = TaoText(header.transform, "CHƯA KẾT NỐI", 11, new Vector2(0, -20), new Vector2(360, 18), TextAnchor.MiddleCenter);
        roomStatusText.color = softText;

        GameObject viewport = new GameObject("ChatViewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(chatPanel.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(1, 1);
        viewportRect.offsetMin = new Vector2(10, 52);
        viewportRect.offsetMax = new Vector2(-10, -42);

        GameObject scrollObj = new GameObject("ChatScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(viewport.transform, false);
        Stretch(scrollObj.GetComponent<RectTransform>(), 0, 0, 0, 0);

        scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.viewport = viewportRect;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        content.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject history = new GameObject("HistoryText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        history.transform.SetParent(content.transform, false);
        RectTransform historyRect = history.GetComponent<RectTransform>();
        historyRect.anchorMin = new Vector2(0, 1);
        historyRect.anchorMax = new Vector2(1, 1);
        historyRect.pivot = new Vector2(0.5f, 1);
        historyRect.anchoredPosition = new Vector2(0, -4);
        historyRect.sizeDelta = new Vector2(0, 0);

        historyText = history.GetComponent<Text>();
        historyText.font = defaultFont;
        historyText.fontSize = 15;
        historyText.color = white;
        historyText.alignment = TextAnchor.UpperLeft;
        historyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        historyText.verticalOverflow = VerticalWrapMode.Overflow;
        historyText.supportRichText = true;
        historyText.lineSpacing = 1.2f;
        historyText.raycastTarget = false;
        history.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        GameObject inputBg = TaoPanelCon("InputBackground", chatPanel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-75, 38), hudInput);
        inputBg.GetComponent<Image>().raycastTarget = true;

        inputField = inputBg.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;

        inputField.customCaretColor = true;
        inputField.caretColor = Color.white;
        inputField.caretWidth = 3;
        inputField.caretBlinkRate = 0.85f;
        inputField.selectionColor = new Color(1f, 1f, 1f, 0.25f);

        Text inputText = TaoText(inputBg.transform, "", 14, new Vector2(10, 0), Vector2.zero, TextAnchor.MiddleLeft);
        Stretch(inputText.rectTransform, 10, 0, -10, 0);

        Text placeholder = TaoText(inputBg.transform, "Nhập tin nhắn...", 14, new Vector2(10, 0), Vector2.zero, TextAnchor.MiddleLeft);
        Stretch(placeholder.rectTransform, 10, 0, -10, 0);
        placeholder.color = softText;

        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;

        inputField.onEndEdit.AddListener((text) =>
        {
            if (NhanPhimEnter())
                GuiTinNhan();
        });

        Button sendButton = TaoIconButton("GỬI", new Vector2(-8, 8), new Vector2(55, 38), hudHeader, chatPanel.transform, 13, new Vector2(1, 0));
        sendButton.onClick.AddListener(GuiTinNhan);

        // Chơi 1 người thì không cần mở sẵn khung chat (đỡ che giao diện); có từ 2 người trở lên mới tự mở
        bool coNhieuNguoi = !string.IsNullOrWhiteSpace(LauncherArgs.Room);
        chatPanel.SetActive(moSanKhungChatKhiVaoGame && coNhieuNguoi);
    }

    private void GuiTinNhan()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

        if (string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            ThemTinNhanHeThong("Join a TCP room before sending a chat message.");
            inputField.text = "";
            FocusOChat();
            return;
        }

        string msg = inputField.text.Trim();
        if (msg.Length > 120) msg = msg.Substring(0, 120);
        inputField.text = "";
        FocusOChat();
        SendChatAsync(msg);
    }

    private async void SendChatAsync(string message)
    {
        try
        {
            Resp response = await ServerClient.CallAsync(new Req
            {
                command = "CHAT_SEND",
                chatMessage = message
            });
            if (response == null || !response.success)
                ThemTinNhanHeThong(response != null ? response.message : "Chat send failed.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[TCP chat] " + ex.GetType().Name);
            ThemTinNhanHeThong("Chat send failed.");
        }
    }

    private async void PollChatAsync()
    {
        pollingChat = true;
        nextChatPoll = Time.unscaledTime + CHAT_POLL_INTERVAL;
        try
        {
            Resp response = await ServerClient.CallAsync(new Req
            {
                command = "CHAT_POLL",
                afterMessageId = lastChatMessageId
            });
            if (response == null || !response.success || response.chatMessages == null)
                return;

            foreach (RoomChatMessageDto message in response.chatMessages)
            {
                if (message == null || message.messageId <= lastChatMessageId)
                    continue;
                lastChatMessageId = message.messageId;
                ThemTinNhan(message.username, message.message, false);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[TCP chat] " + ex.GetType().Name);
        }
        finally
        {
            pollingChat = false;
        }
    }

    private void ThemTinNhanHeThong(string message) => ThemTinNhan("HỆ THỐNG", message, true);

    private void ThemTinNhan(string playerName, string message, bool systemMessage)
    {
        playerName = (playerName ?? "Player").Replace("\n", " ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        message = (message ?? "").Replace("\n", " ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        string line = systemMessage
            ? $"<color=#AAAAAA><i>● {playerName}: {message}</i></color>"
            : $"<color=#FFD54F><b>{playerName}:</b></color> {message}";

        messages.Add(line);
        if (messages.Count > MAX_MESSAGES) messages.RemoveAt(0);

        // Khung chat đang đóng thì chưa cần dựng lại chữ / layout, đợi lúc mở
        if (chatPanel != null && chatPanel.activeSelf) LamMoiLichSu();
        else historyDirty = true;

        if (!systemMessage && chatPanel != null && !chatPanel.activeSelf)
        {
            unreadCount++;
            if (notifBadge != null)
            {
                notifBadge.SetActive(true);
                notifText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
            }
        }
    }

    private void LamMoiLichSu()
    {
        historyDirty = false;
        if (historyText == null) return;

        historyText.text = string.Join("\n", messages);
        scrollDirty = true;
    }

    private void CapNhatScroll()
    {
        scrollDirty = true;
    }

    private Button TaoIconButton(string textVal, Vector2 position, Vector2 size, Color color, Transform parent, int fontSize, Vector2 anchorPivot)
    {
        GameObject obj = new GameObject("IconButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchorPivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.color = color;
        if (roundedSprite != null) { image.sprite = roundedSprite; image.type = Image.Type.Sliced; }

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(color.r + 0.12f, color.g + 0.12f, color.b + 0.12f, color.a);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, color.a);
        colors.selectedColor = color;
        button.colors = colors;

        Text text = TaoText(obj.transform, textVal, fontSize, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 0, 0, 0, 0);
        text.fontStyle = FontStyle.Bold;

        return button;
    }

    private Text TaoText(Transform parent, string value, int size, Vector2 position, Vector2 dimensions, TextAnchor alignment)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text txt = obj.GetComponent<Text>();
        txt.font = defaultFont;
        txt.text = value;
        txt.fontSize = size;
        txt.color = white;
        txt.alignment = alignment;
        txt.supportRichText = true;
        txt.raycastTarget = false;
        return txt;
    }

    private GameObject TaoPanel(string name, Vector2 position, Vector2 size, Color color, Vector2 anchorPivot)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchorPivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (roundedSprite != null) { image.sprite = roundedSprite; image.type = Image.Type.Sliced; }
        return obj;
    }

    private GameObject TaoPanelCon(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (roundedSprite != null) { image.sprite = roundedSprite; image.type = Image.Type.Sliced; }
        return obj;
    }

    private void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void OnDestroy()
    {
        DangGoChat = false;
        if (roundedSprite != null) Destroy(roundedSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}
