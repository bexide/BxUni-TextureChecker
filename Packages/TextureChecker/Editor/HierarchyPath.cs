// 2022-11-24 BeXide,Inc.
// by Y.Hayashi

using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BX.TextureChecker
{
    /// <summary>
    /// Hierarchy内のGameObjectの位置を示す情報
    /// </summary>
    public class HierarchyPath
    {
        public class NodeEntry
        {
            public string NodeName     { get; }
            public int    SiblingIndex { get; }

            public NodeEntry(Transform transform)
            {
                NodeName     = transform.name;
                SiblingIndex = transform.GetSiblingIndex();
            }

            public bool IsMatch(Transform transform)
            {
                return NodeName == transform.name &&
                       SiblingIndex == transform.GetSiblingIndex();
            }
        }

        private NodeEntry[] NodeEntries { get; }

        public HierarchyPath(IEnumerable<NodeEntry> entries)
        {
            NodeEntries = entries.ToArray();
        }

        public GameObject GetGameObject()
        {
            if (NodeEntries == null || NodeEntries.Length <= 0) { return null; }

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
                NodeEntries.Length <= 0) { return string.Empty; }
            return NodeEntries[^1].NodeName;
        }
    }
}
