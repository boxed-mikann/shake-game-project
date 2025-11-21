# シェイク検知システム 検証用プログラム群

## 概要

このプログラム群は、現在のシェイク検知システムの動作を検証するために、フレームごとの詳細なデータを収集・可視化します。

**構成：**
1. **子機ファームウェア** (`child_device_validation.ino`) - センサー値と監視中の全変数をフレームごと親機に送信
2. **親機ファームウェア** (`parent_device_validation.ino`) - 受け取ったデータをProcessingに転送
3. **Processing検証アプリ** (`shake_game_validation.pde`) - リアルタイムグラフ表示、CSV保存

---

## 機能詳細

### 子機ファームウェア (`child_device_validation.ino`)

毎フレーム（50ms=20FPS）以下のデータを送信します：

| データ項目 | 型 | 説明 |
|---------|------|------|
| frameCount | uint32_t | フレーム番号（0から連続カウント） |
| acX, acY, acZ | int16_t | MPU-6050からの生加速度値 |
| totalAccel | float | 合成加速度（√(x² + y² + z²)） |
| jerk | float | ジャーク（加速度の変化率） |
| dotProduct | int32_t | ベクトル内積（シェイク判定用） |
| shakeCount | int | シェイク検知カウント |
| isShaking | uint8_t | 現在のシェイク状態（0=IDLE, 1=SHAKING） |
| baseVecX, baseVecY, baseVecZ | int16_t | シェイク開始時の基準ベクトル成分 |
| childID | int | 子機ID |

**送信フォーマット：** ESP-NOW（無線）で親機に送信

---

### 親機ファームウェア (`parent_device_validation.ino`)

受け取ったデータをシリアルにCSV形式で出力します：

```
VDATA,frameCount,childID,acX,acY,acZ,totalAccel,jerk,dotProduct,shakeCount,isShaking,baseVecX,baseVecY,baseVecZ
```

**例：**
```
VDATA,0,0,150,200,-100,315.2,125.3,0,0,0,0,0,0
VDATA,1,0,145,205,-95,320.1,128.5,0,0,0,0,0,0
VDATA,2,0,2000,1500,500,2627.5,5420.1,150,1,1,125,94,31
```

---

### Processing検証アプリ (`shake_game_validation.pde`)

リアルタイムでデータをグラフ化し、複数の表示モードで検査できます。

#### 表示モード（Mキーで切り替え）

1. **AcX, AcY, AcZ** - 3軸の加速度を個別に表示（赤・緑・青）
   - スケール：±30000
   - 用途：センサー値の動きを確認

2. **Total Accel & Jerk** - 合成加速度を表示
   - スケール：0～30000
   - 用途：全体的な動きの大きさを確認

3. **Shake Count & State** - シェイク検知数と状態
   - 青いハイライト：シェイク状態
   - 用途：検知タイミングを確認

4. **Dot Product** - ベクトル内積を表示
   - スケール：±50000
   - 赤いライン：閾値（0）
   - 用途：振り戻し判定ロジックを確認

5. **Base Vector Components** - 基準ベクトルの各成分
   - スケール：±100
   - 用途：シェイク開始時の方向を確認

#### キーボード操作

| キー | 機能 |
|------|------|
| **SPACE** | 一時停止・再開 |
| **M** | 表示モード切り替え |
| **0-9** | 子機ID選択（複数の子機から1つを選択） |
| **E** | CSVファイルに出力 |
| **C** | グラフデータをクリア |
| **R** | シリアルポートに再接続 |

#### グラフ操作

- **水平方向** - 時間軸（左から右へフレーム進行）
- **垂直方向** - 各データの値（センサーごとにスケーリング）
- **最大表示点数** - 500フレーム（デフォルト）

---

## セットアップ手順

### 1. ハードウェア準備

- 親機(ESP32)と子機(複数)を用意
- 両方をUSB（またはシリアル）で接続
- LEDをGPIO 13に接続（子機）

### 2. ファームウェアアップロード

**子機：**
```
ファイル: firmware/child_device/child_device_validation/child_device_validation.ino
Arduino IDEで開く → シリアルポート選択 → アップロード
```

**親機：**
```
ファイル: firmware/parent_device/parent_device_validation/parent_device_validation.ino
Arduino IDEで開く → シリアルポート選択 → アップロード
```

### 3. Processingアプリ実行

```
ファイル: software/processing/shake_game_validation/shake_game_validation.pde
Processing IDEで開く → 実行ボタンをクリック
```

シリアルポートが自動検出されます。接続状況がウィンドウ左上に表示されます。

---

## 操作フロー

### 1. データ収集

1. Processingアプリを起動
2. 子機を振動させる
3. グラフにリアルタイムで各値が表示される
4. 複数の子機を使用する場合は数字キー(0-9)で切り替え

### 2. 検証作業

1. **Mキー** で異なる表示モードを選択
2. 各モード中心にデータを観察
3. 異常値や予期しない動作があれば、グラフから判定タイミングを確認

### 3. データ保存

1. **Eキー** でCSVを出力
2. `validation_YYYYMMDD_HHMMSS.csv` が生成される
3. ExcelやPythonで詳細分析可能

### 4. 調整・改善

ファームウェアのパラメータを変更する場合：

**子機（`child_device_validation.ino`）：**
```cpp
const float JERK_THRESHOLD = 10000.0;        // ジャーク閾値
const int32_t DOT_PRODUCT_THRESHOLD = 0;     // 内積閾値
const int DEBOUNCE_DELAY = 0;                // デバウンス時間
const int VECTOR_SHIFT = 2;                  // ベクトルシフト量
```

パラメータ変更後、再度アップロード → Processingで検証

---

## トラブルシューティング

### シリアル接続できない

- 親機を確認（usually COM3～COM5）
- Processingコンソールに "No serial ports found!" と出ている
- → ProcessingスケッチのPortIndex を手動で変更（接続情報に合わせて）

### データが途切れ途切れ

- 無線の干渉の可能性
- ESP-NOWの送受信リトライを確認
- 子機と親機の距離を1m以内に

### グラフが見切れる

- ウィンドウサイズを大きくする（スケール自動調整）
- 表示モードを切り替えて見やすい表示を選択

### CSV出力に全データが含まれていない

- Processingで表示しているのは直近500フレーム
- スクロールして古いデータを見たい場合は、別途アップロード対応が必要

---

## 技術詳細

### データ通信フロー

```
[子機] --ESP-NOW--> [親機] --Serial@115200--> [Processing]
20FPS              転送遅延<1ms                リアルタイム表示
```

### センサースケーリング

各グラフは、異なるデータ値域に対応するため、スケーリング率が異なります：

| 項目 | スケール範囲 | 用途 |
|-----|-----------|------|
| Accel (X,Y,Z) | ±30,000 | 加速度の振れ幅を表示 |
| Total Accel | 0～30,000 | 全体的な動き |
| Jerk | 0～100,000 | ジャークの急激さ |
| Dot Product | ±50,000 | 内積による振り戻し判定 |
| Base Vector | ±100 | シェイク開始時の方向 |

---

## 参考：検知アルゴリズムの可視化

このシステムでは以下のアルゴリズムが動作しています：

### 1. ジャーク検知フェーズ（isShaking = false）
- `jerk > JERK_THRESHOLD` でシェイク開始
- 基準ベクトルを保存：`baseVec = (AcX - prevAcX, AcY - prevAcY, AcZ - prevAcZ)`
- この時点で `isShaking = true`, `shakeCount++`
- 内積は0で送信される

### 2. 振り戻し判定フェーズ（isShaking = true）
- **毎フレーム現在の差分ベクトルを計算**：`currentDelta = (AcX - prevAcX, AcY - prevAcY, AcZ - prevAcZ)`
- **毎フレーム内積を計算・送信**：`dotProduct = baseVec · currentDelta`
  - これはProcessingの「Dot Product History (Detailed)」グラフに表示される実際の判定値
  - シェイク状態が終わるまで、毎フレームこの値が更新される
- `dotProduct <= 0` で振り戻し判定成立、`isShaking = false` に遷移

**グラフでの確認方法：**
- **モード4「Dot Product (Shake State)」** - 簡潔な表示
- **モード5「Dot Product History (Detailed)」** - 詳細表示
  - 紫のライン：毎フレームの内積値
  - オレンジの点：シェイク中に実際に計算・判定に使われた値
  - 赤いライン：判定閾値（0）← ここより下（負）になると振り戻し判定成立

---

## CSV出力フォーマット

生成されるCSVの例：

```csv
FrameCount,ChildID,AcX,AcY,AcZ,TotalAccel,Jerk,DotProduct,ShakeCount,IsShaking,BaseVecX,BaseVecY,BaseVecZ
0,0,150,200,-100,315.20,125.30,0,0,0,0,0,0
1,0,145,205,-95,320.10,128.50,0,0,0,0,0,0
...
250,0,2000,1500,500,2627.50,5420.10,150,1,1,125,94,31
...
```

このCSVを使用して、外部ツール（Python、Matlab等）でさらに詳細な分析が可能です。

---

**作成日**: 2025/11/21  
**バージョン**: 1.0
