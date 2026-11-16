using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class XoaNhanVatVaMu
{
    private static readonly string[] CompanhCanXoa = { "RiderOnScooter", "PlayerEquipmentSync", "IKHelper" };

    private static bool LaNhanVatHoacMu(string ten)
    {
        string t = ten.Trim().ToLower();
        return t == "nhomnhanvat" || t == "nhommu"
            || t.StartsWith("character") || t.StartsWith("helmet") || t.Contains("t-pose")
            || t == "diemnon" || t.StartsWith("diemtay") || t.StartsWith("diemchan");
    }

    [MenuItem("Tools/Xóa Nhân Vật + Mũ khỏi xe")]
    public static void Chay()
    {
        GameObject[] chon = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        if (chon.Length == 0)
        {
            EditorUtility.DisplayDialog("Chưa chọn prefab", "Chọn prefab xe (Assets/Resources/Scooter) trong cửa sổ Project rồi chạy lại.", "OK");
            return;
        }

        foreach (GameObject asset in chon)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                // 1. Gom các object nhân vật / mũ / điểm bám tay chân
                var canXoa = new List<GameObject>();
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t == root.transform) continue;

                    bool thieuAsset = PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset
                                      && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject);

                    if (!LaNhanVatHoacMu(t.name) && !thieuAsset) continue;

                    // Không xóa nhầm object chứa điểm bám camera / icon minimap
                    bool chuaDoQuanTrong = false;
                    foreach (Transform con in t.GetComponentsInChildren<Transform>(true))
                    {
                        string c = con.name.Trim();
                        if (c == "pointFollowTarget" || c == "MinimapIcon") { chuaDoQuanTrong = true; break; }
                    }
                    if (chuaDoQuanTrong)
                    {
                        Debug.LogWarning($"[XÓA NHÂN VẬT] Bỏ qua '{t.name}' vì bên trong có pointFollowTarget/MinimapIcon.");
                        continue;
                    }

                    canXoa.Add(t.gameObject);
                }

                // 2. Đếm component thừa và script thiếu
                int soComp = 0, soScriptThieu = 0;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (Component c in t.GetComponents<Component>())
                    {
                        if (c == null) { soScriptThieu++; continue; }
                        if (System.Array.IndexOf(CompanhCanXoa, c.GetType().Name) >= 0) soComp++;
                    }
                }

                string ds = canXoa.Count == 0 ? "(không có)" : string.Join("\n- ", canXoa.ConvertAll(g => g.name));
                bool dongY = EditorUtility.DisplayDialog("Xóa nhân vật + mũ",
                    $"{path}\n\nObject sẽ xóa ({canXoa.Count}):\n- {ds}\n\nComponent thừa sẽ xóa: {soComp}\nScript thiếu: {soScriptThieu}\n\nTiếp tục?",
                    "Xóa", "Hủy");
                if (!dongY) continue;

                // 3. Xóa object
                int daXoa = 0;
                foreach (GameObject go in canXoa)
                {
                    if (go == null) continue; // đã bị xóa cùng cha
                    try { Object.DestroyImmediate(go); daXoa++; }
                    catch (System.Exception e) { Debug.LogWarning($"[XÓA NHÂN VẬT] Không xóa được '{go.name}': {e.Message}"); }
                }

                // 4. Xóa component thừa
                int daXoaComp = 0;
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && System.Array.IndexOf(CompanhCanXoa, mb.GetType().Name) >= 0)
                    {
                        Object.DestroyImmediate(mb);
                        daXoaComp++;
                    }
                }

                // 5. Script thiếu: hỏi trước vì có thể đó là script xe bị sai tên file
                int daXoaThieu = 0;
                if (soScriptThieu > 0 && EditorUtility.DisplayDialog("Script thiếu",
                    $"Còn {soScriptThieu} script thiếu.\nXóa luôn?\n\n(Chọn 'Giữ' nếu đó là script xe bị sai tên file. Khi đó đổi tên file cho khớp tên class thay vì xóa.)",
                    "Xóa", "Giữ"))
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        daXoaThieu += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);

                string kq = $"Đã xóa {daXoa} object, {daXoaComp} component thừa, {daXoaThieu} script thiếu.";
                Debug.Log($"[XÓA NHÂN VẬT] {path}: {kq}");
                EditorUtility.DisplayDialog("Xong", kq, "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
    }
}