using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadPhysicsFixer : EditorWindow
{
    [SerializeField] private float bounceThreshold = 6f;       // Va chạm chậm hơn mức này sẽ không nảy
    [SerializeField] private float maxDepenetration = 2f;      // Tốc độ đẩy xe ra khi bị lún (thấp = hết "búng" lên)
    [SerializeField] private float fixedTimestep = 0.01f;      // 100 lần/giây, xe ôm đường mượt hơn mức mặc định 0.02
    [SerializeField] private int solverIterations = 12;
    [SerializeField] private int velocityIterations = 4;
    [SerializeField] private float contactOffset = 0.01f;
    [SerializeField] private float xoaColliderLeThap = 0.5f;  // Collider con trên đường thấp hơn mức này (so với mặt đường) bị coi là thừa

    [MenuItem("Tools/Sửa Vật Lý Xe & Đường")]
    public static void MoCuaSo()
    {
        GetWindow<RoadPhysicsFixer>("Vật lý xe & đường");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1. CÀI ĐẶT VẬT LÝ TOÀN DỰ ÁN", EditorStyles.boldLabel);
        bounceThreshold = EditorGUILayout.FloatField("Bounce Threshold", bounceThreshold);
        maxDepenetration = EditorGUILayout.FloatField("Max Depenetration Velocity", maxDepenetration);
        fixedTimestep = EditorGUILayout.FloatField("Fixed Timestep (s)", fixedTimestep);
        solverIterations = EditorGUILayout.IntField("Solver Iterations", solverIterations);
        velocityIterations = EditorGUILayout.IntField("Solver Velocity Iterations", velocityIterations);
        contactOffset = EditorGUILayout.FloatField("Default Contact Offset", contactOffset);
        if (GUILayout.Button("ÁP DỤNG CÀI ĐẶT VẬT LÝ", GUILayout.Height(30))) ApDungCaiDat();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("2. COLLIDER THỪA TRÊN ĐƯỜNG (gây gồ ghề / nảy)", EditorStyles.boldLabel);
        xoaColliderLeThap = EditorGUILayout.FloatField("Coi là thừa nếu cao dưới (m)", xoaColliderLeThap);
        if (GUILayout.Button("QUÉT & BÁO CÁO (không xóa)", GUILayout.Height(28))) QuetDuong(false);
        if (GUILayout.Button("QUÉT & XÓA COLLIDER THỪA TRÊN ĐƯỜNG", GUILayout.Height(28))) QuetDuong(true);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("3. SỬA XE", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Chọn PREFAB XE (trong Project, thường ở thư mục Resources) rồi bấm nút. Có thể chọn nhiều prefab.", MessageType.Info);
        if (GUILayout.Button("SỬA XE ĐANG CHỌN", GUILayout.Height(34))) SuaXe();
    }

    // =========================================================================
    // 1. CÀI ĐẶT VẬT LÝ
    // =========================================================================
    private static bool DatFloat(SerializedObject so, string ten, float giaTri)
    {
        SerializedProperty p = so.FindProperty(ten);
        if (p == null) { Debug.LogWarning($"Không tìm thấy thuộc tính {ten}"); return false; }
        p.floatValue = giaTri;
        return true;
    }

    private static bool DatInt(SerializedObject so, string ten, int giaTri)
    {
        SerializedProperty p = so.FindProperty(ten);
        if (p == null) { Debug.LogWarning($"Không tìm thấy thuộc tính {ten}"); return false; }
        p.intValue = giaTri;
        return true;
    }

    private void ApDungCaiDat()
    {
        Object[] dyn = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
        Object[] tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
        if (dyn.Length == 0 || tm.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không đọc được ProjectSettings. Hãy chỉnh tay trong Edit > Project Settings > Physics / Time.", "OK");
            return;
        }

        var sd = new SerializedObject(dyn[0]);
        DatFloat(sd, "m_BounceThreshold", bounceThreshold);
        DatFloat(sd, "m_DefaultMaxDepenetrationVelocity", maxDepenetration);
        DatFloat(sd, "m_DefaultContactOffset", contactOffset);
        DatInt(sd, "m_DefaultSolverIterations", solverIterations);
        DatInt(sd, "m_DefaultSolverVelocityIterations", velocityIterations);
        sd.ApplyModifiedProperties();

        var st = new SerializedObject(tm[0]);
        DatFloat(st, "Fixed Timestep", fixedTimestep);
        st.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Xong", "Đã cập nhật Physics và Time. Nếu Edit > Project Settings đang mở thì đóng/mở lại để thấy giá trị mới.", "OK");
    }

    // =========================================================================
    // 2. COLLIDER THỪA TRÊN ĐƯỜNG
    // =========================================================================

    // Trả về đoạn đường gần nhất (đi ngược lên cây cha) chứa transform này.
    // Đường của bạn lồng nhau, nên phải biết collider thuộc đoạn nào để không xóa nhầm collider của đoạn khác.
    private static GameObject ChuDuong(Transform t, HashSet<GameObject> tapDuong)
    {
        while (t != null)
        {
            if (tapDuong.Contains(t.gameObject)) return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    private void QuetDuong(bool xoa)
    {
        var roads = new List<GameObject>();
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (go.name.ToLower().Contains("road_") && go.GetComponent<MeshFilter>() != null)
                roads.Add(go);
        }

        if (roads.Count == 0)
        {
            EditorUtility.DisplayDialog("Quét đường", "Không thấy đoạn đường nào (tên chứa \"Road_\").", "OK");
            return;
        }

        var roadSet = new HashSet<GameObject>(roads);
        var tapCollider = new HashSet<Collider>();   // mọi collider thuộc cây đường
        var mainBounds = new List<Bounds>();
        int khongCoCollider = 0, conThua = 0, daXoa = 0, conNhieu = 0;

        Undo.IncrementCurrentGroup();
        int nhom = Undo.GetCurrentGroup();

        foreach (GameObject road in roads)
        {
            Collider main = road.GetComponent<Collider>();
            Collider[] tatCa = road.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in tatCa) tapCollider.Add(c);

            if (main == null)
            {
                khongCoCollider++;
                Debug.LogWarning($"[THIẾU COLLIDER] {road.name}: chưa có collider. Hãy chạy tool tối ưu đường trước.", road);
                continue;
            }

            mainBounds.Add(main.bounds);
            float matDuong = main.bounds.max.y;

            foreach (Collider c in tatCa)
            {
                if (c == main || c.isTrigger) continue;

                // Collider thuộc một đoạn đường khác (đường lồng nhau) -> tuyệt đối không đụng
                if (ChuDuong(c.transform, roadSet) != road) continue;

                if (c.bounds.max.y <= matDuong + xoaColliderLeThap)
                {
                    conThua++;
                    if (xoa)
                    {
                        Debug.Log($"[XÓA] Collider thừa trên {road.name}: {c.name} ({c.GetType().Name})", road);
                        Undo.DestroyObjectImmediate(c);
                        daXoa++;
                    }
                    else
                    {
                        Debug.LogWarning($"[THỪA] {road.name} có collider con thấp: {c.name} ({c.GetType().Name}) - chồng lên mặt đường", c.gameObject);
                    }
                }
                else conNhieu++;
            }
        }

        // Collider ngoài cây đường nhưng nằm chồng lên mặt đường (nền, mặt biển, trang trí...)
        int chongLen = 0;
        foreach (Collider c in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || tapCollider.Contains(c)) continue;
            if (EditorUtility.IsPersistent(c.gameObject) || !c.gameObject.scene.IsValid()) continue;
            if (c.GetComponentInParent<Rigidbody>() != null) continue;

            Bounds cb = c.bounds;
            foreach (Bounds rb in mainBounds)
            {
                if (!rb.Intersects(cb)) continue;
                // Chạm vào phần mặt đường: đỉnh collider nằm gần/cao hơn mặt đường một chút
                if (cb.max.y >= rb.max.y - 0.3f && cb.max.y <= rb.max.y + 0.5f)
                {
                    chongLen++;
                    Debug.LogWarning($"[CHỒNG LÊN ĐƯỜNG] {c.name} ({c.GetType().Name}) - kiểm tra, có thể cần tắt collider này (bấm vào dòng để chọn)", c.gameObject);
                    break;
                }
            }
        }

        Undo.CollapseUndoOperations(nhom);
        if (xoa) UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        string msg =
            $"Quét {roads.Count} đoạn đường.\n" +
            $"- Đoạn chưa có collider: {khongCoCollider}\n" +
            (xoa ? $"- Đã xóa {daXoa} collider con thừa thật sự\n"
                 : $"- Collider con thừa phát hiện: {conThua} (chưa xóa)\n") +
            $"- Collider cao (lan can, cột...) được giữ: {conNhieu}\n" +
            $"- Collider NGOÀI đường nhưng chồng lên mặt đường: {chongLen} (xem Console, tự quyết định tắt)";
        Debug.Log(msg);
        EditorUtility.DisplayDialog("Kết quả quét", msg, "OK");
    }

    // =========================================================================
    // 3. SỬA XE
    // =========================================================================
    private PhysicsMaterial LayHoacTaoMaterialXe()
    {
        const string duongDan = "Assets/Physics/VehiclePhysics.asset";
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(duongDan);
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Physics")) AssetDatabase.CreateFolder("Assets", "Physics");
            mat = new PhysicsMaterial("VehiclePhysics");
            AssetDatabase.CreateAsset(mat, duongDan);
        }

        // Xe di chuyển bằng cách đặt thẳng vận tốc nên không cần ma sát
        mat.bounciness = 0f;
        mat.dynamicFriction = 0f;
        mat.staticFriction = 0f;
        mat.frictionCombine = PhysicsMaterialCombine.Minimum; // luôn lấy ma sát thấp nhất giữa xe và đường
        mat.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private void SuaXe()
    {
        GameObject[] chon = Selection.GetFiltered<GameObject>(SelectionMode.Assets | SelectionMode.TopLevel);
        if (chon.Length == 0) chon = Selection.gameObjects;

        if (chon.Length == 0)
        {
            EditorUtility.DisplayDialog("Chưa chọn xe", "Hãy chọn prefab xe trong cửa sổ Project (hoặc xe trong Hierarchy) rồi bấm lại.", "OK");
            return;
        }

        PhysicsMaterial mat = LayHoacTaoMaterialXe();
        string baoCao = "";

        foreach (GameObject xe in chon)
        {
            Rigidbody[] rbs = xe.GetComponentsInChildren<Rigidbody>(true);
            Collider[] cols = xe.GetComponentsInChildren<Collider>(true);

            if (rbs.Length == 0)
            {
                baoCao += $"- {xe.name}: KHÔNG có Rigidbody, bỏ qua (chọn nhầm?)\n";
                continue;
            }

            foreach (Rigidbody rb in rbs)
            {
                Undo.RecordObject(rb, "Sửa Rigidbody xe");
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.maxDepenetrationVelocity = maxDepenetration;
                EditorUtility.SetDirty(rb);
            }

            int soCol = 0, mesh = 0;
            foreach (Collider c in cols)
            {
                if (c.isTrigger) continue;
                Undo.RecordObject(c, "Sửa Collider xe");
                c.sharedMaterial = mat;
                EditorUtility.SetDirty(c);
                soCol++;

                // MeshCollider không Convex trên Rigidbody động không va chạm đúng -> chuyển sang Convex
                MeshCollider mc = c as MeshCollider;
                if (mc != null)
                {
                    if (!mc.convex) mc.convex = true;
                    mesh++;
                }
            }

            baoCao += $"- {xe.name}: {rbs.Length} Rigidbody (Interpolate + Continuous Dynamic), {soCol} collider (bounce = 0, ma sát = 0)";
            if (mesh > 0) baoCao += $"\n  Đã chuyển {mesh} MeshCollider sang Convex (nếu xe vẫn lún thì thay bằng Box/Capsule)";
            baoCao += "\n";

            if (PrefabUtility.IsPartOfPrefabAsset(xe)) EditorUtility.SetDirty(xe);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Sửa xe:\n" + baoCao);
        EditorUtility.DisplayDialog("Đã sửa xe", baoCao, "OK");
    }
}