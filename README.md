# Texture Checker

テクスチャ設定を検査するエディタ拡張

## 使用方法

* UnityEditorメニュー → BeXide → Texture Compression Check
* UnityEditorメニュー → BeXide → Texture Size Quality Check

### Texture Compression Check

#### 結果の見方

##### 独立したテクスチャが圧縮されていません

- アトラスに登録されていないテクスチャです。アトラスに登録するのを忘れていないでしょうか。もしくはインポート設定に誤りがある可能性があります。非圧縮が意図したものであるか確認してください。

##### 独立したテクスチャの大きさがPOTではありません。

- アトラスに登録されていないテクスチャです。アトラスに登録するのを忘れていないでしょうか。もしくはテクスチャサイズに誤りがあります（256とか512とかの、2の累乗ではない）。

##### アトラスに登録されたテクスチャが圧縮されています。

- アトラスに登録するテクスチャのインポート設定が間違っています。非圧縮にしてください。

ランタイムでUnityが使うものではないテクスチャ（アプリアイコンとかエディタ用リソース）にも警告が出る場合がありますが、これは無視して大丈夫です。