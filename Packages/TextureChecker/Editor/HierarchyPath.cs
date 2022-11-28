// 2022-11-24 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace BX.TextureChecker
{
    /// <summary>
    /// Hierarchy内のGameObjectの位置を示す情報
    /// </summary>
    [Serializable]
    public class HierarchyPath
    {
        [Serializable]
        public class NodeEntry
        {
            [SerializeField]
            private string m_nodeName;

            [SerializeField]
            private int m_siblingIndex;

            public string NodeName     => m_nodeName;
            public int    SiblingIndex => m_siblingIndex;

            public NodeEntry(Transform transform)
            {
                m_nodeName     = transform.name;
                m_siblingIndex = transform.GetSiblingIndex();
            }

            public bool IsMatch(Transform transform)
            {
                return NodeName == transform.name &&
                       SiblingIndex == transform.GetSiblingIndex();
            }
            
            #region Equality

            public override int GetHashCode()
            {
                return NodeName.GetHashCode() ^ SiblingIndex.GetHashCode();
            }
        
            public override bool Equals(object obj)
            {
                return obj is NodeEntry nodeEntry && Equals(nodeEntry);
            }

            private bool Equals(NodeEntry rhv)
            {
                return NodeName == rhv.NodeName && SiblingIndex == rhv.SiblingIndex;
            }

            public static bool operator ==(NodeEntry lhv, NodeEntry rhv)
            {
                System.Object lho = lhv;
                System.Object rho = rhv;

                bool lNull = lho == null;
                bool rNull = rho == null;

                if (lNull && rNull) { return true; }
                if (lNull || rNull) { return false; }
                
                return lhv.Equals(rhv);
            }

            public static bool operator !=(NodeEntry lhv, NodeEntry rhv)
            {
                return !(lhv == rhv);
            }

            #endregion
        }

        [SerializeField]
        private List<NodeEntry> m_nodeEntries;

        private List<NodeEntry> NodeEntries => m_nodeEntries;

        public HierarchyPath(IEnumerable<NodeEntry> entries)
        {
            m_nodeEntries = entries.ToList();
        }

        public GameObject GetGameObject()
        {
            if (NodeEntries == null || NodeEntries.Count <= 0) { return null; }

            Transform currentNode = null;
            foreach (var entry in NodeEntries)
            {
                if (currentNode == null)
                {
                    var rootNode = GetRootNode(entry);
                    Debug.Assert(
                        rootNode != null,
                        $"entry [{entry.NodeName}({entry.SiblingIndex})] cannot find in root");
                    currentNode = rootNode.transform;
                }
                else
                {
                    currentNode = GetChildNode(currentNode, entry);
                    Debug.Assert(currentNode != null);
                }
            }

            if (currentNode == null) { return null; }

            return currentNode.gameObject;
        }

        private GameObject GetRootNode(NodeEntry entry)
        {
            // ルートノード取得
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
            {
                // Sceneモード
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    foreach (var rootGameObject in scene.GetRootGameObjects())
                    {
                        if (entry.IsMatch(rootGameObject.transform)) { return rootGameObject; }
                    }
                }
            }
            else
            {
                // Prefabモード
                return prefabStage.prefabContentsRoot;
            }
            return null;
        }

        private Transform GetChildNode(Transform parent, NodeEntry entry)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (entry.IsMatch(child)) { return child; }
            }
            return null;
        }

        /// <summary>
        /// 既存のGameObjectから新たに生成
        /// </summary>
        public static HierarchyPath Create(GameObject gameObject)
        {
            var ancestors = new List<NodeEntry>();
            for (var trans = gameObject.transform; trans != null; trans = trans.parent)
            {
                ancestors.Add(new NodeEntry(trans));
            }

            return new HierarchyPath(ancestors.AsEnumerable().Reverse());
        }

        public override string ToString()
        {
            if (NodeEntries == null ||
                NodeEntries.Count <= 0) { return string.Empty; }
            //return NodeEntries[^1].NodeName;
            return NodeEntries.Last().NodeName;
        }
        
        #region Equality

        public override int GetHashCode()
        {
            return NodeEntries.Aggregate(0, (hash, next) => hash ^ next.GetHashCode());
        }
        
        public override bool Equals(object obj)
        {
            return obj is HierarchyPath hierarchyPath && Equals(hierarchyPath);
        }

        private bool Equals(HierarchyPath rhv)
        {
            return GetHashCode() == rhv.GetHashCode();
        }

        public static bool operator ==(HierarchyPath lhv, HierarchyPath rhv)
        {
            System.Object lho = lhv;
            System.Object rho = rhv;

            bool lNull = lho == null || lhv.NodeEntries.Count <= 0;
            bool rNull = rho == null || rhv.NodeEntries.Count <= 0;

            if (lNull && rNull) { return true; }
            if (lNull || rNull) { return false; }
                
            return lhv.Equals(rhv);
        }

        public static bool operator !=(HierarchyPath lhv, HierarchyPath rhv)
        {
            return !(lhv == rhv);
        }

        #endregion
    }
}
