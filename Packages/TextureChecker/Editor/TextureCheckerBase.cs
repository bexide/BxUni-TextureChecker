// 2022-11-17 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D;

namespace BX.TextureChecker
{
    public class TextureCheckerBase : EditorWindow
    {
        protected static readonly string s_defaultPath = "Assets/Application";

        protected enum InformationType
        {
            Info, Warning, Error,
        }

        // GUI表示内部情報
        protected GUIStyle  m_logStyleOdd;
        protected GUIStyle  m_logStyleEven;
        protected GUIStyle  m_logStyleSelected;
        protected Texture2D m_icon;
        protected Texture2D m_errorIconSmall;
        protected Texture2D m_warningIconSmall;
        protected Texture2D m_infoIconSmall;

        MultiColumnHeader               m_columnHeader;
        MultiColumnHeaderState.Column[] m_columns;

        private Vector2 m_informationScrollPosition;

        private       int m_viewIndex = 0;
        private const int k_pageViews = 100;

        /// <summary>対象フォルダ</summary>
        protected DefaultAsset TargetFolder { get; set; }

        /// <summary>アトラスに登録されているテクスチャ一覧</summary>
        protected Dictionary<string, bool> AtlasedTextureMap { get; set; }

        protected struct InformationEntry
        {
            public InformationType m_type;
            public string          m_assetPath;
            public string          m_objectPath;
            public string          m_text;
        }

        protected string CurrentAssetPath  { get; set; }

        protected List<InformationEntry> InformationList { get; set; }
        protected bool                   IsCompleted     { get; set; }

        /// <summary>
        /// 情報エントリを追加
        /// </summary>
        /// <param name="assetPath">アセット欄</param>
        /// <param name="objectPath">オブジェクト欄</param>
        /// <param name="type">情報タイプ</param>
        /// <param name="message">メッセージ文字列</param>
        private void AddInformation(
            string          assetPath,
            string          objectPath,
            InformationType type,
            string          message)
        {
            InformationList.Add(
                new InformationEntry
                {
                    m_assetPath  = assetPath,
                    m_objectPath = objectPath,
                    m_type       = type,
                    m_text       = message,
                });
        }

        protected void AddInformationLog(string objectPath, string message)
        {
            AddInformation(
                CurrentAssetPath,
                objectPath,
                InformationType.Info,
                message);
        }

        protected void AddInformationWarning(string objectPath, string message)
        {
            AddInformation(
                CurrentAssetPath,
                objectPath,
                InformationType.Warning,
                message);
        }

        protected void AddInformationError(string objectPath, string message)
        {
            AddInformation(
                CurrentAssetPath,
                objectPath,
                InformationType.Error,
                message);
        }

        protected void AddInformationLog(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAssetPath,
                GetHierarchyPath(gameObject),
                InformationType.Info,
                message);
        }

        protected void AddInformationWarning(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAssetPath,
                GetHierarchyPath(gameObject),
                InformationType.Warning,
                message);
        }

        protected void AddInformationError(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAssetPath,
                GetHierarchyPath(gameObject),
                InformationType.Error,
                message);
        }

        public string GetHierarchyPath(GameObject gameObject)
        {
            var ancestors = new List<string>();
            for (var trans = gameObject.transform; trans != null; trans = trans.parent)
            {
                ancestors.Add(trans.name);
            }
            return string.Join("/", ancestors.AsEnumerable().Reverse());
        }

        /// <summary>
        /// 情報の描画
        /// </summary>
        protected void DrawInformation()
        {
            // 情報ウィンドウ
            if (InformationList == null || !IsCompleted) { return; }

            // カラムヘッダ
            var headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = m_columnHeader.height;
            float xScroll = 0;
            m_columnHeader.OnGUI(headerRect, xScroll);

            if (InformationList.Count == 0)
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
                 i < Math.Min(m_viewIndex + k_pageViews, InformationList.Count);
                 i++)
            {
                var info = InformationList[i];
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

            if (m_viewIndex + k_pageViews < InformationList.Count &&
                GUILayout.Button(
                    "次のページ",
                    GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                m_viewIndex                 += k_pageViews;
                m_informationScrollPosition =  Vector2.zero;
            }

            EditorGUILayout.EndScrollView();
        }

        protected void Clear()
        {
            InformationList = null;
            IsCompleted     = false;
        }

        /// <summary>初期化</summary>
        protected virtual void Initialize()
        {
            if (TargetFolder == null)
            {
                TargetFolder = AssetDatabase.LoadAssetAtPath(
                    s_defaultPath,
                    typeof(DefaultAsset)) as DefaultAsset;
            }

            m_errorIconSmall
                = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            m_warningIconSmall
                = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            m_infoIconSmall
                = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            var logStyle = new GUIStyle();

            Texture2D logBgOdd;
            Texture2D logBgEven;
            Texture2D logBgSelected;

            if (EditorGUIUtility.isProSkin)
            {
                logStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                logBgOdd = EditorGUIUtility.Load(
                    "builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility.Load(
                    "builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected
                    = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png")
                        as Texture2D;
            }
            else
            {
                logStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
                logBgOdd = EditorGUIUtility.Load(
                    "builtin skins/lightskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility.Load(
                    "builtin skins/lightskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected
                    = EditorGUIUtility.Load("builtin skins/lightskin/images/menuitemhover.png")
                        as Texture2D;
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

        /// <summary>
        /// SpriteAtlas 情報を収集
        /// </summary>
        /// <returns></returns>
        protected IEnumerator CollectSpriteAtlas()
        {
            AtlasedTextureMap = new();

            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { targetPath });
            if (guids.Length <= 0) { yield break; }

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
                        CurrentAssetPath = path;
                        yield return ReadSpriteAtlas(spriteAtlas);
                    }
                }

                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "read SpriteAtlas",
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
        /// SpriteAtlasに含まれるSpriteのGUIDを収集する
        /// </summary>
        /// <param name="spriteAtlas"></param>
        /// <returns></returns>
        protected IEnumerator ReadSpriteAtlas(SpriteAtlas spriteAtlas)
        {
            var  packingSettings    = spriteAtlas.GetPackingSettings();
            bool enableTightPacking = packingSettings.enableTightPacking;
            if (!enableTightPacking)
            {
                AddInformationWarning("", "TightPackingではないSpriteAtlasです");
            }

            var serializedObject = new SerializedObject(spriteAtlas);
            var sizeProp         = serializedObject.FindProperty("m_PackedSprites.Array.size");
            if (sizeProp == null || sizeProp.propertyType != SerializedPropertyType.ArraySize)
            {
                yield break;
            }

            int size = sizeProp.intValue;
            for (int i = 0; i < size; i++)
            {
                var dataProp
                    = serializedObject.FindProperty($"m_PackedSprites.Array.data[{i}]");
                if (dataProp != null)
                {
                    string spritePath
                        = AssetDatabase.GetAssetPath(dataProp.objectReferenceValue);
                    string spriteGUID = AssetDatabase.AssetPathToGUID(spritePath);

                    if (AtlasedTextureMap.TryGetValue(spriteGUID, out _))
                    {
                        AddInformationError(spritePath, "SpriteのSpriteAtlasへの登録が重複しています");
                    }
                    else { AtlasedTextureMap[spriteGUID] = enableTightPacking; }
                }
            }
        }
    }
}