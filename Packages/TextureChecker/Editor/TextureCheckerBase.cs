// 2022-11-17 BeXide,Inc.
// by Y.Hayashi

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D;

namespace BX.TextureChecker
{
    internal abstract class TextureCheckerBase : EditorWindow
    {
        protected static readonly string s_defaultPath = "Assets/Application";

        // GUI表示内部情報
        //protected GUIStyle   m_logStyleOdd;
        //protected GUIStyle   m_logStyleEven;
        //protected GUIStyle   m_logStyleSelected;
        protected Texture2D  m_errorIconSmall;
        protected Texture2D  m_warningIconSmall;
        protected Texture2D  m_infoIconSmall;
        protected GUIContent m_errorIconContent;
        protected GUIContent m_warningIconContent;
        protected GUIContent m_infoIconContent;

        MultiColumnHeader               m_columnHeader;
        MultiColumnHeaderState.Column[] m_columns;

        private Vector2 m_informationScrollPosition;

        private       int m_viewIndex = 0;
        private const int k_pageViews = 100;

        private float m_cachedPosition1;
        private bool  m_showIgnoreAssets;

        /// <summary>設定</summary>
        protected TextureCheckerSettings Settings { get; set; }

        /// <summary>アトラスに登録されているテクスチャ一覧</summary>
        protected Dictionary<string, bool> AtlasedTextureMap { get; set; }

        /// <summary>
        /// 検査結果のログレベル
        /// </summary>
        protected enum InformationType
        {
            Info, Warning, Error,
        }

        /// <summary>
        /// 検査結果のコードごとのテキスト
        /// </summary>
        protected Dictionary<string, string> CodeTextMap = new Dictionary<string, string>();

        /// <summary>
        /// 検査結果のエントリ
        /// </summary>
        protected class InformationEntry
        {
            public InformationType Type          { get; }
            public GUID            AssetGuid     { get; }
            public string          ObjectPath    { get; }
            public HierarchyPath   HierarchyPath { get; }
            public string          Code          { get; }
            public bool            Ignore        { get; set; }
            public bool?           NewIgnore     { get; set; }

            public InformationEntry(
                InformationType type,
                GUID            assetGuid,
                string          objectPath,
                HierarchyPath   hierarchyPath,
                string          code,
                bool            ignore)
            {
                Type          = type;
                AssetGuid     = assetGuid;
                ObjectPath    = objectPath;
                HierarchyPath = hierarchyPath;
                Code          = code;
                Ignore        = ignore;
            }

            public string GetObjectString()
            {
                return HierarchyPath != null ? HierarchyPath.ToString() : ObjectPath;
            }

            public int GetObjectInstanceId()
            {
                var obj = HierarchyPath != null
                    ? HierarchyPath.GetGameObject()
                    : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ObjectPath);

                return obj == null ? 0 : obj.GetInstanceID();
            }
        }

        /// <summary>
        /// 検査結果
        /// </summary>
        protected List<InformationEntry> InformationList { get; }
            = new List<InformationEntry>();

        /// <summary>
        /// 表示用（ソート済み）の検査結果
        /// </summary>
        protected List<InformationEntry> DisplayList { get; } = new List<InformationEntry>();

        /// <summary>
        /// アセットの無視・表示折りたたみ情報
        /// </summary>
        protected class AssetEntry
        {
            public GUID  Guid      { get; }
            public bool  Ignore    { get; set; }
            public bool? NewIgnore { get; set; }
            public bool  FoldOut   { get; set; }

            public AssetEntry(GUID guid, bool ignore)
            {
                Guid    = guid;
                Ignore  = ignore;
                FoldOut = false;
            }
        }

        protected Dictionary<GUID, AssetEntry> AssetStatusMap { get; }
            = new Dictionary<GUID, AssetEntry>();

        protected void ClearInformation()
        {
            InformationList.Clear();
            DisplayList.Clear();
            AssetStatusMap.Clear();
        }

        // 検査のステータス
        protected GUID CurrentAsset { get; set; }

        protected bool IsCompleted    { get; set; }
        protected bool HasInformation => InformationList.Count > 0;

        /// <summary>
        /// 情報エントリを追加
        /// </summary>
        /// <param name="assetPath">アセット欄</param>
        /// <param name="objectPath">オブジェクト欄</param>
        /// <param name="hierarchyPath">オブジェクト欄</param>
        /// <param name="type">情報タイプ</param>
        /// <param name="code">情報コード</param>
        private void AddInformation(
            GUID            assetPath,
            string          objectPath,
            HierarchyPath   hierarchyPath,
            InformationType type,
            string          code)
        {
            bool ignoreObject = Settings.IgnoreAssetObjectSet.Contains(
                new AssetObject(assetPath, objectPath, hierarchyPath));

            var entry = new InformationEntry(
                type,
                assetPath,
                objectPath,
                hierarchyPath,
                code,
                ignoreObject);

            InformationList.Add(entry);

            if (hierarchyPath != null || !string.IsNullOrEmpty(objectPath))
            {
                var guid = entry.AssetGuid;
                if (!AssetStatusMap.ContainsKey(guid))
                {
                    bool ignoreAsset = Settings.IgnoreAssetSet.Contains(guid);
                    AssetStatusMap.Add(guid, new AssetEntry(guid, ignoreAsset));
                }
            }
        }

        protected void AddInformationLog(string objectPath, string code)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Info, code);
        }

        protected void AddInformationWarning(string objectPath, string code)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Warning, code);
        }

        protected void AddInformationError(string objectPath, string code)
        {
            AddInformation(CurrentAsset, objectPath, null, InformationType.Error, code);
        }

        protected void AddInformationLog(GameObject gameObject, string code)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Info,
                code);
        }

        protected void AddInformationWarning(GameObject gameObject, string code)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Warning,
                code);
        }

        protected void AddInformationError(GameObject gameObject, string code)
        {
            AddInformation(
                CurrentAsset,
                string.Empty,
                HierarchyPath.Create(gameObject),
                InformationType.Error,
                code);
        }

        protected void AddInformationLog(string code)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Info, code);
        }

        protected void AddInformationWarning(string code)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Warning, code);
        }

        protected void AddInformationError(string code)
        {
            AddInformation(CurrentAsset, string.Empty, null, InformationType.Error, code);
        }

        /// <summary>初期化</summary>
        protected virtual void Initialize(string checkerId)
        {
            LoadSettings(checkerId);
            InitializeIcons();
            //InitializeLogStyle();
            InitializeMultiColumnHeader();
            InitializeCodeTexts();
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
                    width      = 20f,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Asset"),
                    width               = 100f,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Ignore"),
                    width               = 16f,
                    autoResize          = false,
                    headerTextAlignment = TextAlignment.Center
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Information"),
                    width               = 100f,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
            };
            m_columnHeader
                = new MultiColumnHeader(new MultiColumnHeaderState(m_columns)) { height = 25 };
            m_columnHeader.ResizeToFit();
            m_columnHeader.sortingChanged += OnSortingChanged;
        }

        /// <summary>
        /// ソートカラム変更
        /// </summary>
        /// <param name="columnHeader"></param>
        private void OnSortingChanged(MultiColumnHeader columnHeader)
        {
            UpdateDisplayList();
        }

#if false
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
                logBgOdd = EditorGUIUtility
                    .Load("builtin skins/darkskin/images/cnentrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility
                    .Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected = EditorGUIUtility
                    .Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;
            }
            else
            {
                logStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
                logBgOdd = EditorGUIUtility
                    .Load("builtin skins/lightskin/images/cnentrybackodd.png") as Texture2D;
                logBgEven = EditorGUIUtility
                    .Load("builtin skins/lightskin/images/cnentrybackeven.png") as Texture2D;
                logBgSelected = EditorGUIUtility
                    .Load("builtin skins/lightskin/images/menuitemhover.png") as Texture2D;
            }

            m_logStyleOdd                        = new GUIStyle(logStyle);
            m_logStyleEven                       = new GUIStyle(logStyle);
            m_logStyleSelected                   = new GUIStyle(logStyle);
            m_logStyleOdd.normal.background      = logBgOdd;
            m_logStyleEven.normal.background     = logBgEven;
            m_logStyleSelected.normal.background = logBgSelected;
        }
#endif

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

            m_errorIconContent   = new GUIContent(m_errorIconSmall);
            m_warningIconContent = new GUIContent(m_warningIconSmall);
            m_infoIconContent    = new GUIContent(m_infoIconSmall);
        }

        private void InitializeCodeTexts()
        {
            CodeTextMap["W001"] = "TightPackingではないSpriteAtlasです";
            CodeTextMap["E001"] = "SpriteのSpriteAtlasへの登録が重複しています";
        }

        /// <summary>
        /// 情報の描画
        /// </summary>
        protected void DrawInformation()
        {
            // 情報ウィンドウ
            if (!IsCompleted) { return; }

            // カラムヘッダ
            var headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = m_columnHeader.height;
            float xScroll = 0;
            m_columnHeader.OnGUI(headerRect, xScroll);

            EditorGUILayout.Space(4);

            if (DisplayList.Count == 0)
            {
                EditorGUILayout.HelpBox("見つかりませんでした。", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            int   maxPage     = (DisplayList.Count - 1) / k_pageViews + 1;
            int   curPage     = (m_viewIndex / k_pageViews) + 1;
            var   pageContent = new GUIContent($"Page {curPage}/{maxPage}");
            float pageWidth   = EditorStyles.label.CalcSize(pageContent).x;
            EditorGUILayout.LabelField(pageContent, GUILayout.Width(pageWidth));
            EditorGUI.BeginDisabledGroup(m_viewIndex <= 0);
            if (GUILayout.Button("前のページ", GUILayout.MaxWidth(200)))
            {
                m_viewIndex -= k_pageViews;
            }
            EditorGUI.EndDisabledGroup();

            if (Event.current.type == EventType.Repaint)
            {
                m_cachedPosition1 = GUILayoutUtility.GetLastRect().xMax + 2f;
            }
            float ignorePosition = Enumerable.Range(0, 2)
                .Select(i => m_columnHeader.GetColumnRect(i).width)
                .Sum();
            GUILayoutUtility.GetRect(ignorePosition - m_cachedPosition1, 1f);

            bool showIgnore = EditorGUILayout.ToggleLeft("無視を表示", m_showIgnoreAssets);
            if (showIgnore != m_showIgnoreAssets)
            {
                m_showIgnoreAssets = showIgnore;
                UpdateDisplayList();
                return;
            }
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUID lastGuid    = default;
            bool lastFoldOut = true;

            m_informationScrollPosition = EditorGUILayout.BeginScrollView(
                m_informationScrollPosition,
                false,
                false);

            //bool even = false;
            for (int i = m_viewIndex;
                 i < Math.Min(m_viewIndex + k_pageViews, DisplayList.Count);
                 i++)
            {
                var info = DisplayList[i];

                //var logStyle = even ? m_logStyleOdd : m_logStyleEven;
                //even = !even;

                if (info.HierarchyPath == null && string.IsNullOrEmpty(info.ObjectPath))
                {
                    // アセットのみ
                    lastGuid    = default;
                    lastFoldOut = true;
                }
                else if (info.AssetGuid != lastGuid)
                {
                    lastGuid = info.AssetGuid;
                    var assetStatus = AssetStatusMap[lastGuid];
                    lastFoldOut = assetStatus.FoldOut;
                    EditorGUILayout.BeginHorizontal();

                    GUILayoutUtility.GetRect(m_columnHeader.GetColumnRect(0).width, 22f);
                    var foldRect = GUILayoutUtility.GetLastRect();

                    // アセット FoldOut
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorStyles.foldout.Draw(
                            foldRect,
                            false,
                            true,
                            lastFoldOut,
                            false);
                    }
                    else if (Event.current.type == EventType.MouseDown &&
                             foldRect.Contains(Event.current.mousePosition))
                    {
                        assetStatus.FoldOut = lastFoldOut = !lastFoldOut;
                        Event.current.Use();
                    }

                    // Asset Field
                    string assetPath = AssetDatabase.GUIDToAssetPath(info.AssetGuid);
                    if (GUILayout.Button(
                            assetPath,
                            EditorStyles.objectField,
                            GUILayout.Width(m_columnHeader.GetColumnRect(1).width - 2f)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        EditorGUIUtility.PingObject(obj);
                    }

                    // Ignore Toggle Field
                    bool currentIgnore = assetStatus.NewIgnore ?? assetStatus.Ignore;
                    bool newIgnore = EditorGUILayout.Toggle(
                        currentIgnore,
                        GUILayout.Width(m_columnHeader.GetColumnRect(3).width - 2f));
                    if (newIgnore != currentIgnore) { assetStatus.NewIgnore = newIgnore; }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }

                if (lastFoldOut)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Information Type Icon
                    var iconContent = info.Type switch
                    {
                        InformationType.Info    => m_infoIconContent,
                        InformationType.Warning => m_warningIconContent,
                        _                       => m_errorIconContent
                    };
                    EditorGUILayout.LabelField(
                        iconContent,
                        GUILayout.Width(m_columnHeader.GetColumnRect(0).width - 2f));

                    // Asset Field
                    if (info.HierarchyPath == null && string.IsNullOrEmpty(info.ObjectPath))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(info.AssetGuid);
                        if (GUILayout.Button(
                                assetPath,
                                EditorStyles.objectField,
                                GUILayout.Width(m_columnHeader.GetColumnRect(1).width - 2f)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                                assetPath);
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    else
                    {
                        // Object Field
                        if (GUILayout.Button(
                                info.GetObjectString(),
                                EditorStyles.objectField,
                                GUILayout.Width(m_columnHeader.GetColumnRect(1).width - 2f)))
                        {
                            int instanceId = info.GetObjectInstanceId();
                            Selection.activeInstanceID = instanceId;
                            EditorGUIUtility.PingObject(instanceId);
                        }
                    }

                    // Ignore Toggle Field
                    bool currentIgnore = info.NewIgnore ?? info.Ignore;
                    bool newIgnore = EditorGUILayout.Toggle(
                        currentIgnore,
                        GUILayout.Width(m_columnHeader.GetColumnRect(2).width - 2f));
                    if (newIgnore != currentIgnore) { info.NewIgnore = newIgnore; }

                    // Information Text Field
                    EditorGUILayout.LabelField(
                        CodeTextMap[info.Code],
                        GUILayout.Width(m_columnHeader.GetColumnRect(3).width - 2f));

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(pageContent, GUILayout.Width(pageWidth));
            EditorGUI.BeginDisabledGroup(m_viewIndex + k_pageViews >= DisplayList.Count);
            if (GUILayout.Button("次のページ", GUILayout.MaxWidth(200)))
            {
                m_viewIndex                 += k_pageViews;
                m_informationScrollPosition =  Vector2.zero;
            }
            EditorGUI.EndDisabledGroup();
            GUILayoutUtility.GetRect(ignorePosition - m_cachedPosition1, 1);
            if (GUILayout.Button("無視フラグ保存", GUILayout.MaxWidth(200))) { SaveIgnoreFlags(); }
            if (GUILayout.Button("無視フラグクリア", GUILayout.MaxWidth(200))) { ClearIgnoreFlags(); }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// InformationListが完成したときの処理
        /// </summary>
        protected void Complete()
        {
            IsCompleted = true;
            UpdateDisplayList();
        }

        protected void UpdateDisplayList()
        {
            DisplayList.Clear();
            var sortedInformation = GetSortedInformationList();
            if (m_showIgnoreAssets)
            {
                // 無視対象も表示
                foreach (var info in sortedInformation) { DisplayList.Add(info); }
            }
            else
            {
                // 無視対象をフィルタ
                foreach (var info in sortedInformation.Where(
                             info => !info.Ignore &&
                                     ((info.HierarchyPath == null &&
                                       string.IsNullOrEmpty(info.ObjectPath)) ||
                                      !AssetStatusMap[info.AssetGuid].Ignore)))
                {
                    DisplayList.Add(info);
                }
            }
        }

        protected IEnumerable<InformationEntry> GetSortedInformationList()
        {
            int[] sortedColumns = m_columnHeader.state.sortedColumns;
            if (m_columnHeader.sortedColumnIndex < 0 ||
                sortedColumns.Length == 0) { return InformationList; }

            IOrderedEnumerable<InformationEntry> sortedInformation = null;
            foreach (int columnIndex in sortedColumns)
            {
                bool isAscending = m_columnHeader.IsSortedAscending(columnIndex);
                var  selector    = GetSelector(columnIndex);

                if (sortedInformation == null)
                {
                    sortedInformation = isAscending
                        ? InformationList.OrderBy(selector)
                        : InformationList.OrderByDescending(selector);
                }
                else
                {
                    sortedInformation = isAscending
                        ? sortedInformation.ThenBy(selector)
                        : sortedInformation.ThenByDescending(selector);
                }
            }

            return sortedInformation;

            Func<InformationEntry, string> GetSelector(int columnIndex)
            {
                return columnIndex switch
                {
                    1 => info => AssetDatabase.GUIDToAssetPath(info.AssetGuid),
                    3 => info => info.Code,
                    _ => info => string.Empty
                };
            }
        }

        /// <summary>
        /// 変化のあった無視フラグを保存
        /// </summary>
        private void SaveIgnoreFlags()
        {
            foreach (var info in DisplayList)
            {
                if (info.NewIgnore is { } ignore)
                {
                    info.Ignore = ignore;
                    Settings.SetIgnoreAssetObject(
                        new AssetObject(info.AssetGuid, info.ObjectPath, info.HierarchyPath),
                        ignore);
                    info.NewIgnore = null;
                }
            }

            foreach (var status in AssetStatusMap.Values)
            {
                if (status.NewIgnore is { } ignore)
                {
                    status.Ignore = ignore;
                    Settings.SetIgnoreAsset(status.Guid, ignore);
                    status.NewIgnore = null;
                }
            }

            EditorUtility.SetDirty(Settings);
            UpdateDisplayList();
        }

        /// <summary>
        /// 全ての無視フラグをクリア
        /// </summary>
        private void ClearIgnoreFlags()
        {
            foreach (var info in InformationList)
            {
                info.Ignore    = false;
                info.NewIgnore = null;
            }

            foreach (var guid in AssetStatusMap.Keys)
            {
                AssetStatusMap[guid].Ignore    = false;
                AssetStatusMap[guid].NewIgnore = null;
            }

            Settings.ClearIgnoreSet();
            EditorUtility.SetDirty(Settings);
            UpdateDisplayList();
        }

        protected virtual void Clear()
        {
            ClearInformation();
            IsCompleted = false;
        }

        /// <summary>
        /// SpriteAtlas 情報を収集
        /// </summary>
        /// <returns></returns>
        protected IEnumerator CollectSpriteAtlas()
        {
            AtlasedTextureMap = new Dictionary<string, bool>();

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
            if (!enableTightPacking) { AddInformationWarning("W001"); }

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
                        AddInformationError(spritePath, "E001");
                    }
                    else { AtlasedTextureMap[spriteGUID] = enableTightPacking; }
                }
            }
        }
    }
}