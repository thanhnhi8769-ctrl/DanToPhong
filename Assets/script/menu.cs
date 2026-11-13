using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Nút bật/tắt nhạc (không bắt buộc)")]
    public TMP_Text nhanNutNhac;       // chữ trên nút: "NHẠC: BẬT" / "NHẠC: TẮT"
    public GameObject iconNhacBat;     // icon loa bật (nếu có)
    public GameObject iconNhacTat;     // icon loa gạch chéo (nếu có)

    private const string KEY_NHAC = "NhacBat";
    private bool nhacDangBat = true;

    void Start()
    {
        // Nhớ lựa chọn của người chơi từ lần trước
        nhacDangBat = PlayerPrefs.GetInt(KEY_NHAC, 1) == 1;
        ApDungNhac();
    }

    // Gắn vào nút bật/tắt nhạc
    public void BatTatNhac()
    {
        nhacDangBat = !nhacDangBat;
        PlayerPrefs.SetInt(KEY_NHAC, nhacDangBat ? 1 : 0);
        PlayerPrefs.Save();
        ApDungNhac();
    }

    private void ApDungNhac()
    {
        // Âm lượng tổng: giữ nguyên khi chuyển scene nên vào game vẫn tắt/mở theo lựa chọn
        AudioListener.volume = nhacDangBat ? 1f : 0f;

        if (nhanNutNhac != null) nhanNutNhac.text = nhacDangBat ? "NHẠC: BẬT" : "NHẠC: TẮT";
        if (iconNhacBat != null) iconNhacBat.SetActive(nhacDangBat);
        if (iconNhacTat != null) iconNhacTat.SetActive(!nhacDangBat);
    }

    // Hàm gọi khi bấm nút PLAY -> sang màn hình chọn Xe, Mũ, Nhân vật & Map
    public void ChoiGame()
    {
        SceneManager.LoadScene("SelectScene");
    }

    // Hàm gọi khi bấm nút SHOP -> sang cửa hàng mua đồ
    public void MoShop()
    {
        SceneManager.LoadScene("ShopScene");
    }

    // Hàm gọi khi bấm nút QUIT
    public void ThoatGame()
    {
        Debug.Log("Đã thoát game!");
        Application.Quit();
    }
}