# メニュー背景の適用

タイトルにB（レンガ遺跡）、ステージ選択にC（奥行きのある森林）を適用した。

画像はAssets/UI/Backgrounds/Title_BrickRuins.png、Select_LayeredForest.png。各SceneのCanvas直下のMenuBackgroundが表示する。ArtworkはRawImageとAspectRatioFitter（EnvelopeParent）を使い、縦横比を保ちながら画面全体を覆う。画面比率に合わせて外側を切り取り、RectMask2Dで画面外を隠す。

背景は最背面に配置し、画像とReadabilityShadeのraycastTargetをfalseにした。文字を読みやすくするため、タイトル42%、選択画面24%の暗色を重ねている。この濃さは各SceneのReadabilityShadeのImage.color.aで調整できる。

画像はPointフィルター、圧縮なし、MipMapなし、元画像のサイズを保持してインポート。Tools/UIで配置を再生成しても背景が維持されるようMenuScreenBuilderとMenuBackgroundSetupを連携した。ゲーム本編の背景・移動・物理・保存処理は変更していない。

開いているEditorとは別の検証用コピーで確認し、確認済みの2つのSceneと画像設定を反映した。1280×720と800×600の表示を画像で確認。遊び方、設定、続きから、選択、Stage2への移動、タイトルへ戻る実クリック、解放状態、ボタンの重なり・画面内配置の131項目が成功した。ログにコンパイルエラーや実行例外はなかった。検証した進行データは終了時に元へ戻した。

プレビュー: title-1280.png / title-800.png / selection-1280.png / selection-800.png。
操作確認: test.txt。変更前のScene: Before。
