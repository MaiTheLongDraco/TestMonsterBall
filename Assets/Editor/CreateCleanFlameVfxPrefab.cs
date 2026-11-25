using UnityEngine;
using UnityEditor;
using System.IO;

namespace Aiara.EditorTools
{
    public static class CreateCleanFlameVfxPrefab
    {
        private const string SourcePrefabPath = "Assets/UNI VFX/Characters & Artifacts/Prefabs/Destructive Elements Flame Skinned Mesh.prefab";
        private const string TargetDir = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs";
        private const string TargetPrefabPath = TargetDir + "/Clean_Flame_VFX.prefab";

        [MenuItem("Tools/StreetFighter/Extract Clean Flame VFX Prefab")]
        public static void ExtractPrefab()
        {
            if (!Directory.Exists(TargetDir))
            {
                Directory.CreateDirectory(TargetDir);
            }

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"[ExtractCleanFlame] Không tìm thấy source prefab tại {SourcePrefabPath}");
                return;
            }

            // Instantiate source prefab tạm thời
            var tempObj = Object.Instantiate(sourcePrefab);
            tempObj.name = "Temp_Extraction_Root";

            // Tìm GameObject mang VisualEffect hoặc tên chứa VFX
            Transform vfxChild = tempObj.transform.Find("Destructive Elements Flame VFX");
            if (vfxChild == null)
            {
                var allVfx = tempObj.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
                if (allVfx.Length > 0)
                {
                    vfxChild = allVfx[0].transform;
                }
            }

            if (vfxChild == null)
            {
                Debug.LogError("[ExtractCleanFlame] Không tìm thấy VisualEffect child trong source prefab!");
                Object.DestroyImmediate(tempObj);
                return;
            }

            // Tách con VFX ra khỏi root (loại bỏ hoàn toàn model Ellen)
            vfxChild.SetParent(null);
            vfxChild.name = "Clean_Flame_VFX";
            vfxChild.localPosition = Vector3.zero;
            vfxChild.localRotation = Quaternion.identity;
            vfxChild.localScale = Vector3.one;

            // Xóa ngay lập tức root cũ (chứa toàn bộ model Ellen và các xương)
            Object.DestroyImmediate(tempObj);

            // Kiểm tra và dọn dẹp bất kỳ mesh renderer nào nếu còn sót lại
            var meshFilters = vfxChild.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.gameObject.name.Contains("Ellen") || mf.gameObject.name.Contains("Mesh") || mf.gameObject.name.Contains("Key"))
                {
                    Object.DestroyImmediate(mf.gameObject);
                }
            }
            var skinnedMeshes = vfxChild.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var sm in skinnedMeshes)
            {
                if (sm.gameObject.name.Contains("Ellen") || sm.gameObject.name.Contains("Mesh") || sm.gameObject.name.Contains("Key"))
                {
                    Object.DestroyImmediate(sm.gameObject);
                }
            }

            // Gỡ bỏ tất cả missing scripts nếu có
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(vfxChild.gameObject);
            var allDescendants = vfxChild.GetComponentsInChildren<Transform>(true);
            foreach (var d in allDescendants)
            {
                if (d != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(d.gameObject);
                }
            }

            // Lưu thành Prefab sạch
            PrefabUtility.SaveAsPrefabAsset(vfxChild.gameObject, TargetPrefabPath);
            Object.DestroyImmediate(vfxChild.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ExtractCleanFlame] ĐÃ TẠO THÀNH CÔNG PREFAB NGỌN LỬA SẠCH TẠI: {TargetPrefabPath}");
        }
    }
}

