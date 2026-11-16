
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

public class RoadObstacleOptimizer : EditorWindow
{
    private LayerMask groundMask;
    private Vector2 scroll;
    private readonly List<Collider> duong = new List<Collider>();
    private readonly List<Collider> vatCan = new List<Collider>();
    private readonly List<string> canhBao = new List<string>();

    private int soDaToiUu;
    private string baoCao = "Chưa quét Scene.";

    [MenuItem("Tools/Road & Obstacle Optimizer")]
    public static void OpenWindow()
    {
        GetWindow<RoadObstacleOptimizer>(
            "Road Optimizer"
        );
    }

    private void OnEnable()
    {
        int layer = LayerMask.NameToLayer("Ground");

        if (layer >= 0)
            groundMask = 1 << layer;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "ROAD & OBSTACLE OPTIMIZER",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Quét và tối ưu Collider đường và vật cản. " +
            "Không tự xóa Collider, không đổi hình dạng Collider " +
            "và không sửa Rigidbody của xe.",
            MessageType.Info
        );

        EditorGUILayout.Space(5);

        groundMask = EditorGUILayout.MaskField(
            "Layer mặt đường",
            groundMask,
            UnityEditorInternal.InternalEditorUtility.layers
        );

        EditorGUILayout.Space(8);

        if (GUILayout.Button("1. QUÉT TOÀN BỘ COLLIDER", GUILayout.Height(35)))
        {
            QuetScene();
        }

        GUI.enabled = duong.Count + vatCan.Count > 0;

        if (GUILayout.Button("2. TỐI ƯU COLLIDER AN TOÀN", GUILayout.Height(40)))
        {
            ToiUuCollider();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "KẾT QUẢ QUÉT",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField("Collider đường: " + duong.Count);
        EditorGUILayout.LabelField("Collider vật cản: " + vatCan.Count);
        EditorGUILayout.LabelField("Đã tối ưu: " + soDaToiUu);

        EditorGUILayout.Space(5);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.TextArea(
            baoCao,
            GUILayout.ExpandHeight(true),
            GUILayout.MinHeight(150)
        );

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("XÓA KẾT QUẢ QUÉT"))
        {
            duong.Clear();
            vatCan.Clear();
            canhBao.Clear();
            soDaToiUu = 0;
            baoCao = "Đã xóa kết quả.";
        }
    }

    // =========================================================
    // QUÉT SCENE
    // =========================================================

    private void QuetScene()
    {
        duong.Clear();
        vatCan.Clear();
        canhBao.Clear();

        Collider[] all =
            FindObjectsByType<Collider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int groundLayer = LayerMask.NameToLayer("Ground");

        foreach (Collider col in all)
        {
            if (col == null)
                continue;

            // Bỏ qua Collider của xe.
            if (col.GetComponentInParent<scooter>() != null)
                continue;

            bool laDuong =
                groundLayer >= 0 &&
                (groundMask.value & (1 << col.gameObject.layer)) != 0;

            if (laDuong)
                duong.Add(col);
            else
                vatCan.Add(col);

            KiemTraCollider(col, laDuong);
        }

        TaoBaoCao();

        Debug.Log(
            $"[RoadOptimizer] Đường: {duong.Count}, " +
            $"vật cản: {vatCan.Count}, cảnh báo: {canhBao.Count}"
        );
    }

    // =========================================================
    // KIỂM TRA COLLIDER
    // =========================================================

    private void KiemTraCollider(Collider col, bool laDuong)
    {
        string ten = LayTenDayDu(col.transform);

        if (!col.enabled)
        {
            canhBao.Add(
                $"[ĐANG TẮT] {ten}"
            );
        }

        if (col.isTrigger)
        {
            canhBao.Add(
                $"[TRIGGER] {ten} - kiểm tra xem có chủ ý không."
            );
        }

        if (col is BoxCollider box)
        {
            Vector3 size = box.size;

            if (size.x < 0.01f ||
                size.y < 0.01f ||
                size.z < 0.01f)
            {
                canhBao.Add(
                    $"[BOX QUÁ NHỎ] {ten} | size: {size}"
                );
            }
        }

        if (col is MeshCollider mesh)
        {
            Rigidbody attached =
                mesh.attachedRigidbody;

            if (attached != null &&
                !attached.isKinematic &&
                !mesh.convex)
            {
                canhBao.Add(
                    $"[MESH KHÔNG CONVEX + RIGIDBODY ĐỘNG] {ten}"
                );
            }

            if (laDuong && mesh.convex)
            {
                canhBao.Add(
                    $"[ĐƯỜNG CONVEX] {ten} - kiểm tra lại thiết lập."
                );
            }
        }

        if (laDuong &&
            col.attachedRigidbody != null &&
            !col.attachedRigidbody.isKinematic)
        {
            canhBao.Add(
                $"[ĐƯỜNG CÓ RIGIDBODY ĐỘNG] {ten}"
            );
        }

        if (col.contactOffset > 0.05f)
        {
            canhBao.Add(
                $"[CONTACT OFFSET CAO] {ten} | {col.contactOffset:F3}"
            );
        }
    }

    // =========================================================
    // TỐI ƯU AN TOÀN
    // =========================================================

    private void ToiUuCollider()
    {
        soDaToiUu = 0;

        int boQua = 0;

        Undo.SetCurrentGroupName(
            "Optimize Road And Obstacle Colliders"
        );

        int undoGroup = Undo.GetCurrentGroup();

        foreach (Collider col in duong)
        {
            if (col == null)
            {
                boQua++;
                continue;
            }

            if (ToiUuMotCollider(col, true))
                soDaToiUu++;
        }

        foreach (Collider col in vatCan)
        {
            if (col == null)
            {
                boQua++;
                continue;
            }

            if (ToiUuMotCollider(col, false))
                soDaToiUu++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        TaoBaoCao();

        Debug.Log(
            $"[RoadOptimizer] Hoàn tất. " +
            $"Đã tối ưu: {soDaToiUu}, bỏ qua: {boQua}"
        );

        EditorUtility.DisplayDialog(
            "Đã tối ưu Collider",
            $"Đã xử lý: {soDaToiUu}\n" +
            $"Bỏ qua: {boQua}\n" +
            $"Cảnh báo: {canhBao.Count}\n\n" +
            "Không có Collider nào bị tự động xóa.",
            "OK"
        );
    }

    private bool ToiUuMotCollider(
        Collider col,
        bool laDuong)
    {
        bool thayDoi = false;

        // Không thay đổi Collider đang tắt hoặc Trigger.
        // Có thể chúng đang được dùng có chủ ý.
        if (!col.enabled || col.isTrigger)
            return false;

        // Ghi nhận Undo trước khi sửa.
        Undo.RecordObject(col, "Optimize Collider");

        // Chuẩn hóa Contact Offset.
        float contactMoi = Mathf.Clamp(
            col.contactOffset,
            0.01f,
            0.02f
        );

        if (!Mathf.Approximately(
            col.contactOffset,
            contactMoi))
        {
            col.contactOffset = contactMoi;
            thayDoi = true;
        }

        // Tối ưu MeshCollider tĩnh hoặc Rigidbody Kinematic.
        if (col is MeshCollider mesh)
        {
            Rigidbody attached =
                mesh.attachedRigidbody;

            bool coTheToiUu =
                attached == null || attached.isKinematic;

            if (coTheToiUu)
            {
                MeshColliderCookingOptions options =
                    MeshColliderCookingOptions.CookForFasterSimulation |
                    MeshColliderCookingOptions.EnableMeshCleaning |
                    MeshColliderCookingOptions.WeldColocatedVertices;

                if (mesh.cookingOptions != options)
                {
                    mesh.cookingOptions = options;
                    thayDoi = true;
                }
            }
        }

        if (thayDoi)
        {
            EditorUtility.SetDirty(col);
            EditorUtility.SetDirty(col.gameObject);
        }

        return thayDoi;
    }

    // =========================================================
    // BÁO CÁO
    // =========================================================

    private void TaoBaoCao()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("========== ROAD COLLIDERS ==========");

        foreach (Collider c in duong)
        {
            if (c == null) continue;

            sb.AppendLine(
                $"{c.GetType().Name} | {LayTenDayDu(c.transform)}"
            );
        }

        sb.AppendLine();
        sb.AppendLine("========== OBSTACLE COLLIDERS ==========");

        foreach (Collider c in vatCan)
        {
            if (c == null) continue;

            sb.AppendLine(
                $"{c.GetType().Name} | {LayTenDayDu(c.transform)}"
            );
        }

        sb.AppendLine();
        sb.AppendLine("========== CẢNH BÁO ==========");

        if (canhBao.Count == 0)
        {
            sb.AppendLine("Không phát hiện cảnh báo.");
        }
        else
        {
            foreach (string s in canhBao)
                sb.AppendLine(s);
        }

        baoCao = sb.ToString();
    }

    private string LayTenDayDu(Transform t)
    {
        string ten = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            ten = t.name + "/" + ten;
        }

        return ten;
    }
}
