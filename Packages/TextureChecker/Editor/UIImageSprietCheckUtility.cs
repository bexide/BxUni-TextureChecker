// 2022-10-31 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace BX.TextureChecker
{
    /// <summary>
    /// ImageのRectのサイズがSpriteのサイズと一致しているかを検証する
    /// TightPackingされたSpriteAtlasに登録されているSpriteを使うImageにUseSpriteMeshが設定されているかを検証する
    /// </summary>
    public class UIImageSprietCheckUtility : TextureCheckerBase
    {
        [MenuItem("BeXide/UI Image Sprite Size Check")]
        private static void Create()
        {
            var window =
                GetWindow<UIImageSprietCheckUtility>(
                    utility: true,
                    title: "UI Sprite Size Checker",
                    focus: true);
            window.Initialize();
        }

        // GUI表示内部情報
        private int      m_mode;
        private string[] k_modeTexts = { "Prefab", "Scene", "Hierarchy" };

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これは UI Image コンポーネント設定をチェックするツールです。",
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
            yield return CollectSpriteAtlas();

            switch (m_mode)
            {
            case 0: // prefab
                yield return CheckPrefabs();
                break;

            case 1: // scene
                yield return CheckScenes();
                break;

            case 2: // hierarchy
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

                yield return CheckAssetAtPath(path);

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

        private IEnumerator CheckHierarchy()
        {
            var gameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject))
                .Where(obj => AssetDatabase.GetAssetOrScenePath(obj).Contains(".unity"))
                .OfType<GameObject>()
                .ToList();

            foreach (var go in gameObjects)
            {
                if (go.TryGetComponent<Image>(out var image))
                {
                    CurrentObjectPath = go.name;
                    CheckImage(image);
                    yield return null;
                }
            }
        }

        private IEnumerator CheckAssetAtPath(string path)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            Debug.Log($"Path [{path}] ({assetType})");

            CurrentAssetPath = path;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                if (obj is Image image)
                {
                    CurrentObjectPath = obj.name;
                    CheckImage(image);
                    yield return null;
                }
            }
        }

        private void CheckImage(Image image)
        {
            var sprite = image.sprite;
            if (sprite == null)
            {
                AddInformationError("ImageにSpriteが設定されていません");
                return;
            }

            var spriteSize = sprite.rect.size;
            var rectSize   = (image.gameObject.transform as RectTransform).sizeDelta;

            //Debug.Log($"[{image.name}]:sprite=[{image.sprite.name}] spriteSize={spriteSize}, rectSize={rectSize}");

            if ((image.type == Image.Type.Simple || image.type == Image.Type.Filled) &&
                spriteSize != rectSize) { AddInformationWarning("RectサイズがSpriteサイズと一致しません"); }

            if (image.type == Image.Type.Simple && !image.useSpriteMesh)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (AtlasedTextureMap.TryGetValue(guid, out bool enableTightPacking))
                {
                    if (enableTightPacking)
                    {
                        AddInformationWarning(
                            "TightPackingされたSpriteAtlasに登録されていますがUseSpriteMeshがOFFです");
                    }
                }
            }

        }
    }
}
