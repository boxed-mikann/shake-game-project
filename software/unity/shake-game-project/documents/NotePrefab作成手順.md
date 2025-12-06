# ノートプレファブの作成手順

## 単ノート（NoteObjectV2）プレファブの作成

### 1. 基本オブジェクトを作成
1. Hierarchy で右クリック → `2D Object` → `Sprites` → `Circle`（または任意のスプライト）
2. 名前を `NotePrefab` に変更

### 2. コンポーネントをアタッチ
1. `NotePrefab` を選択
2. Inspector で `Add Component` → `NoteObjectV2` スクリプトをアタッチ
3. `Sprite Renderer` が自動的にアタッチされているはずです

### 3. 設定
- **Transform**:
  - Position: (0, 0, 0)
  - Scale: (1, 1, 1)
  
- **Sprite Renderer**:
  - Sprite: 任意のノートスプライト
  - Color: 白（デフォルト）
  - Sorting Layer: Default
  - Order in Layer: 0

- **NoteObjectV2**:
  - Sprite Renderer: 自動参照されるはず
  - Scale Start: 0.3
  - Scale End: 1.0

### 4. プレファブ化
1. `NotePrefab` オブジェクトを Hierarchy から Project ウィンドウの `Assets/Prefabs/` フォルダにドラッグ
2. Hierarchy のオブジェクトは削除してOK

---

## 長押しノート（LongNoteObjectV2）プレファブの作成

### 1. 基本オブジェクトを作成
1. Hierarchy で右クリック → `Create Empty`
2. 名前を `LongNotePrefab` に変更

### 2. 子オブジェクトを作成
#### 2-1. LineRenderer用
1. `LongNotePrefab` を選択
2. `Add Component` → `Line Renderer`

#### 2-2. HeadSprite（開始マーカー）
1. `LongNotePrefab` を右クリック → `2D Object` → `Sprites` → `Circle`
2. 名前を `HeadSprite` に変更
3. Scale を (0.5, 0.5, 0.5) に設定

#### 2-3. TailSprite（終了マーカー）
1. `LongNotePrefab` を右クリック → `2D Object` → `Sprites` → `Circle`
2. 名前を `TailSprite` に変更
3. Scale を (0.5, 0.5, 0.5) に設定

### 3. LongNoteObjectV2 スクリプトをアタッチ
1. `LongNotePrefab` を選択
2. Inspector で `Add Component` → `LongNoteObjectV2` スクリプトをアタッチ

### 4. 設定
- **Line Renderer**:
  - Positions: 2個（デフォルト）
  - Width: 0.2
  - Color Gradient: Cyan → Cyan（または任意）
  - Material: Default-Line（または Sprites-Default）

- **LongNoteObjectV2**:
  - Line Renderer: ドラッグ＆ドロップで自分自身のLineRendererを設定
  - Head Sprite: HeadSpriteのSpriteRendererをドラッグ
  - Tail Sprite: TailSpriteのSpriteRendererをドラッグ
  - Line Width: 0.2
  - Active Color: Cyan
  - Completed Color: Gray

### 5. プレファブ化
1. `LongNotePrefab` オブジェクトを Project の `Assets/Prefabs/` フォルダにドラッグ
2. Hierarchy のオブジェクトは削除してOK

---

## 注意点

- ノートのZ座標は -1 程度に設定（背景より手前に表示）
- デバイスアイコンのZ座標は -2 程度（ノートより手前）
- プレファブは一度作成したら、Project ウィンドウから編集可能
