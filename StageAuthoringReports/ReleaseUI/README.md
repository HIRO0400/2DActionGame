# 公開準備UI 実装・確認記録

## 実装した機能

- ポーズ: ステージ中はEscまたは右上の「ポーズ」。時間・物理・音声を停止し、再開、リスタート、ステージ選択、タイトル、音量設定を選べる。
- 音量設定: BGMと効果音を別々に調整、全体ミュート、設定の保存。音量0を正しく無音として扱う。初期値は既存Mixerの音量を維持する。
- 日本語フォント: Noto Sans CJK JP Regularを同梱。メニュー、クローンHUD、操作説明、エリアガイド、既存のTextMeshPro表示にも適用する。
- 保存: ステージ解放、各ステージのクリア記録、音量とミュートをPlayerPrefsへ保存。従来の解放データは引き継ぎ、不正な解放値は実在するStage1〜5へ補正する。
- クリア画面: ゴール時に記録を保存して停止。「次のステージ」「ステージ選択」「タイトル」「もう一度プレイ」を選べる。Stage5には次ステージのボタンを出さない。
- ステージ選択: 2列のボタンに「クリア済み／挑戦可能／未解放」を表示。存在しないStage6は非表示。未解放ステージは押せない。
- 進行の初期化: タイトルと選択画面の左下から、確認画面を経て初期化。音量設定を残す。キャンセルで記録を保持する。

途中のプレイヤー位置やコイン取得状態は保存しない。リスタート時はステージ開始状態に戻り、死亡時は既存の最終接地点付近への復帰を使う。

## Unity側の設定

追加のScene配置は不要。GameMenusが起動時に常駐UIとInput System対応のEventSystemを生成する。既存SceneのEventSystemは実行中に停止し、二重処理を防ぐ。SceneやPrefabの保存状態は変えない。

共有アセットは `Assets/Resources/ReleaseAssets.asset`。既存の `Assets/Audio/GameAudioMixer.mixer` と `Assets/Fonts/NotoSansCJKjp-Regular.otf` を参照する。フォントのライセンス・著作権表記は `Assets/StreamingAssets/Licenses` にあり、ビルドにも含まれる。

ゲーム入力は現在の新Input Systemを維持した。ポーズ中の入力をゲームへ流さず、再開ボタンのクリックでクローンを誤生成しない。ジャンプ中のポーズとポーズ中のキー解放も確認する。

## 検証

機能の順番ごとにPlay Modeで検証し、その都度成功してから次へ進めた。テストはボタン関数の直接呼び出しだけに頼らず、仮想マウス／キーボードの入力をUIへ送ってクリック、スライダー、Escを確認した。

|記録|対象|
|---|---|
|phase1-test.txt|ポーズ、再開、時間・音声停止、各画面への移動|
|phase2-test.txt|BGM／効果音の独立調整、ミュート、音量0、保存|
|phase3-test.txt|同梱フォント、日本語字形、TextMeshPro表示|
|phase4-test.txt|保存、旧データの移行、範囲補正、初期化とキャンセル|
|phase5-test.txt|ジャンプ中のポーズ、実際のゴール、クリア画面、選択画面、最終クリア|
|campaign-test.txt|Stage2〜5の実攻略、必須クローン、復帰、背景、ゴール|
|stage1-test.txt|Stage1の移動、ジャンプ、コイン、クローン、死亡復帰、背景、ゴール: 25項目成功|
|regression-test.txt|既存機能の回帰確認: 75項目成功|
|native-test.txt|Windowsアプリの入力、フォント、HUD、保存、画面遷移、初期化: 14項目成功|
|build.txt|Windows64開発ビルド成功、ビルドエラー0件|

確認画像: `pause.png`、`settings.png`、`reset-confirmation.png`、`clear.png`、`completed.png`、`selection.png`。

Stage2〜5の実攻略は191項目成功。Windowsアプリの実描画も `native-pause.png` で確認した。アプリの実行ログに例外はなかった。フォントのライセンスと著作権表記がビルド内に含まれていることも確認した。異なるPC、他OSでの動作、および実機スピーカーでの聞こえ方は今回の自動検証の対象外。

検証用コードの保存復元処理にあったコンパイルエラーは修正し、キャンペーンを再実行した。検証に使った進行・音量の設定は終了時に元へ戻す。

## 主な実装先

- `Assets/Script/GameMenus.cs`: ポーズ、設定、クリア画面、入力と画面遷移。
- `Assets/Script/AudioSettings.cs`: Mixerへの反映と音量保存。
- `Assets/Script/UIFont.cs`: 共有フォントとTextMeshPro対応。
- `Assets/Script/ProgressStore.cs`: 保存、移行、初期化。
- `Assets/Script/StageManager.cs` / `StageSelectManager.cs`: ゴール連携と選択画面。
- `Assets/Script/Player.cs`: ポーズ時の入力停止。

制作時の初期状態と機能ごとのバックアップはこのフォルダの `Initial`、`BeforeAudio`、`BeforeFont`、`BeforeSave`、`BeforeClear` に保存している。

Windows検証用ビルドの生成方法は `ReleaseUIBuild.Run`。開発ビルドを `--release-ui-smoke` 付きで起動すると専用のUI確認を行う。通常起動では走らず、通常の非開発ビルドには確認コードを含めない。

今回の検証アプリ: `.repair-checkpoints/ReleaseUIPlayer/2DActionGame.exe`。配布用リリースの作成・公開は行っていない。
