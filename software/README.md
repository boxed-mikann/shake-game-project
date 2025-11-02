# Processing Display Software

親機から受信したデータをリアルタイムで表示するProcessingプログラムです。

## 動作環境

- Processing 4.0以上
- USB Serial ライブラリ（Processing付属）

## セットアップ

1. `shake_game_display.pde` をProcessingで開く
2. COM ポートを確認（親機が接続されているポート）
3. コード内の `Serial.list()[0]` を必要に応じて修正
4. ▶ ボタンで実行

## 表示内容

- 全プレイヤー（20人）のシェイク回数
- 加速度値（リアルタイム）
- グリッドレイアウト（10×2）

## カスタマイズ

`draw()` 関数内で表示位置やサイズを調整できます。