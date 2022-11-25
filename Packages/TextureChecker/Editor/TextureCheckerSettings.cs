// 2022-11-24 BeXide,Inc.
// by Y.Hayashi

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BX.TextureChecker
{
    /// <summary>
    /// TextureCheckerの設定を保存するアセット
    /// </summary>
    internal class TextureCheckerSettings : ScriptableObject, ISerializationCallbackReceiver
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
        private HashSet<AssetObject> m_ignoreAssetObjectSet = new();

        public HashSet<AssetObject> IgnoreAssetObjectSet => m_ignoreAssetObjectSet;

        public void AddIgnoreAssetObject(AssetObject assetObject)
        {
            m_ignoreAssetObjectSet.Add(assetObject);
            EditorUtility.SetDirty(this);
        }

        public void RemoveIgnoreAssetObject(AssetObject assetObject)
        {
            m_ignoreAssetObjectSet.Remove(assetObject);
            EditorUtility.SetDirty(this);
        }

        public void SetIgnoreAssetObject(AssetObject assetObject, bool active)
        {
            if (active)
            {
                AddIgnoreAssetObject(assetObject);
            }
            else
            {
                RemoveIgnoreAssetObject(assetObject);
            }
        }

        #region Serializer

        [SerializeField]
        private List<AssetObject> m_ignoreAssetObjectList = new();

        public void OnBeforeSerialize()
        {
            m_ignoreAssetObjectList.Clear();
            m_ignoreAssetObjectList = new(m_ignoreAssetObjectSet);
        }

        public void OnAfterDeserialize()
        {
            m_ignoreAssetObjectSet.Clear();
            foreach (var item in m_ignoreAssetObjectList) { m_ignoreAssetObjectSet.Add(item); }
        }

        #endregion
    }
}