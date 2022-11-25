// 2022-11-22 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;

namespace BX.TextureChecker
{
    /// <summary>
    /// コンポーネントの設定をチェックする機能のテンプレート
    /// </summary>
    internal abstract class UIComponentChecker : TextureCheckerBase
    {
        // GUI表示内部情報
        protected enum TargetMode
        {
            Prefab,
            Scene,
            Hierarchy,
        }
        protected int      m_mode;
        protected string[] k_modeTexts = Enum.GetNames(typeof(TargetMode));

        /// <summary>
        /// 機能の説明
        /// </summary>
        protected abstract string GetLabel();

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                GetLabel(),
                new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                });
            EditorGUILayout.Space();

            var newTarget =
                EditorGUILayout.ObjectField(
                    "対象フォルダ",
                    Settings.TargetFolder,
                    typeof(DefaultAsset),
                    allowSceneObjects: false);
            Settings.TargetFolder = newTarget as DefaultAsset;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("対象アセット種別");
            m_mode = GUILayout.SelectionGrid(
                m_mode,
                k_modeTexts,
                k_modeTexts.Length,
                new GUIStyle(EditorStyles.radioButton));
            EditorGUILayout.EndHorizontal();

            if (!IsCompleted)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください。",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                PreExecute();
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(!HasInformation);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { Clear(); }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawInformation();
        }

        private void PreExecute()
        {
            ClearInformation();
        }
        
        protected virtual IEnumerator Execute()
        {
            switch ((TargetMode)m_mode)
            {
            case TargetMode.Prefab:
                yield return CheckPrefabs();
                break;

            case TargetMode.Scene:
                yield return CheckScenes();
                break;

            case TargetMode.Hierarchy:
                CurrentAsset = default;
                CheckHierarchy();
                break;
            }

            Complete();
        }

        private IEnumerator CheckPrefabs()
        {
            // Prefabを列挙
            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { targetPath });
            if (prefabGuids.Length <= 0) { yield break; }

            int guidsLength = prefabGuids.Length;
            for (int i = 0; i < guidsLength; i++)
            {
                string guid = prefabGuids[i];
                //Debug.Log($"[{guid}]");
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($" cannot get path from GUID [{guid}]");
                    continue;
                }

                CurrentAsset = new GUID(guid);
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AssetDatabase.OpenAsset(prefabAsset);
                CheckHierarchy();

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "プレファブを集計中",
                        $"{i + 1}/{guidsLength}",
                        (float)(i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }

                yield return null;
            }

            EditorUtility.ClearProgressBar();
        }

        private IEnumerator CheckScenes()
        {
            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { targetPath });
            if (sceneGuids.Length <= 0) { yield break; }

            int guidsLength = sceneGuids.Length;
            for (int i = 0; i < guidsLength; i++)
            {
                string guid = sceneGuids[i];
                //Debug.Log($"[{guid}]");
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($" cannot get path from GUID [{guid}]");
                    continue;
                }
                Debug.Log($"Scene path[{path}]");

                EditorSceneManager.OpenScene(path);
                CurrentAsset = new GUID(guid);
                CheckHierarchy();

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "シーンを集計中",
                        $"{i + 1}/{guidsLength}",
                        (float)(i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }

                yield return null;
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// HierarchyのGameObjectをチェック
        /// </summary>
        private void CheckHierarchy()
        {
            var gameObjects = GetGameObjectsInHierarchy();
            foreach (var go in gameObjects)
            {
                CheckComponent(go);
            }
        }

        /// <summary>
        /// 現在開かれているHierarchyのGameObjectを取得 
        /// </summary>
        List<GameObject> GetGameObjectsInHierarchy()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                // Sceneモード
                return Resources.FindObjectsOfTypeAll(typeof(GameObject))
                    .Where(obj => AssetDatabase.GetAssetOrScenePath(obj).EndsWith(".unity"))
                    .Select(obj => obj as GameObject)
                    .ToList(); 
            }
            else
            {
                // Prefabモード
                return Resources.FindObjectsOfTypeAll(typeof(GameObject))
                    .Where(obj => string.IsNullOrEmpty(AssetDatabase.GetAssetOrScenePath(obj)))
                    .Select(obj => obj as GameObject)
                    .ToList();
            }
        }

        /// <summary>
        /// チェック関数本体
        /// </summary>
        /// <param name="gameObject">チェックするGameObject</param>
        protected abstract void CheckComponent(GameObject gameObject);
    }
}
