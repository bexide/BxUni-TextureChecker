# BX Texture Checker

プロジェクトに含まれるテクスチャ設定と関連するUI項目を検査するエディタ拡張

## インストール

### Package Manager からのインストール

* Package Manager → Scoped Registries に以下を登録
  * URL: https://package.openupm.com
  * Scope: jp.co.bexide
* Package Manager → My Registries から以下を選択して Install
  * BxUni Texture Checker 

## 使用方法

### 起動

* UnityEditorメニュー → BeXide → Texture Checker
    * Texture Compression Check
    * UI Image Sprite Size Check
    * UI Raycast Status Check

起動するとウィンドウが開きます。

![](images/TextureChecker01.png)

「チェック」ボタンを押すと検査が始まり、検査が終了すると結果がウィンドウ内に表示されます。

注意：もし編集中のシーンに保存していない変更がある場合は検査することができません。
変更をセーブするか破棄してから検査を開始してください。

![](images/TextureChecker02.png)

### 検査結果に表示されたアセットを選択する

検査結果に表示されたアセット名をクリックすると、プロジェクトウィンドウでそのアセットが選択されます。

### アセット別の検査結果の表示を展開する

![](images/TextureChecker_unfold.png)

アセットに複数の問題が含まれる場合は、アセットごとに表示がまとめられています。
内容を表示するには、アセットの左にある三角のマークを押すとオブジェクトレベルの表示が展開されます。

### オブジェクトをヒエラルキーで選択する

まずそのオブジェクトが含まれるアセット（プレファブまたはシーン）をヒエラルキーに読み込みます。
（アセット名をクリックしてプロジェクトウィンドウで選択し、ダブルクリックするなどして開きます。）
その状態で検査結果のオブジェクト名をクリックするとヒエラルキー上でそのオブジェクトが選択されます。

### 検査結果を項目ごとにソートする

![](images/TextureChecker_sort.png)

結果表示領域の先頭にあるカラムヘッダの項目をクリックするとその項目で内容がソートされます。

### 結果を無視するアセット・オブジェクトを設定する

![](images/TextureChecker_ignore.png)

「ignore」カラムにあるチェックボックスを設定すると、アセットやオブジェクトごとに検査結果を非表示にできます。
チェックボックスを変更したらウィンドウ下部の「無視フラグ保存」ボタンを押すと反映されます。

「無視を表示」チェックボックスを入れると既に非表示になっている項目も表示されます。

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

## お問い合わせ

* 不具合のご報告は GitHub の Issues へ
* その他お問い合わせは mailto:tech-info@bexide.co.jp へ
