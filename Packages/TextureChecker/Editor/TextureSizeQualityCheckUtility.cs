using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using Unity.Mathematics;

namespace BX.TextureChecker
{
    public class TextureSizeQualityCheckUtility : EditorWindow
    {
        static readonly string DefaultPath = "Assets/Application";

        static readonly string HPFShaderName = "Unlit/Highpass3x3";

        private enum InformationType
        {
            Info, Warning, Error,
        }

        private struct InformationEntry
        {
            public InformationType m_type;
            public Object m_object;
            public string m_text;
        }

        public DefaultAsset TargetFolder { get; set; }

        public float ResultThreshold { get; set; } = 0.2f;

        public int SizeThreshold { get; set; } = 32;

        private Texture2D CurrentAsset { get; set; }

        private List<InformationEntry> Informations { get; set; }
        private bool IsCompleted { get; set; } = false;
        private int SortTypeIndex { get; set; }
        static readonly string[] SortTypeNames = {"ピーク値","ピクセル数","パス"};

        private void AddInformation(Object obj, InformationType type, string message)
        {
            Informations.Add(new InformationEntry { m_object = obj, m_type = type, m_text = message, });
        }
        private void AddInformationLog(string message)
        {
            AddInformation(CurrentAsset, InformationType.Info, message);
        }
        private void AddInformationWarning(string message)
        {
            AddInformation(CurrentAsset, InformationType.Warning, message);
        }
        private void AddInformationError(string message)
        {
            AddInformation(CurrentAsset, InformationType.Error, message);
        }

        private Shader HighPassShader { get; set; }
        private Material HighPassMaterial { get; set; }

        private struct CheckResultType
        {
            public Object m_obj;
            public float m_error;
            public long m_pixelCount;
        }
        private List<CheckResultType> CheckResults { get; set; }

        [MenuItem("BeXide/Texture size quality check")]
        private static void Create()
        {
            var window = GetWindow<TextureSizeQualityCheckUtility>(utility: true, title: "Texture Size Quality Checker", focus: true);
            window.Initialize();
        }

        private void Initialize()
        {
            if (TargetFolder == null)
            {
                TargetFolder = AssetDatabase.LoadAssetAtPath(DefaultPath, typeof(DefaultAsset)) as DefaultAsset;
            }

            HighPassShader = Shader.Find(HPFShaderName);
            Debug.Assert(HighPassShader != null);
            HighPassMaterial = new Material(HighPassShader);

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

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これはテクスチャの大きさが内容に対して大きすぎないかをチェックするツールです。",
                new GUIStyle(GUI.skin.label) { wordWrap = true, });
            EditorGUILayout.Space();

            var newTarget = EditorGUILayout.ObjectField("対象フォルダ", TargetFolder, typeof(DefaultAsset), allowSceneObjects: false);
            TargetFolder = newTarget as DefaultAsset;

            SizeThreshold = EditorGUILayout.IntSlider("サイズしきい値", SizeThreshold, 0, 512);

            ResultThreshold = EditorGUILayout.Slider("結果しきい値", ResultThreshold, 0f, 5f);

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
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120)))
            {
                Clear();
            }
            if (GUILayout.Button("保存", GUILayout.MaxWidth(120)))
            {
                Save();
            }
            EditorGUILayout.Space();
            int newIndex = EditorGUILayout.Popup("ソート", SortTypeIndex, SortTypeNames);
            if (newIndex != SortTypeIndex)
            {
                SortTypeIndex = newIndex;
                ChangeSortType();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // 情報ウィンドウ
            if (Informations != null)
            {
                if (Informations.Any())
                {
                    m_informationScrollPosition = EditorGUILayout.BeginScrollView(
                    m_informationScrollPosition, false, false);

                    bool even = false;
                    foreach (var info in Informations)
                    {
                        var icon =
                            info.m_type == InformationType.Info ? m_infoIconSmall :
                            info.m_type == InformationType.Warning ? m_warningIconSmall :
                            m_errorIconSmall;

                        var logStyle = even ? m_logStyleOdd : m_logStyleEven;
                        even = !even;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent(icon), GUILayout.MaxWidth(32f));
                        EditorGUILayout.ObjectField(info.m_object, info.m_object.GetType(), allowSceneObjects: false);
                        EditorGUILayout.LabelField(info.m_text);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
                else if (IsCompleted)
                {
                    EditorGUILayout.HelpBox("見つかりませんでした。", MessageType.Warning);
                }
            }
        }

        private void Clear()
        {
            Informations = null;
            CheckResults = null;
            IsCompleted = false;
        }

        private IEnumerator Execute()
        {
            Informations = new List<InformationEntry>();
            CheckResults = new List<CheckResultType>();

            yield return CheckTexture2D();

            IsCompleted = true;
        }

        private IEnumerator CheckTexture2D()
        {
            // テクスチャアセットを列挙
            string targetPath = AssetDatabase.GetAssetPath(TargetFolder);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { targetPath });
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
                    //DebugLabelReplace(path);
                    var texture2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture2d == null)
                    {
                        Debug.LogError($" cannot load from path [{path}]");
                    }
                    else
                    {
                        CurrentAsset = texture2d;
                        yield return CheckTexture2D(texture2d);
                    }
                }
                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar("集計中",
                    $"{i + 1}/{guidsLength}", (float)(i + 1) / guidsLength))
                {
                    // キャンセルされた
                    break;
                }
            }
            EditorUtility.ClearProgressBar();

            // ソートと表示
            ChangeSortType();
        }

        private void ChangeSortType()
        {
            Informations.Clear();
            IOrderedEnumerable<CheckResultType> results;
            switch (SortTypeIndex)
            {
            case 0:
                results = CheckResults.OrderBy(r => r.m_error);
                break;
            case 1:
                results = CheckResults.OrderByDescending(r => r.m_pixelCount);
                break;
            default:
                results = CheckResults.OrderBy(r => AssetDatabase.GetAssetPath(r.m_obj));
                break;
            }
            foreach (var result in results)
            {
                AddInformation(result.m_obj, InformationType.Warning, $"\te={result.m_error}");
            }
        }

        private void Save()
        {
            string filePath = EditorUtility.SaveFilePanel("名前を付けて結果を保存", "", "TextureQualityCheckerResult", "txt");
            using (var stream = new System.IO.StreamWriter(filePath))
            {
                foreach (var info in Informations)
                {
                    stream.WriteLine(AssetDatabase.GetAssetPath(info.m_object));
                }
            }
        }

        /// <summary>
        /// ひとつのテクスチャについて調査
        /// </summary>
        private IEnumerator CheckTexture2D(Texture2D texture2d)
        {
            //AddInformationLog($" format:{texture2d.format}");

            if (texture2d.width <= SizeThreshold &&
                texture2d.height <= SizeThreshold)
            {
                // 縦横サイズがどちらもしきい値より小さい
                yield break;
            }


            var filteredImage = GetFilteredImage(texture2d);

            float m = GetImageMagnitude(filteredImage);
            if (m < ResultThreshold)
            {
                //AddInformationWarning($" e={e}");
                long pixelCount = texture2d.width * texture2d.height;
                CheckResults.Add(new CheckResultType { m_obj = CurrentAsset, m_error = m, m_pixelCount = pixelCount });
            }

            yield break;
        }

        private Texture2D GetFilteredImage(Texture2D texture)
        {
            int w = texture.width;
            int h = texture.height;

            var tempRT = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(texture, tempRT, HighPassMaterial);

            var previousRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            var ret = new Texture2D(w, h);
            ret.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            ret.Apply();
            RenderTexture.active = previousRT;

            RenderTexture.ReleaseTemporary(tempRT);
            return ret;
        }

        private float GetImageMagnitude(Texture2D tex)
        {
            var pixels = tex.GetPixels().Select(p => new float4(p.r, p.g, p.b, p.a)).ToArray();
            float peak = 0f;
            int count = pixels.Length;
            for (int i = 0; i < count; i++)
            {
                float ls = math.length(pixels[i]);
                peak = math.max(peak, ls);
            }
            return peak;
        }

#if false
        private float CalcfErrorFactor(Texture2D t1, Texture2D t2)
        {
            Debug.Assert(t1.width == t2.width && t1.height == t2.height);
            float error = 0f;

            var pixels1 = t1.GetPixels().Select(p => GetEffectiveFloat4(p)).ToArray();
            var pixels2 = t2.GetPixels().Select(p => GetEffectiveFloat4(p)).ToArray();


            int count = pixels1.Length;
            for (int i = 0; i < count; i++)
            {
                float ls = math.length(pixels1[i] - pixels2[i]);
                error += ls;
            }

            //Debug.Log($"e = {error}");
            return error / count;
        }

        private float4 GetEffectiveFloat4(Color c)
        {
            float3 rgb = new float3(c.r, c.g, c.b);
            float4 rgba = new float4(rgb * c.a, c.a);
            return rgba;
        }
#endif
    }
}