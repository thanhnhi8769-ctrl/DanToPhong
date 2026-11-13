using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SelectManager : MonoBehaviour
{
    [System.Serializable]
    public class SelectItem
    {
        public string itemName;
        public GameObject model3D;
    }

    [System.Serializable]
    public class MapOption
    {
        public string displayName;
        public string sceneName;
    }

    [Header("Danh sách trang bị (Thứ tự giống hệt ShopScene)")]
    public SelectItem[] scooters;
    public SelectItem[] helmets;
    public SelectItem[] characters;

    [Header("Danh sách Map đua")]
    public MapOption[] maps;

    [Header("Sân khấu trưng bày 3D")]
    public Transform showcaseStage;
    public float rotateSpeed = 40f;

    [Header("Giao diện UI")]
    public TMP_Text itemNameText; // Kéo ItemNameText vào đây
    public TMP_Text warningText;  // Kéo dòng chữ thông báo vào đây
    public Button confirmButton;  // Kéo ConfirmButton vào đây

    private string currentTab = "Scooter";
    private int scooterIndex = 0;
    private int helmetIndex = 0;
    private int characterIndex = 0;
    private int mapIndex = 0;

    // 3 trục xoay riêng cho từng hộp kính trưng bày
    private Transform trucXoayXe;
    private Transform trucXoayMu;
    private Transform trucXoayNhanVat;

    private void Start()
    {
        scooterIndex = PlayerInventory.GetEquippedIndex("Scooter");
        helmetIndex = PlayerInventory.GetEquippedIndex("Helmet");
        characterIndex = PlayerInventory.GetEquippedIndex("Character");

        string savedMap = PlayerInventory.GetSelectedMap();
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].sceneName == savedMap)
            {
                mapIndex = i;
                break;
            }
        }

        CanChinhCameraVaSanKhau();
        ChuanHoaTatCaModel();
        SelectTab("Scooter");
    }

    private void CanChinhCameraVaSanKhau()
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 0.9f, 0f);
            Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        if (showcaseStage == null)
        {
            GameObject stageObj = new GameObject("AutoSelectStage");
            showcaseStage = stageObj.transform;
        }

        showcaseStage.position = Vector3.zero;
        showcaseStage.rotation = Quaternion.identity;
        showcaseStage.localScale = Vector3.one;

        // Ẩn toàn bộ object cũ bên trong sân khấu nếu không nằm trong danh sách khai báo
        for (int i = showcaseStage.childCount - 1; i >= 0; i--)
        {
            Transform child = showcaseStage.GetChild(i);
            if (!LaModelTrongDanhSach(child.gameObject))
            {
                child.gameObject.SetActive(false);
            }
        }

        // Tạo 3 lồng kính trong suốt màu xanh rêu xếp ngang hàng trước mặt Camera
        trucXoayXe = TaoHopKinhXanhReu("HopKinh_Xe", new Vector3(-1.55f, 1.05f, 4.3f), new Vector3(1.25f, 1.35f, 1.25f));
        trucXoayMu = TaoHopKinhXanhReu("HopKinh_Mu", new Vector3(0f, 1.05f, 4.3f), new Vector3(1.15f, 1.35f, 1.15f));
        trucXoayNhanVat = TaoHopKinhXanhReu("HopKinh_NhanVat", new Vector3(1.55f, 1.05f, 4.3f), new Vector3(1.25f, 1.35f, 1.25f));
    }

    private bool LaModelTrongDanhSach(GameObject obj)
    {
        if (scooters != null) foreach (var s in scooters) if (s.model3D == obj) return true;
        if (helmets != null) foreach (var h in helmets) if (h.model3D == obj) return true;
        if (characters != null) foreach (var c in characters) if (c.model3D == obj) return true;
        return false;
    }

    private Transform TaoHopKinhXanhReu(string tenHop, Vector3 viTri, Vector3 kichThuocHop)
    {
        GameObject hopCha = new GameObject(tenHop);
        hopCha.transform.SetParent(showcaseStage, false);
        hopCha.transform.position = viTri;

        // 1. Tạo khối hộp kính trong suốt màu xanh rêu
        GameObject kinh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kinh.name = "KinhTrongSuot";
        Destroy(kinh.GetComponent<Collider>());
        kinh.transform.SetParent(hopCha.transform, false);
        kinh.transform.localPosition = Vector3.zero;
        kinh.transform.localScale = kichThuocHop;

        Material matKinh = TaoMaterialKinhXanhReu(new Color(0.28f, 0.45f, 0.32f, 0.24f));
        kinh.GetComponent<MeshRenderer>().material = matKinh;

        // 2. Tạo đế hộp màu xanh rêu đậm ở đáy
        GameObject deHop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deHop.name = "DeHopXanhReu";
        Destroy(deHop.GetComponent<Collider>());
        deHop.transform.SetParent(hopCha.transform, false);
        deHop.transform.localPosition = new Vector3(0f, -kichThuocHop.y * 0.5f - 0.03f, 0f);
        deHop.transform.localScale = new Vector3(kichThuocHop.x + 0.06f, 0.06f, kichThuocHop.z + 0.06f);

        Shader unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Standard");
        Material matDe = new Material(unlit);
        matDe.color = new Color(0.18f, 0.32f, 0.22f);
        deHop.GetComponent<MeshRenderer>().material = matDe;

        // 3. Vẽ viền khung xanh rêu sáng quanh 12 cạnh hộp
        VeVienHop(hopCha.transform, kichThuocHop, new Color(0.45f, 0.72f, 0.50f, 0.9f));

        // 4. Tạo trục xoay bên trong lòng hộp kính
        GameObject truc = new GameObject("TrucXoayBenTrong");
        truc.transform.SetParent(hopCha.transform, false);
        truc.transform.localPosition = Vector3.zero;
        return truc.transform;
    }

    private Material TaoMaterialKinhXanhReu(Color mauKinh)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            Material mat = new Material(urpLit);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", mauKinh);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }

        Shader std = Shader.Find("Standard");
        Material stdMat = new Material(std);
        stdMat.SetFloat("_Mode", 3f);
        stdMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        stdMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        stdMat.SetInt("_ZWrite", 0);
        stdMat.DisableKeyword("_ALPHATEST_ON");
        stdMat.EnableKeyword("_ALPHABLEND_ON");
        stdMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        stdMat.renderQueue = 3000;
        stdMat.color = mauKinh;
        return stdMat;
    }

    private void VeVienHop(Transform parent, Vector3 size, Color mauVien)
    {
        GameObject lineObj = new GameObject("VienHopKinh");
        lineObj.transform.SetParent(parent, false);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.018f;
        lr.endWidth = 0.018f;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        lr.material = new Material(shader);
        lr.startColor = mauVien;
        lr.endColor = mauVien;

        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;

        Vector3[] pts = new Vector3[]
        {
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, -y, z), new Vector3(-x, -y, z), new Vector3(-x, -y, -z),
            new Vector3(-x,  y, -z), new Vector3(x,  y, -z), new Vector3(x, -y, -z), new Vector3(x,  y, -z),
            new Vector3( x,  y,  z), new Vector3(x, -y,  z), new Vector3(x,  y,  z),
            new Vector3(-x,  y,  z), new Vector3(-x, -y,  z), new Vector3(-x,  y,  z),
            new Vector3(-x,  y, -z)
        };
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
    }

    private void Update()
    {
        // Xoay cả 3 món (Xe, Mũ, Nhân vật) trong 3 hộp kính
        if (trucXoayXe != null) trucXoayXe.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        if (trucXoayMu != null) trucXoayMu.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        if (trucXoayNhanVat != null) trucXoayNhanVat.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void KhoaVatLyVaChuyenDong(GameObject obj)
    {
        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (Animator anim in obj.GetComponentsInChildren<Animator>(true)) anim.enabled = false;
        foreach (MonoBehaviour s in obj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (s != null && s != this) s.enabled = false;
        }
    }

    private void ChuanHoaTatCaModel()
    {
        CanGiuaDanhSach(scooters, trucXoayXe, 1.05f);
        CanGiuaDanhSach(helmets, trucXoayMu, 0.85f);
        CanGiuaDanhSach(characters, trucXoayNhanVat, 1.08f);
    }

    private bool DaDuocGanChoTrucKhac(GameObject obj, Transform trucHienTai)
    {
        return obj.transform.parent != null && obj.transform.parent != trucHienTai &&
               (obj.transform.parent == trucXoayXe || obj.transform.parent == trucXoayMu || obj.transform.parent == trucXoayNhanVat);
    }

    private void CanGiuaDanhSach(SelectItem[] list, Transform trucCha, float targetSize)
    {
        if (list == null || trucCha == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].model3D == null) continue;

            // Nếu là Prefab từ Project hoặc bị gán trùng với món khác -> tạo bản sao độc lập
            if (!list[i].model3D.scene.IsValid() || DaDuocGanChoTrucKhac(list[i].model3D, trucCha))
            {
                list[i].model3D = Instantiate(list[i].model3D);
            }

            KhoaVatLyVaChuyenDong(list[i].model3D);

            list[i].model3D.transform.SetParent(trucCha, false);
            list[i].model3D.SetActive(true);
            list[i].model3D.transform.localPosition = Vector3.zero;
            list[i].model3D.transform.localRotation = Quaternion.identity;
            list[i].model3D.transform.localScale = Vector3.one;

            Renderer[] renderers = list[i].model3D.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = new Bounds(trucCha.position, Vector3.zero);
                bool hasValidBounds = false;

                foreach (Renderer r in renderers)
                {
                    if (r.bounds.size.sqrMagnitude > 0.0001f)
                    {
                        if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                        else bounds.Encapsulate(r.bounds);
                    }
                }

                if (hasValidBounds)
                {
                    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (maxDim > 0.001f)
                    {
                        float scaleFactor = Mathf.Clamp(targetSize / maxDim, 0.05f, 20f);
                        list[i].model3D.transform.localScale = Vector3.one * scaleFactor;
                    }

                    bounds = new Bounds(trucCha.position, Vector3.zero);
                    hasValidBounds = false;
                    foreach (Renderer r in renderers)
                    {
                        if (r.bounds.size.sqrMagnitude > 0.0001f)
                        {
                            if (!hasValidBounds) { bounds = r.bounds; hasValidBounds = true; }
                            else bounds.Encapsulate(r.bounds);
                        }
                    }
                    if (hasValidBounds)
                    {
                        list[i].model3D.transform.position += (trucCha.position - bounds.center);
                    }
                }
            }

            list[i].model3D.SetActive(false);
        }
    }

    public void SelectTab(string tabName)
    {
        currentTab = tabName;
        CapNhatGiaoDien();
    }

    public void NextItem()
    {
        if (currentTab == "Scooter" && scooters.Length > 0) scooterIndex = (scooterIndex + 1) % scooters.Length;
        else if (currentTab == "Helmet" && helmets.Length > 0) helmetIndex = (helmetIndex + 1) % helmets.Length;
        else if (currentTab == "Character" && characters.Length > 0) characterIndex = (characterIndex + 1) % characters.Length;
        else if (currentTab == "Map" && maps.Length > 0) mapIndex = (mapIndex + 1) % maps.Length;

        CapNhatGiaoDien();
    }

    public void PreviousItem()
    {
        if (currentTab == "Scooter" && scooters.Length > 0) scooterIndex = (scooterIndex - 1 + scooters.Length) % scooters.Length;
        else if (currentTab == "Helmet" && helmets.Length > 0) helmetIndex = (helmetIndex - 1 + helmets.Length) % helmets.Length;
        else if (currentTab == "Character" && characters.Length > 0) characterIndex = (characterIndex - 1 + characters.Length) % characters.Length;
        else if (currentTab == "Map" && maps.Length > 0) mapIndex = (mapIndex - 1 + maps.Length) % maps.Length;

        CapNhatGiaoDien();
    }

    private void CapNhatGiaoDien()
    {
        HienThiModelTheoIndex(scooters, scooterIndex);
        HienThiModelTheoIndex(helmets, helmetIndex);
        HienThiModelTheoIndex(characters, characterIndex);

        if (itemNameText != null)
        {
            if (currentTab == "Scooter" && scooters.Length > 0)
                itemNameText.text = FormatTen("XE", "Scooter", scooters[scooterIndex].itemName, scooterIndex);
            else if (currentTab == "Helmet" && helmets.Length > 0)
                itemNameText.text = FormatTen("MŨ", "Helmet", helmets[helmetIndex].itemName, helmetIndex);
            else if (currentTab == "Character" && characters.Length > 0)
                itemNameText.text = FormatTen("NHÂN VẬT", "Character", characters[characterIndex].itemName, characterIndex);
            else if (currentTab == "Map" && maps.Length > 0)
                itemNameText.text = $"MAP: {maps[mapIndex].displayName}";
        }

        bool duDieuKien = PlayerInventory.IsUnlocked("Scooter", scooterIndex)
                       && PlayerInventory.IsUnlocked("Helmet", helmetIndex)
                       && PlayerInventory.IsUnlocked("Character", characterIndex);

        if (confirmButton != null) confirmButton.interactable = duDieuKien;

        if (warningText != null)
        {
            warningText.text = duDieuKien
                ? "<color=#00FF00>SẴN SÀNG RA ĐƯỜNG ĐUA!</color>"
                : "<color=#FF4444>BẠN CHƯA MUA MÓN ĐANG CHỌN (VỀ SHOP ĐỂ MUA)!</color>";
        }
    }

    private void HienThiModelTheoIndex(SelectItem[] list, int index)
    {
        if (list == null) return;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].model3D != null) list[i].model3D.SetActive(false);
        }

        if (index >= 0 && index < list.Length && list[index].model3D != null)
        {
            list[index].model3D.SetActive(true);
        }
    }

    private string FormatTen(string nhan, string category, string tenMon, int index)
    {
        bool owned = PlayerInventory.IsUnlocked(category, index);
        return owned ? $"{nhan}: {tenMon}" : $"{nhan}: {tenMon} <color=#FF5555>(CHƯA MUA)</color>";
    }

    public void ConfirmAndGoToLobby()
    {
        PlayerInventory.SetEquippedIndex("Scooter", scooterIndex);
        PlayerInventory.SetEquippedIndex("Helmet", helmetIndex);
        PlayerInventory.SetEquippedIndex("Character", characterIndex);

        if (maps.Length > 0) PlayerInventory.SetSelectedMap(maps[mapIndex].sceneName);
        SceneManager.LoadScene("LobbyScene");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("menu");
    }
}