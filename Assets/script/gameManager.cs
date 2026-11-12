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
    private float thoiGianCoTheQuaLai = 0f;

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

    private TMP_Text thongBaoThoatText;

    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    private void Awake() { if (instance == null) instance = this; }

    private void Start()
    {
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

            if (!PhotonNetwork.IsConnected)
            {
                Time.timeScale = 0f;
            }
        }
        else
        {
            daBatDauDua = true;
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

    // Tự động kích hoạt ngay khi có người chơi thoát khỏi phòng
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        string tenNguoiThoat = string.IsNullOrEmpty(otherPlayer.NickName)
            ? $"Người chơi #{otherPlayer.ActorNumber}"
            : otherPlayer.NickName;

        int conLai = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;

        StopAllCoroutines();
        StartCoroutine(HienThongBaoNguoiThoat($"{tenNguoiThoat} đã thoát khỏi phòng! (Còn {conLai} người)", conLai));
    }

    private IEnumerator HienThongBaoNguoiThoat(string noiDungThoat, int conLai)
    {
        // Lấy đúng Canvas đang chứa đồng hồ đếm giờ (chắc chắn đang bật trên màn hình)
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
        }

        yield return new WaitForSecondsRealtime(4f);

        if (thongBaoThoatText != null)
        {
            thongBaoThoatText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Cho phép bấm ESC bất cứ lúc nào (kể cả khi đang mở Bảng Hướng Dẫn)
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
        if (!PhotonNetwork.IsConnected) Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanelObject != null) pausePanelObject.SetActive(false);

        // Nếu người chơi chưa bấm nút Bắt Đầu Đua thì mở lại bảng hướng dẫn
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
        else SceneManager.LoadScene("LobbyScene");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
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
            if (timeGameObject != null) timeGameObject.SetActive(false);
            if (winGameObject != null) winGameObject.SetActive(true);
        }
        else
        {
            KetThucGame();
        }
    }
}