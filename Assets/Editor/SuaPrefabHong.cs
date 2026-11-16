using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SuaPrefabHong
{
    [MenuItem("Tools/Sửa Prefab Hỏng (script + prefab con thiếu)")]
    public static void Chay()
    {
        GameObject[] chon = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        if (chon.Length == 0)
        {
            EditorUtility.DisplayDialog("Chưa chọn prefab", "Chọn prefab (vd. Scooter) trong cửa sổ Project rồi chạy lại.", "OK");
            return;
        }

        foreach (GameObject asset in chon)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var thieu = new List<GameObject>();
                int scriptThieu = 0;

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;

                    if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset &&
                        PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    {
                        thieu.Add(go);
                        continue;
                    }

                    foreach (Component c in go.GetComponents<Component>())
                        if (c == null) scriptThieu++;
                }

                string ds = thieu.Count == 0 ? "(không có)" : string.Join("\n- ", thieu.ConvertAll(g => g.name));
                Debug.Log($"[SỬA PREFAB] {path}\nScript thiếu: {scriptThieu}\nPrefab con thiếu: {thieu.Count}\n- {ds}");

                if (scriptThieu == 0 && thieu.Count == 0)
                {
                    EditorUtility.DisplayDialog("Không có gì để sửa", path + " không có script/prefab con thiếu.", "OK");
                    continue;
                }

                bool xoaCon = thieu.Count > 0 && EditorUtility.DisplayDialog(
                    "Prefab con thiếu",
                    $"{thieu.Count} prefab con bị mất asset:\n- {ds}\n\nXóa chúng khỏi {asset.name}?\n(Chọn 'Giữ' nếu bạn sẽ tìm lại asset gốc.)",
                    "Xóa", "Giữ");

                if (xoaCon)
                    foreach (GameObject g in thieu) Object.DestroyImmediate(g);

                int daXoaScript = 0;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    daXoaScript += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[SỬA PREFAB] Đã xóa {daXoaScript} script thiếu" + (xoaCon ? $", {thieu.Count} prefab con thiếu." : "."));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
    }
}