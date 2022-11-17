// 2022-10-31 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using UnityEngine;
using UnityEngine.U2D;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace BX.TextureChecker
{
    /// <summary>
    /// Imageのサイズがスプライトのサイズと一致しているかを検証する
    /// </summary>
    public class SpriteSizeCheckUtility : TextureCheckerBase
    {
        private struct InformationEntry
        {
            public InformationType m_type;
            public string          m_assetPath;
            public string          m_objectPath;
            public string          m_text;
        }

        private string CurrentAssetPath  { get; set; }
        private string CurrentObjectPath { get; set; }

        private List<SpriteSizeCheckUtility.InformationEntry> Informations { get; set; }
        private bool                                          IsCompleted  { get; set; }

        private void AddInformation(
            string          assetPath,
            string          objectPath,
            InformationType type,
            string          message)
        {
            Informations.Add(
                new SpriteSizeCheckUtility.InformationEntry
                {
                    m_assetPath  = assetPath,
                    m_objectPath = objectPath,
                    m_type       = type,
                    m_text       = message,
                });
        }

        private void AddInformationLog(string message)
        {
            AddInformation(CurrentAssetPath, CurrentObjectPath, InformationType.Info, message);
        }

        private void AddInformationWarning(string message)
        {
            AddInformation(
                CurrentAssetPath,
                CurrentObjectPath,
                InformationType.Warning,
                message);
        }

        private void AddInformationError(string message)
        {
            AddInformation(CurrentAssetPath, CurrentObjectPath, InformationType.Error, message);
        }

        private void AddAssetInformationWarning(
            string assetPath,
            string objectPath,
            string message)
        {
            CurrentAssetPath  = assetPath;
            CurrentObjectPath = objectPath;
            AddInformationWarning(message);
        }


        [MenuItem("BeXide/UI Sprite Size Check")]
        private static void Create()
        {
            var window =
                GetWindow<SpriteSizeCheckUtility>(
                    utility: true,
                    title: "UI Sprite Size Checker",
                    focus: true);
            window.Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();

            // マルチカラムヘッダ
            m_columns = new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    width = 20,
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Asset Path"),
                    width               = 200,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Object"),
                    width               = 100,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Information"),
                    width               = 200,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
            };
            m_columnHeader
                = new MultiColumnHeader(new MultiColumnHeaderState(m_columns)) { height = 25 };
            m_columnHeader.ResizeToFit();
        }

        // GUI表示内部情報
        MultiColumnHeader               m_columnHeader;
        MultiColumnHeaderState.Column[] m_columns;

        private Vector2 m_informationScrollPosition;

        private       int m_viewIndex = 0;
        private const int k_pageViews = 100;

        private int      m_mode;
        private string[] k_modeTexts = { "Prefab", "Scene", "Hierarchy" };

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これは UI Image コンポーネントのサイズがSpriteと合致しているかをチェックするツールです。",
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

            if (Informations == null)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください。",
                    MessageType.Info);
            }
            else { DrawInformation(); }

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(Informations == null);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { Clear(); }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInformation()
        {
            // 情報ウィンドウ
            if (Informations == null || !IsCompleted) { return; }

            // カラムヘッダ
            var headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = m_columnHeader.height;
            float xScroll = 0;
            m_columnHeader.OnGUI(headerRect, xScroll);

            if (Informations.Count == 0)
            {
                EditorGUILayout.HelpBox("見つかりませんでした。", MessageType.Warning);
                return;
            }

            m_informationScrollPosition = EditorGUILayout.BeginScrollView(
                m_informationScrollPosition,
                false,
                false);

            if (m_viewIndex > 0 &&
                GUILayout.Button(
                    "前のページ",
                    GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                m_viewIndex -= k_pageViews;
            }

            bool even = false;
            for (int i = m_viewIndex;
                 i < Math.Min(m_viewIndex + k_pageViews, Informations.Count);
                 i++)
            {
                var info = Informations[i];
                var icon =
                    info.m_type == InformationType.Info    ? m_infoIconSmall :
                    info.m_type == InformationType.Warning ? m_warningIconSmall :
                                                             m_errorIconSmall;

                var logStyle = even ? m_logStyleOdd : m_logStyleEven;
                even = !even;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    new GUIContent(icon),
                    GUILayout.Width(m_columnHeader.GetColumnRect(0).width));

                if (GUILayout.Button(
                        info.m_assetPath,
                        EditorStyles.objectField,
                        GUILayout.Width(m_columnHeader.GetColumnRect(1).width)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        info.m_assetPath);
                    EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.LabelField(
                    info.m_objectPath,
                    GUILayout.Width(m_columnHeader.GetColumnRect(2).width));

                EditorGUILayout.LabelField(
                    info.m_text,
                    GUILayout.Width(m_columnHeader.GetColumnRect(3).width));

                EditorGUILayout.EndHorizontal();
            }

            if (m_viewIndex + k_pageViews < Informations.Count &&
                GUILayout.Button(
                    "次のページ",
                    GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                m_viewIndex                 += k_pageViews;
                m_informationScrollPosition =  Vector2.zero;
            }

            EditorGUILayout.EndScrollView();
        }

        private void Clear()
        {
            Informations = null;
            IsCompleted  = false;
        }

        private IEnumerator Execute()
        {
            Informations = new List<SpriteSizeCheckUtility.InformationEntry>();

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
                    yield return CheckObject(image);
                }
            }
        }

        private IEnumerator CheckAssetAtPath(string path)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            Debug.Log($"Path [{path}] ({assetType})");

            CurrentAssetPath = path;
#if false
            var importer = AssetImporter.GetAtPath(path);
            yield return CheckAsset(importer);
#endif
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                if (obj is Image image) { yield return CheckObject(image); }
            }
        }

        private IEnumerator CheckObject(UnityEngine.Object obj)
        {
            //Debug.Log($"Object [{obj.name}] Type=[{obj.GetType()}]");
            CurrentObjectPath = obj.name;
#if false
            var serializedObject = new SerializedObject(obj);
            var property = serializedObject.GetIterator();

            while (property.Next(true)) { yield return DumpSerializedProperty(property); }
            yield break;
#endif
            if (obj is Image image) { CheckImage(image); }
            yield break;
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
        }
    }
}
