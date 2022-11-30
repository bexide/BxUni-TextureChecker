// 2022-10-31 BeXide,Inc.
// by Y.Hayashi

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BX.TextureChecker
{
    /// <summary>
    /// ImageのRectのサイズがSpriteのサイズと一致しているかを検証する
    /// TightPackingされたSpriteAtlasに登録されているSpriteを使うImageにUseSpriteMeshが設定されているかを検証する
    /// </summary>
    internal class UIImageSpriteChecker : UIComponentChecker
    {
        [MenuItem("BeXide/Texture Checker/UI Image Sprite Size Check")]
        private static void Create()
        {
            var window =
                GetWindow<UIImageSpriteChecker>(
                    utility: true,
                    title: "UI Sprite Size Checker",
                    focus: true);
            window.Initialize("UISpriteSizeChecker");
        }

        protected override string GetLabel() => "UI.Imageの設定をチェックします";

        protected override void Initialize(string id)
        {
            base.Initialize(id);
            CodeTextMap["E301"] = "ImageにSpriteが設定されていません";
            CodeTextMap["W301"] = "RectサイズがSpriteサイズと一致しません";
            CodeTextMap["W302"] = "TightPackingされたSpriteAtlasに登録されていますがUseSpriteMeshがOFFです";
        }

        protected override IEnumerator Execute()
        {
            yield return CollectSpriteAtlas();
            yield return base.Execute();
        }

        protected override void CheckComponent(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Image image)) { CheckImage(image); }
        }

        private void CheckImage(Image image)
        {
            var sprite = image.sprite;
            if (sprite == null)
            {
                AddInformationError(image.gameObject, "E301");
                return;
            }

            var spriteSize = sprite.rect.size;
            var rectSize   = (image.gameObject.transform as RectTransform).sizeDelta;

            //Debug.Log($"[{image.name}]:sprite=[{image.sprite.name}] spriteSize={spriteSize}, rectSize={rectSize}");

            if ((image.type == Image.Type.Simple || image.type == Image.Type.Filled) &&
                spriteSize != rectSize) { AddInformationWarning(image.gameObject, "W301"); }

            if (image.type == Image.Type.Simple && !image.useSpriteMesh)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (AtlasedTextureMap.TryGetValue(guid, out bool enableTightPacking))
                {
                    if (enableTightPacking) { AddInformationWarning(image.gameObject, "W302"); }
                }
            }
        }
    }
}
