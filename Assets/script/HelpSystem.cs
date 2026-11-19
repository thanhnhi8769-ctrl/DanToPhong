using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HelpSystem : MonoBehaviour
{
    private const float HELP_LEFT = 28f;
    private const float HELP_BOTTOM = 28f;
    private const float HELP_SIZE = 54f;

    // Nút "?" CỐ ĐỊNH ở góc trái dưới của MỌI scene.
    // MIC / CHAT / khung chat của MultiplayerAllInOne nằm bên phải nút này (từ x = 96).

    // Bảng màu tối tương phản cao: nền đậm, chữ trắng rõ nét, số thứ tự vàng
    private static readonly Color MAU_CHU_DAO = new Color(0.95f, 0.97f, 1f, 1f);
    private static readonly Color MAU_VIEN_KHUNG = new Color(0.35f, 0.41f, 0.52f, 1f);
    private static readonly Color MAU_NEN_KHUNG = new Color(0.08f, 0.10f, 0.14f, 0.99f);
    private static readonly Color MAU_NUT_XAM = new Color(0.22f, 0.26f, 0.34f, 1f);
    private static readonly Color MAU_CHU_NOI_DUNG = new Color(1f, 1f, 1f, 1f);
    private const string HEX_SO_THU_TU = "#FFC83D";

    private class NoiDungHelp
    {
        public string tieuDe;
        public string[] cacBuoc;
        public bool laGame;
    }

    // =========================================================================
    // NỘI DUNG THEO SCENE + CHẾ ĐỘ (1 NGƯỜI / NHIỀU NGƯỜI)
    // =========================================================================
    private static NoiDungHelp LayNoiDung(string tenScene, bool nhieuNguoi)
    {
        string t = tenScene.ToLowerInvariant();

        if (t.Contains("game"))
        {
            if (nhieuNguoi)
            {
                return new NoiDungHelp
                {
                    laGame = true,
                    tieuDe = "IN-GAME GUIDE - MULTIPLAYER",
                    cacBuoc = new[]
                    {
                        "<b>STARTING:</b> Press <b>START RACE</b> (or <b>Enter</b>). The race begins once <b>every player</b> in the room is ready.",

                        "<b>CONTROLS:</b> <b>W / Up Arrow</b> accelerate; <b>S / Down Arrow</b> brake or reverse; <b>A / Left Arrow</b> turn left; <b>D / Right Arrow</b> turn right; <b>Space</b> jump.",

                        "<b>CHECKPOINTS:</b> Passing each checkpoint adds <b>+5 seconds</b> to your own timer.",

                        "<b>WATER HAZARD:</b> Falling into water removes <b>5 seconds</b> and respawns you back at the start.",

                        "<b>HOW TO WIN:</b> Be the <b>first to cross the finish line</b>, or be the <b>last racer left</b> in the race. The winner earns <b>+50 coins</b>. Use coins in the <b>SHOP</b> to unlock new scooter colors.",

                        "<b>ELIMINATION & SPECTATING:</b> When your timer reaches zero you are eliminated. Use <b>Q / E</b> or <b>Left / Right Arrow</b> to spectate other racers until the race ends.",

                        "<b>CHAT & VOICE:</b> Use the <b>CHAT</b> and <b>MIC</b> buttons at the bottom-left to talk with other racers. Press <b>Enter</b> to start typing.",

                        "<b>SHORTCUTS:</b> <b>M</b> toggles background music, <b>F1</b> opens or closes this guide, <b>ESC</b> opens the menu (the race keeps running for everyone)."
                    }
                };
            }

            return new NoiDungHelp
            {
                laGame = true,
                tieuDe = "IN-GAME GUIDE - SOLO",
                cacBuoc = new[]
                {
                    "<b>STARTING:</b> Press <b>START RACE</b> (or <b>Enter</b>) to begin. Your timer only runs once the race has started.",

                    "<b>CONTROLS:</b> <b>W / Up Arrow</b> accelerate; <b>S / Down Arrow</b> brake or reverse; <b>A / Left Arrow</b> turn left; <b>D / Right Arrow</b> turn right; <b>Space</b> jump.",

                    "<b>CHECKPOINTS:</b> Passing each checkpoint adds <b>+5 seconds</b> to your timer.",

                    "<b>WATER HAZARD:</b> Falling into water removes <b>5 seconds</b> and respawns you back at the start.",

                    "<b>HOW TO WIN:</b> Cross the finish line before your timer reaches zero to earn <b>+10 coins</b>. Use coins in the <b>SHOP</b> to unlock new scooter colors.",

                    "<b>TIME'S UP:</b> If your timer reaches zero, the race ends and you can restart or return to the menu.",

                    "<b>SHORTCUTS:</b> <b>M</b> toggles background music, <b>F1</b> opens or closes this guide, <b>ESC</b> pauses the game."
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
            if (nhieuNguoi)
            {
                return new NoiDungHelp
                {
                    tieuDe = "VEHICLE & MAP SELECTION",
                    cacBuoc = new[]
                    {
                        "Use the <b>LEFT / RIGHT ARROW</b> buttons or <b>A / D</b> keys to choose among your unlocked scooters. Each player picks their own scooter color.",

                        "Only scooters marked <b>OWNED - READY</b> can be confirmed. Locked scooters can be purchased in the <b>SHOP</b>.",

                        "Only the <b>ROOM HOST</b> can change the race map. Everyone else races on the map the host selects.",

                        "Press <b>CONFIRM</b> (or <b>Enter</b>) to save your selection and proceed to the lobby. Press <b>ESC</b> to return to the menu."
                    }
                };
            }

            return new NoiDungHelp
            {
                tieuDe = "VEHICLE & MAP SELECTION",
                cacBuoc = new[]
                {
                    "Use the <b>LEFT / RIGHT ARROW</b> buttons or <b>A / D</b> keys to choose among your unlocked scooters.",

                    "Only scooters marked <b>OWNED - READY</b> can be confirmed. Locked scooters can be purchased in the <b>SHOP</b>.",

                    "Choose the race map with the arrows below the map preview.",

                    "Press <b>CONFIRM</b> (or <b>Enter</b>) to save your selection and proceed to the lobby. Press <b>ESC</b> to return to the menu."
                }
            };
        }

        if (t.Contains("lobby"))
        {
            if (nhieuNguoi)
            {
                return new NoiDungHelp
                {
                    tieuDe = "LOBBY GUIDE - MULTIPLAYER",
                    cacBuoc = new[]
                    {
                        "Share the <b>5-character room code</b> with friends. Use <b>COPY CODE</b> anytime to copy it again (up to 4 players per room).",

                        "Friends enter the code and press <b>JOIN ROOM</b> to join your lobby.",

                        "The <b>ROOM HOST</b> presses <b>START GAME</b> to load the race for everyone.",

                        "In the race, every player must press <b>START RACE</b> before it begins. Press <b>EXIT</b> or <b>ESC</b> to leave the room."
                    }
                };
            }

            return new NoiDungHelp
            {
                tieuDe = "LOBBY GUIDE",
                cacBuoc = new[]
                {
                    "Press <b>CREATE ROOM</b> to host a match. The 5-character room code is copied automatically so you can share it with friends.",

                    "Enter a 5-character room code and press <b>JOIN ROOM</b> to join a friend's lobby (up to 4 players per room).",

                    "Use <b>COPY CODE</b> anytime to copy the current room code again.",

                    "Press <b>START GAME</b> to load the race. If nobody else has joined, you will race solo. Press <b>EXIT</b> or <b>ESC</b> to leave."
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
                    "Press <b>PLAY</b> to pick your scooter and map, then enter the lobby.",

                    "Race <b>solo</b>, or create a room / join with a friend's 5-character room code to race with up to 4 players.",

                    "Open the <b>SHOP</b> to spend earned coins on 25 custom-painted scooters.",

                    "During a race, checkpoints add <b>+5 seconds</b> while touching water removes <b>5 seconds</b>.",

                    "Solo: cross the finish line before time runs out (<b>+10 coins</b>). Multiplayer: cross the finish line first or be the last racer remaining (<b>+50 coins</b>)."
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

    // Trong scene game: lấy đúng chế độ của GameManager. Scene khác: dựa vào phòng Photon hiện tại.
    private static bool LaNhieuNguoi()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null) return gm.CheDoNhieuNguoi;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return false;

        int soNguoi = PhotonNetwork.CurrentRoom.PlayerCount;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SoNguoi", out object v) && v != null)
            soNguoi = Mathf.Max(soNguoi, System.Convert.ToInt32(v));

        return soNguoi > 1;
    }

    private static string DungChuoiNoiDung(string[] cacBuoc)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < cacBuoc.Length; i++)
        {
            sb.Append("<b><color=").Append(HEX_SO_THU_TU).Append(">")
                .Append(i + 1)
                .Append(".</color></b>  ")
                .Append(cacBuoc[i]);

            if (i < cacBuoc.Length - 1)
                sb.Append("\n");
        }

        return sb.ToString();
    }

    private static bool daDangKy = false;
    private static Sprite spriteTron;
    private static Sprite spriteBo;

    private static HelpSystem hienTai;

    private GameObject panel;
    private bool laGame = false;
    private string tenScene;

    private TMP_Text tieuDeText;
    private TMP_Text noiDungText;
    private RectTransform khungRect;
    private RectTransform nutHelpRect;

    private float thoiDiemTao;
    private bool daTaoNutXemThem = false;

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
        NoiDungHelp nd = LayNoiDung(scene.name, false);
        if (nd == null) return;

        foreach (HelpSystem h in FindObjectsByType<HelpSystem>(
            FindObjectsSortMode.None))
        {
            if (h != null && h.gameObject.scene == scene)
                return;
        }

        GameObject go = new GameObject("HelpSystem");
        SceneManager.MoveGameObjectToScene(go, scene);

        go.AddComponent<HelpSystem>().XayDung(scene.name, nd);
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

    private void OnDestroy()
    {
        if (hienTai == this) hienTai = null;
    }

    // GameManager gọi khi bấm ESC: nếu bảng F1 đang mở thì đóng nó và báo đã xử lý xong phím ESC
    public static bool DongNeuDangMo()
    {
        if (hienTai == null || hienTai.panel == null || !hienTai.panel.activeSelf) return false;

        hienTai.Dong();
        return true;
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

        // Nội dung được dựng lại mỗi lần mở để đúng với chế độ 1 người / nhiều người lúc đó
        if (bat) LamMoiNoiDung();

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

    private void LamMoiNoiDung()
    {
        NoiDungHelp nd = LayNoiDung(tenScene, LaNhieuNguoi());
        if (nd == null) return;

        laGame = nd.laGame;

        if (tieuDeText != null) tieuDeText.text = nd.tieuDe;
        if (noiDungText != null)
        {
            noiDungText.text = DungChuoiNoiDung(nd.cacBuoc);
            noiDungText.color = MAU_CHU_NOI_DUNG;
            noiDungText.lineSpacing = 10f;
            noiDungText.paragraphSpacing = nd.laGame ? 16f : 24f;
        }

        if (khungRect != null)
            khungRect.sizeDelta = new Vector2(1080f, nd.laGame ? 780f : 660f);
    }

    private void CanViTriNutHelp()
    {
        if (nutHelpRect == null) return;

        nutHelpRect.anchoredPosition = new Vector2(HELP_LEFT, HELP_BOTTOM);
    }

    private void XayDung(string ten, NoiDungHelp nd)
    {
        hienTai = this;
        tenScene = ten;
        laGame = nd.laGame;
        thoiDiemTao = Time.unscaledTime;
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
        // Cao hơn mọi canvas của scene (kể cả khung chat = 5000) để bảng hướng dẫn / nút "?" không bao giờ bị che
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // NÚT HELP (?)
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
        nr.sizeDelta = new Vector2(HELP_SIZE, HELP_SIZE);
        nutHelpRect = nr;
        CanViTriNutHelp();

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
            new Color(0f, 0f, 0f, 0.78f),
            null
        );

        nen.raycastTarget = true;
        Giang(nen.rectTransform, 0f, 0f, 0f, 0f);

        Button nenBtn = nen.gameObject.AddComponent<Button>();
        nenBtn.targetGraphic = nen;
        nenBtn.transition = Selectable.Transition.None;
        nenBtn.onClick.AddListener(Dong);

        Image vien = TaoAnh(
            "Vien",
            panel.transform,
            MAU_VIEN_KHUNG,
            spriteBo
        );

        RectTransform vr = vien.rectTransform;
        vr.anchorMin = vr.anchorMax = new Vector2(0.5f, 0.5f);
        vr.pivot = new Vector2(0.5f, 0.5f);
        vr.anchoredPosition = Vector2.zero;
        vr.sizeDelta = new Vector2(
            1086f,
            (nd.laGame ? 780f : 660f) + 6f
        );

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
            1080f,
            nd.laGame ? 780f : 660f
        );
        khungRect = kr;

        TMP_Text tieuDe = TaoChu(
            "TieuDe",
            khung.transform,
            44f,
            MAU_CHU_DAO,
            TextAlignmentOptions.Center
        );

        tieuDe.text = nd.tieuDe;
        tieuDeText = tieuDe;

        RectTransform tr = tieuDe.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -24f);
        tr.sizeDelta = new Vector2(-80f, 64f);

        tieuDe.enableAutoSizing = true;
        tieuDe.fontSizeMin = 24f;
        tieuDe.fontSizeMax = 44f;
        tieuDe.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text noiDung = TaoChu(
            "NoiDung",
            khung.transform,
            28f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.TopLeft
        );

        noiDung.fontStyle = FontStyles.Normal;
        noiDung.text = DungChuoiNoiDung(nd.cacBuoc);
        noiDung.textWrappingMode = TextWrappingModes.Normal;
        noiDung.enableAutoSizing = true;
        noiDung.fontSizeMin = 18f;
        noiDung.fontSizeMax = 28f;
        noiDung.lineSpacing = 10f;
        noiDung.paragraphSpacing = nd.laGame ? 16f : 24f;
        noiDungText = noiDung;

        RectTransform nrr = noiDung.rectTransform;
        nrr.anchorMin = Vector2.zero;
        nrr.anchorMax = Vector2.one;
        nrr.offsetMin = new Vector2(64f, 125f);
        nrr.offsetMax = new Vector2(-64f, -105f);

        // NÚT CLOSE
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
        dr.anchoredPosition = new Vector2(0f, 28f);
        dr.sizeDelta = new Vector2(260f, 68f);

        Button dong = dongImg.gameObject.AddComponent<Button>();
        dong.targetGraphic = dongImg;
        dong.onClick.AddListener(Dong);

        dongImg.gameObject.AddComponent<ButtonFx>();

        TMP_Text dongChu = TaoChu(
            "Label",
            dongImg.transform,
            32f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.Center
        );

        dongChu.text = "CLOSE";
        Giang(dongChu.rectTransform, 0f, 0f, 0f, 0f);

        panel.SetActive(false);
    }

    private void TaoNutXemThemTutorial()
    {
        Transform tutorial = null;

        foreach (Transform t in FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            if (t != null && t.name == "TutorialPanel" && t.gameObject.scene == gameObject.scene)
            {
                tutorial = t;
                break;
            }
        }

        if (tutorial == null) return;

        // Tìm nút START RACE (cùng cách chấm điểm như GameManager)
        RectTransform nutStart = null;
        int diemTot = 0;

        foreach (Button b in tutorial.GetComponentsInChildren<Button>(true))
        {
            if (b == null || b.gameObject.name == "NutXemThemHelp") continue;

            int diem = 1;
            string ten = b.gameObject.name.ToLowerInvariant();
            if (ten == "button") diem += 20;
            if (ten.Contains("start") || ten.Contains("begin")) diem += 30;

            foreach (TMP_Text label in b.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label == null) continue;
                string chu = label.text.ToUpperInvariant();
                if (chu.Contains("START") || chu.Contains("BẮT ĐẦU") || chu.Contains("WAITING"))
                    diem += 100;
            }

            if (diem > diemTot)
            {
                diemTot = diem;
                nutStart = b.transform as RectTransform;
            }
        }

        RectTransform cha = (nutStart != null ? nutStart.parent : tutorial) as RectTransform;
        if (cha == null) return;

        Image nen = TaoAnh(
            "NutXemThemHelp",
            cha,
            MAU_NUT_XAM,
            spriteBo
        );

        nen.raycastTarget = true;

        RectTransform r = nen.rectTransform;
        r.sizeDelta = new Vector2(210f, 58f);

        if (nutStart != null)
        {
            // Tính vị trí theo toạ độ thế giới nên đúng với mọi kiểu anchor / pivot của nút START RACE
            Vector3[] goc = new Vector3[4];
            nutStart.GetWorldCorners(goc);

            Vector3 duoiGiua = (goc[0] + goc[3]) * 0.5f;
            Vector3 local = cha.InverseTransformPoint(duoiGiua);

            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(
                local.x - cha.rect.center.x,
                local.y - cha.rect.center.y - 16f
            );
        }
        else
        {
            r.anchorMin = r.anchorMax = new Vector2(1f, 0f);
            r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-60f, 40f);
        }

        Button btn = nen.gameObject.AddComponent<Button>();
        btn.targetGraphic = nen;
        btn.onClick.AddListener(MoBangHuongDan);

        nen.gameObject.AddComponent<ButtonFx>();

        TMP_Text chu2 = TaoChu(
            "Label",
            nen.transform,
            23f,
            MAU_CHU_NOI_DUNG,
            TextAlignmentOptions.Center
        );

        chu2.text = "MORE HELP";
        Giang(chu2.rectTransform, 0f, 0f, 0f, 0f);

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