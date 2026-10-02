# Stage1とステージ制作ツール

Stage1を「足場を残して、もう一度」という導入ステージに作り直しました。既存のPlayer、クローン、コイン、Goal、カメラを使用します。敵は導入では配置せず、操作の学習を優先しています。

## Unityで開く

`Assets/Scenes/Stage/Stage1.unity`を開いてPlayします。A/Dまたは左右矢印で移動、Spaceでジャンプ、左クリックを離すとクローンを残します。Spaceの長押しで高く跳べます。各エリアには操作案内が表示されます。

旧Stage1は`Assets/Scenes/Archive/Stage1_BeforeAuthoring.unity`に保存しています。Stage2とビルド対象Sceneの一覧は変更していません。Stage1のゴールは既存Stage2へ進みます。

## 配置

1マス＝1 Unityユニット。範囲の右端は含みません。地面の標準上面はy=0です。

| エリア | x範囲 | 内容・目的 |
|---|---|---|
| A | -2〜12 | 開始(2.5,0.5)、x=8〜11の高さ1の段差で移動と長押しジャンプ |
| B | 12〜27 | x=20〜24の高さ4の段差。手前にクローンを残して乗り、ジャンプで攻略 |
| C | 27〜40 | コイン(28.5,1.2)、高さ2の足場x=31〜34、高さ3の任意足場x=34〜38とコイン(36,4.2) |
| D | 40〜56 | 穴x=43〜46、トゲy=-3〜-2。地面からの長押しジャンプで越える |
| E | 56〜76 | 高さ4の足場x=60〜65でクローンの応用。高さ2の下りx=65〜67、ゴール(70.5,0.5) |

独立したチェックポイントは追加していません。既存の「最後に接地した地面付近へ戻る」仕組みを使用します。クローン生成後は戻った位置から着地するまで操作がロックされます。

標準Playerの地上速度は6、空中速度は3.6。長押しジャンプの概算は高さ3.68、同じ高さへの横移動5.27です。短押しでは大きく減ります。高さ4の段差はクローンを使う課題で、通常ジャンプだけで越える前提にはしていません。概算より実際のPlay結果を優先してください。

## 制作の繰り返し

1. `Tools > Stage Authoring > ステージ制作`を開き、Stage1の配置データを選びます。
2. 配置データのInspectorでTerrain、Hazards、Placements、Areasを編集します。TerrainとHazardsはRectIntの位置・幅・高さで指定します。Placementsには既存Prefabと座標を指定します。
3. Scene上の範囲表示とジャンプ目安を確認し、「配置と参照をチェック」を押します。これは必須参照や開始・ゴールの地面を確認するもので、攻略可能性の証明ではありません。
4. 「現在のSceneの生成配置だけを更新」を押してからSceneを保存します。生成ルート内の手修正は置き換わります。ルート外の装飾は残ります。更新はUndo可能です。
5. Playして、長押し／短押し、コイン未取得、落下、リスポーン、クローンの上限、ゴールを確認します。問題があればデータを直して再生成します。

次のステージは配置データを複製し、sceneNameをStage2等にして、別の保存先へ「新規Sceneを作成」します。このボタンは既存Sceneを上書きしません。実際のScene名とsceneNameを一致させてください。新規Sceneを遊べるようにするにはBuild ProfilesのScene Listにも登録します。

`Assets/Scenes/Templates/StageTemplate.unity`は基本の床・Player・Goalを備えたテンプレートです。RuntimeのPlayerなどを作り直さず、EditorでTilemapとPrefabを配置する方式です。地形はStatic Rigidbody2DとCompositeCollider2Dでタイル境界をまとめ、トゲはSpikeレイヤーのTriggerにします。AudioManagerは独立したSceneルート、GoalはStaticでStageManagerを明示参照します。

## 検証

`StageAuthoringBootstrap.Build`は初回構築用で、Stage1を再生成します。普段は制作ウィンドウを使用してください。

`StageAuthoringPlaytest.Run`はUnityのPlay Modeで仮想入力を送り、移動・段差・クローン・コイン・穴・ゴールを確認します。`StageAuthoringValidation.Run`は再生成時の装飾保持と構成を確認し、既存のゲーム機能・背景の回帰チェックも実行します。いずれもバッチ実行用で終了時にUnityを閉じます。結果はこのフォルダのテキストと`.repair-checkpoints/unity-validation.txt`に出力します。

自動操作では初見の分かりやすさや背景の見た目・音の品質まで判断できないため、最後にUnity Editorで手動プレイしてください。

今回の検証では、Play Modeの攻略・収集・トゲからの復帰・Stage2への遷移の23項目、および既存機能の75項目がすべて通りました。再生成で手置きの装飾を残せることも確認しています。開始地点、クローン用段差、穴のカメラ描画画像も確認済みです。画像は`Stage1_View_4.png`、`Stage1_View_21.png`、`Stage1_View_44.png`です。これらは指定位置からの静止画で、ゲーム中の操作案内は含みません。

描画画像はバッチ用の`StageAuthoringPreview.Run`で再出力できます。既存Stage1の保存コピーは元のSceneと内容が一致することを確認しています。
