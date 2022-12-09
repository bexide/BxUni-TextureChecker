// 2022-11-25 BeXide,Inc.
// by Y.Hayashi

using System;
using UnityEditor;
using UnityEngine;

namespace BxUni.TextureChecker
{
    /// <summary>
    /// 特定のアセットを記録するクラス
    /// </summary>
    [Serializable]
    internal class AssetObject : ISerializationCallbackReceiver
    {
        private GUID          Asset         { get; set; }
        private string        ObjectPath    => m_objectPath;
        private HierarchyPath HierarchyPath => m_hierarchyPath;

        public AssetObject(GUID asset, string objectPath, HierarchyPath hierarchyPath)
        {
            Asset           = asset;
            m_objectPath    = objectPath;
            m_hierarchyPath = hierarchyPath;
        }

        // 同値判定
        #region Equality

        public override int GetHashCode()
        {
            return Asset.GetHashCode() ^
                   ObjectPath.GetHashCode() ^
                   (HierarchyPath == null ? 0: HierarchyPath.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return obj is AssetObject assetObject && Equals(assetObject);
        }

        private bool Equals(AssetObject rhv)
        {
            return Asset == rhv.Asset &&
                   ObjectPath == rhv.ObjectPath &&
                   HierarchyPath == rhv.HierarchyPath;
        }

        public static bool operator ==(AssetObject lhv, AssetObject rhv)
        {
            System.Object lho = lhv;
            System.Object rho = rhv;

            bool lNull = lho == null;
            bool rNull = rho == null;

            if (lNull && rNull) { return true; }
            if (lNull || rNull) { return false; }

            return lhv.Equals(rhv);
        }

        public static bool operator !=(AssetObject lhv, AssetObject rhv)
        {
            return !(lhv == rhv);
        }

        #endregion

        // シリアライズ処理
        #region Serializer

        [SerializeField]
        private string m_asset;

        [SerializeField]
        private string m_objectPath;

        [SerializeField]
        private HierarchyPath m_hierarchyPath;

        public void OnBeforeSerialize()
        {
            m_asset = Asset.ToString();
        }

        public void OnAfterDeserialize()
        {
            Asset = new GUID(m_asset);
        }

        #endregion
    }
}