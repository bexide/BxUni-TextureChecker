# Texture Checker

テクスチャ設定を検査するエディタ拡張

## 使用方法

* UnityEditorメニュー → BeXide
  * Texture Checker
    * Texture Compression Check
    * Texture Size Quality Check
  * UI Checker
    * UI Image Sprite Size Check
    * UI Raycast Status Check

### Texture Compression Check

テクスチャアセットについて、アトラス(SpriteAtlas)への登録状況と圧縮設定をチェックするツールです。

#### 結果の見方

##### 独立したテクスチャが圧縮されていません

- アトラスに登録されていないテクスチャです。
アトラスに登録するのを忘れていないでしょうか。
もしくはインポート設定に誤りがある可能性があります。
非圧縮が意図したものであるか確認してください。

##### 独立したテクスチャの大きさがPOTではありません。

- アトラスに登録されていないテクスチャです。
アトラスに登録するのを忘れていないでしょうか。
もしくはテクスチャサイズに誤りがあります（256とか512とかの、2の累乗ではない）。

##### アトラスに登録されたテクスチャが圧縮されています。

- アトラスに登録するテクスチャのインポート設定が間違っています。
非圧縮にしてください。

- ランタイムでUnityが使うものではないテクスチャ（アプリアイコンとかエディタ用リソース）にも
警告が出る場合がありますが、これは無視して大丈夫です。

### UI Image Sprite Size Check

- UIに使われているSpriteについて、サイズ設定が適切かどうかを検査します。

#### 結果の見方

##### RectサイズがSpriteサイズと一致しません

- RectTransformのサイズがSpriteのネイティブサイズと一致していません。
UIはCanvasの解像度に沿った解像度のテクスチャを使用することで画素密度の不均等を防ぎます。
拡大・縮小されない、常にカメラに正対するオブジェクトに関してはRectTransformのサイズにSpriteのサイズを合わせることが推奨されます。

##### TightPackingされたSpriteAtlasに登録されていますがUseSpriteMeshがOFFです

- SpriteAtlasをTightPackingするとアトラスの収納効率が向上しますが、
TightPackingされたアトラスからは、Spriteを矩形で切り出すことができません。
そのため、UIでそのようなSpriteを使用する際にはUseSpriteMeshのチェックを入れておくことが必要です。

### UI Raycast Status Check

UI要素について無駄なRaycastCheckが行われていないか検査します。

#### 結果の見方

##### EventHandlerに包含されないRaycastTargetが有効です

- オブジェクトのRaycastTargetが有効ですが、それを明示的に受け取るオブジェクトが見つかりません。
単に入力をブロックするオブジェクトとして使われる場合もありますが、そうではない場合に無駄にチェックされていないか確認してください。

##### 親Rectに包含されているRaycastTargetが有効です

- RaycastTargetは有効でそれを処理するコンポーネントも存在しますが、親のオブジェクトの
RectTransformにこのオブジェクトのRectTransformが完全に包含されているため個別に処理する意味がありません。
無駄を省くためにこのオブジェクトのRaycastTargetは外したほうがよいでしょう。
- 