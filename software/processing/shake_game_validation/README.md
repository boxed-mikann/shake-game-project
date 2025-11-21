# クイックスタートガイド - シェイク検知検証システム

## 1分で始める

### パート1：ファームウェアのアップロード（Arduino IDE使用）

**子機の場合：**
1. Arduino IDEで以下を開く：
   ```
   firmware/child_device/child_device_validation/child_device_validation.ino
   ```
2. ツール → ボード → "ESP32 Dev Module" を選択
3. ツール → シリアルポート → 子機のポートを選択
4. アップロードボタンをクリック

**親機の場合：**
1. Arduino IDEで以下を開く：
   ```
   firmware/parent_device/parent_device_validation/parent_device_validation.ino
   ```
2. ツール → ボード → "ESP32 Dev Module" を選択
3. ツール → シリアルポート → 親機のポート を選択
4. アップロードボタンをクリック

### パート2：Processingで表示

1. Processingをインストール（https://processing.org/download/）
2. Processing IDEで以下を開く：
   ```
   software/processing/shake_game_validation/shake_game_validation.pde
   ```
3. 実行ボタン（▶）をクリック
4. ウィンドウが立ち上がり、シリアルポート接続情報が表示される

### パート3：検証開始

1. 子機を揺らす
2. グラフに加速度・ジャーク・シェイク検知状態がリアルタイムで表示される
3. **M キー** で表示モードを切り替えて異なる値を確認
4. **E キー** で CSV に保存

---

## 主なキー操作

| キー | 動作 |
|------|------|
| M | 表示モード切り替え（5種類） |
| SPACE | 一時停止・再開 |
| 0-9 | 子機ID選択 |
| E | CSVエクスポート |
| C | グラフクリア |
| R | 再接続 |

---

## 表示モード説明（M キーで切り替え）

1. **AcX, AcY, AcZ** - 3軸の加速度
2. **Total Accel & Jerk** - 全体的な動きとジャーク
3. **Shake Count & State** - シェイク検知カウント＆状態
4. **Dot Product** - 内積（振り戻し判定に使用）
5. **Base Vector Components** - シェイク開始時の方向情報

---

## よくある問題

**Q: シリアル接続できない**  
A: Processingのコンソール出力を確認。"No serial ports found!" が出ていれば、親機をUSBで接続し直す。または `shake_game_validation.pde` の `portIndex` を手動で変更。

**Q: グラフが表示されない**  
A: 1～2秒待つ。データが到着すると自動表示される。キー'C'でクリアして再スタート。

**Q: CSV出力後、データが少ない**  
A: 正常な挙動。画面に表示される直近500フレームのみ出力される。長時間記録したい場合は、Processingのコード改修が必要。

---

## 次のステップ

- 詳細ドキュメント：`docs/VALIDATION_SYSTEM.md` を参照
- アルゴリズムの仕様：`docs/shake_detection_algorithm.md` を参照

---

**バージョン**: 1.0  
**更新日**: 2025/11/21
