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

    private bool daTaoXe = false;
    private static bool daDangKySceneLoaded = false;

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
        KiemTraVaTaoManager(scene);
    }

    private static void KiemTraVaTaoManager(Scene scene)
    {
        string ten = scene.name.ToLower();

        if (ten.Contains("select") || ten.Contains("shop") || ten.Contains("menu") || ten.Contains("lobby"))
            return;

        if (ten == "game")
        {
            string m = LayMapDaChon();
            if (!string.IsNullOrEmpty(m) && m != "game" && Application.CanStreamedLevelBeLoaded(m))
            {
                SceneManager.LoadScene(m);
                return;
            }
        }

        // Tự nhận diện màn chơi nếu tên scene có "game"/"map" HOẶC trong scene có GameManager
        bool laSceneDua = ten.Contains("game") || ten.Contains("map") || FindFirstObjectByType<GameManager>() != null;
        if (!laSceneDua) return;

        if (FindFirstObjectByType<GameNetworkManager>() == null)
        {
            GameObject obj = new GameObject("Auto_GameNetworkManager");
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
        string tenScene = SceneManager.GetActiveScene().name.ToLower();
        if (tenScene.Contains("select") || tenScene.Contains("shop") || tenScene.Contains("menu") || tenScene.Contains("lobby"))
            return;

        TuDongTimSpawnPointsNeuThieu();
        StartCoroutine(ChoVaoPhongVaSinhXe());
    }

    private void TuDongTimSpawnPointsNeuThieu()
    {
        if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null) return;

        var ds = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string ten = t.name.ToLower();
            if (ten.Contains("spawnpoint") || ten.Contains("spawn_point") || ten == "spawn")
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
        Vector3 batDau = goc + Vector3.up * 5f;
        RaycastHit[] cac = Physics.RaycastAll(batDau, Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore);

        float tot = float.MaxValue;
        bool laRoad = false;
        Vector3 diem = goc;
        bool coHit = false;

        foreach (RaycastHit h in cac)
        {
            bool road = h.collider.name.ToLower().Contains("road");
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

    private GameObject TaiPrefabXe()
    {
        GameObject prefab = Resources.Load<GameObject>(tenPrefabXeGoc);
        if (prefab != null) return prefab;

        // Dự phòng nếu tên Prefab trong Resources viết hoa/thường khác nhau
        foreach (GameObject go in Resources.LoadAll<GameObject>(""))
        {
            if (go != null && (go.name.Equals(tenPrefabXeGoc, System.StringComparison.OrdinalIgnoreCase)
                || go.GetComponent<scooter>() != null))
            {
                tenPrefabXeGoc = go.name;
                return go;
            }
        }

        Debug.LogError($"[GameNetworkManager] Không tìm thấy Prefab xe '{tenPrefabXeGoc}' trong thư mục Resources!");
        return null;
    }

    private IEnumerator ChoVaoPhongVaSinhXe()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
        {
            float timeout = 2.5f;
            while (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

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

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            try
            {
                Hashtable trangBi = new Hashtable
                {
                    { "Scooter", indexXe }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(trangBi);
            }
            catch { }

            object[] dataKhoiTao = new object[] { indexXe };
            xe = PhotonNetwork.Instantiate(tenPrefabXeGoc, viTriSpawn, gocSpawn, 0, dataKhoiTao);
        }
        else
        {
            GameObject prefab = TaiPrefabXe();
            if (prefab != null)
                xe = Instantiate(prefab, viTriSpawn, gocSpawn);
        }

        if (xe != null)
        {
            xe.name = "XE_CUA_TOI";
            xe.SetActive(true);

            KichHoatDungMauXe(xe, indexXe, kichThuocXeChuan);

            xe.SendMessage("LuuDiemXuatPhat", SendMessageOptions.DontRequireReceiver);

            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                Transform diemBamCam = xe.transform.Find("pointFollowTarget");
                cam.Target.TrackingTarget = (diemBamCam != null) ? diemBamCam : xe.transform;
            }
        }

        yield return new WaitForSecondsRealtime(0.25f);
        DongBoTatCaXeTrongPhong();
    }

    public static void KichHoatDungMauXe(GameObject xeRoot, int indexXe, float kichThuocMucTieu = 1.6f)
    {
        if (xeRoot == null) return;

        VehicleCatalog.VehicleData data = VehicleCatalog.Get(indexXe);

        // 1. Tắt các bộ phận xe cũ rời rạc
        string[] doCu = { "ElectricScooter", "ElectricScooter_T", "ElectricScooter_wheel1", "ElectricScooter_wheel2" };
        foreach (string ten in doCu)
        {
            Transform cu = TimNodeTheoTen(xeRoot.transform, ten);
            if (cu != null) cu.gameObject.SetActive(false);
        }

        Transform mauTrang = TimNodeTheoTen(xeRoot.transform, "ElectricScooterDefault");
        Transform mauDen = TimNodeTheoTen(xeRoot.transform, "ElectricScooterDefaultBlack");

        if (mauTrang != null) mauTrang.gameObject.SetActive(false);
        if (mauDen != null) mauDen.gameObject.SetActive(false);

        // 2. Chọn đúng node gốc theo data.sourceModel
        Transform mauDuocChon = TimNodeTheoTen(xeRoot.transform, data.sourceModel);
        if (mauDuocChon == null)
            mauDuocChon = (data.sourceModel == "ElectricScooterDefaultBlack") ? mauDen : mauTrang;

        if (mauDuocChon == null)
        {
            Debug.LogWarning($"[XE] Không tìm thấy model '{data.sourceModel}' trong Prefab!");
            return;
        }

        // 3. Bật toàn bộ cây cha và cây con của mẫu được chọn
        Transform p = mauDuocChon;
        while (p != null && p != xeRoot.transform)
        {
            p.gameObject.SetActive(true);
            p = p.parent;
        }

        foreach (Transform t in mauDuocChon.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.SetActive(true);
            if (t.gameObject.layer == LayerMask.NameToLayer("Minimap"))
                t.gameObject.layer = 0;
        }

        // 4. Sơn đúng màu xe theo VehicleCatalog
        VehicleCatalog.ApplyPaint(mauDuocChon.gameObject, data.color);

        // 5. Tính kích thước an toàn (tránh lỗi Renderer.bounds bị 0 khi Time.timeScale = 0)
        Physics.SyncTransforms();
        Renderer[] renderers = mauDuocChon.GetComponentsInChildren<Renderer>(true);
        Bounds b = new Bounds();
        bool coMesh = false;

        foreach (Renderer r in renderers)
        {
            r.enabled = true;
            if (r.bounds.size.sqrMagnitude <= 0.00001f) continue;

            if (!coMesh) { b = r.bounds; coMesh = true; }
            else b.Encapsulate(r.bounds);
        }

        if (coMesh)
        {
            float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxDim > 0.05f && Mathf.Abs(maxDim - kichThuocMucTieu) > 0.05f)
            {
                float heSo = Mathf.Clamp(kichThuocMucTieu / maxDim, 0.05f, 20f);
                mauDuocChon.localScale *= heSo;
                Physics.SyncTransforms();

                coMesh = false;
                foreach (Renderer r in renderers)
                {
                    if (r.bounds.size.sqrMagnitude <= 0.00001f) continue;
                    if (!coMesh) { b = r.bounds; coMesh = true; }
                    else b.Encapsulate(r.bounds);
                }
            }

            Vector3 lech = new Vector3(
                xeRoot.transform.position.x - b.center.x,
                xeRoot.transform.position.y - b.min.y,
                xeRoot.transform.position.z - b.center.z
            );
            mauDuocChon.position += lech;
        }
    }

    private void DongBoTatCaXeTrongPhong()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom) return;

        foreach (PhotonView pv in FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (pv == null || pv.GetComponent<scooter>() == null) continue;

            int idx = 0;
            if (pv.InstantiationData != null && pv.InstantiationData.Length > 0 && pv.InstantiationData[0] is int)
            {
                idx = (int)pv.InstantiationData[0];
            }
            else if (pv.Owner != null)
            {
                idx = VehicleCatalog.GetPlayerVehicleIndex(pv.Owner);
            }

            KichHoatDungMauXe(pv.gameObject, idx, kichThuocXeChuan);
        }
    }

    private static Transform TimNodeTheoTen(Transform root, string ten)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == ten) return t;
        }
        return null;
    }
}