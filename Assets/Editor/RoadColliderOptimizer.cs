using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RoadColliderOptimizer : EditorWindow
{
    public enum KieuCollider { Box, Mesh }

    [System.Serializable]
    public class QuyTac
    {
        public string tuKhoa;
        public KieuCollider kieu;
        public QuyTac(string tuKhoa, KieuCollider kieu) { this.tuKhoa = tuKhoa; this.kieu = kieu; }
    }

    // Tên chứa từ khóa -> loại collider (khớp quy tắc đầu tiên)
    // Road_Straight_Long -> Box | Road_Corner_90, Road_Corner_90_Wide -> Mesh mặt đường
    [SerializeField]
    private List<QuyTac> quyTac = new List<QuyTac>
    {
        new QuyTac("Road_Straight", KieuCollider.Box),
        new QuyTac("Road_Corner", KieuCollider.Mesh),
    };

    [SerializeField] private float doDayBox = 0.6f;          // Box dày xuống dưới (m), mặt trên đúng bằng mặt đường
    [SerializeField] private float moRongMep = 0.02f;        // Box nới thêm mỗi bên (m) để hết hở mối nối
    [SerializeField] private bool dongBoCaoDo = true;        // Kéo mặt đường mọi đoạn về cùng độ cao
    [SerializeField] private float dungSai = 0.5f;           // Chỉ chỉnh nếu lệch <= giá trị này (m)
    [SerializeField] private float dungSaiPhang = 0.04f;     // Điểm cách mặt đường <= giá trị này được ép phẳng (vạch kẻ, nhiễu)
    [SerializeField] private bool giuLeDuong = true;         // Collider cua giữ cả lề/vỉa thấp (không để xe lọt xuống lề)
    [SerializeField] private float doCaoLeToiDa = 0.4f;      // Lề/vỉa cao tối đa so với mặt đường được giữ (m)
    [SerializeField] private bool taoPhysicsMaterial = true; // Gán Physics Material bounce = 0

    private SerializedObject so;
    private Vector2 cuon;
    private readonly Dictionary<string, float> cacheH = new Dictionary<string, float>();
    private readonly Dictionary<string, Mesh> cacheMesh = new Dictionary<string, Mesh>();

    private class Info
    {
        public MeshFilter mf;
        public KieuCollider kieu;
        public int truc;
        public float dau;
        public Vector3 up;
        public float scaleTruc;
        public float h;     // độ cao mặt đường trong không gian local (dọc theo up)
        public float yMat;  // độ cao mặt đường trong thế giới
    }

    [MenuItem("Tools/Tự Động Tối Ưu Collider Cho Đường")]
    public static void MoCuaSo()
    {
        GetWindow<RoadColliderOptimizer>("Tối ưu đường");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
    }

    // =========================================================================
    // GIAO DIỆN
    // =========================================================================
    private void OnGUI()
    {
        so.Update();
        cuon = EditorGUILayout.BeginScrollView(cuon);

        EditorGUILayout.LabelField("QUY TẮC LOẠI ĐƯỜNG", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Box: đường thẳng (nhẹ, phẳng tuyệt đối).\nMesh: khúc cua - tự dựng mesh chỉ gồm mặt đường đã ép phẳng.", MessageType.Info);
        EditorGUILayout.PropertyField(so.FindProperty("quyTac"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ĐỒNG BỘ ĐỘ CAO (chống chìm / nảy ở mối nối)", EditorStyles.boldLabel);
        dongBoCaoDo = EditorGUILayout.Toggle("Đồng bộ cao độ mặt đường", dongBoCaoDo);
        if (dongBoCaoDo) dungSai = EditorGUILayout.FloatField("Dung sai chỉnh (m)", dungSai);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("COLLIDER", EditorStyles.boldLabel);
        doDayBox = EditorGUILayout.FloatField("Box: độ dày xuống dưới (m)", doDayBox);
        moRongMep = EditorGUILayout.FloatField("Box: mở rộng mép (m)", moRongMep);
        dungSaiPhang = EditorGUILayout.FloatField("Ép phẳng điểm cách mặt <= (m)", dungSaiPhang);
        giuLeDuong = EditorGUILayout.Toggle("Khúc cua: giữ cả lề/vỉa thấp", giuLeDuong);
        if (giuLeDuong) doCaoLeToiDa = EditorGUILayout.FloatField("Lề cao tối đa (m)", doCaoLeToiDa);
        taoPhysicsMaterial = EditorGUILayout.Toggle("Gán Physics Material (bounce = 0)", taoPhysicsMaterial);

        EditorGUILayout.Space();
        if (GUILayout.Button("Liệt kê tên các loại đường trong Scene", GUILayout.Height(24))) LietKeTen();
        if (GUILayout.Button("KIỂM TRA (không sửa, báo đoạn bị lệch/chìm)", GUILayout.Height(28))) KiemTra();
        if (GUILayout.Button("TỐI ƯU TOÀN BỘ ĐƯỜNG", GUILayout.Height(38))) ThucHien();

        EditorGUILayout.EndScrollView();
    }

    // =========================================================================
    // TIỆN ÍCH
    // =========================================================================
    private bool TimKieu(string ten, out KieuCollider kieu)
    {
        string t = ten.ToLower();
        foreach (QuyTac q in quyTac)
        {
            if (string.IsNullOrWhiteSpace(q.tuKhoa)) continue;
            if (t.Contains(q.tuKhoa.Trim().ToLower())) { kieu = q.kieu; return true; }
        }
        kieu = KieuCollider.Box;
        return false;
    }

    private void LietKeTen()
    {
        var dem = new SortedDictionary<string, int>();
        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!mf.gameObject.scene.IsValid() || EditorUtility.IsPersistent(mf.gameObject)) continue;
            string n = Regex.Replace(mf.gameObject.name, @"\s*\(\d+\)$", "");
            if (!n.ToLower().Contains("road")) continue;
            dem[n] = dem.ContainsKey(n) ? dem[n] + 1 : 1;
        }

        string msg = "";
        foreach (var kv in dem) msg += $"- {kv.Key}  (x{kv.Value})\n";
        if (msg == "") msg = "Không thấy object nào có tên chứa \"road\".";
        Debug.Log("Các loại đường trong Scene:\n" + msg);
        EditorUtility.DisplayDialog("Các loại đường trong Scene", msg, "OK");
    }

    // Trục local gần hướng "lên" thế giới nhất
    private static void TimTrucDung(Transform t, out int truc, out float dau, out Vector3 up)
    {
        float best = -1f;
        truc = 1;
        dau = 1f;
        for (int i = 0; i < 3; i++)
        {
            Vector3 a = Vector3.zero;
            a[i] = 1f;
            float d = Vector3.Dot(t.TransformDirection(a), Vector3.up);
            if (Mathf.Abs(d) > best)
            {
                best = Mathf.Abs(d);
                truc = i;
                dau = d >= 0f ? 1f : -1f;
            }
        }
        up = Vector3.zero;
        up[truc] = dau;
    }

    // Độ cao MẶT ĐƯỜNG THẬT: mức phẳng hướng lên có tổng diện tích lớn nhất (bỏ qua vỉa, lan can, cột đèn)
    private float LayH(Mesh m, Vector3 up, int truc, float dau, float scaleTruc)
    {
        string key = $"{m.GetInstanceID()}_{truc}_{dau}";
        if (cacheH.TryGetValue(key, out float daCo)) return daCo;

        Vector3[] v = m.vertices;
        int[] tr = m.triangles;
        float bin = 0.02f / scaleTruc;

        var dienTich = new Dictionary<int, float>();
        var tongH = new Dictionary<int, float>();

        for (int i = 0; i < tr.Length; i += 3)
        {
            Vector3 a = v[tr[i]], b = v[tr[i + 1]], c = v[tr[i + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a);
            float area = n.magnitude * 0.5f;
            if (area < 1e-9f) continue;
            if (Vector3.Dot(n.normalized, up) < 0.95f) continue;

            float h = Vector3.Dot((a + b + c) / 3f, up);
            int k = Mathf.RoundToInt(h / bin);
            dienTich[k] = (dienTich.TryGetValue(k, out float s1) ? s1 : 0f) + area;
            tongH[k] = (tongH.TryGetValue(k, out float s2) ? s2 : 0f) + area * h;
        }

        float ketQua;
        if (dienTich.Count == 0)
        {
            ketQua = dau > 0f ? m.bounds.max[truc] : -m.bounds.min[truc];
        }
        else
        {
            float tot = -1f;
            int kTot = 0;
            foreach (int k in dienTich.Keys)
            {
                float s = Get(dienTich, k - 1) + Get(dienTich, k) + Get(dienTich, k + 1);
                if (s > tot) { tot = s; kTot = k; }
            }
            float sa = Get(dienTich, kTot - 1) + Get(dienTich, kTot) + Get(dienTich, kTot + 1);
            float sh = Get(tongH, kTot - 1) + Get(tongH, kTot) + Get(tongH, kTot + 1);
            ketQua = sh / sa;
        }

        cacheH[key] = ketQua;
        return ketQua;
    }

    private static float Get(Dictionary<int, float> d, int k)
    {
        return d.TryGetValue(k, out float v) ? v : 0f;
    }

    private Info TaoInfo(MeshFilter mf, KieuCollider kieu)
    {
        var info = new Info { mf = mf, kieu = kieu };
        TimTrucDung(mf.transform, out info.truc, out info.dau, out info.up);
        info.scaleTruc = Mathf.Max(0.0001f, Mathf.Abs(mf.transform.lossyScale[info.truc]));
        info.h = LayH(mf.sharedMesh, info.up, info.truc, info.dau, info.scaleTruc);
        info.yMat = mf.transform.TransformPoint(info.up * info.h).y;
        return info;
    }

    private List<Info> ThuThap()
    {
        var ds = new List<Info>();
        var daCo = new HashSet<MeshFilter>();

        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (!TimKieu(go.name, out KieuCollider kieu)) continue;

            MeshFilter own = go.GetComponent<MeshFilter>();
            if (own != null)
            {
                if (own.sharedMesh != null && daCo.Add(own)) ds.Add(TaoInfo(own, kieu));
            }
            else
            {
                foreach (MeshFilter c in go.GetComponentsInChildren<MeshFilter>(true))
                    if (c.sharedMesh != null && daCo.Add(c)) ds.Add(TaoInfo(c, kieu));
            }
        }
        return ds;
    }

    private static float TrungVi(List<Info> ds)
    {
        var y = new List<float>();
        foreach (Info i in ds) y.Add(i.yMat);
        y.Sort();
        return y[y.Count / 2];
    }

    // =========================================================================
    // KIỂM TRA (không sửa gì)
    // =========================================================================
    private void KiemTra()
    {
        cacheH.Clear();
        List<Info> ds = ThuThap();
        if (ds.Count == 0)
        {
            EditorUtility.DisplayDialog("Kiểm tra", "Không object nào khớp quy tắc.", "OK");
            return;
        }

        float chuan = TrungVi(ds);
        int lech = 0;
        foreach (Info i in ds)
        {
            float d = i.yMat - chuan;
            if (Mathf.Abs(d) < 0.005f) continue;
            lech++;
            Debug.LogWarning($"[ĐƯỜNG LỆCH] {i.mf.name}: mặt đường {(d > 0 ? "CAO hơn" : "THẤP hơn (dễ bị chìm)")} chuẩn {Mathf.Abs(d) * 100f:F1} cm", i.mf.gameObject);
        }

        string msg = $"Đã kiểm tra {ds.Count} đoạn đường.\nĐộ cao mặt đường chuẩn: Y = {chuan:F3}\nSố đoạn lệch > 0.5 cm: {lech}\n(Xem Console, bấm vào dòng cảnh báo để chọn đúng đoạn)";
        Debug.Log(msg);
        EditorUtility.DisplayDialog("Kết quả kiểm tra", msg, "OK");
    }

    // =========================================================================
    // TỐI ƯU
    // =========================================================================
    private void ThucHien()
    {
        cacheH.Clear();
        cacheMesh.Clear();

        List<Info> ds = ThuThap();
        if (ds.Count == 0)
        {
            EditorUtility.DisplayDialog("Không có gì để làm", "Không object nào khớp quy tắc. Bấm \"Liệt kê tên\" rồi sửa lại từ khóa nhé.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int nhom = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tối ưu đường");

        // ---- 1. Đồng bộ độ cao mặt đường ----
        int daChinh = 0, boQua = 0;
        float chuan = TrungVi(ds);

        if (dongBoCaoDo)
        {
            foreach (Info i in ds)
            {
                float lech = chuan - i.yMat;
                if (Mathf.Abs(lech) < 0.0005f) continue;

                if (Mathf.Abs(lech) > dungSai)
                {
                    boQua++;
                    Debug.LogWarning($"[BỎ QUA] {i.mf.name} lệch {lech * 100f:F0} cm (> dung sai). Có thể là dốc/cầu, hoặc tăng dung sai nếu đó là đoạn bị chìm.", i.mf.gameObject);
                    continue;
                }

                Undo.RecordObject(i.mf.transform, "Đồng bộ cao độ đường");
                i.mf.transform.position += Vector3.up * lech;
                i.yMat += lech;
                daChinh++;
            }
        }

        // ---- 2. Collider ----
        PhysicsMaterial mat = taoPhysicsMaterial ? LayHoacTaoMaterial() : null; // Unity cũ: đổi thành PhysicMaterial
        int soBox = 0, soMesh = 0, loiMesh = 0;

        foreach (Info i in ds)
        {
            GameObject go = i.mf.gameObject;
            Collider col;

            if (i.kieu == KieuCollider.Box)
            {
                col = CaiBox(go, i);
                soBox++;
            }
            else
            {
                col = CaiMesh(go, i);
                if (col == null) { loiMesh++; continue; }
                soMesh++;
            }

            if (mat != null) col.sharedMaterial = mat;
            EditorUtility.SetDirty(col);
        }

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkAllScenesDirty();

        string msg =
            $"Đã tối ưu {ds.Count} đoạn đường:\n" +
            $"- {soBox} đoạn Box Collider (mặt trên = mặt đường thật)\n" +
            $"- {soMesh} khúc cua Mesh Collider (chỉ mặt đường, đã ép phẳng)\n" +
            (taoPhysicsMaterial ? "- Physics Material bounce = 0\n" : "") +
            (dongBoCaoDo ? $"- Đồng bộ mặt đường về Y = {chuan:F3}: chỉnh {daChinh} đoạn, bỏ qua {boQua} đoạn lệch quá dung sai (xem Console)\n" : "") +
            (loiMesh > 0 ? $"- {loiMesh} khúc cua không dựng được mesh mặt đường (xem Console)\n" : "");

        Debug.Log(msg);
        EditorUtility.DisplayDialog("Thành công!", msg, "OK");
    }

    // Box: mặt trên đúng bằng mặt đường, dày xuống dưới, nới mép để hết hở
    private Collider CaiBox(GameObject go, Info info)
    {
        MeshCollider mc = go.GetComponent<MeshCollider>();
        if (mc != null) Undo.DestroyObjectImmediate(mc);

        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider>(go);
        else Undo.RecordObject(bc, "Cập nhật Box Collider");

        Bounds lb = info.mf.sharedMesh.bounds;
        Vector3 size = lb.size;
        Vector3 center = lb.center;
        Vector3 scale = go.transform.lossyScale;

        for (int a = 0; a < 3; a++)
        {
            if (a == info.truc) continue;
            float s = Mathf.Max(0.0001f, Mathf.Abs(scale[a]));
            size[a] += 2f * moRongMep / s;
        }

        float day = doDayBox / info.scaleTruc;
        float matLocal = info.dau * info.h; // toạ độ mặt đường dọc theo trục dọc (local)

        size[info.truc] = day;
        center[info.truc] = info.dau > 0f ? matLocal - day * 0.5f : matLocal + day * 0.5f;

        bc.size = size;
        bc.center = center;
        bc.isTrigger = false;
        bc.enabled = true;
        return bc;
    }

    // Mesh Collider: dùng mesh chỉ gồm mặt đường đã ép phẳng
    private Collider CaiMesh(GameObject go, Info info)
    {
        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (bc != null) Undo.DestroyObjectImmediate(bc);

        Mesh meshMat = LayMeshMat(info);
        if (meshMat == null)
        {
            Debug.LogWarning($"[LỖI] {go.name}: không dựng được mesh mặt đường. Kiểm tra Read/Write của mesh gốc.", go);
            return null;
        }

        MeshCollider mc = go.GetComponent<MeshCollider>();
        if (mc == null) mc = Undo.AddComponent<MeshCollider>(go);
        else Undo.RecordObject(mc, "Cập nhật Mesh Collider");

        mc.sharedMesh = meshMat;
        mc.convex = false;
        mc.isTrigger = false;
        mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                            | MeshColliderCookingOptions.EnableMeshCleaning
                            | MeshColliderCookingOptions.UseFastMidphase;
        mc.enabled = true;
        return mc;
    }

    private Mesh LayMeshMat(Info info)
    {
        Mesh src = info.mf.sharedMesh;
        string key = $"{src.GetInstanceID()}_{info.truc}_{info.dau}";
        if (cacheMesh.TryGetValue(key, out Mesh daCo)) return daCo;

        Mesh moi = XayMeshMat(src, info);
        if (moi == null) { cacheMesh[key] = null; return null; }

        const string thuMuc = "Assets/RoadColliders";
        if (!AssetDatabase.IsValidFolder(thuMuc)) AssetDatabase.CreateFolder("Assets", "RoadColliders");

        string tenSach = Regex.Replace(src.name, @"[^\w\-]", "_");
        string duongDan = $"{thuMuc}/{tenSach}_{info.truc}{(info.dau > 0f ? "p" : "n")}_surface.asset";

        Mesh daLuu = AssetDatabase.LoadAssetAtPath<Mesh>(duongDan);
        if (daLuu != null)
        {
            EditorUtility.CopySerialized(moi, daLuu); // giữ nguyên GUID, không mất liên kết
            DestroyImmediate(moi);
            moi = daLuu;
        }
        else
        {
            AssetDatabase.CreateAsset(moi, duongDan);
        }

        cacheMesh[key] = moi;
        return moi;
    }

    private Mesh XayMeshMat(Mesh src, Info info)
    {
        Vector3[] v = src.vertices;
        int[] tr = src.triangles;
        Vector3 up = info.up;
        float h = info.h;
        float phang = dungSaiPhang / info.scaleTruc;
        float caoLe = (giuLeDuong ? doCaoLeToiDa : dungSaiPhang) / info.scaleTruc;

        var map = new Dictionary<int, int>();
        var nv = new List<Vector3>();
        var nt = new List<int>();

        for (int i = 0; i < tr.Length; i += 3)
        {
            int i0 = tr[i], i1 = tr[i + 1], i2 = tr[i + 2];
            Vector3 a = v[i0], b = v[i1], c = v[i2];

            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-14f) continue;
            float dot = Vector3.Dot(n.normalized, up);

            float ha = Vector3.Dot(a, up), hb = Vector3.Dot(b, up), hc = Vector3.Dot(c, up);
            float lo = Mathf.Min(ha, Mathf.Min(hb, hc));
            float hi = Mathf.Max(ha, Mathf.Max(hb, hc));

            bool ok = giuLeDuong
                ? (dot >= 0.5f && lo >= h - phang && hi <= h + caoLe)
                : (dot >= 0.95f && lo >= h - phang && hi <= h + phang);
            if (!ok) continue;

            int[] idx = { i0, i1, i2 };
            foreach (int id in idx)
            {
                if (!map.TryGetValue(id, out int ni))
                {
                    Vector3 p = v[id];
                    float hp = Vector3.Dot(p, up);
                    if (Mathf.Abs(hp - h) <= phang) p += up * (h - hp); // ép đúng mặt phẳng
                    ni = nv.Count;
                    nv.Add(p);
                    map[id] = ni;
                }
                nt.Add(ni);
            }
        }

        if (nt.Count == 0) return null;

        Mesh m = new Mesh { name = src.name + "_surface" };
        if (nv.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(nv);
        m.SetTriangles(nt, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private PhysicsMaterial LayHoacTaoMaterial()
    {
        const string duongDan = "Assets/Physics/RoadPhysics.asset";
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(duongDan);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder("Assets/Physics")) AssetDatabase.CreateFolder("Assets", "Physics");

        mat = new PhysicsMaterial("RoadPhysics")
        {
            bounciness = 0f,
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(mat, duongDan);
        return mat;
    }
}