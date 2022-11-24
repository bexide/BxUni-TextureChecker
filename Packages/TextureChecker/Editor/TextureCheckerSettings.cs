// 2022-11-24 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BX.TextureChecker
{
    [Serializable]
    public class AssetObject
    {
        public GUID          m_asset;
        public string        m_objectPath;
        public HierarchyPath m_objectHierarchy;
    }

    /// <summary>
    /// TextureCheckerの設定を保存するアセット
    /// </summary>
    public class TextureCheckerSettings : ScriptableObject
    {
        /// <summary>
        /// デフォルトの検査対象パス
        /// </summary>
        [SerializeField]
        private DefaultAsset m_targetFolder;

        public DefaultAsset TargetFolder
        {
            get => m_targetFolder;
            set
            {
                if (value != m_targetFolder)
                {
                    m_targetFolder = value;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        /// <summary>
        /// 無視するアセット／オブジェクトのリスト
        /// </summary>
        [SerializeField]
        private List<AssetObject> m_ignoreAssetObjectList;

        public IReadOnlyList<AssetObject> IgnoreAssetObjectList => m_ignoreAssetObjectList;

        public void AddIgnoreAssetObject(AssetObject assetObject)
        {
            m_ignoreAssetObjectList.Add(assetObject);
            EditorUtility.SetDirty(this);
        }

        public void RemoveIgnoreAssetObject(AssetObject assetObject)
        {
            m_ignoreAssetObjectList.Remove(assetObject);
            EditorUtility.SetDirty(this);
        }
    }
}