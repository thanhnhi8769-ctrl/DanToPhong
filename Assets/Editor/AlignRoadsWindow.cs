using UnityEngine;
using UnityEditor;

public class AlignRoadsWindow : EditorWindow
{
    private float targetY = 0f;
    private bool resetRotations = true; // Tự động reset góc X và Z về 0 để tránh lệch gờ

    [MenuItem("Tools/Căn Chỉnh Mặt Đường (Align Roads Tool)")]
    public static void ShowWindow()
    {
        GetWindow<AlignRoadsWindow>("Align Roads Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("CÔNG CỤ CĂN CHỈNH ĐƯỜNG ĐUA", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetY = EditorGUILayout.FloatField("Độ cao Y mục tiêu:", targetY);
        resetRotations = EditorGUILayout.Toggle("Reset Rotation X & Z (Chống lệch gờ)", resetRotations);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Căn chỉnh các đối tượng ĐANG CHỌN", GUILayout.Height(35)))
        {
            AlignSelectedObjects();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("2. Căn chỉnh TẤT CẢ đường trong Scene (Chứa chữ 'road')", GUILayout.Height(35)))
        {
            AlignAllSceneRoads();
        }
    }

    private void AlignSelectedObjects()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Bạn chưa chọn đối tượng nào trong Hierarchy hoặc Scene!", "OK");
            return;
        }

        Undo.RecordObjects(selected, "Align Selected Roads");

        int count = 0;
        foreach (GameObject obj in selected)
        {
            Vector3 pos = obj.transform.position;
            pos.y = targetY;
            obj.transform.position = pos;

            if (resetRotations)
            {
                Vector3 rot = obj.transform.eulerAngles;
                rot.x = 0f;
                rot.z = 0f;
                obj.transform.eulerAngles = rot;
            }
            count++;
        }

        Debug.Log($"[Align Roads Tool] Đã căn chỉnh xong {count} đối tượng đang chọn!");
        EditorUtility.DisplayDialog("Thành công", $"Đã căn chỉnh xong {count} đối tượng!", "OK");
    }

    private void AlignAllSceneRoads()
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        int count = 0;

        foreach (Transform t in allTransforms)
        {
            // Kiểm tra tên object có chứa chữ "road" không (không phân biệt hoa thường)
            if (t.name.ToLower().Contains("road"))
            {
                Undo.RecordObject(t.gameObject, "Align Scene Roads");

                Vector3 pos = t.position;
                pos.y = targetY;
                t.position = pos;

                if (resetRotations)
                {
                    Vector3 rot = t.eulerAngles;
                    rot.x = 0f;
                    rot.z = 0f;
                    t.eulerAngles = rot;
                }
                count++;
            }
        }

        Debug.Log($"[Align Roads Tool] Đã quét và căn chỉnh {count} đoạn đường trong Scene về Y = {targetY}!");
        EditorUtility.DisplayDialog("Thành công", $"Đã quét và căn chỉnh xong {count} đoạn đường trong Scene!", "OK");
    }
}