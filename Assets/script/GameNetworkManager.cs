using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Unity.Cinemachine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameNetworkManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    [Header("Tên Prefab gốc duy nhất trong thư mục Resources")]
    public string tenPrefabXeGoc = "Scooter";

    [Header("Kích thước & vị trí xe trong game")]
    public float kichThuocXeChuan = 1.6f;
    public Vector3 doLechSpawn = Vector3.zero;
    public float doCaoThaXe = 0.05f;

    [Header("Mạng (tăng để xe người khác chạy êm hơn)")]
    [Tooltip("Số gói gửi lên server mỗi giây (mặc định Photon = 30)")]
    public int tanSoGui = 30;
    [Tooltip("Số lần đồng bộ vị trí xe mỗi giây (mặc định Photon = 10 nên xe người khác bị giật)")]
    public int tanSoDongBo = 30;

    [Header("Chẩn đoán")]
    public bool hienThongTinDebugXe = false;

    private const float CHU_KY_QUET_SKIN = 1.5f;

    private bool daTaoXe = false;
    private static bool daDangKySceneLoaded = false;

    private readonly Dictionary<int, int> xeDaApSkin = new Dictionary<int, int>();
    private float thoiDiemQuetTiepTheo = 0f;

    // Renderer đã được tách material riêng (mỗi xe một bản, tránh dùng chung màu giữa các xe)
    private static readonly HashSet<int> rendererDaTachMat = new HashSet<int>();
    private static readonly RaycastHit[] bufferRay = new RaycastHit[32];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiTaoHeThong()
    {
        if (!daDangKySceneLoaded)
        {
            daDangKySceneLoaded = true;
            SceneManager.sceneLoaded += KhiChuyenScene;
        }
        KiemTraVaTaoManager(SceneManager.GetActiveScene());
    }

    private static void KhiChuyenScene(Scene scene, LoadSceneMode mode)
    {
        rendererDaTachMat.Clear();
        KiemTraVaTaoManager(scene);
    }

    private static void KiemTraVaTaoManager(Scene scene)
    {
        string ten = scene.name.ToLower();

        if (ten.Contains("select")) return;

        if (ten == "game")
        {
            string m = LayMapDaChon();
            if (!string.IsNullOrEmpty(m) && m != "game" && Application.CanStreamedLevelBeLoaded(m))
            {
                // Đang ở trong phòng Online thì dùng hàm load scene của Photon, chơi Offline thì dùng của Unity
                if (PhotonNetwork.InRoom) PhotonNetwork.LoadLevel(m);
                else SceneManager.LoadScene(m);
                return;
            }
        }

        if (!ten.Contains("game")) return;

        // Kiểm tra theo ĐÚNG scene vừa nạp (scene cũ có thể chưa kịp bị huỷ)
        bool daCo = false;
        foreach (GameNetworkManager g in FindObjectsByType<GameNetworkManager>(FindObjectsSortMode.None))
        {
            if (g != null && g.gameObject.scene == scene) { daCo = true; break; }
        }

        if (!daCo)
        {
            GameObject obj = new GameObject("Auto_GameNetworkManager");
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.AddComponent<GameNetworkManager>();
        }
    }

    private static string LayMapDaChon()
    {
        if (PhotonNetwork.InRoom
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Map", out object v)
            && v is string s && !string.IsNullOrEmpty(s))
            return s;

        return PlayerInventory.GetSelectedMap();
    }

    public static string LayTenSceneVaoGame()
    {
        string m = LayMapDaChon();
        return (!string.IsNullOrEmpty(m) && m != "game" && Application.CanStreamedLevelBeLoaded(m)) ? m : "game";
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name.ToLower().Contains("select")) return;

        PhotonNetwork.IsMessageQueueRunning = true;
        PhotonNetwork.SendRate = Mathf.Clamp(tanSoGui, 10, 60);
        PhotonNetwork.SerializationRate = Mathf.Clamp(tanSoDongBo, 10, 60);
        TuDongTimSpawnPointsNeuThieu();
        xeDaApSkin.Clear();
        daTaoXe = false;
        StartCoroutine(ChoVaoPhongVaSinhXe());
    }

    // Rời game (thoát / về menu / đổi map): bật bộ đếm chờ ở lobby để game kịp dọn và tải lại
    
        private void OnDestroy()
    {
        if (daTaoXe && (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom))
            VaoGameCooldown.BatDau();
    }


    // OnGUI chạy 2 lần mỗi frame và sinh rác ngay cả khi không vẽ gì: chỉ biên dịch khi cần debug
    // (thêm XE_DEBUG vào Project Settings > Player > Scripting Define Symbols để bật lại)
#if XE_DEBUG
    private void OnGUI()
    {
        if (!hienThongTinDebugXe) return;

        GUI.color = Color.yellow;
        GUI.Label(new Rect(10f, 10f, 1100f, 600f),
            "[DEBUG XE] đã chọn=" + VehicleCatalog.GetSelectedIndex() +
            " | PlayerInventory=" + PlayerInventory.GetEquippedIndex("Scooter") +
            " | tổng xe=" + VehicleCatalog.Count + "\n" +
            VehicleCatalog.thongTinToCuoi);
    }
#endif

    private void Update()
    {
        if (Time.unscaledTime < thoiDiemQuetTiepTheo) return;
        thoiDiemQuetTiepTheo = Time.unscaledTime + CHU_KY_QUET_SKIN;
        QuetVaApSkinXeMoi();
    }

    private static bool ChuaChuoi(string s, string tuKhoa)
    {
        return s.IndexOf(tuKhoa, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void TuDongTimSpawnPointsNeuThieu()
    {
        if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null) return;

        var ds = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string ten = t.name;
            if (ChuaChuoi(ten, "spawnpoint") || ChuaChuoi(ten, "spawn_point") ||
                string.Equals(ten, "spawn", System.StringComparison.OrdinalIgnoreCase))
                ds.Add(t);
        }

        if (ds.Count > 0)
        {
            ds.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            spawnPoints = ds.ToArray();
        }
    }

    private Vector3 DatLenMatDuong(Vector3 goc)
    {
        Vector3 batDau = goc + Vector3.up * 3f;
        int n = Physics.RaycastNonAlloc(batDau, Vector3.down, bufferRay, 40f, ~0, QueryTriggerInteraction.Ignore);

        float tot = float.MaxValue;
        bool laRoad = false;
        Vector3 diem = goc;
        bool coHit = false;

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = bufferRay[i];
            bool road = ChuaChuoi(h.collider.name, "road");
            if (!coHit || (road && !laRoad) || (road == laRoad && h.distance < tot))
            {
                diem = h.point;
                tot = h.distance;
                laRoad = road;
                coHit = true;
            }
        }

        return (coHit ? diem : goc) + Vector3.up * doCaoThaXe;
    }

    private IEnumerator ChoVaoPhongVaSinhXe()
    {
        if (PhotonNetwork.IsConnected)
        {
            float timeout = 10f;
            while ((!PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (daTaoXe) yield break;
        daTaoXe = true;

        if (GameObject.Find("XE_CUA_TOI") != null) yield break;

        Vector3 viTriSpawn = DatLenMatDuong(new Vector3(3f, 2f, 4f));
        Quaternion gocSpawn = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null)
        {
            int playerIndex = (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.ActorNumber > 0)
                ? (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length
                : 0;

            Transform chosenSpawn = spawnPoints[playerIndex];
            gocSpawn = chosenSpawn.rotation;

            Vector3 mucTieu = chosenSpawn.position + chosenSpawn.TransformDirection(doLechSpawn);
            viTriSpawn = DatLenMatDuong(mucTieu);
        }

        int indexXe = VehicleCatalog.GetSelectedIndex();
        GameObject xe = null;

        if (PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("[NETWORK] Đang kết nối Photon nhưng chưa vào Room!");
                yield break;
            }

            try
            {
                Hashtable trangBi = new Hashtable
                {
                    { "Scooter", indexXe },
                    { "Helmet", PlayerInventory.GetEquippedIndex("Helmet") },
                    { "Character", PlayerInventory.GetEquippedIndex("Character") }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(trangBi);
            }
            catch { }

            object[] dataKhoiTao = new object[] { indexXe };
            xe = PhotonNetwork.Instantiate(tenPrefabXeGoc, viTriSpawn, gocSpawn, 0, dataKhoiTao);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(tenPrefabXeGoc);
            if (prefab != null)
                xe = Instantiate(prefab, viTriSpawn, gocSpawn);
        }

        if (xe != null)
        {
            xe.name = "XE_CUA_TOI";
            xe.SetActive(true);

            NangXeKhoiMatDuong(xe, viTriSpawn.y - doCaoThaXe);
            KichHoatDungMauXe(xe, indexXe, kichThuocXeChuan);

            PhotonView pvXe = xe.GetComponent<PhotonView>();
            if (pvXe != null) xeDaApSkin[pvXe.ViewID] = indexXe;

            xe.SendMessage("LuuDiemXuatPhat", SendMessageOptions.DontRequireReceiver);

            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                Transform diemBamCam = xe.transform.Find("pointFollowTarget");
                cam.Target.TrackingTarget = (diemBamCam != null) ? diemBamCam : xe.transform;
            }

            StartCoroutine(DamBaoMauXeSauKhiSpawn(xe, indexXe));
        }
    }

    // Chỉ áp lại 1 lần sau spawn (trước đây 2 lần, mỗi lần dựng lại bảng node + SyncTransforms)
    private IEnumerator DamBaoMauXeSauKhiSpawn(GameObject xe, int indexXe)
    {
        yield return new WaitForSeconds(0.2f);
        if (xe != null) KichHoatDungMauXe(xe, indexXe, kichThuocXeChuan);
    }

    private void NangXeKhoiMatDuong(GameObject xe, float matDuongY)
    {
        Physics.SyncTransforms();

        float day = DayColliderXe(xe, null);
        if (float.IsNaN(day) || day >= matDuongY) return;

        float nang = matDuongY - day + 0.02f;
        if (nang > 2f) return;

        xe.transform.position += Vector3.up * nang;

        Rigidbody rb = xe.GetComponent<Rigidbody>();
        if (rb != null) rb.position = xe.transform.position;

        Physics.SyncTransforms();
    }

    private static readonly List<Collider> bufferCollider = new List<Collider>(16);

    private static float DayColliderXe(GameObject xeRoot, Transform boQua)
    {
        float thap = float.MaxValue;

        bool gocCoCollider = false;
        xeRoot.GetComponents(bufferCollider);
        for (int i = 0; i < bufferCollider.Count; i++)
        {
            Collider g = bufferCollider[i];
            if (g != null && g.enabled && !g.isTrigger) { gocCoCollider = true; break; }
        }

        xeRoot.GetComponentsInChildren(true, bufferCollider);
        for (int i = 0; i < bufferCollider.Count; i++)
        {
            Collider c = bufferCollider[i];
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (boQua != null && c.transform.IsChildOf(boQua)) continue;
            if (gocCoCollider && c is MeshCollider && c.gameObject != xeRoot) continue;

            thap = Mathf.Min(thap, c.bounds.min.y);
        }
        bufferCollider.Clear();

        return thap == float.MaxValue ? float.NaN : thap;
    }

    // Xe người khác: mỗi xe lấy màu theo InstantiationData của CHÍNH xe đó.
    // CustomProperties của người chơi chỉ dùng khi xe không có InstantiationData.
    private void QuetVaApSkinXeMoi()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom) return;

        List<scooter> ds = scooter.TatCaXe;
        if (ds.Count <= 1) return;

        for (int k = 0; k < ds.Count; k++)
        {
            scooter sc = ds[k];
            if (sc == null) continue;

            if (!sc.TryGetComponent(out PhotonView pv) || pv.IsMine) continue;

            int idx = -1;
            object[] dt = pv.InstantiationData;
            if (dt != null && dt.Length > 0 && dt[0] != null)
            {
                idx = System.Convert.ToInt32(dt[0]);
            }
            else if (pv.Owner != null && pv.Owner.CustomProperties != null &&
                     pv.Owner.CustomProperties.TryGetValue("Scooter", out object propVal) && propVal != null)
            {
                idx = System.Convert.ToInt32(propVal);
            }

            if (idx < 0) continue;

            if (xeDaApSkin.TryGetValue(pv.ViewID, out int daAp) && daAp == idx)
                continue;

            sc.ApDungMauXe(idx);
            xeDaApSkin[pv.ViewID] = idx;
        }
    }

    // Bảng tên -> node dựng 1 lần cho mỗi lần áp màu
    private static Transform bangGoc;
    private static readonly Dictionary<string, Transform> bangNode = new Dictionary<string, Transform>();
    private static readonly List<Transform> bufferNode = new List<Transform>(256);
    private static readonly List<Renderer> bufferRenderer = new List<Renderer>(64);

    private static void LapBangNode(Transform root)
    {
        bangGoc = root;
        bangNode.Clear();

        root.GetComponentsInChildren(true, bufferNode);
        for (int i = 0; i < bufferNode.Count; i++)
        {
            Transform t = bufferNode[i];
            string ten = t.name;
            if (!bangNode.ContainsKey(ten)) bangNode.Add(ten, t);
        }
        bufferNode.Clear();
    }

    public static void KichHoatDungMauXe(GameObject xeRoot, int indexXe, float kichThuocMucTieu = 1.6f)
    {
        if (xeRoot == null) return;

        LapBangNode(xeRoot.transform);
        try
        {
            KichHoatNoiBo(xeRoot, indexXe, kichThuocMucTieu);
        }
        finally
        {
            bangGoc = null;
            bangNode.Clear();
            bufferNode.Clear();
            bufferRenderer.Clear();
        }
    }

    private static readonly string[] doCuTen = { "ElectricScooter", "ElectricScooter_T", "ElectricScooter_wheel1", "ElectricScooter_wheel2" };

    private static void KichHoatNoiBo(GameObject xeRoot, int indexXe, float kichThuocMucTieu)
    {
        for (int i = 0; i < doCuTen.Length; i++)
        {
            Transform cu = TimNodeTheoTen(xeRoot.transform, doCuTen[i]);
            if (cu != null) cu.gameObject.SetActive(false);
        }

        Transform mauTrang = TimNodeTheoTen(xeRoot.transform, "ElectricScooterDefault");
        if (mauTrang == null) mauTrang = TimNodeTheoTen(xeRoot.transform, "Scooter_1");

        Transform mauDen = TimNodeTheoTen(xeRoot.transform, "ElectricScooterDefaultBlack");
        if (mauDen == null) mauDen = TimNodeTheoTen(xeRoot.transform, "Scooter_2");

        var danhSachMau = new List<Transform>();
        if (mauTrang != null) danhSachMau.Add(mauTrang);
        if (mauDen != null) danhSachMau.Add(mauDen);

        Transform nhomXe = TimNodeTheoTen(xeRoot.transform, "NhomXe");
        if (nhomXe != null)
        {
            foreach (Transform con in nhomXe)
            {
                if (!danhSachMau.Contains(con))
                    danhSachMau.Add(con);
            }
        }

        bool coDuLieu = VehicleCatalog.Count > 0;
        VehicleCatalog.VehicleData du = default;
        Transform mauDuocChon = null;

        if (coDuLieu)
        {
            du = VehicleCatalog.Get(Mathf.Clamp(indexXe, 0, VehicleCatalog.Count - 1));
            if (!string.IsNullOrEmpty(du.sourceModel))
                mauDuocChon = TimNodeTheoTen(xeRoot.transform, du.sourceModel);
        }

        if (mauDuocChon == null)
        {
            if (danhSachMau.Count == 0)
            {
                Debug.LogWarning("[XE] Không tìm thấy mẫu xe nào trong Prefab!");
                return;
            }
            mauDuocChon = danhSachMau[Mathf.Clamp(indexXe, 0, danhSachMau.Count - 1)];
        }
        else if (!danhSachMau.Contains(mauDuocChon))
        {
            danhSachMau.Add(mauDuocChon);
        }

        for (int i = 0; i < danhSachMau.Count; i++)
        {
            Transform m = danhSachMau[i];
            if (m != null && m != mauDuocChon && m.gameObject.activeSelf)
                m.gameObject.SetActive(false);
        }

        if (coDuLieu)
        {
            for (int i = 0; i < VehicleCatalog.Count; i++)
            {
                string sm = VehicleCatalog.Get(i).sourceModel;
                if (string.IsNullOrEmpty(sm)) continue;

                Transform n = TimNodeTheoTen(xeRoot.transform, sm);
                if (n == null || n == mauDuocChon) continue;
                if (mauDuocChon.IsChildOf(n) || n.IsChildOf(mauDuocChon)) continue;

                if (n.gameObject.activeSelf) n.gameObject.SetActive(false);
            }
        }

        Transform p = mauDuocChon;
        while (p != null && p != xeRoot.transform)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p = p.parent;
        }

        int layerMinimap = LayerMask.NameToLayer("Minimap");
        mauDuocChon.GetComponentsInChildren(true, bufferNode);
        for (int i = 0; i < bufferNode.Count; i++)
        {
            GameObject go = bufferNode[i].gameObject;
            if (!go.activeSelf) go.SetActive(true);
            if (go.layer == layerMinimap) go.layer = 0;
        }
        bufferNode.Clear();

        mauDuocChon.GetComponentsInChildren(true, bufferRenderer);
        Renderer[] renderers = bufferRenderer.ToArray();
        bufferRenderer.Clear();

        Bounds b = new Bounds();
        bool coMesh = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            r.enabled = true;
            if (!coMesh) { b = r.bounds; coMesh = true; }
            else b.Encapsulate(r.bounds);
        }

        if (coMesh)
        {
            float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxDim > 0.00001f && Mathf.Abs(maxDim - kichThuocMucTieu) > 0.05f)
            {
                float heSo = kichThuocMucTieu / maxDim;
                mauDuocChon.localScale *= heSo;

                coMesh = false;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (!coMesh) { b = r.bounds; coMesh = true; }
                    else b.Encapsulate(r.bounds);
                }
            }

            float yDay = xeRoot.transform.position.y;
            Physics.SyncTransforms();
            float dayCollider = DayColliderXe(xeRoot, mauDuocChon);
            if (!float.IsNaN(dayCollider))
            {
                float chenh = xeRoot.transform.position.y - dayCollider;
                if (chenh >= 0f && chenh <= 1.5f) yDay = dayCollider;
            }

            Vector3 lech = new Vector3(
                xeRoot.transform.position.x - b.center.x,
                yDay - b.min.y,
                xeRoot.transform.position.z - b.center.z
            );
            mauDuocChon.position += lech;

            DongBoBoxColliderThanXe(xeRoot, mauDuocChon);
        }

        if (coDuLieu)
        {
            // Mỗi xe một bản material riêng: đổi màu xe này không làm đổi màu các xe khác
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !rendererDaTachMat.Add(r.GetInstanceID())) continue;
                r.materials = r.materials;
            }

            VehicleCatalog.ApplyPaint(mauDuocChon.gameObject, du.color);
        }
    }

    // Chỉ dùng bounds của renderer (đọc thẳng từ transform) nên không cần SyncTransforms ở đây
    private static void DongBoBoxColliderThanXe(GameObject xeRoot, Transform mauDuocChon)
    {
        Bounds bWorld = new Bounds();
        bool co = false;

        mauDuocChon.GetComponentsInChildren(true, bufferRenderer);
        for (int i = 0; i < bufferRenderer.Count; i++)
        {
            Renderer r = bufferRenderer[i];
            if (!r.enabled || r.bounds.size.sqrMagnitude <= 0.0001f) continue;
            if (!co) { bWorld = r.bounds; co = true; }
            else bWorld.Encapsulate(r.bounds);
        }
        bufferRenderer.Clear();
        if (!co) return;

        BoxCollider box = xeRoot.GetComponent<BoxCollider>();
        if (box == null) box = xeRoot.AddComponent<BoxCollider>();

        box.isTrigger = false;
        box.enabled = true;
        box.center = xeRoot.transform.InverseTransformPoint(bWorld.center);

        Vector3 s = xeRoot.transform.lossyScale;
        box.size = new Vector3(
            Mathf.Max(0.45f, bWorld.size.x / Mathf.Max(0.01f, Mathf.Abs(s.x))),
            Mathf.Max(0.55f, bWorld.size.y / Mathf.Max(0.01f, Mathf.Abs(s.y))),
            Mathf.Max(0.45f, bWorld.size.z / Mathf.Max(0.01f, Mathf.Abs(s.z)))
        );
    }

    private static Transform TimNodeTheoTen(Transform root, string ten)
    {
        if (root == bangGoc)
        {
            bangNode.TryGetValue(ten, out Transform kq);
            return kq;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == ten) return t;
        }
        return null;
    }
}