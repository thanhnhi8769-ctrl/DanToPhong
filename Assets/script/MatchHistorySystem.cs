using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

public class MatchHistorySystem : MonoBehaviour
{
    [Serializable]
    public class MatchRecord
    {
        public string thoiGian;
        public string tenMap;
        public string tenXe;
        public string cheDo;     // "SOLO" hoặc "MULTIPLAYER"
        public string ketQua;    // "WIN" hoặc "ELIMINATED"
        public int xuNhanDuoc;
    }

    [Serializable]
    private class MatchHistoryData
    {
        public int tongTran;
        public int soTranThang;
        public List<MatchRecord> danhSach = new List<MatchRecord>();
    }

    private const string SAVE_KEY = "DanToPhong_MatchHistory_v1";
    private const int MAX_RECORDS = 20;

    private static bool daDangKy = false;
    private static bool daLuuTranHienTai = false;
    private GameObject panelLichSu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiTao()
    {
        if (!daDangKy)
        {
            daDangKy = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        daLuuTranHienTai = false;

        if (scene.name.ToLowerInvariant().Contains("menu"))
        {
            GameObject go = new GameObject("MatchHistorySystem");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<MatchHistorySystem>().TaoNutVaBangMenu();
        }
    }

    /// <summary>
    /// Gọi hàm này ở chỗ kết thúc trận trong gameManager.cs (khi Win hoặc Hết giờ)
    /// </summary>
    public static void LuuKetQuaTran(bool thang, int xuThuong)
    {
        if (daLuuTranHienTai) return;
        daLuuTranHienTai = true;

        MatchHistoryData data = DocDuLieu();
        data.tongTran++;
        if (thang) data.soTranThang++;

        bool laMulti = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1;
        int xeIdx = VehicleCatalog.GetSelectedIndex();

        MatchRecord rec = new MatchRecord
        {
            thoiGian = DateTime.Now.ToString("dd/MM HH:mm"),
            tenMap = SceneManager.GetActiveScene().name,
            tenXe = VehicleCatalog.GetName(xeIdx),
            cheDo = laMulti ? "MULTI" : "SOLO",
            ketQua = thang ? "WIN" : "LOSE",
            xuNhanDuoc = xuThuong
        };

        data.danhSach.Insert(0, rec);
        if (data.danhSach.Count > MAX_RECORDS)
            data.danhSach.RemoveRange(MAX_RECORDS, data.danhSach.Count - MAX_RECORDS);

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static MatchHistoryData DocDuLieu()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
            return new MatchHistoryData();

        try
        {
            MatchHistoryData d = JsonUtility.FromJson<MatchHistoryData>(json);
            return d ?? new MatchHistoryData();
        }
        catch
        {
            return new MatchHistoryData();
        }
    }

    private void TaoNutVaBangMenu()
    {
        GameObject canvasObj = new GameObject("HistoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 490;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Nút HISTORY ở góc phải dưới Menu (tông xám đồng bộ HelpSystem)
        GameObject btnObj = new GameObject("NutHistory", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvasObj.transform, false);

        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(1f, 0f);
        br.anchoredPosition = new Vector2(-28f, 28f);
        br.sizeDelta = new Vector2(180f, 54f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.28f, 0.31f, 0.36f, 0.96f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(MoBangLichSu);

        TMP_Text btnTxt = TaoChu("Label", btnObj.transform, 24f, new Color(0.92f, 0.93f, 0.95f), TextAlignmentOptions.Center);
        btnTxt.text = "HISTORY";
        GiangDay(btnTxt.rectTransform);

        // Panel Lịch Sử
        panelLichSu = new GameObject("BangLichSu", typeof(RectTransform), typeof(Image));
        panelLichSu.transform.SetParent(canvasObj.transform, false);
        GiangDay((RectTransform)panelLichSu.transform);
        panelLichSu.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject khung = new GameObject("Khung", typeof(RectTransform), typeof(Image));
        khung.transform.SetParent(panelLichSu.transform, false);
        RectTransform kr = khung.GetComponent<RectTransform>();
        kr.anchorMin = kr.anchorMax = new Vector2(0.5f, 0.5f);
        kr.sizeDelta = new Vector2(1060f, 680f);
        khung.GetComponent<Image>().color = new Color(0.11f, 0.12f, 0.14f, 0.98f);

        Outline vien = khung.AddComponent<Outline>();
        vien.effectColor = new Color(0.75f, 0.79f, 0.84f, 1f);
        vien.effectDistance = new Vector2(3f, -3f);

        TMP_Text tieuDe = TaoChu("TieuDe", khung.transform, 42f, new Color(0.75f, 0.79f, 0.84f), TextAlignmentOptions.Center);
        tieuDe.text = "MATCH HISTORY";
        RectTransform tr = tieuDe.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -24f);
        tr.sizeDelta = new Vector2(-60f, 60f);

        TMP_Text noiDung = TaoChu("NoiDung", khung.transform, 24f, new Color(0.92f, 0.93f, 0.95f), TextAlignmentOptions.TopLeft);
        noiDung.fontStyle = FontStyles.Normal;
        RectTransform nr = noiDung.rectTransform;
        nr.anchorMin = Vector2.zero;
        nr.anchorMax = Vector2.one;
        nr.offsetMin = new Vector2(50f, 115f);
        nr.offsetMax = new Vector2(-50f, -100f);

        // Nút Close
        GameObject closeObj = new GameObject("NutClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(khung.transform, false);
        RectTransform cr = closeObj.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(0.5f, 0f);
        cr.anchoredPosition = new Vector2(0f, 26f);
        cr.sizeDelta = new Vector2(240f, 64f);
        closeObj.GetComponent<Image>().color = new Color(0.28f, 0.31f, 0.36f, 0.96f);
        closeObj.GetComponent<Button>().onClick.AddListener(() => panelLichSu.SetActive(false));

        TMP_Text closeTxt = TaoChu("Label", closeObj.transform, 30f, Color.white, TextAlignmentOptions.Center);
        closeTxt.text = "CLOSE";
        GiangDay(closeTxt.rectTransform);

        panelLichSu.SetActive(false);
    }

    private void MoBangLichSu()
    {
        if (panelLichSu == null) return;

        MatchHistoryData data = DocDuLieu();
        TMP_Text noiDung = panelLichSu.transform.Find("Khung/NoiDung")?.GetComponent<TMP_Text>();

        if (noiDung != null)
        {
            float tyLeThang = data.tongTran > 0 ? (data.soTranThang * 100f / data.tongTran) : 0f;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>TOTAL MATCHES:</b> {data.tongTran}   |   <b>WINS:</b> {data.soTranThang}   |   <b>WIN RATE:</b> {tyLeThang:0.#}%\n");

            if (data.danhSach.Count == 0)
            {
                sb.AppendLine("No matches played yet. Finish a race to record your history!");
            }
            else
            {
                int hienThi = Mathf.Min(data.danhSach.Count, 10);
                for (int i = 0; i < hienThi; i++)
                {
                    MatchRecord r = data.danhSach[i];
                    string mauKQ = r.ketQua == "WIN" ? "#7EE787" : "#F85149";
                    sb.AppendLine($"<b>{i + 1}.</b> [{r.thoiGian}]  <b>{r.tenMap}</b>  ·  {r.tenXe}  ({r.cheDo})  —  <b><color={mauKQ}>{r.ketQua}</color></b>  (+{r.xuNhanDuoc} COINS)");
                }
            }

            noiDung.text = sb.ToString();
        }

        panelLichSu.SetActive(true);
    }

    private static TMP_Text TaoChu(string ten, Transform cha, float size, Color mau, TextAlignmentOptions align)
    {
        GameObject o = new GameObject(ten, typeof(RectTransform));
        o.transform.SetParent(cha, false);
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = mau;
        t.alignment = align;
        return t;
    }

    private static void GiangDay(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}