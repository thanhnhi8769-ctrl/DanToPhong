using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public float thoiGianChoPhepVeDich = 30f;
    public float thoiGianHoiKhiQuaCheckPoint = 5f;
    public float thoiGianTruKhiChamNuoc = 5f; // Số giây bị trừ mỗi lần rơi xuống Sea_Plane
    private float thoiGianCoTheQuaLai = 0f;
    private float thoiGianCoTheBiTruNuoc = 0f;

    [Header("Hiệu ứng đồng xu CheckPoint")]
    public float tocDoXoayDongXu = 120f; // Tốc độ xoay tròn tại chỗ (độ/giây)
    private GameObject[] danhSachDongXu;

    public bool ketThucGame = false;
    public bool winGame = false;
    private bool isPaused = false;

    public bool daBatDauDua = false;

    private int soNguoiDaSanSang = 0;
    private bool banThanDaBam = false;

    [Header("Giao diện UI")]
    public GameObject timeGameObject;
    public GameObject gameOverObject;
    public GameObject winGameObject;
    public GameObject tutorialPanelObject;
    public GameObject pausePanelObject;
    public TMP_Text readyStatusText;

    [Header("Âm thanh & Nhạc nền")]
    public AudioClip nhacNenDuaXe;
    public AudioClip amThanhCheckPoint;
    public AudioClip amThanhWin;
    public AudioClip amThanhGameOver;
    [Range(0f, 1f)] public float amLuongNhacNen = 0.4f;

    private AudioSource nhacNenSource;
    private AudioSource hieuUngSource;

    private TMP_Text thongBaoThoatText;
    private TMP_Text thongBaoSuKienText;
    private Coroutine coroutineSuKien;

    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null) instance = this;

        nhacNenSource = gameObject.AddComponent<AudioSource>();
        nhacNenSource.loop = true;
        nhacNenSource.playOnAwake = false;
        nhacNenSource.volume = amLuongNhacNen;

        hieuUngSource = gameObject.AddComponent<AudioSource>();
        hieuUngSource.playOnAwake = false;
    }

    private void Start()
    {
        // Tự động gom tất cả đồng xu mang Tag "CheckPoint" trên bản đồ để xoay tại chỗ
        danhSachDongXu = GameObject.FindGameObjectsWithTag("CheckPoint");

        if (timeGameObject != null) timeGameObject.SetActive(true);
        if (gameOverObject != null) gameOverObject.SetActive(false);
        if (winGameObject != null) winGameObject.SetActive(false);
        if (pausePanelObject != null) pausePanelObject.SetActive(false);

        if (readyStatusText == null)
        {
            GameObject objText = GameObject.Find("ReadyStatusText");
            if (objText != null) readyStatusText = objText.GetComponent<TMP_Text>();
        }
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);

        HienThiThoiGianGame();

        if (tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(true);
            daBatDauDua = false;
            BatNhacNen();

            if (!PhotonNetwork.IsConnected)
            {
                Time.timeScale = 0f;
            }
        }
        else
        {
            daBatDauDua = true;
            BatNhacNen();
        }
    }

    private void BatNhacNen()
    {
        if (nhacNenSource != null && nhacNenDuaXe != null && !nhacNenSource.isPlaying)
        {
            nhacNenSource.clip = nhacNenDuaXe;
            nhacNenSource.volume = amLuongNhacNen;
            nhacNenSource.Play();
        }
    }

    public void BatDauGame()
    {
        if (banThanDaBam) return;
        banThanDaBam = true;

        if (tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(false);
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            photonView.RPC("BaoDanhSanSang", RpcTarget.AllBuffered);
        }
        else
        {
            daBatDauDua = true;
            Time.timeScale = 1f;
            BatNhacNen();
        }
    }

    [PunRPC]
    void BaoDanhSanSang()
    {
        soNguoiDaSanSang++;
        int tongSoNguoi = PhotonNetwork.CurrentRoom.PlayerCount;

        if (soNguoiDaSanSang >= tongSoNguoi)
        {
            daBatDauDua = true;
            Time.timeScale = 1f;

            if (readyStatusText != null)
            {
                readyStatusText.gameObject.SetActive(false);
            }
            HienThiThoiGianGame();
            BatNhacNen();
        }
        else
        {
            if (readyStatusText != null)
            {
                readyStatusText.gameObject.SetActive(true);
                readyStatusText.text = banThanDaBam
                    ? $"Bạn đã sẵn sàng! Đang chờ người chơi khác ({soNguoiDaSanSang}/{tongSoNguoi})..."
                    : $"Đối thủ đã sẵn sàng ({soNguoiDaSanSang}/{tongSoNguoi})! Bấm BẮT ĐẦU ĐUA để chạy!";
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        string tenNguoiThoat = string.IsNullOrEmpty(otherPlayer.NickName)
            ? $"Người chơi #{otherPlayer.ActorNumber}"
            : otherPlayer.NickName;

        int conLai = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;

        StartCoroutine(HienThongBaoNguoiThoat($"{tenNguoiThoat} đã thoát khỏi phòng! (Còn {conLai} người)", conLai));
    }

    private IEnumerator HienThongBaoNguoiThoat(string noiDungThoat, int conLai)
    {
        if (thongBaoThoatText == null)
        {
            Canvas canvasChinh = (timeGameObject != null)
                ? timeGameObject.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();

            if (canvasChinh != null)
            {
                GameObject obj = new GameObject("ThongBaoThoatUI");
                obj.transform.SetParent(canvasChinh.transform, false);
                obj.transform.SetAsLastSibling();

                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -110f);
                rect.sizeDelta = new Vector2(1000f, 80f);

                thongBaoThoatText = obj.AddComponent<TextMeshProUGUI>();
                thongBaoThoatText.fontSize = 36;
                thongBaoThoatText.fontStyle = FontStyles.Bold;
                thongBaoThoatText.alignment = TextAlignmentOptions.Center;
                thongBaoThoatText.color = Color.yellow;
            }
        }

        if (thongBaoThoatText != null)
        {
            thongBaoThoatText.transform.SetAsLastSibling();
            thongBaoThoatText.gameObject.SetActive(true);
            thongBaoThoatText.text = noiDungThoat;
        }

        if (!daBatDauDua && PhotonNetwork.InRoom && soNguoiDaSanSang >= conLai)
        {
            daBatDauDua = true;
            Time.timeScale = 1f;
            if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
            HienThiThoiGianGame();
            BatNhacNen();
        }

        yield return new WaitForSecondsRealtime(4f);

        if (thongBaoThoatText != null)
        {
            thongBaoThoatText.gameObject.SetActive(false);
        }
    }

    private void HienThongBaoSuKien(string noiDung, Color mauChu, float thoiGianHien = 2.2f)
    {
        if (thongBaoSuKienText == null)
        {
            Canvas canvasChinh = (timeGameObject != null)
                ? timeGameObject.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();

            if (canvasChinh != null)
            {
                GameObject obj = new GameObject("ThongBaoSuKienUI");
                obj.transform.SetParent(canvasChinh.transform, false);
                obj.transform.SetAsLastSibling();

                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.72f);
                rect.anchorMax = new Vector2(0.5f, 0.72f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(1100f, 90f);

                thongBaoSuKienText = obj.AddComponent<TextMeshProUGUI>();
                thongBaoSuKienText.fontSize = 42;
                thongBaoSuKienText.fontStyle = FontStyles.Bold;
                thongBaoSuKienText.alignment = TextAlignmentOptions.Center;
                thongBaoSuKienText.outlineWidth = 0.25f;
                thongBaoSuKienText.outlineColor = Color.black;
            }
        }

        if (thongBaoSuKienText != null)
        {
            if (coroutineSuKien != null) StopCoroutine(coroutineSuKien);
            coroutineSuKien = StartCoroutine(ChayHienThongBaoSuKien(noiDung, mauChu, thoiGianHien));
        }
    }

    private IEnumerator ChayHienThongBaoSuKien(string noiDung, Color mauChu, float thoiGianHien)
    {
        thongBaoSuKienText.transform.SetAsLastSibling();
        thongBaoSuKienText.gameObject.SetActive(true);
        thongBaoSuKienText.color = mauChu;
        thongBaoSuKienText.text = noiDung;

        yield return new WaitForSecondsRealtime(thoiGianHien);

        if (thongBaoSuKienText != null)
        {
            thongBaoSuKienText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Xoay tròn tất cả các đồng xu CheckPoint tại chỗ
        if (danhSachDongXu != null)
        {
            for (int i = 0; i < danhSachDongXu.Length; i++)
            {
                if (danhSachDongXu[i] != null && danhSachDongXu[i].activeInHierarchy)
                {
                    danhSachDongXu[i].transform.Rotate(0f, tocDoXoayDongXu * Time.deltaTime, 0f, Space.World);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!ketThucGame && !winGame)
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    if (tutorialPanelObject != null && tutorialPanelObject.activeSelf)
                    {
                        tutorialPanelObject.SetActive(false);
                    }
                    PauseGame();
                }
            }
        }

        bool coTheDemNguoc = PhotonNetwork.IsConnected
            ? (daBatDauDua && !ketThucGame && !winGame)
            : (daBatDauDua && !ketThucGame && !winGame && !isPaused);

        if (coTheDemNguoc)
        {
            thoiGianChoPhepVeDich -= Time.deltaTime;
            if (thoiGianChoPhepVeDich <= 0)
            {
                thoiGianChoPhepVeDich = 0;
                KetThucGame();
            }
            HienThiThoiGianGame();
        }

        if (winGame)
        {
            if (timeGameObject != null) timeGameObject.SetActive(false);
            if (winGameObject != null) winGameObject.SetActive(true);
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pausePanelObject != null) pausePanelObject.SetActive(true);
        if (nhacNenSource != null && nhacNenSource.isPlaying) nhacNenSource.Pause();
        if (!PhotonNetwork.IsConnected) Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanelObject != null) pausePanelObject.SetActive(false);
        if (nhacNenSource != null && !nhacNenSource.isPlaying) nhacNenSource.UnPause();

        if (!daBatDauDua && !banThanDaBam && tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(true);
        }

        if (PhotonNetwork.IsConnected || daBatDauDua)
        {
            Time.timeScale = 1f;
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else SceneManager.LoadScene("menu");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("menu");
    }

    public void HienThiThoiGianGame()
    {
        if (timeGameObject != null)
        {
            TextMeshProUGUI timeText = timeGameObject.GetComponent<TextMeshProUGUI>();
            if (timeText != null)
            {
                timeText.SetText(Mathf.FloorToInt(thoiGianChoPhepVeDich).ToString());
            }
        }
    }

    public void KetThucGame()
    {
        if (ketThucGame || winGame) return;
        ketThucGame = true;

        if (nhacNenSource != null) nhacNenSource.Stop();
        if (hieuUngSource != null && amThanhGameOver != null)
        {
            hieuUngSource.PlayOneShot(amThanhGameOver);
        }

        if (timeGameObject != null) timeGameObject.SetActive(false);
        if (gameOverObject != null) gameOverObject.SetActive(true);
    }

    public void QuaCheckPoint()
    {
        if (!ketThucGame && !winGame)
        {
            if (Time.time < thoiGianCoTheQuaLai) return;
            thoiGianCoTheQuaLai = Time.time + 0.5f;
            thoiGianChoPhepVeDich += thoiGianHoiKhiQuaCheckPoint;
            HienThiThoiGianGame();

            if (hieuUngSource != null && amThanhCheckPoint != null)
            {
                hieuUngSource.PlayOneShot(amThanhCheckPoint);
            }

            HienThongBaoSuKien($"+{Mathf.FloorToInt(thoiGianHoiKhiQuaCheckPoint)} GIÂY! (ĐÃ QUA CHECKPOINT)", Color.white, 2f);
        }
    }

    // Trừ thời gian khi xe nhảy/rơi xuống mặt nước Sea_Plane
    public void ChamNuocTruThoiGian()
    {
        if (!ketThucGame && !winGame && daBatDauDua)
        {
            if (Time.time < thoiGianCoTheBiTruNuoc) return;
            thoiGianCoTheBiTruNuoc = Time.time + 1.5f; // Giãn cách 1.5s để không bị trừ dồn dập

            thoiGianChoPhepVeDich -= thoiGianTruKhiChamNuoc;
            if (thoiGianChoPhepVeDich <= 0f)
            {
                thoiGianChoPhepVeDich = 0f;
                HienThiThoiGianGame();
                HienThongBaoSuKien($"-{Mathf.FloorToInt(thoiGianTruKhiChamNuoc)} GIÂY! (RƠI XUỐNG NƯỚC)", Color.white, 2.5f);
                KetThucGame();
                return;
            }

            HienThiThoiGianGame();
            HienThongBaoSuKien($"-{Mathf.FloorToInt(thoiGianTruKhiChamNuoc)} GIÂY! (RƠI XUỐNG NƯỚC)", Color.white, 2f);
        }
    }

    public void QuaWinPoint()
    {
        if (ketThucGame || winGame) return;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("ThongBaoCoNguoiThang", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            winGame = true;
            if (nhacNenSource != null) nhacNenSource.Stop();
            if (hieuUngSource != null && amThanhWin != null) hieuUngSource.PlayOneShot(amThanhWin);
            HienThongBaoSuKien("CHÚC MỪNG! BẠN ĐÃ CHẠM CỔNG CHECKWIN!", Color.white, 4f);
            if (winGameObject != null) winGameObject.SetActive(true);
        }
    }

    [PunRPC]
    void ThongBaoCoNguoiThang(int actorNumberNguoiThang)
    {
        if (ketThucGame || winGame) return;

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumberNguoiThang)
        {
            winGame = true;
            if (nhacNenSource != null) nhacNenSource.Stop();
            if (hieuUngSource != null && amThanhWin != null) hieuUngSource.PlayOneShot(amThanhWin);
            HienThongBaoSuKien("CHÚC MỪNG! BẠN ĐÃ VỀ ĐÍCH ĐẦU TIÊN!", Color.white, 4f);
            if (timeGameObject != null) timeGameObject.SetActive(false);
            if (winGameObject != null) winGameObject.SetActive(true);
        }
        else
        {
            HienThongBaoSuKien($"NGƯỜI CHƠI #{actorNumberNguoiThang} ĐÃ CHẠM CỔNG CHECKWIN TRƯỚC!", Color.white, 4f);
            KetThucGame();
        }
    }
}