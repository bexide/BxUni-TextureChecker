// 2022-11-24 BeXide,Inc.
// by Y.Hayashi

using System.Collections.Generic;
using System.Linq;
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
        private HashSet<GUID> m_ignoreAssetSet = new HashSet<GUID>();

        private HashSet<AssetObject> m_ignoreAssetObjectSet = new HashSet<AssetObject>();

        public HashSet<GUID>        IgnoreAssetSet       => m_ignoreAssetSet;
        public HashSet<AssetObject> IgnoreAssetObjectSet => m_ignoreAssetObjectSet;

        public void SetIgnoreAsset(GUID assetGuid, bool active)
        {
            if (active) { m_ignoreAssetSet.Add(assetGuid); }
            else { m_ignoreAssetSet.Remove(assetGuid); }
        }

        public void SetIgnoreAssetObject(AssetObject assetObject, bool active)
        {
            if (active) { m_ignoreAssetObjectSet.Add(assetObject); }
            else { m_ignoreAssetObjectSet.Remove(assetObject); }
        }

        public void ClearIgnoreSet()
        {
            m_ignoreAssetObjectSet.Clear();
        }

        #region Serializer

        [SerializeField]
        private List<AssetObject> m_ignoreAssetObjectList = new List<AssetObject>();

        [SerializeField]
        private List<string> m_ignoreAssetList = new List<string>();

        public void OnBeforeSerialize()
        {
            m_ignoreAssetObjectList.Clear();
            m_ignoreAssetObjectList = new List<AssetObject>(m_ignoreAssetObjectSet);

            m_ignoreAssetList.Clear();
            m_ignoreAssetList = m_ignoreAssetSet.Select(guid => guid.ToString()).ToList();
        }

        public void OnAfterDeserialize()
        {
            m_ignoreAssetObjectSet.Clear();
            foreach (var item in m_ignoreAssetObjectList) { m_ignoreAssetObjectSet.Add(item); }

            m_ignoreAssetSet.Clear();
            foreach (string item in m_ignoreAssetList) { m_ignoreAssetSet.Add(new GUID(item)); }
        }

        #endregion
    }
}