// 2022-11-21 BeXide,Inc.
// by Y.Hayashi

using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BX.TextureChecker
{
    /// <summary>
    /// </summary>
    internal class UIRaycastChecker : UIComponentChecker
    {
        protected override string GetLabel() =>
            "これはUI.GraphicコンポーネントのRaycastTarget設定をチェックするツールです";

        [MenuItem("BeXide/Texture Checker/UI Raycast Status Check")]
        private static void Create()
        {
            var window =
                GetWindow<UIRaycastChecker>(
                    utility: false,
                    title: "UI Raycast Status Checker",
                    focus: true);
            window.Initialize("UIRaycastStatusChecker");
        }

        protected override void Initialize(string id)
        {
            base.Initialize(id);
            CodeTextMap["W401"] = "EventHandlerに包含されないRaycastTargetが有効です";
            CodeTextMap["W402"] = "親Rectに包含されているRaycastTargetが有効です";
        }

        protected override void CheckComponent(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Graphic graphic))
            {
                CheckGraphicRaycastTarget(graphic);
            }
        }

        private void CheckGraphicRaycastTarget(Graphic graphic)
        {
            var rectTransform = graphic.gameObject.transform as RectTransform;
            Debug.Assert(rectTransform != null);

            if (rectTransform.TryGetComponent<IEventSystemHandler>(out _))
            {
                // このノードでイベントを処理
                return;
            }

            // イベントが伝播する元ノードを検索
            var eventCatcherNode = rectTransform.parent;
            while (eventCatcherNode != null &&
                   !eventCatcherNode.TryGetComponent<IEventSystemHandler>(out _))
            {
                eventCatcherNode = eventCatcherNode.parent;
            }

            if (eventCatcherNode == null)
            {
                if (graphic.raycastTarget)
                {
                    AddInformationWarning(graphic.gameObject, "W401");
                }
                return;
            }

            var rectWorldCorners = new Vector3[4];
            //rectTransform.ForceUpdateRectTransforms();
            rectTransform.GetWorldCorners(rectWorldCorners);

            var eventCatcherRectTransform
                = eventCatcherNode.gameObject.transform as RectTransform;
            Debug.Assert(eventCatcherRectTransform != null);

            var eventWorldCorners = new Vector3[4];
            //eventCatcherRectTransform.ForceUpdateRectTransforms();
            eventCatcherRectTransform.GetWorldCorners(eventWorldCorners);

            Debug.Log($" {graphic.name}:eventCatcher=[{eventCatcherNode.gameObject.name}]");
            //Debug.Log($"   catcher={string.Join(",",eventWorldCorners)}");
            //Debug.Log($"   rect   ={string.Join(",",rectWorldCorners)}");

            if (CornerContains(eventWorldCorners, rectWorldCorners) &&
                graphic.raycastTarget)
            {
                AddInformationWarning(graphic.gameObject, "W402");
            }

            bool CornerContains(Vector3[] outerCorners, Vector3[] innerCorners)
            {
                return innerCorners[0].x >= outerCorners[0].x &&
                       innerCorners[0].y >= outerCorners[0].y &&
                       innerCorners[2].x <= outerCorners[2].x &&
                       innerCorners[2].y <= outerCorners[2].y;
            }
        }
    }
}
