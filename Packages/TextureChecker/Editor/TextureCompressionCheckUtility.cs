using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.U2D;

using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace BX
{
    /// <summary>
    /// テクスチャアセットの圧縮設定をチェックするツール
    /// </summary>
    public class TextureCompressionCheckUtility : EditorWindow
    {
        private enum InformationType
        {
            Info,
            Warning,
            Error,
        }

        private struct InformationEntry
        {
            public InformationType m_type;
            public string m_path;
            public string m_text;
        }

        private readonly string[] k_platformStrings =
        {
            "Default", "Standalone", "Web", "iPhone", "Android", "WebGL", "Windows Store Apps",
            "PS4", "XboxOne", "Nintendo Switch", "tvOS",
        };

        public DefaultAsset TargetFolder { get; set; }

        private HashSet<string> AtlasedTextureGUIDs { get; set; }
        private string CurrentPath { get; set; }

        private List<InformationEntry> Informations { get; set; }
        private bool IsCompleted { get; set; } = false;

        private void AddInformation(string path, InformationType type, string message)
        {
            Informations.Add(new InformationEntry {m_path = path, m_type = type, m_text = message,});
        }

        private void AddInformationLog(string message)
        {
            AddInformation(CurrentPath, InformationType.Info, message);
        }

        private void AddInformationWarning(string message)
        {
            AddInformation(CurrentPath, InformationType.Warning, message);
        }

        private void AddInformationError(string message)
        {
            AddInformation(CurrentPath, InformationType.Error, message);
        }

        private void AddAssetInformationWarning(string path, string message)
        {
            CurrentPath = path;
            AddInformationWarning(message);
        }


        [MenuItem("BeXide/Texture Compression Check")]
        private static void Create()
        {
            var window =
                GetWindow<TextureCompressionCheckUtility>(utility: true, title: "Texture Compression Checker",
                    focus: true);
            window.Initialize();
        }

        private void Initialize()
        {
            if (TargetFolder == null)
            {
                TargetFolder =
                    AssetDatabase.LoadAssetAtPath("Assets/Application", typeof(DefaultAsset)) as DefaultAsset;
            }

            m_errorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            m_warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            m_infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            var logStyle = new GUIStyle();

            Texture2D logBgOdd;
            Texture2D logBgEven;
            Texture2D logBgSelected;

            if (EditorGUIUtility.isProSkin)
            {
                logStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                logBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;
            }
            else
            {
                logStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
                logBgOdd = EditorGUIUtility.Load("builtin skins/lightskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility.Load("builtin skins/lightskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected = EditorGUIUtility.Load("builtin skins/lightskin/images/menuitemhover.png") as Texture2D;
            }

            m_logStyleOdd = new GUIStyle(logStyle);
            m_logStyleEven = new GUIStyle(logStyle);
            m_logStyleSelected = new GUIStyle(logStyle);
            m_logStyleOdd.normal.background = logBgOdd;
            m_logStyleEven.normal.background = logBgEven;
            m_logStyleSelected.normal.background = logBgSelected;
        }

        // GUI表示内部情報
        GUIStyle m_logStyleOdd;
        GUIStyle m_logStyleEven;
        GUIStyle m_logStyleSelected;
        Texture2D m_icon;
        Texture2D m_errorIconSmall;
        Texture2D m_warningIconSmall;
        Texture2D m_infoIconSmall;

        private Vector2 m_informationScrollPosition;

        private int m_viewIndex = 0;
        private const int k_pageViews = 100;

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これはテクスチャアセットについて，圧縮状態をチェックするツールです。",
                new GUIStyle(GUI.skin.label) {wordWrap = true,});
            EditorGUILayout.Space();

            var newTarget =
                EditorGUILayout.ObjectField("対象フォルダ", TargetFolder, typeof(DefaultAsset), allowSceneObjects: false);
            TargetFolder = newTarget as DefaultAsset;

            if (Informations == null)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください。",
                    MessageType.Info);
            }

            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(Informations == null);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120)))
            {
                Clear();
            }

            EditorGUI.EndDisabledGroup();

            // 情報ウィンドウ
            if (Informations == null || !IsCompleted)
            {
                return;
            }

            if (Informations.Count == 0)
            {
                EditorGUILayout.HelpBox("見つかりませんでした。", MessageType.Warning);
                return;
            }

            m_informationScrollPosition = EditorGUILayout.BeginScrollView(
                m_informationScrollPosition, false, false);

            if (m_viewIndex > 0 &&
                GUILayout.Button("前のページ", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                m_viewIndex -= k_pageViews;
            }

            bool even = false;
            for (int i = m_viewIndex; i < Math.Min(m_viewIndex + k_pageViews, Informations.Count); i++)
            {
                var info = Informations[i];
                var icon =
                    info.m_type == InformationType.Info ? m_infoIconSmall :
                    info.m_type == InformationType.Warning ? m_warningIconSmall :
                    m_errorIconSmall;

                var logStyle = even ? m_logStyleOdd : m_logStyleEven;
                even = !even;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(icon), GUILayout.MaxWidth(32f));
                if (GUILayout.Button(info.m_path, EditorStyles.objectField,
                    GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.4f)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.m_path);
                    EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.LabelField(info.m_text);
                EditorGUILayout.EndHorizontal();
            }

            if (m_viewIndex + k_pageViews < Informations.Count &&
                GUILayout.Button("次のページ", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                m_viewIndex += k_pageViews;
                m_informationScrollPosition = Vector2.zero;
            }

            EditorGUILayout.EndScrollView();
        }

        private void Clear()
        {
            Informations = null;
            IsCompleted = false;
        }

        private IEnumerator Execute()
        {
            Informations = new List<InformationEntry>();

            yield return CollectSpriteAtlas();
            yield return CheckTexture2D();

            IsCompleted = true;
        }

        private IEnumerator CollectSpriteAtlas()
        {
            AtlasedTextureGUIDs = new HashSet<string>();

            // SpriteAtlas 情報を収集
            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
            string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] {targetPath});
            if (guids.Length <= 0)
            {
                yield break;
            }

            int guidsLength = guids.Length;
            for (int i = 0; i < guidsLength; i++)
            {
                string guid = guids[i];
                //Debug.Log($"[{guid}]");
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($" cannot get path from GUID [{guid}]");
                }
                else
                {
                    var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                    if (spriteAtlas == null)
                    {
                        Debug.LogError($" cannot load from path [{path}]");
                    }
                    else
                    {
                        yield return ReadSpriteAtlas(spriteAtlas);
                    }
                }

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar("read SpriteAtlas",
                    $"{i + 1}/{guidsLength}", (float) (i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// SpriteAtlasに含まれるSpriteのGUIDを収集する
        /// </summary>
        /// <param name="spriteAtlas"></param>
        /// <returns></returns>
        private IEnumerator ReadSpriteAtlas(SpriteAtlas spriteAtlas)
        {
            var serializedObject = new SerializedObject(spriteAtlas);
            var sizeProp = serializedObject.FindProperty("m_PackedSprites.Array.size");
            if (sizeProp != null && sizeProp.propertyType == SerializedPropertyType.ArraySize)
            {
                int size = sizeProp.intValue;
                for (int i = 0; i < size; i++)
                {
                    var dataProp = serializedObject.FindProperty($"m_PackedSprites.Array.data[{i}]");
                    if (dataProp != null)
                    {
                        string spritePath = AssetDatabase.GetAssetPath(dataProp.objectReferenceValue);
                        string spriteGUID = AssetDatabase.AssetPathToGUID(spritePath);
                        AtlasedTextureGUIDs.Add(spriteGUID);
                    }
                }

            }

            yield break;
        }

        private IEnumerator CheckTexture2D()
        {
            // テクスチャアセットを列挙
            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] {targetPath});
            if (guids.Length <= 0)
            {
                yield break;
            }

            int guidsLength = guids.Length;
            for (int i = 0; i < guidsLength; i++)
            {
                string guid = guids[i];
                //Debug.Log($"[{guid}]");
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($" cannot get path from GUID [{guid}]");
                    continue;
                }

                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter == null)
                {
                    Debug.LogError($" cannot get TextureImporter at [{path}]");
                    continue;
                }

                // テクスチャサイズ
                var widthAndHeight = GetWidthAndHeight(textureImporter);
                // サイズはPOT、もしくはPOTに丸められる設定か
                bool isPOT = (Mathf.IsPowerOfTwo(widthAndHeight.x) && Mathf.IsPowerOfTwo(widthAndHeight.y)) ||
                             (textureImporter.npotScale != TextureImporterNPOTScale.None);

                // 全プラットフォーム別のインポート設定を取得
                var settings = k_platformStrings.Select(platformString =>
                        platformString == "Default"
                            ? textureImporter.GetDefaultPlatformTextureSettings()
                            : textureImporter.GetPlatformTextureSettings(platformString))
                    .ToArray();
                settings[0].overridden = true;
                Debug.Assert(settings[0].overridden);

                if (AtlasedTextureGUIDs.Contains(guid))
                {
                    if (settings.Any(s => s.overridden
                                          && !IsRawFormat(s.format, s.textureCompression)))
                    {
                        var compressionMessage = new List<string>();
                        for (int j = 0; j < k_platformStrings.Length; j++)
                        {
                            var s = settings[j];
                            if (s.overridden && !IsRawFormat(s.format, s.textureCompression))
                            {
                                compressionMessage.Add($"{k_platformStrings[j]}={s.format}");
                            }
                        }

                        AddAssetInformationWarning(path,
                            $"アトラスに登録されたテクスチャが圧縮されています。({string.Join(",", compressionMessage)})");
                    }
                }
                else
                {
                    if (settings.Any(s => s.overridden
                                          && IsRawFormat(s.format, s.textureCompression)))
                    {
                        AddAssetInformationWarning(path, "独立したテクスチャが圧縮されていません。");
                    }
                    else if (!isPOT)
                    {
                        AddAssetInformationWarning(path, "独立したテクスチャの大きさがPOTではありません。");
                    }
                }

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar("集計中",
                    $"{i + 1}/{guidsLength}", (float) (i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private Vector2Int GetWidthAndHeight(TextureImporter importer)
        {
            object[] args = new object[2] {0, 0};
            var methodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight",
                BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(importer, args);

            return new Vector2Int((int) args[0], (int) args[1]);
        }

        private bool IsRawFormat(TextureImporterFormat format, TextureImporterCompression compression)
        {
            return
                format == TextureImporterFormat.ARGB32 ||
                format == TextureImporterFormat.RGBA32 ||
                format == TextureImporterFormat.RGB24 ||
                format == TextureImporterFormat.Alpha8 ||
                format == TextureImporterFormat.R8 ||
                (format == TextureImporterFormat.Automatic && compression == TextureImporterCompression.Uncompressed);
        }

        private IEnumerator DumpSprite(Sprite sprite)
        {
            var serializedObject = new SerializedObject(sprite);
            var iter = serializedObject.GetIterator();
            if (iter.Next(true))
            {
                do
                {
                    yield return DumpSerializedProperty(iter);
                } while (iter.Next(false));
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
                Debug.Log($"{prop.propertyPath}:Generic");
#if true
                var child = prop.Copy();
                var end = prop.GetEndProperty(true);
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
                Debug.Log($"{prop.propertyPath}:Int({prop.intValue})");
                break;
            case SerializedPropertyType.Boolean:
                Debug.Log($"{prop.propertyPath}:Bool({prop.boolValue})");
                break;
            case SerializedPropertyType.Float:
                Debug.Log($"{prop.propertyPath}:Float({prop.floatValue})");
                break;
            case SerializedPropertyType.String:
                Debug.Log($"{prop.propertyPath}:String({prop.stringValue})");
                break;
            case SerializedPropertyType.Color:
                Debug.Log($"{prop.propertyPath}:Color({prop.colorValue})");
                break;
            case SerializedPropertyType.ObjectReference:
                Debug.Log($"{prop.propertyPath}:Reference({prop.objectReferenceValue}),type={prop.type}");
#if true
                var child1 = prop.Copy();
                var end1 = prop.GetEndProperty(true);
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
                Debug.Log($"{prop.propertyPath}:LayerMask({prop.intValue})");
                break;
            case SerializedPropertyType.Enum:
                Debug.Log($"{prop.propertyPath}:Enum({prop.enumValueIndex})");
                break;
            case SerializedPropertyType.Vector2:
                Debug.Log($"{prop.propertyPath}:Vector2({prop.vector2Value})");
                break;
            case SerializedPropertyType.Vector3:
                Debug.Log($"{prop.propertyPath}:Vector3({prop.vector3Value})");
                break;
            case SerializedPropertyType.Rect:
                Debug.Log($"{prop.propertyPath}:Rect({prop.rectValue})");
                break;
            case SerializedPropertyType.ArraySize:
                Debug.Log($"{prop.propertyPath}:ArraySize({prop.intValue})");
                break;
            case SerializedPropertyType.Character:
                Debug.Log($"{prop.propertyPath}:Character");
                break;
            case SerializedPropertyType.AnimationCurve:
                Debug.Log($"{prop.propertyPath}:AnimationCurve");
                break;
            case SerializedPropertyType.Bounds:
                Debug.Log($"{prop.propertyPath}:Bounds({prop.boundsValue})");
                break;
            case SerializedPropertyType.Gradient:
                Debug.Log($"{prop.propertyPath}:Gradient");
                break;
            case SerializedPropertyType.Quaternion:
                Debug.Log($"{prop.propertyPath}:Quaternion({prop.quaternionValue})");
                break;
            default:
                Debug.Log($"{prop.propertyPath}:(other type={prop.propertyType})");
                break;

            }
        }
    }
}