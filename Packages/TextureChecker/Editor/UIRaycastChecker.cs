// 2022-11-21 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BX.TextureChecker
{
    /// <summary>
    /// </summary>
    public class UIRaycastChecker : TextureCheckerBase
    {
        [MenuItem("BeXide/UI Raycast Status Check")]
        private static void Create()
        {
            var window =
                GetWindow<UIRaycastChecker>(
                    utility: true,
                    title: "UI Raycast Status Checker",
                    focus: true);
            window.Initialize();
        }

        // GUI表示内部情報
        private enum TargetMode
        {
            Prefab,
            Scene,
            Hierarchy,
        }
        private int m_mode;
        private string[]   k_modeTexts = Enum.GetNames(typeof(TargetMode));

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これは UI コンポーネント設定をチェックするツールです。",
                new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                });
            EditorGUILayout.Space();

            var newTarget =
                EditorGUILayout.ObjectField(
                    "対象フォルダ",
                    TargetFolder,
                    typeof(DefaultAsset),
                    allowSceneObjects: false);
            TargetFolder = newTarget as DefaultAsset;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("対象アセット種別");
            m_mode = GUILayout.SelectionGrid(
                m_mode,
                k_modeTexts,
                k_modeTexts.Length,
                new GUIStyle(EditorStyles.radioButton));
            EditorGUILayout.EndHorizontal();

            if (InformationList == null)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください。",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(InformationList == null);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { Clear(); }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawInformation();
        }

        private IEnumerator Execute()
        {
            InformationList = new List<InformationEntry>();

            switch ((TargetMode)m_mode)
            {
            case TargetMode.Prefab:
                yield return CheckPrefabs();
                break;

            case TargetMode.Scene:
                yield return CheckScenes();
                break;

            case TargetMode.Hierarchy:
                CurrentAssetPath = "";
                yield return CheckHierarchy();
                break;
            }

            IsCompleted = true;
        }

        private IEnumerator CheckPrefabs()
        {
            // Prefabを列挙
            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
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

                CurrentAssetPath = path;
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AssetDatabase.OpenAsset(prefabAsset);
                yield return CheckHierarchy();

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "プレファブを集計中",
                        $"{i + 1}/{guidsLength}",
                        (float)(i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private IEnumerator CheckScenes()
        {
            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
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
                CurrentAssetPath = path;
                yield return CheckHierarchy();

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "シーンを集計中",
                        $"{i + 1}/{guidsLength}",
                        (float)(i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// HierarchyのGameObjectをチェック
        /// </summary>
        private IEnumerator CheckHierarchy()
        {
            var gameObjects = GetGameObjectsInHierarchy();
            foreach (var go in gameObjects)
            {
                if (go.TryGetComponent<Graphic>(out var graphic))
                {
                    CheckGraphicRaycastTarget(graphic);
                    yield return null;
                }
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
            
        private void CheckGraphicRaycastTarget(Graphic graphic)
        {
            var rectTransform = graphic.gameObject.transform as RectTransform;
            Debug.Assert(rectTransform != null);

            if (rectTransform.TryGetComponent<IEventSystemHandler>(out _))
            {
                // このノードでイベントを処理
                return;
            }

            // イベントが伝播する元ノードを検索
            var eventCatcherNode = rectTransform.parent;
            while (eventCatcherNode != null &&
                   !eventCatcherNode.TryGetComponent<IEventSystemHandler>(out _))
            {
                eventCatcherNode = eventCatcherNode.parent;
            }

            if (eventCatcherNode == null)
            {
                if (graphic.raycastTarget)
                {
                    AddInformationWarning(
                        graphic.gameObject,
                        "EventHandlerに包含されないRaycastTargetが有効です");
                }
                return;
            }

            var rectWorldCorners = new Vector3[4];
            //rectTransform.ForceUpdateRectTransforms();
            rectTransform.GetWorldCorners(rectWorldCorners);

            var eventCatcherRectTransform
                = eventCatcherNode.gameObject.transform as RectTransform;
            Debug.Assert(eventCatcherRectTransform != null);

            var eventWorldCorners = new Vector3[4];
            //eventCatcherRectTransform.ForceUpdateRectTransforms();
            eventCatcherRectTransform.GetWorldCorners(eventWorldCorners);

            Debug.Log($" {graphic.name}:eventCatcher=[{eventCatcherNode.gameObject.name}]");
            //Debug.Log($"   catcher={string.Join(",",eventWorldCorners)}");
            //Debug.Log($"   rect   ={string.Join(",",rectWorldCorners)}");

            if (CornerContains(eventWorldCorners, rectWorldCorners) &&
                graphic.raycastTarget)
            {
                AddInformationWarning(
                    graphic.gameObject,
                    "親Rectに包含されているRaycastTargetが有効です");
            }

            bool CornerContains(Vector3[] outerCorners, Vector3[] innerCorners)
            {
                return innerCorners[0].x >= outerCorners[0].x &&
                       innerCorners[0].y >= outerCorners[0].y &&
                       innerCorners[2].x <= outerCorners[2].x &&
                       innerCorners[2].y <= outerCorners[2].y;
            }
        }
    }
}
