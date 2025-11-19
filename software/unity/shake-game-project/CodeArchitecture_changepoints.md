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
