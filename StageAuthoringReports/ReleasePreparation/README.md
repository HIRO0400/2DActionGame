# 現在の状態: 今回の実装は巻き戻し済み

Input System専用設定ではOnGUIのランタイム入力が利用できないことを確認したため、ユーザーの指示に従い、ポーズ追加前の状態へ戻した。
処理を直接呼ぶ自動テストとステージ攻略は通過したが、ボタン入力の互換性の確認が不足していた。下記の実装内容・phase-testログは取り下げた試作の記録であり、現在のゲームには追加機能は残っていない。
変更した8ファイルは、該当機能の着手前バックアップとバイト単位で一致することを確認。今回追加した保存キーも削除した。既存UnlockedStage、ステージ、HUDは維持した。
巻き戻し後のUnityコンパイル・既存機能75項目は通過。ログ: rollback.log。

次の実装ではCanvas + uGUI + InputSystemUIInputModuleを使い、処理の直接呼び出しに加えて実際のポインター入力を検証する。
公式の互換性説明: https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/use-imgui-alongside-input-system.md

---
以下は取り下げた試作の記録。

# 公開準備の機能追加

実装順: ポーズ → 音量設定 → フォント → 保存 → クリア画面・ステージ選択。
各段階の着手前のファイルはBeforePause、BeforeAudio、BeforeFont、BeforeSave、BeforeClearに退避した。前から存在するステージ制作の変更は保持している。

## 操作

- ステージ中にEsc: ポーズ / 再開。再開、リスタート、ステージ選択、タイトル、音量設定を選択できる。
- 右上の「設定」: タイトル・選択画面・ステージ中から音量設定を開く。ステージ中は停止して開く。
- BGMと効果音を個別に調整し、ミュートを切り替える。「保存して閉じる」で設定を保存する。初期音量は元のMixer設定に合わせている。
- タイトル・選択画面の左下「進行データの初期化」: 確認画面でキャンセル／初期化を選択。音量設定は残る。
- ゴール後: 自動的には移動せず、クリア画面で次のステージ、選択、タイトル、再プレイを選ぶ。Stage5では全ステージクリア表示。
- 選択画面: クリア済み、挑戦可能、未解放を表示。未制作のStage6は非表示。

## 保存と互換性

既存のUnlockedStageを維持する。Progress.Version=1へ移行し、既に解放されているステージの手前をクリア済みとして引き継ぐ。解放値は実在する有効ステージの範囲へ補正する。ゴール時はクリア記録と解放状況をPlayerPrefs.Saveで直ちに保存する。
追加したキー: Progress.Version、Progress.Cleared.N、Settings.BGM、Settings.SFX、Settings.Muted。
ステージ内のクローン数・位置や復帰地点は永続保存しない。既存のステージ開始・復帰仕様を維持する。

## 構成

GameMenusは起動時に自動生成され、Scene間で維持する。手動のScene配置は不要。
ReleaseAssets.assetはMixerと日本語フォントへの参照を持ち、Resourcesから読み込む。
UIFontは同梱フォントをHUD・IMGUI・TextMeshProへ適用する。共有フォントはPlayerHUDの破棄時に削除しない。
テストはEditor限定。クリア画面の選択を経由する仕様に合わせ、従来の自動攻略テストも更新した。テストは進行記録を退避・復元する。

## 検証

pause-test.txt、phase2-test.txt〜phase5-test.txt: 機能ごとのPlay Mode検証。
各段階で先行機能も再確認した。
総合攻略・既存回帰検証の結果は別ファイルへ記録する。

## フォント

Noto Sans CJK JP Regularを公式notofonts/noto-cjkから未改変で同梱。
配布時にStreamingAssets/Licenses/NotoSansCJK-LICENSE.txtも含める。
フォントの元データには権利表示が含まれる。フォント本体の取り込み設定はincludeFontData=1。
出典: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Japanese
ライセンス原文: https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE

公開先用のビルド設定、販売・申請手続き、別PC/OSでの確認は今回の対象外。
