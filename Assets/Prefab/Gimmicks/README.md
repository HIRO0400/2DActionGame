# ステージ配置用ギミック

`Assets/Prefab/Gimmicks` のプレハブをHierarchyへドラッグして配置します。
既存のPlayer、Ground、Cloneレイヤーとステージの処理を利用します。

| プレハブ | 動作・調整箇所 |
|---|---|
| PressureSwitchDoor | 床スイッチと扉のセット。Playerまたはクローンがスイッチ上にいる間、扉が開きます。子のSwitchDoor_PlateとSwitchDoor_Doorを移動して配置します。PressurePlateのDoor参照は設定済みです。 |
| MovingPlatform | 下から通過できる移動足場。Travelは配置位置から折り返し地点までの移動量、Periodは往復にかかる秒数です。初期値は横5、5秒。 |
| CollapsingPlatform | 乗ると点滅し、0.85秒後に崩れます。3秒後、Playerと重ならないタイミングで復元します。Collapse Delay、Restore Delayで調整できます。 |
| OneWayPlatform | 下から通過し、上から着地できる単体の足場。 |
| OneWayTilemap | 一方通行床を描くためのGridと設定済みTilemap。見本の3タイルを配置済みです。 |

## スイッチと扉

セットの親オブジェクトを移動すると、スイッチと扉も一緒に移動します。
親のY座標を地面の上面に合わせると、初期配置ではスイッチは左4、扉は親の位置に配置されます。
スイッチの下には通常の地面を用意してください。スイッチ自身は床ではありません。
扉はPlayerやクローンが中にいる間は閉まりません。
セットを複製しても参照は複製したセット内の扉につながります。

## 足場の大きさ

移動・崩れる・単体の一方通行足場は幅3、高さ0.4です。
大きさを変える場合はSprite RendererのSizeとBox Collider 2DのSizeを揃えてください。
親のScaleは(1,1,1)、Rotationは(0,0,0)のまま、Positionで配置してください。
特に扉・崩れる足場の安全判定はこの配置を前提としています。
移動・崩れる足場はリスポーン地点を更新しません。

## Tile Paletteで一方通行床を描く

1. `OneWayTilemap.prefab`をHierarchyへドロップします。
2. `Window > 2D > Tile Palette`を開き、`GimmickPalette`を選びます。
3. Hierarchyで`OneWayTilemap/OneWayFloor`を選択し、Tile Paletteの描画先（Active Tilemap）もこのオブジェクトにします。
4. パレットの水色のタイルを選び、Sceneビューに床を描きます。初期の見本3タイルは消して構いません。
5. Play Modeで下から通り抜け、上から着地できることを確認します。

タイルとパレットは`Assets/TilePalettes/Gimmicks`に保存されています。
通常のGround Tilemapへこのタイルを描いただけでは一方通行になりません。
一方通行の動作は専用TilemapのCollider、Platform Effector、StagePlatform設定で行います。
同じTilemapに高さの違う足場を描けます。床は1セル厚の横列として描くのがおすすめです。

## プレハブを追加し直す場合

`Tools > Stage Authoring > Create Missing Gimmick Prefabs`で不足しているアセットだけを作成できます。
配置済みのSceneや、編集済みの既存プレハブは上書きしません。
