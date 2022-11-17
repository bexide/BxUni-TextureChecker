// 2022-11-17 BeXide,Inc.
// by Y.Hayashi

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BX.TextureChecker
{
    public class TextureCheckerBase : EditorWindow
    {
        protected static readonly string s_defaultPath = "Assets/Application";

        public enum InformationType
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

        /// <summary>対象フォルダ</summary>
        public DefaultAsset TargetFolder { get; set; }

        /// <summary>アトラスに登録されているテクスチャ一覧</summary>
        protected HashSet<string> AtlasedTextureGUIDs { get; set; }

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
        }

        protected IEnumerator CollectSpriteAtlas()
        {
            AtlasedTextureGUIDs = new HashSet<string>();

            // SpriteAtlas 情報を収集
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
                    else { yield return ReadSpriteAtlas(spriteAtlas); }
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
            var serializedObject = new SerializedObject(spriteAtlas);
            var sizeProp         = serializedObject.FindProperty("m_PackedSprites.Array.size");
            if (sizeProp != null && sizeProp.propertyType == SerializedPropertyType.ArraySize)
            {
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
                        AtlasedTextureGUIDs.Add(spriteGUID);
                    }
                }

            }

            yield break;
        }
    }
}