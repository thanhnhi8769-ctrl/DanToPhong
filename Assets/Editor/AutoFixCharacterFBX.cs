
using UnityEngine;
using UnityEditor;
using System;

public class AutoFixCharacterFBX : EditorWindow
{
    [MenuItem("Tools/Character/Auto Fix Humanoid FBX")]
    public static void FixCharacterFBX()
    {
        string[] assets = AssetDatabase.FindAssets("t:Model");
        int fixedCount = 0;
        int skippedCount = 0;

        foreach (string guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Chỉ xử lý model có tên liên quan đến nhân vật.
            string name = fileName.ToLowerInvariant();

            bool isCharacter =
                name.Contains("character") ||
                name.Contains("t-pose") ||
                name.Contains("tpose");

            if (!isCharacter)
            {
                skippedCount++;
                continue;
            }

            ModelImporter importer =
                AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
                continue;

            try
            {
                importer.animationType =
                    ModelImporterAnimationType.Human;

                importer.avatarSetup =
                    ModelImporterAvatarSetup.CreateFromThisModel;

                importer.importAnimation = true;

                importer.SaveAndReimport();

                Debug.Log(
                    "[AUTO FIX] Đã cấu hình Humanoid: " + path
                );

                fixedCount++;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[AUTO FIX] Không thể sửa " + path +
                    "\n" + e.Message
                );
            }
        }

        AssetDatabase.Refresh();

        Debug.Log(
            "HOÀN TẤT! Đã cấu hình: " + fixedCount +
            " model. Bỏ qua: " + skippedCount +
            " model không khớp tên."
        );

        EditorUtility.DisplayDialog(
            "Auto Fix Character",
            "Đã xử lý " + fixedCount +
            " model nhân vật.\n" +
            "Kiểm tra Console để xem kết quả.",
            "OK"
        );
    }
}
