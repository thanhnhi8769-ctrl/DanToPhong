using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using ExitGames.Client.Photon;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class MultiplayerAllInOne : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte CHAT_EVENT = 77;
    private const int MAX_MESSAGES = 40;

    // Scooter và GameManager/SelectManager đọc cờ này: đang gõ chat thì không xử lý phím tắt/di chuyển
    public static bool DangGoChat { get; private set; }

    [Header("Cài đặt UI")]
    [Tooltip("Kéo thả font chữ vào đây. Nếu để trống, hệ thống sẽ tự tìm font mặc định.")]
    public Font customFont;

    [Header("Photon Voice")]
    private PunVoiceClient voiceClient;
    private Recorder recorder;
    private GameObject speakerTemplate;
    private bool voiceOn;

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

    // THÔNG BÁO TIN NHẮN MỚI
    private int unreadCount = 0;
    private GameObject notifBadge;
    private Text notifText;

    private readonly List<string> messages = new List<string>();
    private Font defaultFont;
    private Sprite roundedSprite;
    private Texture2D roundedTexture;

    private readonly Color hudGray = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    private readonly Color hudPanel = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    private readonly Color hudHeader = new Color(0.2f, 0.2f, 0.2f, 1f);
    private readonly Color hudInput = new Color(0.05f, 0.05f, 0.05f, 0.9f);
    private readonly Color micActive = new Color(0.2f, 0.65f, 0.3f, 0.9f);
    private readonly Color white = new Color(0.95f, 0.95f, 0.95f, 1f);
    private readonly Color softText = new Color(0.7f, 0.75f, 0.8f, 1f);

    private const string TEXT_CHAT = "CHAT";
    private const string TEXT_MIC_ON = "MIC ON";
    private const string TEXT_MIC_OFF = "MIC OFF";

    // Sprite bo góc tự vẽ bằng code
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
        TaoVoice();
        TaoCanvas();
        CapNhatTrangThaiPhong();
        CapNhatNutMic();

        ThemTinNhanHeThong("Hệ thống Chat đã sẵn sàng!");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        DangGoChat = false;
    }

    private void Update()
    {
        bool dangMo = chatPanel != null && chatPanel.activeSelf;
        bool dangGo = dangMo && inputField != null && inputField.isFocused;
        DangGoChat = dangGo;

        // Hỗ trợ nhấn Enter / Numpad Enter để gửi ngay hoặc focus vào ô nhập
        if (dangMo && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
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

    private void FocusOChat()
    {
        if (inputField == null) return;
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void TaoEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject obj = new GameObject("MultiplayerEventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
    }

    private void TaoVoice()
    {
        voiceClient = FindFirstObjectByType<PunVoiceClient>();
        if (voiceClient == null) voiceClient = gameObject.AddComponent<PunVoiceClient>();
        voiceClient.UsePunAppSettings = true;
        voiceClient.AutoConnectAndJoin = true;

        recorder = FindFirstObjectByType<Recorder>();
        if (recorder == null) recorder = gameObject.AddComponent<Recorder>();
        recorder.SourceType = Recorder.InputSourceType.Microphone;
        recorder.RecordWhenJoined = true;
        recorder.TransmitEnabled = false;

        recorder.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling48000;

        voiceClient.PrimaryRecorder = recorder;
        voiceClient.AddRecorder(recorder);

        speakerTemplate = new GameObject("RuntimeSpeakerTemplate");
        speakerTemplate.transform.SetParent(transform, false);
        AudioSource source = speakerTemplate.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        speakerTemplate.AddComponent<Speaker>();

        voiceClient.SpeakerPrefab = speakerTemplate;
        speakerTemplate.hideFlags = HideFlags.HideInHierarchy;
    }

    private void ToggleMic()
    {
        if (!PhotonNetwork.InRoom) { ThemTinNhanHeThong("Bạn cần vào phòng trước khi bật micro."); return; }
        if (recorder == null) { ThemTinNhanHeThong("Không tìm thấy thiết bị ghi âm."); return; }

        voiceOn = !voiceOn;
        if (voiceOn) recorder.RecordingEnabled = true;
        recorder.TransmitEnabled = voiceOn;

        CapNhatNutMic();
        ThemTinNhanHeThong(voiceOn ? "Bạn đã bật micro." : "Bạn đã tắt micro.");
    }

    private void CapNhatNutMic()
    {
        if (micText != null) micText.text = voiceOn ? TEXT_MIC_ON : TEXT_MIC_OFF;
        if (micButton != null)
        {
            Image image = micButton.GetComponent<Image>();
            if (image != null) image.color = voiceOn ? micActive : hudGray;
        }
    }

    public override void OnJoinedRoom()
    {
        voiceOn = false;
        if (recorder != null) recorder.TransmitEnabled = false;
        CapNhatNutMic();
        CapNhatTrangThaiPhong();
        ThemTinNhanHeThong("Đã kết nối vào phòng.");
    }

    public override void OnLeftRoom()
    {
        voiceOn = false;
        if (recorder != null) recorder.TransmitEnabled = false;
        CapNhatNutMic();
        CapNhatTrangThaiPhong();
        ThemTinNhanHeThong("Bạn đã rời phòng.");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (newPlayer != null) ThemTinNhanHeThong(TenNguoiChoi(newPlayer) + " đã tham gia.");
        CapNhatTrangThaiPhong();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer != null) ThemTinNhanHeThong(TenNguoiChoi(otherPlayer) + " đã rời đi.");
        CapNhatTrangThaiPhong();
    }

    private static string TenNguoiChoi(Player p)
    {
        if (p == null) return "Người chơi";
        return string.IsNullOrEmpty(p.NickName) ? "Người chơi #" + p.ActorNumber : p.NickName;
    }

    private void CapNhatTrangThaiPhong()
    {
        if (roomStatusText == null) return;
        roomStatusText.text = PhotonNetwork.InRoom ? "TRONG PHÒNG: " + PhotonNetwork.CurrentRoom.PlayerCount : "CHƯA KẾT NỐI";
    }

    private void TaoCanvas()
    {
        GameObject obj = new GameObject("MultiplayerOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        TaoNutMic(new Vector2(20, 20));
        TaoNutChat(new Vector2(100, 20));

        TaoKhungChat();
    }

    private void TaoNutChat(Vector2 position)
    {
        chatButton = TaoIconButton(TEXT_CHAT, position, new Vector2(70, 40), hudGray, canvas.transform, 15, Vector2.zero);
        chatButtonText = chatButton.GetComponentInChildren<Text>();

        // Chấm đỏ thông báo tin nhắn (góc trên bên phải nút Chat)
        notifBadge = TaoPanelCon("NotifBadge", chatButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-5, -5), new Vector2(22, 22), new Color(0.9f, 0.2f, 0.2f, 1f));
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
                CapNhatScroll();
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
        chatPanel = TaoPanel("ChatPanel", new Vector2(20, 70), new Vector2(375, 400), hudPanel, Vector2.zero);

        GameObject header = TaoPanelCon("Header", chatPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 40), hudHeader);

        Text title = TaoText(header.transform, "PHÒNG CHAT", 14, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Stretch(title.rectTransform, 0, 0, 0, 10);
        title.fontStyle = FontStyle.Bold;

        roomStatusText = TaoText(header.transform, "CHƯA KẾT NỐI", 11, new Vector2(0, -22), new Vector2(375, 20), TextAnchor.MiddleCenter);
        roomStatusText.color = softText;

        GameObject viewport = new GameObject("ChatViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(chatPanel.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(1, 1);
        viewportRect.offsetMin = new Vector2(10, 55);
        viewportRect.offsetMax = new Vector2(-10, -45);

        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

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
        contentRect.sizeDelta = new Vector2(-10, 0);
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject history = new GameObject("HistoryText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        history.transform.SetParent(content.transform, false);
        RectTransform historyRect = history.GetComponent<RectTransform>();
        historyRect.anchorMin = new Vector2(0, 1);
        historyRect.anchorMax = new Vector2(1, 1);
        historyRect.pivot = new Vector2(0.5f, 1);
        historyRect.anchoredPosition = new Vector2(0, -5);
        historyRect.sizeDelta = new Vector2(0, 0);

        historyText = history.GetComponent<Text>();
        historyText.font = defaultFont;
        historyText.fontSize = 14;
        historyText.color = white;
        historyText.alignment = TextAnchor.UpperLeft;
        historyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        historyText.verticalOverflow = VerticalWrapMode.Overflow;
        historyText.supportRichText = true;
        historyText.lineSpacing = 1.2f;
        history.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        GameObject inputBg = TaoPanelCon("InputBackground", chatPanel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-75, 40), hudInput);

        inputField = inputBg.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;

        // Bật con trỏ nhấp nháy (caret) rõ nét màu trắng
        inputField.customCaretColor = true;
        inputField.caretColor = Color.white;
        inputField.caretWidth = 3;
        inputField.caretBlinkRate = 0.85f;
        inputField.selectionColor = new Color(1f, 1f, 1f, 0.25f);

        Text inputText = TaoText(inputBg.transform, "", 14, new Vector2(10, 0), Vector2.zero, TextAnchor.MiddleLeft);
        Stretch(inputText.rectTransform, 10, 0, -10, 0);

        Text placeholder = TaoText(inputBg.transform, "Nhập...", 14, new Vector2(10, 0), Vector2.zero, TextAnchor.MiddleLeft);
        Stretch(placeholder.rectTransform, 10, 0, -10, 0);
        placeholder.color = softText;

        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;

        // Gửi tin nhắn ngay khi nhấn Enter (kể cả khi InputField vừa mất focus)
        inputField.onEndEdit.AddListener((text) =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GuiTinNhan();
            }
        });

        Button sendButton = TaoIconButton("GỬI", new Vector2(-8, 8), new Vector2(55, 40), hudHeader, chatPanel.transform, 13, new Vector2(1, 0));
        sendButton.onClick.AddListener(GuiTinNhan);

        chatPanel.SetActive(false);
    }

    private void GuiTinNhan()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

        if (!PhotonNetwork.InRoom)
        {
            ThemTinNhanHeThong("Bạn chưa kết nối vào phòng nên không thể gửi tin nhắn.");
            inputField.text = "";
            FocusOChat();
            return;
        }

        string msg = inputField.text.Trim();
        if (msg.Length > 120) msg = msg.Substring(0, 120);

        object[] data = { TenNguoiChoi(PhotonNetwork.LocalPlayer), msg };
        PhotonNetwork.RaiseEvent(CHAT_EVENT, data, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

        inputField.text = "";
        FocusOChat();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CHAT_EVENT) return;
        object[] data = photonEvent.CustomData as object[];
        if (data != null && data.Length >= 2) ThemTinNhan(data[0] as string, data[1] as string, false);
    }

    private void ThemTinNhanHeThong(string message) => ThemTinNhan("HỆ THỐNG", message, true);

    private void ThemTinNhan(string playerName, string message, bool systemMessage)
    {
        playerName = (playerName ?? "Player").Replace("\n", " ");
        message = (message ?? "").Replace("\n", " ");

        string line = systemMessage
            ? $"<color=#AAAAAA><i>● {playerName}: {message}</i></color>"
            : $"<b>{playerName}</b>\n{message}";

        messages.Add(line);
        if (messages.Count > MAX_MESSAGES) messages.RemoveAt(0);

        if (historyText != null)
        {
            historyText.text = string.Join("\n\n", messages);
            CapNhatScroll();
        }

        // Tăng bộ đếm tin chưa đọc nếu khung chat đang đóng
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

    private void CapNhatScroll()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
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
        if (recorder != null) { recorder.TransmitEnabled = false; recorder.RecordingEnabled = false; }
        if (speakerTemplate != null) Destroy(speakerTemplate);
        if (roundedSprite != null) Destroy(roundedSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}