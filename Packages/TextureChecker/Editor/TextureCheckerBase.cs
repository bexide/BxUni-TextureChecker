﻿// 2022-11-17 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.U2D;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D;

namespace BX.TextureChecker
{
    public abstract class TextureCheckerBase : EditorWindow
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
        protected Texture2D m_errorIconSmall;
        protected Texture2D m_warningIconSmall;
        protected Texture2D m_infoIconSmall;

        MultiColumnHeader               m_columnHeader;
        MultiColumnHeaderState.Column[] m_columns;

        private Vector2 m_informationScrollPosition;

        private       int m_viewIndex = 0;
        private const int k_pageViews = 100;

        /// <summary>設定</summary>
        protected TextureCheckerSettings Settings { get; set; }

        /// <summary>アトラスに登録されているテクスチャ一覧</summary>
        protected Dictionary<string, bool> AtlasedTextureMap { get; set; }

        protected class InformationEntry
        {
            public InformationType Type          { get; }
            public GUID            AssetGuid     { get; }
            public string          ObjectPath    { get; }
            public HierarchyPath   HierarchyPath { get; }
            public string          Text          { get; }

            public InformationEntry(
                InformationType type,
                GUID            assetGuid,
                string          objectPath,
                HierarchyPath   hierarchyPath,
                string          text)
            {
                Type          = type;
                AssetGuid     = assetGuid;
                ObjectPath    = objectPath;
                HierarchyPath = hierarchyPath;
                Text          = text;
            }

            public string GetObjectString()
            {
                return HierarchyPath != null ? HierarchyPath.ToString() : ObjectPath;
            }

            public int GetObjectInstanceId()
            {
                UnityEngine.Object obj;
                if (HierarchyPath != null) { obj = HierarchyPath.GetGameObject(); }
                else { obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ObjectPath); }
                return obj.GetInstanceID();
            }
        }

        protected GUID CurrentAsset  { get; set; }

        protected List<InformationEntry> InformationList { get; set; }
        protected bool                   IsCompleted     { get; set; }

        /// <summary>
        /// 情報エントリを追加
        /// </summary>
        /// <param name="assetPath">アセット欄</param>
        /// <param name="objectPath">オブジェクト欄</param>
        /// <param name="hierarchyPath">オブジェクト欄</param>
        /// <param name="type">情報タイプ</param>
        /// <param name="message">メッセージ文字列</param>
        private void AddInformation(
            GUID            assetPath,
            string          objectPath,
            HierarchyPath   hierarchyPath,
            InformationType type,
            string          message)
        {
            InformationList.Add(
                new InformationEntry(
                    type,
                    assetPath,
                    objectPath,
                    hierarchyPath,
                    message));
        }

        protected void AddInformationLog(string objectPath, string message)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Info, message);
        }

        protected void AddInformationWarning(string objectPath, string message)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Warning, message);
        }

        protected void AddInformationError(string objectPath, string message)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Error, message);
        }

        protected void AddInformationLog(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Info,
                message);
        }

        protected void AddInformationWarning(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Warning,
                message);
        }

        protected void AddInformationError(GameObject gameObject, string message)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Error,
                message);
        }

        protected void AddInformationLog(string message)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Info, message);
        }

        protected void AddInformationWarning(string message)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Warning, message);
        }

        protected void AddInformationError(string message)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Error, message);
        }

        /// <summary>初期化</summary>
        protected virtual void Initialize(string checkerId)
        {
            LoadSettings(checkerId);
            InitializeIcons();
            InitializeLogStyle();
            InitializeMultiColumnHeader();
        }

        private void LoadSettings(string id)
        {
            string settingsPath = $"Assets/Editor/BxTextureChecker_{id}.asset";
            Settings = AssetDatabase.LoadAssetAtPath<TextureCheckerSettings>(settingsPath);

            if (Settings == null)
            {
                Settings = CreateInstance<TextureCheckerSettings>();
                CheckDirectory(settingsPath);
                AssetDatabase.CreateAsset(Settings, settingsPath);
            }

            if (Settings.TargetFolder == null)
            {
                Settings.TargetFolder = AssetDatabase.LoadAssetAtPath(
                    s_defaultPath,
                    typeof(DefaultAsset)) as DefaultAsset;
            }
        }

        private void CheckDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
            
        /// <summary>
        /// マルチカラムヘッダ初期化
        /// </summary>
        private void InitializeMultiColumnHeader()
        {
            m_columns = new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    width = 16,
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
        /// スタイル初期化
        /// </summary>
        private void InitializeLogStyle()
        {
            var logStyle = new GUIStyle();

            Texture2D logBgOdd;
            Texture2D logBgEven;
            Texture2D logBgSelected;

            if (EditorGUIUtility.isProSkin)
            {
                logStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                logBgOdd = EditorGUIUtility.Load(
                    "builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven
                    = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as
                        Texture2D;
                logBgSelected
                    = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png")
                        as Texture2D;
            }
            else
            {
                logStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
                logBgOdd = EditorGUIUtility.Load(
                    "builtin skins/lightskin/images/cn entrybackodd.png") as Texture2D;
                logBgEven
                    = EditorGUIUtility.Load("builtin skins/lightskin/images/cnentrybackeven.png") as
                        Texture2D;
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
        }

        /// <summary>
        /// アイコンリソース読み込み
        /// </summary>
        private void InitializeIcons()
        {
            m_errorIconSmall
                = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            m_warningIconSmall
                = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            m_infoIconSmall
                = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
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

            EditorGUILayout.Space(4);

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
                    info.Type == InformationType.Info    ? m_infoIconSmall :
                    info.Type == InformationType.Warning ? m_warningIconSmall :
                                                             m_errorIconSmall;

                var logStyle = even ? m_logStyleOdd : m_logStyleEven;
                even = !even;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    new GUIContent(icon),
                    GUILayout.Width(m_columnHeader.GetColumnRect(0).width-2));

                string assetPath = AssetDatabase.GUIDToAssetPath(info.AssetGuid);
                if (GUILayout.Button(
                        assetPath,
                        EditorStyles.objectField,
                        GUILayout.Width(m_columnHeader.GetColumnRect(1).width-2)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button(
                        info.GetObjectString(),
                        EditorStyles.objectField,
                        GUILayout.Width(m_columnHeader.GetColumnRect(2).width-2)))
                {
                    int instanceId = info.GetObjectInstanceId();
                    Selection.activeInstanceID = instanceId; 
                    EditorGUIUtility.PingObject(instanceId);
                }

                EditorGUILayout.LabelField(
                    info.Text,
                    GUILayout.Width(m_columnHeader.GetColumnRect(3).width-2));

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

        /// <summary>
        /// SpriteAtlas 情報を収集
        /// </summary>
        /// <returns></returns>
        protected IEnumerator CollectSpriteAtlas()
        {
            AtlasedTextureMap = new();

            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
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
                        CurrentAsset = new GUID(guid);
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
                AddInformationWarning("TightPackingではないSpriteAtlasです");
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
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(spritePath);
                        AddInformationError(
                            spritePath,
                            "SpriteのSpriteAtlasへの登録が重複しています");
                    }
                    else { AtlasedTextureMap[spriteGUID] = enableTightPacking; }
                }
            }
        }
    }
}