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
    public class UIImageSpriteChecker : UIComponentChecker
    {
        [MenuItem("BeXide/UI Checker/UI Image Sprite Size Check")]
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

        protected override IEnumerator Execute()
        {
            yield return CollectSpriteAtlas();
            yield return base.Execute();
        }

        protected override void CheckComponent(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Image image))
            {
                CheckImage(image);
            }
        }

        private void CheckImage(Image image)
        {
            var sprite = image.sprite;
            if (sprite == null)
            {
                AddInformationError(image.gameObject, "ImageにSpriteが設定されていません");
                return;
            }

            var spriteSize = sprite.rect.size;
            var rectSize   = (image.gameObject.transform as RectTransform).sizeDelta;

            //Debug.Log($"[{image.name}]:sprite=[{image.sprite.name}] spriteSize={spriteSize}, rectSize={rectSize}");

            if ((image.type == Image.Type.Simple || image.type == Image.Type.Filled) &&
                spriteSize != rectSize)
            {
                AddInformationWarning(image.gameObject, "RectサイズがSpriteサイズと一致しません");
            }

            if (image.type == Image.Type.Simple && !image.useSpriteMesh)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (AtlasedTextureMap.TryGetValue(guid, out bool enableTightPacking))
                {
                    if (enableTightPacking)
                    {
                        AddInformationWarning(
                            image.gameObject,
                            "TightPackingされたSpriteAtlasに登録されていますがUseSpriteMeshがOFFです");
                    }
                }
            }
        }
    }
}
