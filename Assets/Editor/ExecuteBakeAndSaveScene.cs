using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Aiara.Editor
{
    public static class ExecuteBakeAndSaveScene
    {
        [MenuItem("Tools/StreetFighter/Force Execute Bake Now")]
        public static void ForceExecute()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[ExecuteBake] Bắt đầu Bake Scene hoàn chỉnh...");
            SetupStreetFighterScene.BakeAndClean();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[ExecuteBake] ĐÃ BAKE VÀ LƯU THÀNH CÔNG SCENE!");
        }
    }
}

