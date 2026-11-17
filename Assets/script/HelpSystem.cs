using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HelpSystem : MonoBehaviour
{
    private const float HELP_LEFT = 28f;
    private const float HELP_BOTTOM = 28f;
    private const float HELP_GAME_BOTTOM = 88f;
    private const float HELP_SIZE = 54f;

    // Tông màu xám bạc chủ đạo
    private static readonly Color MAU_CHU_DAO = new Color(0.75f, 0.79f, 0.84f, 1f);
    private static readonly Color MAU_NEN_KHUNG = new Color(0.11f, 0.12f, 0.14f, 0.98f);
    private static readonly Color MAU_NUT_XAM = new Color(0.28f, 0.31f, 0.36f, 0.96f);
    private static readonly Color MAU_CHU_NOI_DUNG = new Color(0.92f, 0.93f, 0.95f, 1f);
    private const string HEX_SO_THU_TU = "#B8C0C8";

    private class NoiDungHelp
    {
        public string tieuDe;
        public string[] cacBuoc;
        public bool laGame;
    }

    private static NoiDungHelp LayNoiDung(string tenScene)
    {
        string t = tenScene.ToLowerInvariant();

        if (t.Contains("game"))
        {
            return new NoiDungHelp
            {
                laGame = true,
                tieuDe = "IN-GAME GUIDE",
                cacBuoc = new[]
                {
                    "<b>STARTING:</b> Press <b>START RACE</b>. In multiplayer, all players must be ready before the race begins.",

                    "<b>CONTROLS:</b> <b>W / Up Arrow</b> accelerate; <b>S / Down Arrow</b> brake or reverse; <b>A / Left Arrow</b> turn left; <b>D / Right Arrow</b> turn right; <b>Space</b> jump.",

                    "<b>CHECKPOINTS:</b> Passing each checkpoint adds <b>+5 seconds</b> to your timer.",

                    "<b>WATER HAZARD:</b> Falling into water removes <b>5 seconds</b> and respawns you back at the start.",

                    "<b>COINS & REWARDS:</b> Win a multiplayer race to earn <b>+50 coins</b>, or win a solo race to earn <b>+10 coins</b>. Use coins in the <b>SHOP</b> to unlock new scooter colors.",

                    "<b>ELIMINATION & SPECTATING:</b> When your timer reaches zero, you are eliminated. In multiplayer, use <b>Q / E</b> or <b>Left / Right Arrow</b> to spectate other racers.",

                    "<b>SHORTCUTS:</b> Press <b>M</b> to toggle background music, <b>F1</b> to open or close this guide, and <b>ESC</b> to open the menu."
                }
            };
        }

        if (t.Contains("shop"))
        {
            return new NoiDungHelp
            {
                tieuDe = "SHOP GUIDE",
                cacBuoc = new[]
                {
                    "Use the <b>LEFT / RIGHT ARROW</b> buttons or <b>A / D</b> keys to browse all 25 scooter paint editions.",

                    "Your owned scooters are displayed on the <b>MINI SHELF</b> on the left side of the showroom.",

                    "Press <b>BUY</b> to unlock the selected scooter with your coins, or press <b>EQUIP</b> to select a scooter you already own.",

                    "<b>EQUIPPED</b> means the scooter is currently active and ready for your next race.",

                    "Win races to earn more coins: <b>+50 coins</b> for a multiplayer win or <b>+10 coins</b> for a solo win."
                }
            };
        }

        if (t.Contains("select"))
        {
            return new NoiDungHelp
            {
                tieuDe = "VEHICLE & MAP SELECTION",
                cacBuoc = new[]
                {
                    "Use the <b>LEFT / RIGHT ARROW</b> buttons or <b>A / D</b> keys to choose among your unlocked scooters.",

                    "Only scooters marked <b>OWNED - READY</b> can be confirmed. Locked scooters can be purchased in the <b>SHOP</b>.",

                    "The <b>ROOM HOST</b> selects the race map using the arrows below the map preview.",

                    "Press <b>CONFIRM</b> (or <b>Enter</b>) to save your selection and proceed to the lobby. Press <b>ESC</b> to return to the menu."
                }
            };
        }

        if (t.Contains("lobby"))
        {
            return new NoiDungHelp
            {
                tieuDe = "LOBBY GUIDE",
                cacBuoc = new[]
                {
                    "Press <b>CREATE ROOM</b> to host a match. The 5-character room code is copied automatically so you can share it with friends.",

                    "Enter a 5-character room code and press <b>JOIN ROOM</b> to join a friend's lobby (up to 4 players per room).",

                    "Use <b>COPY CODE</b> anytime to copy the current room code again.",

                    "The <b>ROOM HOST</b> presses <b>START GAME</b> to launch the race for everyone. Press <b>EXIT</b> or <b>ESC</b> to leave."
                }
            };
        }

        if (t.Contains("menu"))
        {
            return new NoiDungHelp
            {
                tieuDe = "HOW TO PLAY",
                cacBuoc = new[]
                {
                    "Press <b>PLAY</b> to pick your scooter and map, then enter the multiplayer lobby.",

                    "Create a room or join with a friend's 5-character room code to race together.",

                    "Open the <b>SHOP</b> to spend earned coins on 25 custom-painted scooters.",

                    "During a race, checkpoints add <b>+5 seconds</b> while touching water removes <b>5 seconds</b>.",

                    "Cross the finish line first or be the last racer remaining to win (+50 coins multiplayer / +10 coins solo)."
                }
            };
        }

        return new NoiDungHelp
        {
            tieuDe = "GAME GUIDE",
            cacBuoc = new[]
            {
                "Use the on-screen buttons or keyboard controls shown in this scene.",
                "Open the SHOP to unlock and equip custom scooter colors.",
                "Press <b>F1</b> anytime to open or close this guide."
            }
        };
    }

    private static bool daDangKy = false;
    private static Sprite spriteTron;
    private static Sprite spriteBo;

    private GameObject panel;
    private bool laGame = false;

    private CursorLockMode lockCu;
    private bool hienCu;
    private bool daLuuChuot = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiTao()
    {
        if (!daDangKy)
        {
            daDangKy = true;
            SceneManager.sceneLoaded += (scene, mode) =>
                TaoChoScene(scene);
        }

        TaoChoScene(SceneManager.GetActiveScene());
    }

    private static void TaoChoScene(Scene scene)
    {
        NoiDungHelp nd = LayNoiDung(scene.name);
        if (nd == null) return;

        foreach (HelpSystem h in FindObjectsByType<HelpSystem>(
            FindObjectsSortMode.None))
        {
            if (h != null && h.gameObject.scene == scene)
                return;
        }

        GameObject go = new GameObject("HelpSystem");
        SceneManager.MoveGameObjectToScene(go, scene);

        go.AddComponent<HelpSystem>().XayDung(nd);
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F1))
            Chuyen();
#endif
    }

    private void OnDisable()
    {
        KhoiPhucChuot();
    }

    private void Chuyen()
    {
        if (panel == null) return;

        DatHienThi(!panel.activeSelf);
    }

    private void Dong()
    {
        DatHienThi(false);
    }

    private void DatHienThi(bool bat)
    {
        if (panel == null) return;

        panel.SetActive(bat);

        if (!laGame) return;

        if (bat)
        {
            if (!daLuuChuot)
            {
                lockCu = Cursor.lockState;
                hienCu = Cursor.visible;
                daLuuChuot = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            KhoiPhucChuot();
        }
    }

    private void KhoiPhucChuot()
    {
        if (!daLuuChuot) return;

        daLuuChuot = false;
        Cursor.lockState = lockCu;
        Cursor.visible = hienCu;
    }

    private void XayDung(NoiDungHelp nd)
    {
        laGame = nd.laGame;
        DamBaoEventSystem();

        if (spriteTron == null)
            spriteTron = TaoSprite(64, 32f);

        if (spriteBo == null)
            spriteBo = TaoSprite(64, 20f);

        GameObject canvasObj = new GameObject(
            "HelpCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // NÚT HELP (?) - Tông màu xám
        Image nutImg = TaoAnh(
            "NutHelp",
            canvasObj.transform,
            MAU_NUT_XAM,
            spriteTron
        );

        nutImg.raycastTarget = true;

        RectTransform nr = nutImg.rectTransform;
        nr.anchorMin = nr.anchorMax = Vector2.zero;
        nr.pivot = Vector2.zero;

        nr.anchoredPosition = new Vector2(
            HELP_LEFT,
            nd.laGame ? HELP_GAME_BOTTOM : HELP_BOTTOM
        );

        nr.sizeDelta = new Vector2(HELP_SIZE, HELP_SIZE);

        Button nut = nutImg.gameObject.AddComponent<Button>();
        nut.targetGraphic = nutImg;
        nut.onClick.AddListener(Chuyen);

        nutImg.gameObject.AddComponent<ButtonFx>();

        TMP_Text dauHoi = TaoChu(
            "Label",
            nutImg.transform,
            34f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.Center
        );

        dauHoi.text = "?";
        Giang(dauHoi.rectTransform, 0f, 0f, 0f, 0f);

        // BẢNG HƯỚNG DẪN
        panel = new GameObject(
            "BangHuongDan",
            typeof(RectTransform)
        );

        panel.transform.SetParent(canvasObj.transform, false);
        Giang((RectTransform)panel.transform, 0f, 0f, 0f, 0f);

        Image nen = TaoAnh(
            "NenMo",
            panel.transform,
            new Color(0f, 0f, 0f, 0.72f),
            null
        );

        nen.raycastTarget = true;
        Giang(nen.rectTransform, 0f, 0f, 0f, 0f);

        Button nenBtn = nen.gameObject.AddComponent<Button>();
        nenBtn.targetGraphic = nen;
        nenBtn.transition = Selectable.Transition.None;
        nenBtn.onClick.AddListener(Dong);

        Image khung = TaoAnh(
            "Khung",
            panel.transform,
            MAU_NEN_KHUNG,
            spriteBo
        );

        khung.raycastTarget = true;

        RectTransform kr = khung.rectTransform;
        kr.anchorMin = kr.anchorMax = new Vector2(0.5f, 0.5f);
        kr.pivot = new Vector2(0.5f, 0.5f);
        kr.anchoredPosition = Vector2.zero;
        kr.sizeDelta = new Vector2(
            1040f,
            nd.laGame ? 760f : 640f
        );

        Image vien = TaoAnh(
            "Vien",
            khung.transform,
            MAU_CHU_DAO,
            spriteBo
        );

        vien.transform.SetAsFirstSibling();
        Giang(vien.rectTransform, -3f, -3f, -3f, -3f);

        TMP_Text tieuDe = TaoChu(
            "TieuDe",
            khung.transform,
            46f,
            MAU_CHU_DAO,
            TextAlignmentOptions.Center
        );

        tieuDe.text = nd.tieuDe;

        RectTransform tr = tieuDe.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -28f);
        tr.sizeDelta = new Vector2(-80f, 70f);

        tieuDe.enableAutoSizing = true;
        tieuDe.fontSizeMin = 24f;
        tieuDe.fontSizeMax = 46f;
        tieuDe.textWrappingMode = TextWrappingModes.NoWrap;

        System.Text.StringBuilder sb =
            new System.Text.StringBuilder();

        for (int i = 0; i < nd.cacBuoc.Length; i++)
        {
            sb.Append($"<b><color={HEX_SO_THU_TU}>")
                .Append(i + 1)
                .Append(".</color></b>  ")
                .Append(nd.cacBuoc[i]);

            if (i < nd.cacBuoc.Length - 1)
                sb.Append("\n");
        }

        TMP_Text noiDung = TaoChu(
            "NoiDung",
            khung.transform,
            30f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.TopLeft
        );

        noiDung.fontStyle = FontStyles.Normal;
        noiDung.text = sb.ToString();
        noiDung.textWrappingMode = TextWrappingModes.Normal;
        noiDung.enableAutoSizing = true;
        noiDung.fontSizeMin = 18f;
        noiDung.fontSizeMax = 30f;
        noiDung.paragraphSpacing = nd.laGame ? 14f : 22f;

        RectTransform nrr = noiDung.rectTransform;
        nrr.anchorMin = Vector2.zero;
        nrr.anchorMax = Vector2.one;
        nrr.offsetMin = new Vector2(60f, 120f);
        nrr.offsetMax = new Vector2(-60f, -115f);

        // NÚT CLOSE - Tông màu xám
        Image dongImg = TaoAnh(
            "NutDong",
            khung.transform,
            MAU_NUT_XAM,
            spriteBo
        );

        dongImg.raycastTarget = true;

        RectTransform dr = dongImg.rectTransform;
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0f);
        dr.pivot = new Vector2(0.5f, 0f);
        dr.anchoredPosition = new Vector2(0f, 30f);
        dr.sizeDelta = new Vector2(260f, 70f);

        Button dong = dongImg.gameObject.AddComponent<Button>();
        dong.targetGraphic = dongImg;
        dong.onClick.AddListener(Dong);

        dongImg.gameObject.AddComponent<ButtonFx>();

        TMP_Text dongChu = TaoChu(
            "Label",
            dongImg.transform,
            34f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.Center
        );

        dongChu.text = "CLOSE";
        Giang(dongChu.rectTransform, 0f, 0f, 0f, 0f);

        panel.SetActive(false);

        if (laGame)
            TaoNutXemThemTutorial();
    }

    private void TaoNutXemThemTutorial()
    {
        Transform tutorial = null;

        foreach (Transform t in FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            if (t != null && t.name == "TutorialPanel")
            {
                tutorial = t;
                break;
            }
        }

        if (tutorial == null ||
            tutorial.Find("NutXemThemHelp") != null)
            return;

        Image nen = TaoAnh(
            "NutXemThemHelp",
            tutorial,
            MAU_NUT_XAM,
            spriteBo
        );

        nen.raycastTarget = true;

        RectTransform r = nen.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);

        float leftEdge = 110f;
        TMP_Text content = null;

        foreach (TMP_Text txt in tutorial.GetComponentsInChildren<TMP_Text>(true))
        {
            if (txt != null &&
                txt.gameObject.name.Equals(
                    "ContentText",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                content = txt;
                break;
            }
        }

        RectTransform tutorialRect = tutorial as RectTransform;

        if (content != null && tutorialRect != null)
        {
            RectTransform contentRect = content.rectTransform;
            Vector3[] corners = new Vector3[4];

            contentRect.GetWorldCorners(corners);

            Canvas parentCanvas = tutorial.GetComponentInParent<Canvas>();

            Camera uiCamera =
                parentCanvas != null &&
                parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? parentCanvas.worldCamera
                    : null;

            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    corners[0]
                );

            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tutorialRect,
                screenPoint,
                uiCamera,
                out localPoint))
            {
                leftEdge = localPoint.x - tutorialRect.rect.xMin;
            }
        }

        r.anchoredPosition = new Vector2(
            Mathf.Max(24f, leftEdge),
            72f
        );

        r.sizeDelta = new Vector2(210f, 58f);

        Button btn = nen.gameObject.AddComponent<Button>();
        btn.targetGraphic = nen;
        btn.onClick.AddListener(MoBangHuongDan);

        nen.gameObject.AddComponent<ButtonFx>();

        TMP_Text chu = TaoChu(
            "Label",
            nen.transform,
            23f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.Center
        );

        chu.text = "MORE INFO";
        Giang(chu.rectTransform, 0f, 0f, 0f, 0f);

        nen.transform.SetAsLastSibling();
    }

    private void MoBangHuongDan()
    {
        DatHienThi(true);
    }

    private static void DamBaoEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

#if ENABLE_LEGACY_INPUT_MANAGER
        GameObject es = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );

        DontDestroyOnLoad(es);
#endif
    }

    private static void Giang(
        RectTransform r,
        float trai,
        float duoi,
        float phai,
        float tren)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(trai, duoi);
        r.offsetMax = new Vector2(-phai, -tren);
    }

    private static Image TaoAnh(
        string ten,
        Transform cha,
        Color mau,
        Sprite sprite)
    {
        GameObject o = new GameObject(
            ten,
            typeof(RectTransform),
            typeof(Image)
        );

        o.transform.SetParent(cha, false);

        Image img = o.GetComponent<Image>();
        img.color = mau;
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }

        return img;
    }

    private static TMP_Text TaoChu(
        string ten,
        Transform cha,
        float coChu,
        Color mau,
        TextAlignmentOptions canLe)
    {
        GameObject o = new GameObject(
            ten,
            typeof(RectTransform)
        );

        o.transform.SetParent(cha, false);

        TextMeshProUGUI tmp =
            o.AddComponent<TextMeshProUGUI>();

        tmp.fontSize = coChu;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = canLe;
        tmp.color = mau;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static Sprite TaoSprite(
        int kichThuoc,
        float banKinh)
    {
        Texture2D tex = new Texture2D(
            kichThuoc,
            kichThuoc,
            TextureFormat.RGBA32,
            false
        );

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < kichThuoc; y++)
        {
            for (int x = 0; x < kichThuoc; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float cx = Mathf.Clamp(
                    px,
                    banKinh,
                    kichThuoc - banKinh
                );

                float cy = Mathf.Clamp(
                    py,
                    banKinh,
                    kichThuoc - banKinh
                );

                float d = Vector2.Distance(
                    new Vector2(px, py),
                    new Vector2(cx, cy)
                );

                tex.SetPixel(
                    x,
                    y,
                    new Color(
                        1f,
                        1f,
                        1f,
                        Mathf.Clamp01(banKinh - d + 0.5f)
                    )
                );
            }
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, kichThuoc, kichThuoc),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(
                banKinh,
                banKinh,
                banKinh,
                banKinh
            )
        );
    }
}