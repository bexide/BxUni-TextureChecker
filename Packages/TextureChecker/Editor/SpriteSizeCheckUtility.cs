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

namespace BX
{
    /// <summary>
    /// Imageのサイズがスプライトのサイズと一致しているかを検証する
    /// </summary>
    public class SpriteSizeCheckUtility : EditorWindow
    {
        private enum InformationType
        {
            Info, Warning, Error,
        }

        private struct InformationEntry
        {
            public InformationType m_type;
            public string          m_assetPath;
            public string          m_objectPath;
            public string          m_text;
        }

        public DefaultAsset TargetFolder { get; set; }

        private string CurrentAssetPath  { get; set; }
        private string CurrentObjectPath { get; set; }

        private List<InformationEntry> Informations { get; set; }
        private bool                   IsCompleted  { get; set; }

        private void AddInformation(
            string          assetPath,
            string          objectPath,
            InformationType type,
            string          message)
        {
            Informations.Add(
                new InformationEntry
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
            AddInformation(CurrentAssetPath, CurrentObjectPath, InformationType.Warning, message);
        }

        private void AddInformationError(string message)
        {
            AddInformation(CurrentAssetPath, CurrentObjectPath, InformationType.Error, message);
        }

        private void AddAssetInformationWarning(string assetPath, string objectPath, string message)
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

        private void Initialize()
        {
            if (TargetFolder == null)
            {
                TargetFolder =
                    AssetDatabase.LoadAssetAtPath(
                        "Assets/Application",
                        typeof(DefaultAsset)) as DefaultAsset;
            }

            Texture2D LoadEditorRes(string path)
            {
                var icon = EditorGUIUtility.Load(path) as Texture2D;
                return icon;
            }

            m_errorIconSmall   = LoadEditorRes("icons/console.erroricon.sml.png");
            m_warningIconSmall = LoadEditorRes("icons/console.warnicon.sml.png");
            m_infoIconSmall    = LoadEditorRes("icons/console.infoicon.sml.png");
            var logStyle = new GUIStyle();

            Texture2D logBgOdd;
            Texture2D logBgEven;
            Texture2D logBgSelected;

            if (EditorGUIUtility.isProSkin)
            {
                logStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                logBgOdd = LoadEditorRes("builtin skins/darkskin/images/cn entrybackodd.png");
                logBgEven = LoadEditorRes("builtin skins/darkskin/images/cnentrybackeven.png");
                logBgSelected
                    = LoadEditorRes("builtin skins/darkskin/images/menuitemhover.png");
            }
            else
            {
                logStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
                logBgOdd = LoadEditorRes("builtin skins/lightskin/images/cn entrybackodd.png");
                logBgEven = LoadEditorRes("builtin skins/lightskin/images/cnentrybackeven.png");
                logBgSelected
                    = LoadEditorRes("builtin skins/lightskin/images/menuitemhover.png");
            }

            m_logStyleOdd                        = new GUIStyle(logStyle);
            m_logStyleEven                       = new GUIStyle(logStyle);
            m_logStyleSelected                   = new GUIStyle(logStyle);
            m_logStyleOdd.normal.background      = logBgOdd;
            m_logStyleEven.normal.background     = logBgEven;
            m_logStyleSelected.normal.background = logBgSelected;

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
        
        GUIStyle  m_logStyleOdd;
        GUIStyle  m_logStyleEven;
        GUIStyle  m_logStyleSelected;
        Texture2D m_icon;
        Texture2D m_errorIconSmall;
        Texture2D m_warningIconSmall;
        Texture2D m_infoIconSmall;

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
            Informations = new List<InformationEntry>();

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
                if (obj == null) { continue; }
                yield return CheckObject(obj);
            }
        }

        private IEnumerator CheckObject(UnityEngine.Object obj)
        {
            Debug.Log($"Object [{obj.name}] Type=[{obj.GetType()}]");
            CurrentObjectPath = obj.name;
#if false
            var serializedObject = new SerializedObject(obj);
            var property         = serializedObject.GetIterator();

            while (property.Next(true)) { yield return DumpSerializedProperty(property); }
            yield break;
#endif
            if (obj is Image image)
            {
                CheckImage(image);
            }
            yield break;
        }

        private void CheckImage(Image image)
        {
            var spriteSize = image.sprite.rect.size;
            var rectSize   = (image.gameObject.transform as RectTransform).sizeDelta;
            
            Debug.Log($"[{image.name}]:sprite=[{image.sprite.name}] spriteSize={spriteSize}, rectSize={rectSize}");

            if (spriteSize != rectSize)
            {
                AddInformationWarning("ImageサイズがSpriteサイズと一致しません");
            }
        }

        /// <summary>
        /// SerializedProperty の内容を表示する（デバッグ用）
        /// </summary>
        /// <param name="prop"></param>
        private IEnumerator DumpSerializedProperty(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
            case SerializedPropertyType.Generic:
                // 配列とObject
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Generic");
#if true
                var child = prop.Copy();
                var end   = prop.GetEndProperty(true);
                if (child.Next(true))
                {
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        yield return DumpSerializedProperty(child);
                        if (!child.Next(false))
                            break;
                    }
                }
#else
                Debug.Log($" snip.");
                prop = prop.GetEndProperty(true);
                yield break;
#endif
                break;
            case SerializedPropertyType.Integer:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Int({prop.intValue})");
                break;
            case SerializedPropertyType.Boolean:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Bool({prop.boolValue})");
                break;
            case SerializedPropertyType.Float:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Float({prop.floatValue})");
                break;
            case SerializedPropertyType.String:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:String({prop.stringValue})");
                break;
            case SerializedPropertyType.Color:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Color({prop.colorValue})");
                break;
            case SerializedPropertyType.ObjectReference:
                Debug.Log(
                    $"{prop.depth}:{prop.propertyPath}:Reference({prop.objectReferenceValue}),type={prop.type}");
#if true
                var child1 = prop.Copy();
                var end1   = prop.GetEndProperty(true);
                if (child1.Next(true))
                {
                    while (!SerializedProperty.EqualContents(child1, end1))
                    {
                        yield return DumpSerializedProperty(child1);
                        if (!child1.Next(false))
                            break;
                    }
                }
#endif
                break;
            case SerializedPropertyType.LayerMask:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:LayerMask({prop.intValue})");
                break;
            case SerializedPropertyType.Enum:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Enum({prop.enumValueIndex})");
                break;
            case SerializedPropertyType.Vector2:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Vector2({prop.vector2Value})");
                break;
            case SerializedPropertyType.Vector3:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Vector3({prop.vector3Value})");
                break;
            case SerializedPropertyType.Rect:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Rect({prop.rectValue})");
                break;
            case SerializedPropertyType.ArraySize:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:ArraySize({prop.intValue})");
                break;
            case SerializedPropertyType.Character:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Character");
                break;
            case SerializedPropertyType.AnimationCurve:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:AnimationCurve");
                break;
            case SerializedPropertyType.Bounds:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Bounds({prop.boundsValue})");
                break;
            case SerializedPropertyType.Gradient:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Gradient");
                break;
            case SerializedPropertyType.Quaternion:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Quaternion({prop.quaternionValue})");
                break;
            default:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:(other type={prop.propertyType})");
                break;

            }
        }
    }
}
