// 2020-01-21 BeXide,Inc.
// by Y.Hayashi
// original from bx70beta

using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace BxUni.TextureChecker
{
    /// <summary>
    /// テクスチャアセットの圧縮設定をチェックするツール
    /// </summary>
    internal class TextureCompressionChecker : TextureCheckerBase
    {
        private readonly string[] k_platformStrings =
        {
            "Default",
            "Standalone",
            "Web",
            "iPhone",
            "Android",
            "WebGL",
            "Windows Store Apps",
            "PS4",
            "PS5",
            "XboxOne",
            "Nintendo Switch",
            "tvOS",
        };

        [MenuItem("BeXide/Texture Checker/Texture Compression Check")]
        private static void Create()
        {
            var window =
                GetWindow<TextureCompressionChecker>(
                    utility: false,
                    title: "Texture Compression Checker",
                    focus: true);
            window.Initialize("TextureCompressionChecker");
        }

        protected override void Initialize(string id)
        {
            base.Initialize(id);
            CodeTextMap["W101"] = "アトラスに登録されたテクスチャが圧縮されています";
            CodeTextMap["W102"] = "独立したテクスチャが圧縮されていません";
            CodeTextMap["W103"] = "独立したテクスチャの大きさがPOTではありません";
        }

        /// <summary>
        /// ウィンドウ表示
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "これはテクスチャアセットについて、圧縮状態をチェックするツールです",
                new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                });
            EditorGUILayout.Space();

            var newTarget =
                EditorGUILayout.ObjectField(
                    "対象フォルダ",
                    Settings.TargetFolder,
                    typeof(DefaultAsset),
                    allowSceneObjects: false);
            Settings.TargetFolder = newTarget as DefaultAsset;

            if (!IsCompleted)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(!HasInformation);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { Clear(); }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // 情報ウィンドウ
            DrawInformation();
        }

        private IEnumerator Execute()
        {
            yield return CollectSpriteAtlas();
            yield return CheckTexture2D();

            Complete();
        }

        private IEnumerator CheckTexture2D()
        {
            // テクスチャアセットを列挙
            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { targetPath });
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
                    continue;
                }

                CurrentAsset = new GUID(guid);

                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter == null)
                {
                    Debug.LogError($" cannot get TextureImporter at [{path}]");
                    continue;
                }

                // テクスチャサイズ
                var widthAndHeight = GetWidthAndHeight(textureImporter);
                // サイズはPOT、もしくはPOTに丸められる設定か
                bool isPOT = (Mathf.IsPowerOfTwo(widthAndHeight.x) &&
                              Mathf.IsPowerOfTwo(widthAndHeight.y)) ||
                             (textureImporter.npotScale != TextureImporterNPOTScale.None);

                // 全プラットフォーム別のインポート設定を取得
                var settings = k_platformStrings.Select(
                        platformString =>
                            platformString == "Default"
                                ? textureImporter.GetDefaultPlatformTextureSettings()
                                : textureImporter.GetPlatformTextureSettings(platformString))
                    .ToArray();
                settings[0].overridden = true;
                Debug.Assert(settings[0].overridden);

                if (AtlasedTextureMap.TryGetValue(guid, out bool enableTightPacking))
                {
                    if (settings.Any(
                            s => s.overridden
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

                        //AddInformationWarning("W101", string.Join(",", compressionMessage));
                        AddInformationWarning("W101");
                    }
                }
                else
                {
                    if (settings.Any(
                            s => s.overridden
                                 && IsRawFormat(s.format, s.textureCompression)))
                    {
                        AddInformationWarning("W102");
                    }
                    else if (!isPOT)
                    {
                        AddInformationWarning("W103");
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
        }

        private Vector2Int GetWidthAndHeight(TextureImporter importer)
        {
            var methodInfo = typeof(TextureImporter).GetMethod(
                "GetWidthAndHeight",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(methodInfo != null);

            object[] args = { 0, 0 };
            methodInfo.Invoke(importer, args);

            return new Vector2Int((int)args[0], (int)args[1]);
        }

        private bool IsRawFormat(
            TextureImporterFormat      format,
            TextureImporterCompression compression)
        {
            return
                format == TextureImporterFormat.ARGB32 ||
                format == TextureImporterFormat.RGBA32 ||
                format == TextureImporterFormat.RGB24 ||
                format == TextureImporterFormat.Alpha8 ||
                format == TextureImporterFormat.R8 ||
                (format == TextureImporterFormat.Automatic &&
                 compression == TextureImporterCompression.Uncompressed);
        }

        private IEnumerator DumpSprite(Sprite sprite)
        {
            var serializedObject = new SerializedObject(sprite);
            var iter             = serializedObject.GetIterator();
            if (iter.Next(true))
            {
                do
                {
                    yield return DebugUtility.DumpSerializedProperty(iter);
                } while (iter.Next(false));
            }
        }

    }
}
