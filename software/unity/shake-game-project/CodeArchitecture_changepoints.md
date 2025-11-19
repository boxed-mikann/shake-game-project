# アーキテクチャ変更点

このファイルは、CodeArchitecture.mdに記載されているアーキテクチャから変更を行った点を記録します。
変更が確定したら、CodeArchitecture.mdに反映してこのファイルをクリアします。

---

## 変更履歴

### 2025-11-19: 入力システム・ハンドラー統合リファクタリング
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **IInputSource**: UnityEvent廃止 → TryDequeue方式（約3倍高速化）
2. **IShakeHandler**: HandleShake(string, double) に引数追加
3. **Handler統合**: Phase1～7ShakeHandler（7個）→ NoteShakeHandler + RestShakeHandler（2個）
4. **ShakeResolver**: 直接呼び出し方式 + Strategyパターン
5. **GameConstants**: LAST_SPRINT_SCORE=2, REST_PENALTY=-1 追加

#### 反映日
2025-11-19 - CodeArchitecture.md セクション 3.0.2, 3.0.3, 3.2（Input層）, 3.5（Handlers層）に反映

---

### 2025-11-19: インターフェースとイベントシステムの修正（初期実装）
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **IInputSource**: `event UnityAction` → `UnityEvent` プロパティ（AddListener/RemoveListener対応）
2. **静的イベントアクセス**: Instance経由 → クラス名で直接アクセス
3. **Update()削除**: IInputSourceから削除（MonoBehaviour競合回避）

#### 反映日
2025-11-19 - 上記の入力システムリファクタリングで完全に置き換えられたため、履歴のみ記録

---

### 2025-11-19: タイトル画面復帰機能の実装
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **GameManager.OnShowTitle**: 新規イベント追加（タイトル画面表示用）
2. **GameManager.ShowTitle()**: 新規メソッド追加（タイトル画面表示＋状態リセット）
3. **GameManager.Start()**: 起動時に自動的にShowTitle()を呼び出し
4. **PanelController**: OnShowTitleイベント購読、イベント駆動でパネル表示
5. **全マネージャー**: OnShowTitle購読で状態リセット処理を実装
   - PhaseManager: Coroutine停止、フェーズインデックス・状態変数リセット
   - ScoreManager: スコアをリセット（既存Initialize()再利用）
   - FreezeManager: Coroutine停止、凍結状態解除
   - NoteManager: アクティブなNoteをすべてプールに返却
   - NoteSpawner: スポーンCoroutineを停止
   - ShakeResolver: 入力キューをクリア、ハンドラーをリセット

#### 理由
- ゲーム終了後にタイトル画面に戻る機能が未実装
- 各マネージャーの状態リセット処理が不完全
- DRY原則：アプリ起動時とタイトル復帰を同一イベント（OnShowTitle）で処理

#### 設計原則の準拠
- **イベント駆動設計**: GameManager.OnShowTitleイベントで全システムに通知
- **DRY原則**: 起動時とタイトル復帰を統一処理
- **責務の分離**: 各マネージャーが自身の状態リセット処理を実装
- **疎結合**: イベント経由の通信により、GameManagerは各マネージャーの詳細を知らない

#### 影響範囲
- GameManager.cs: OnShowTitleイベント追加、ShowTitle()メソッド追加、Start()追加
- PanelController.cs: OnShowTitle購読、OnShowTitle()ハンドラー実装
- PhaseManager.cs: OnShowTitle購読、ResetPhaseManager()実装
- ScoreManager.cs: OnShowTitle購読（既存Initialize()再利用）
- FreezeManager.cs: OnShowTitle購読、ResetFreezeState()実装
- NoteManager.cs: OnShowTitle購読（既存ClearAllNotes()再利用）
- NoteSpawner.cs: OnShowTitle購読、StopSpawning()実装
- ShakeResolver.cs: OnShowTitle購読、ResetResolver()実装

#### 反映日
2025-11-19 - CodeArchitecture.md 以下のセクションに反映完了：
- セクション 3.1: GameManager.cs（OnShowTitleイベント、ShowTitle()メソッド追加）
- セクション 3.1: PhaseManager.cs（ResetPhaseManager()追加）
- セクション 3.1: FreezeManager.cs（ResetFreezeState()追加）
- セクション 3.1: ScoreManager.cs（Initialize()の用途拡張）
- セクション 3.3: NoteManager.cs（ClearAllNotes()の用途拡張）
- セクション 3.3: NoteSpawner.cs（StopSpawning()追加）
- セクション 3.2: ShakeResolver.cs（ResetResolver()追加）
- セクション 3.6: PanelController.cs（OnShowTitle購読）
- セクション 7: イベント一覧（OnShowTitleイベント追加）
- セクション 9.1: 実装状況（タイトル画面復帰機能を完了項目に追加）

---

### 2025-11-19: 音符画像のバリエーション機能の実装（SpriteManager導入）
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **SpriteManager.cs**: 新規作成（共通スプライト・プリロード管理）
   - 音符・休符画像をIDベースでペア管理
   - Inspector上で画像配列を設定可能
   - シングルトンパターンで実装
2. **Note.cs**: 画像バリエーション対応
   - `_spriteID`, `_cachedNoteSprite`, `_cachedRestSprite` フィールド追加
   - `SetSpriteID(int id)` メソッド追加（画像参照をキャッシュ）
   - `UpdateSprite()` メソッド追加（フェーズに応じた画像更新）
   - `ResetState()` にキャッシュクリア処理追加
3. **NoteSpawner.cs**: ランダムID設定機能追加
   - 音符生成時に `SpriteManager.GetRandomSpriteID()` でランダムID取得
   - `note.SetSpriteID(randomID)` で画像設定

#### 理由
- 現状は固定の1枚の音符画像のみで、視覚的バリエーションがない
- Assets/Media/Sprites に複数の音符画像が存在するが活用されていない
- ゲームプレイの視覚的豊かさを向上させる
- フェーズ切り替え時の音符⇔休符の対応関係を保つ必要がある

#### 設計原則の準拠
- **IDベース管理**: 同じIDで音符⇔休符の画像をペアで取得
- **キャッシュ最適化**: 生成時に1回だけ画像参照を取得、フェーズ切り替え時は高速アクセス
- **共通スプライト**: 画像実体は1つ、複数のNoteから参照（メモリ効率的）
- **後方互換性**: SpriteManagerがなくても従来のInspector設定で動作
- **責務の分離**: SpriteManagerが画像管理、Noteが表示、NoteSpawnerが生成
- **イベント駆動**: 既存の`PhaseManager.OnPhaseChanged`購読機能を維持

#### パフォーマンス特性
- 生成時: SpriteManagerへのアクセス **2回のみ**（音符画像1回 + 休符画像1回）
- フェーズ切り替え時: **0回**（キャッシュから取得、フィールドアクセスのみ）
- メモリオーバーヘッド: **16バイト/Note**（参照2つ、各8バイト）
- 画像データ: **0バイト増加**（実体は共有、参照のみ保持）

#### 影響範囲
- SpriteManager.cs: 新規作成（Assets/Scripts/Managers/SpriteManager.cs）
- Note.cs: フィールド追加、メソッド追加、既存メソッド修正
- NoteSpawner.cs: SpawnOneNote() に画像ID設定処理を追加

#### 反映日
2025-11-19 - CodeArchitecture.md 以下のセクションに反映完了：
- セクション 3.1: Managers/ - SpriteManager.cs を追加
- セクション 3.3: Gameplay/ - Note.cs の機能説明を更新（IDベース・キャッシュ方式）
- セクション 3.3: Gameplay/ - NoteSpawner.cs の機能説明を更新（ランダムSprite ID設定）
- セクション 4: フォルダ構成 - SpriteManager.cs をManagersフォルダに追加
- セクション 9.1: 実装状況 - 音符画像のバリエーション機能を完了項目に追加

---

### 2025-11-19: UI表示機能の追加と不具合修正（ImplementationTasks.md 修正1～5）
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **修正1: 休符モードで生成された音符が休符画像になっていない**
   - NoteSpawner.cs: `_currentPhase`フィールド追加
   - NoteSpawner.OnPhaseChanged(): 先頭で`_currentPhase`を更新
   - NoteSpawner.SpawnOneNote(): `note.SetPhase(_currentPhase)`呼び出しを追加

2. **修正2: ラストスパートでもフリーズを有効にする**
   - FreezeManager.StartFreeze(): LastSprintPhase無効化ブロックを削除
   - FreezeManager.cs: クラスドキュメントから該当記述を削除

3. **修正3: ゲーム全体のタイマー表示（TextMeshPro）**
   - TimerDisplay.cs: 新規作成（Assets/Scripts/UI/TimerDisplay.cs）
   - GameManager.OnGameStart/OnShowTitleを購読
   - GAME_DURATIONからカウントダウン表示
   - StringBuilderでGC削減

4. **修正4: フェーズ表示（TextMeshPro）**
   - PhaseDisplay.cs: 新規作成（Assets/Scripts/UI/PhaseDisplay.cs）
   - PhaseManager.OnPhaseChangedを購読
   - フェーズ名を表示（♪ 音符フェーズ、💤 休符フェーズ、🔥 ラストスパート）
   - StringBuilderでGC削減

5. **修正5: 最終スコア表示（TextMeshPro）**
   - ResultScoreDisplay.cs: 新規作成（Assets/Scripts/UI/ResultScoreDisplay.cs）
   - GameManager.OnGameOverを購読
   - ScoreManagerから最終スコアを取得して表示
   - StringBuilderでGC削減

#### 理由
- 修正1: RestPhase中に生成される音符が、生成直後は音符画像のまま表示される不具合を修正
- 修正2: LastSprintPhase中もフリーズを有効にすることでゲームバランスを改善
- 修正3～5: ゲーム中の情報表示が不足しているため、ユーザビリティ向上のためUI追加

#### 設計原則の準拠
- **イベント駆動設計**: GameManager/PhaseManagerのイベントを購読
- **責務分離**: 各UIクラスは単一の表示責務を持つ
- **疎結合**: シングルトン参照を最小化、イベント経由で通信
- **メモリリーク防止**: OnDestroy()で必ずイベント購読解除
- **GC削減**: StringBuilderを再利用

#### 影響範囲
- NoteSpawner.cs: フィールド追加、メソッド修正
- FreezeManager.cs: コード削除、ドキュメント修正
- TimerDisplay.cs: 新規作成
- PhaseDisplay.cs: 新規作成
- ResultScoreDisplay.cs: 新規作成

#### 反映日
2025-11-19 - CodeArchitecture.md 以下のセクションに反映完了：
- セクション 3.3: NoteSpawner.cs（生成時のフェーズ設定処理追加、実装例更新、備考追加）
- セクション 3.1: FreezeManager.cs（LastSprintPhase無効化の削除、備考追加）
- セクション 3.6: UI/ - TimerDisplay.cs, PhaseDisplay.cs, ResultScoreDisplay.cs 追加（各クラスの詳細説明）
- セクション 4: フォルダ構成 - UI層に新規クラス3つ追加（コメント付き）
- セクション 9.1: 実装状況 - UI表示機能追加を完了項目7として追加（全修正内容を詳細に記載）

---

### 2025-11-19: UI改善（スライダー表示改善 + 音符生成範囲の画面内制限）
**ステータス**: ✅ CodeArchitecture.mdに反映済み

#### 変更内容
1. **PhaseProgressBar.cs: スライダー表示改善**
   - 進行度計算を反転: `progress = remainingTime / totalDuration` で1→0に減る表示
   - 初期値を1に変更（満タン状態から開始）
   - フェーズごとの色分け機能追加:
     - NotePhase: 青系 `(0.3f, 0.5f, 1f)`
     - RestPhase: オレンジ系 `(1f, 0.6f, 0.2f)`
     - LastSprintPhase: 赤系 `(1f, 0.2f, 0.2f)`
   - Inspector設定可能なカラーフィールド追加
   - `_fillImage`フィールド追加（Slider.fillRectのImageコンポーネント参照）
   - `Start()`で`_fillImage`をキャッシュ
   - `OnPhaseChanged()`でフェーズに応じた色変更処理追加

2. **GameConstants.cs: 定数追加**
   - `NOTE_SPAWN_MARGIN = 0.9f` 追加（音符生成範囲のマージン、画面サイズの90%以内）

3. **NoteSpawner.cs: 動的生成範囲計算**
   - フィールド追加:
     - `_calculatedRangeX`, `_calculatedRangeY` (Inspector表示用)
     - `_mainCamera` (カメラ参照)
   - 既存の`spawnRangeX/Y`をフォールバック用に変更
   - `CalculateSpawnRange()`メソッド新規追加:
     - カメラの`orthographicSize`とアスペクト比から画面範囲を計算
     - `NOTE_SPAWN_MARGIN`を適用して生成範囲を設定
     - DEBUG_MODEでログ出力
   - `OnEnable()`に範囲計算処理を追加（ゲームスタート時のカメラ状態を参照）
   - `SpawnOneNote()`で動的計算された範囲を使用（カメラがない場合はフォールバック）

#### 理由
- **スライダー改善**: 残り時間の直感的な把握、フェーズの視覚的区別を実現
- **生成範囲制限**: 任意の解像度・アスペクト比で音符が画面外に生成されるのを防止

#### 設計原則の準拠
- **イベント駆動設計**: PhaseManager.OnPhaseChangedを購読（既存設計を維持）
- **疎結合**: PhaseManagerへの依存は既存イベントのみ
- **Inspector設定**: 色やマージン値を調整可能
- **後方互換性**: カメラ未設定時はInspector設定値をフォールバック
- **デバッグ性**: 計算結果をInspectorで確認可能、DEBUG_MODEでログ出力

#### パフォーマンス特性
- **スライダー色変更**: フェーズ変更時（数秒に1回）のみ実行
- **生成範囲計算**: OnEnable()時（ゲームスタート時）に1回のみ実行
- **毎フレーム**: 既存処理（進行度計算、スライダー更新）のみ

#### 影響範囲
- PhaseProgressBar.cs: フィールド追加、Start()修正、OnPhaseChanged()修正、Update()修正
- GameConstants.cs: 定数追加
- NoteSpawner.cs: フィールド追加、OnEnable()修正、CalculateSpawnRange()新規追加、SpawnOneNote()修正

#### 反映日
2025-11-19 - CodeArchitecture.md 以下のセクションに反映完了：
- セクション 3.6: UI/PhaseProgressBar.cs - 機能説明更新（色分け、反転表示、最適化情報追加）
- セクション 3.3: Gameplay/NoteSpawner.cs - 機能説明更新（動的範囲計算、データ構造、備考追加）
- セクション 9.1: 実装状況 - UI改善を完了項目8として追加（全変更内容を詳細に記載）

---

### 2025-11-19 19:25:04 HandleShake処理順序変更
- ラグ感の低減を狙って、SE再生を音符破棄の前、最初に持ってきた

## 今後の変更記録用テンプレート

### YYYY-MM-DD: [変更タイトル]
**ステータス**: 🔄 作業中 / ✅ 反映済み / ❌ 却下

#### 変更内容
- [変更点1]
- [変更点2]

#### 理由
[なぜこの変更が必要か]

#### 影響範囲
- [影響を受けるファイル・クラス]

#### 反映日
YYYY-MM-DD - CodeArchitecture.md [セクション番号] に反映
