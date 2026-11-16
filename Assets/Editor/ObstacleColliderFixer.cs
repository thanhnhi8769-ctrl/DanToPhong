
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObstacleColliderFixer : EditorWindow
{
    [Header("Nhóm vật cản")]
    [SerializeField]
    private string nhomThanCay =
        "tree,palm,lamp,pole";

    [SerializeField]
    private string nhomKhac =
        "fence,guardrail,barricade,barrier,cone,hammer,billboard";

    [Header("Kích thước Collider")]
    [SerializeField] private float tiLeThanCay = 0.15f;
    [SerializeField] private float tiLeNgang = 0.85f;
    [SerializeField] private float nguongQuaTo = 1.25f;

    [Header("Phạm vi")]
    [SerializeField] private bool chiSuaCaiQuaTo = true;
    [SerializeField] private bool chiDoiTuongDangChon = false;

    [Header("An toàn")]
    [SerializeField] private bool boQuaMeshCollider = true;
    [SerializeField] private bool boQuaColliderCoRigidbody = true;

    [MenuItem("Tools/Sửa Collider Vật Cản")]
    public static void MoCuaSo()
    {
        GetWindow<ObstacleColliderFixer>("Collider vật cản");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Công cụ chỉ xử lý vật cản. Giữ nguyên hệ thống đường BoxCollider và MeshCollider.",
            MessageType.Info);

        EditorGUILayout.Space(5);

        nhomThanCay = EditorGUILayout.TextField(
            "Nhóm thân mảnh", nhomThanCay);

        nhomKhac = EditorGUILayout.TextField(
            "Nhóm vật cản khác", nhomKhac);

        tiLeThanCay = EditorGUILayout.Slider(
            "Tỉ lệ thân cây", tiLeThanCay, 0.05f, 0.6f);

        tiLeNgang = EditorGUILayout.Slider(
            "Tỉ lệ vật khác", tiLeNgang, 0.3f, 1f);

        nguongQuaTo = EditorGUILayout.FloatField(
            "Ngưỡng quá to", nguongQuaTo);

        chiSuaCaiQuaTo = EditorGUILayout.Toggle(
            "Chỉ sửa Collider quá to", chiSuaCaiQuaTo);

        chiDoiTuongDangChon = EditorGUILayout.Toggle(
            "Chỉ xử lý object đang chọn", chiDoiTuongDangChon);

        boQuaMeshCollider = EditorGUILayout.Toggle(
            "Giữ nguyên MeshCollider", boQuaMeshCollider);

        boQuaColliderCoRigidbody = EditorGUILayout.Toggle(
            "Bỏ qua Collider có Rigidbody", boQuaColliderCoRigidbody);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("QUÉT & BÁO CÁO", GUILayout.Height(30)))
            Chay(false);

        if (GUILayout.Button("SỬA COLLIDER VẬT CẢN", GUILayout.Height(38)))
            Chay(true);
    }

    private static string[] Tach(string s)
    {
        string[] a = s.ToLower().Split(',');

        for (int i = 0; i < a.Length; i++)
            a[i] = a[i].Trim();

        return a;
    }

    private static bool KhopTuKhoa(string ten, string tuKhoa)
    {
        if (string.IsNullOrEmpty(tuKhoa))
            return false;

        int i = ten.IndexOf(
            tuKhoa,
            StringComparison.OrdinalIgnoreCase);

        while (i >= 0)
        {
            if (i == 0 || !char.IsLetterOrDigit(ten[i - 1]))
                return true;

            i = ten.IndexOf(
                tuKhoa,
                i + 1,
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool LaDuongHoacMatDat(Transform t)
    {
        string[] tuKhoa =
        {
            "road", "street", "asphalt",
            "ground", "terrain", "sidewalk",
            "pavement", "floor", "track",
            "duong", "matduong"
        };

        while (t != null)
        {
            string ten = t.name.ToLower();

            foreach (string k in tuKhoa)
            {
                if (KhopTuKhoa(ten, k))
                    return true;
            }

            t = t.parent;
        }

        return false;
    }

    private static int LayNhom(
        Transform t,
        string[] cay,
        string[] khac)
    {
        while (t != null)
        {
            string ten = t.name.ToLower();

            foreach (string k in cay)
                if (KhopTuKhoa(ten, k))
                    return 1;

            foreach (string k in khac)
                if (KhopTuKhoa(ten, k))
                    return 2;

            t = t.parent;
        }

        return 0;
    }

    private static float TheTich(Bounds b)
    {
        return b.size.x * b.size.y * b.size.z;
    }

    private static bool LayBoundsLocal(
        Transform goc,
        out Bounds ketQua)
    {
        ketQua = new Bounds();
        bool co = false;

        foreach (MeshFilter mf in
            goc.GetComponentsInChildren<MeshFilter>(false))
        {
            if (mf.sharedMesh == null)
                continue;

            Bounds mb = mf.sharedMesh.bounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 diem = mb.center +
                    Vector3.Scale(
                        mb.extents,
                        new Vector3(
                            (i & 1) == 0 ? -1 : 1,
                            (i & 2) == 0 ? -1 : 1,
                            (i & 4) == 0 ? -1 : 1));

                Vector3 local =
                    goc.InverseTransformPoint(
                        mf.transform.TransformPoint(diem));

                if (!co)
                {
                    ketQua = new Bounds(local, Vector3.zero);
                    co = true;
                }
                else
                {
                    ketQua.Encapsulate(local);
                }
            }
        }

        return co;
    }

    private static bool LayBoundsWorld(
        Transform goc,
        out Bounds ketQua)
    {
        ketQua = new Bounds();
        bool co = false;

        foreach (Renderer r in
            goc.GetComponentsInChildren<Renderer>(false))
        {
            if (r is ParticleSystemRenderer)
                continue;

            if (!co)
            {
                ketQua = r.bounds;
                co = true;
            }
            else
            {
                ketQua.Encapsulate(r.bounds);
            }
        }

        return co;
    }

    private static int TrucDung(Transform t)
    {
        int tot = 1;
        float max = -1f;

        for (int i = 0; i < 3; i++)
        {
            Vector3 a = Vector3.zero;
            a[i] = 1f;

            float d = Mathf.Abs(
                Vector3.Dot(
                    t.TransformDirection(a),
                    Vector3.up));

            if (d > max)
            {
                max = d;
                tot = i;
            }
        }

        return tot;
    }

    private void Chay(bool sua)
    {
        string[] cay = Tach(nhomThanCay);
        string[] khac = Tach(nhomKhac);

        List<Collider> ds = new List<Collider>();

        if (chiDoiTuongDangChon)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                ds.AddRange(
                    go.GetComponentsInChildren<Collider>(true));
            }
        }
        else
        {
            ds.AddRange(
                FindObjectsByType<Collider>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None));
        }

        int quet = 0;
        int quaTo = 0;
        int daSua = 0;
        int boQua = 0;

        Undo.IncrementCurrentGroup();
        int nhomUndo = Undo.GetCurrentGroup();

        foreach (Collider c in ds)
        {
            if (c == null || c.isTrigger)
                continue;

            if (!c.gameObject.scene.IsValid())
                continue;

            if (EditorUtility.IsPersistent(c.gameObject))
                continue;

            // Bảo vệ toàn bộ hệ thống đường.
            if (LaDuongHoacMatDat(c.transform))
                continue;

            // Không can thiệp vào xe và vật thể động.
            if (boQuaColliderCoRigidbody &&
                c.GetComponentInParent<Rigidbody>() != null)
                continue;

            if (c.GetComponentInParent<scooter>() != null)
                continue;

            int loai = LayNhom(c.transform, cay, khac);

            if (loai == 0)
                continue;

            quet++;

            // Giữ nguyên MeshCollider nếu được yêu cầu.
            if (boQuaMeshCollider && c is MeshCollider)
            {
                boQua++;
                continue;
            }

            // Chỉ xử lý BoxCollider.
            // Không tự ý thay CapsuleCollider hoặc SphereCollider.
            if (!(c is BoxCollider box))
            {
                boQua++;
                continue;
            }

            if (!LayBoundsWorld(c.transform, out Bounds bw) ||
                !LayBoundsLocal(c.transform, out Bounds bl))
            {
                boQua++;
                continue;
            }

            float tiLe =
                TheTich(c.bounds) /
                Mathf.Max(0.0001f, TheTich(bw));

            bool quaToThat = tiLe > nguongQuaTo;

            if (quaToThat)
                quaTo++;

            bool canSua =
                (loai == 1) ||
                !chiSuaCaiQuaTo ||
                quaToThat;

            if (!canSua)
                continue;

            if (!sua)
            {
                Debug.LogWarning(
                    $"[SẼ KIỂM TRA] {c.name} - Tỉ lệ: {tiLe:F2}",
                    c.gameObject);

                continue;
            }

            Undo.RecordObject(box, "Sửa Collider vật cản");

            Vector3 size = bl.size;
            Vector3 tam = bl.center;

            float k = loai == 1
                ? tiLeThanCay
                : tiLeNgang;

            int truc = TrucDung(c.transform);

            for (int a = 0; a < 3; a++)
            {
                if (a == truc)
                    continue;

                size[a] *= k;

                if (loai == 1)
                {
                    float s = Mathf.Max(
                        0.0001f,
                        Mathf.Abs(c.transform.lossyScale[a]));

                    size[a] = Mathf.Max(
                        size[a],
                        0.35f / s);

                    if (bl.min[a] <= 0f &&
                        bl.max[a] >= 0f)
                    {
                        tam[a] = 0f;
                    }
                }
            }

            box.center = tam;
            box.size = size;
            box.isTrigger = false;

            EditorUtility.SetDirty(box);

            daSua++;
        }

        Undo.CollapseUndoOperations(nhomUndo);

        if (sua && daSua > 0)
        {
            UnityEditor.SceneManagement
                .EditorSceneManager.MarkAllScenesDirty();
        }

        string msg =
            $"KẾT QUẢ QUÉT COLLIDER\n\n" +
            $"- Collider vật cản tìm thấy: {quet}\n" +
            $"- Collider quá lớn: {quaTo}\n" +
            $"- Đã chỉnh sửa: {daSua}\n" +
            $"- Bỏ qua: {boQua}\n\n" +
            (sua
                ? "Đã hoàn thành."
                : "Chưa thay đổi Collider nào.");

        Debug.Log(msg);

        EditorUtility.DisplayDialog(
            "Collider vật cản",
            msg,
            "OK");
    }
}
