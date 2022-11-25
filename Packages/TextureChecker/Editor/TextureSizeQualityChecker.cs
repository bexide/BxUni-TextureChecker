// 2020-01-21 BeXide,Inc.
// by Y.Hayashi
// original from bx70beta

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using Unity.Mathematics;

namespace BX.TextureChecker
{
    internal class TextureSizeQualityChecker : TextureCheckerBase
    {
        static readonly string s_hpfShaderName = "Unlit/Highpass3x3";

        public float ResultThreshold { get; set; } = 0.2f;

        public int SizeThreshold { get; set; } = 32;

        private         int      SortTypeIndex { get; set; }
        static readonly string[] SortTypeNames = { "ピーク値", "ピクセル数", "パス" };
        private         Shader   HighPassShader   { get; set; }
        private         Material HighPassMaterial { get; set; }

        private struct CheckResultType
        {
            public Object m_obj;
            public float  m_error;
            public long   m_pixelCount;
        }

        private List<CheckResultType> CheckResults { get; set; }

        [MenuItem("BeXide/Texture Checker/Texture size quality check")]
        private static void Create()
        {
            var window = GetWindow<TextureSizeQualityChecker>(
                utility: true,
                title: "Texture Size Quality Checker",
                focus: true);
            window.Initialize("TextureSizeQualityChecker");
        }

        protected override void Initialize(string id)
        {
            base.Initialize(id);

            HighPassShader = Shader.Find(s_hpfShaderName);
            Debug.Assert(HighPassShader != null);
            HighPassMaterial = new Material(HighPassShader);
        }

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これはテクスチャの大きさが内容に対して大きすぎないかをチェックするツールです。",
                new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                });
            EditorGUILayout.Space();

            var newTarget = EditorGUILayout.ObjectField(
                "対象フォルダ",
                Settings.TargetFolder,
                typeof(DefaultAsset),
                allowSceneObjects: false);
            Settings.TargetFolder = newTarget as DefaultAsset;

            SizeThreshold = EditorGUILayout.IntSlider("サイズしきい値", SizeThreshold, 0, 512);

            ResultThreshold = EditorGUILayout.Slider("結果しきい値", ResultThreshold, 0f, 5f);

            if (!IsCompleted)
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

            EditorGUI.BeginDisabledGroup(!HasInformation);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { Clear(); }
            if (GUILayout.Button("保存", GUILayout.MaxWidth(120))) { Save(); }
            EditorGUILayout.EndHorizontal();

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
            DrawInformation();
        }

        protected override void Clear()
        {
            base.Clear();
            CheckResults = null;
        }

        private IEnumerator Execute()
        {
            CheckResults = new List<CheckResultType>();

            yield return CheckTexture2D();

            Complete();
        }

        private IEnumerator CheckTexture2D()
        {
            // テクスチャアセットを列挙
            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new [] { targetPath });
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
                    //DebugLabelReplace(path);
                    var texture2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture2d == null)
                    {
                        Debug.LogError($" cannot load from path [{path}]");
                    }
                    else
                    {
                        CurrentAsset = new GUID(guid);
                        yield return CheckTexture2D(texture2d);
                    }
                }
                // プログレスバー
                if (EditorUtility.DisplayCancelableProgressBar(
                        "集計中",
                        $"{i + 1}/{guidsLength}",
                        (float)(i + 1) / guidsLength))
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
            ClearInformation();
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
                string path = AssetDatabase.GetAssetPath(result.m_obj);
                CurrentAsset = AssetDatabase.GUIDFromAssetPath(path);
                AddInformationWarning($"\te={result.m_error}");
            }
        }

        private void Save()
        {
            string filePath = EditorUtility.SaveFilePanel(
                "名前を付けて結果を保存",
                "",
                "TextureQualityCheckerResult",
                "txt");
            using var stream = new System.IO.StreamWriter(filePath);

            foreach (var info in InformationList) { stream.WriteLine(info.AssetGuid); }
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
                CheckResults.Add(
                    new CheckResultType
                    {
                        m_obj        = texture2d,
                        m_error      = m,
                        m_pixelCount = pixelCount
                    });
            }

            //yield break;
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