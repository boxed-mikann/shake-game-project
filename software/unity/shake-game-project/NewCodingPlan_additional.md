## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## 要修正項目

### 修正計画概要（2025-11-19作成）

---

### 1. 休符モードの時に生成された音符が休符になっていない

**現状分析**：
- `NoteSpawner.cs`（150-228行目）でSpawn時に`SetSpriteID()`を呼んでいる
- `Note.cs`（1-168行目）で`PhaseManager.OnPhaseChanged`を購読して画像を切り替え
- **問題点**：生成時に現在のフェーズ情報が`Note`に伝わっていない
  - `NoteSpawner`は生成時にSpriteIDのみ設定し、フェーズは設定していない
  - `Note`はイベント購読で次のフェーズ変更時に初めて画像を更新
  - つまり、RestPhaseで生成された音符は、生成直後は音符画像のまま表示される

**修正方針（CodeArchitecture.md準拠）**：
1. `NoteSpawner`はフェーズ変更を保持（既に`PhaseManager.OnPhaseChanged`を購読中）
2. `SpawnOneNote()`で現在保持しているフェーズを使用
3. `Note.SetPhase(Phase phase)`を生成直後に呼び出す
4. これにより生成時に正しい画像（音符/休符）が即座に表示される

**設計的考察**：
- **選択肢1**: `PhaseManager.Instance.GetCurrentPhase()`を毎回呼ぶ
  - メリット: シンプル
  - デメリット: シングルトン参照のオーバーヘッド
- **選択肢2**: `NoteSpawner`がフェーズ情報をローカル保持（推奨）
  - メリット: 既に`OnPhaseChanged`を購読しており、`PhaseChangeData`を受け取っている
  - メリット: フェーズ情報をフィールドに保存すれば、Spawn時は即座にアクセス可能
  - メリット: PhaseManagerへの依存を減らし、疎結合を維持
  - 実装: `private Phase _currentPhase`フィールドを追加

**修正箇所**：
- ファイル：`Assets/Scripts/Gameplay/NoteSpawner.cs`
- 追加フィールド：`private Phase _currentPhase = Phase.NotePhase;`
- 修正メソッド1：`OnPhaseChanged()`でフェーズを保存
- 修正メソッド2：`SpawnOneNote()`で保存したフェーズを使用
- 追加コード（SpawnOneNote内）：
  ```csharp
  // 現在のフェーズを設定（生成時に正しい画像を表示）
  note.SetPhase(_currentPhase);
  ```

**実装手順**：
1. `NoteSpawner.cs`にフィールド追加：`private Phase _currentPhase = Phase.NotePhase;`
2. `OnPhaseChanged(PhaseChangeData phaseData)`メソッド内の先頭で`_currentPhase = phaseData.phaseType;`を追加
3. `SpawnOneNote()`の`note.SetSpriteID(randomID)`の直後に`note.SetPhase(_currentPhase);`を追加
4. コンパイルエラーチェック
5. デバッグモードで動作確認（RestPhaseで休符画像が即座に表示されるか）

**重要**: `OnPhaseChanged()`では既に`StartCoroutine(SpawnLoop(...))`を呼んでいるため、フェーズ保存は**Coroutine開始前**に行う必要がある。

---

### 2. ラストスパートでもフリーズは効くようにする

**現状分析**：
- `FreezeManager.cs`（107-113行目）でLastSprintPhase判定が存在
- コード：
  ```csharp
  if (PhaseManager.Instance != null && 
      PhaseManager.Instance.GetCurrentPhase() == Phase.LastSprintPhase)
  {
      Debug.Log("[FreezeManager] LastSprintPhase detected, freeze disabled");
      return;  // ★ ここでフリーズを無効化
  }
  ```
- **問題点**：要件変更により、ラストスパートでもフリーズを有効にする必要がある

**修正方針（CodeArchitecture.md準拠）**：
1. `FreezeManager.StartFreeze()`からLastSprintPhase判定を削除
2. コメントも削除（ドキュメントから「無効化」記述を削除）
3. シンプルな実装に戻す

**修正箇所**：
- ファイル：`Assets/Scripts/Managers/FreezeManager.cs`
- メソッド：`StartFreeze(float duration)`（約100-120行目）
- 削除対象：
  ```csharp
  // LastSprintPhase 中は凍結しない（無効化）
  if (PhaseManager.Instance != null && 
      PhaseManager.Instance.GetCurrentPhase() == Phase.LastSprintPhase)
  {
      if (GameConstants.DEBUG_MODE)
          Debug.Log("[FreezeManager] LastSprintPhase detected, freeze disabled");
      return;
  }
  ```

**実装手順**：
1. `FreezeManager.cs`の`StartFreeze()`メソッドを開く
2. 上記のLastSprintPhase判定ブロック（約107-114行目）を削除
3. クラスドキュメント（13行目付近）の「LastSprintPhase 中は無効」記述も削除
4. コンパイルエラーチェック
5. デバッグモードで動作確認（LastSprintPhaseでもフリーズが発動するか）

---

### 3. タイマー表示(TextMeshPro)

**現状分析**：
- ゲーム全体の残り時間を表示するUIが存在しない
- `PhaseProgressBar.cs`はフェーズごとの進行度を管理（個別フェーズのタイマー）
- ゲーム全体の制限時間は`GameConstants.GAME_DURATION`で定義（PHASE_SEQUENCEの合計）
- プレイ時間は1分以下（約60秒）なので秒数表示で十分
- **重要**: PhaseManagerが既に全フェーズ完了時に`GameManager.EndGame()`を呼んでいる

**修正方針（CodeArchitecture.md準拠）**：
1. 新規UIクラス`TimerDisplay.cs`を作成（`Assets/Scripts/UI/`）
2. `ScoreDisplay.cs`（既存）をテンプレートとして活用
3. 責務：ゲーム全体の残り時間をTextMeshProで秒数表示（"45s"形式）
4. `GameManager.OnGameStart`を購読してタイマー開始
5. 毎フレームUpdate()で残り時間を減算し、TextMeshProに反映
6. **表示のみに徹する**：ゲーム終了はPhaseManagerが担当（責務分離）
7. StringBuilderでGC削減（ScoreDisplay同様）

**実装内容**：
- ファイル：`Assets/Scripts/UI/TimerDisplay.cs`（新規作成）
- 主要機能：
  - `[SerializeField] private TextMeshProUGUI _timerText;`
  - `GameManager.OnGameStart`を購読（ゲーム全体のタイマー開始）
  - `GameManager.OnShowTitle`を購読（タイマーリセット）
  - `Update()`で`_remainingTime -= Time.deltaTime`
  - 表示形式："45s"（秒数のみ、整数表示）
  - StringBuilderで文字列構築
  - **注意**: 0秒になっても`GameManager.EndGame()`は呼ばない（PhaseManagerが担当）

**設計上の重要ポイント**：
- **責務分離**: ゲーム終了判定はPhaseManagerの責務
- **TimerDisplayの責務**: 視覚的なフィードバックのみ（表示専用）
- PhaseManagerが全フェーズ完了時に`GameManager.EndGame()`を呼ぶ仕組みが既に存在
- タイマー表示が0になる直前にPhaseManagerがゲームを終了するため、自然な動作

**フェーズタイマーとの違い**：
- `PhaseProgressBar`：個別フェーズの進行度（Slider + 内部タイマー）
- `TimerDisplay`：ゲーム全体の残り時間（TextMeshPro表示）
- 完全に独立した責務

**実装手順**：
1. `Assets/Scripts/UI/TimerDisplay.cs`を新規作成
2. `ScoreDisplay.cs`を参考に基本構造をコピー
3. フィールド：`_timerText`, `_remainingTime`, `_isRunning`
4. `Start()`で`GameManager.OnGameStart.AddListener(OnGameStart)`と`GameManager.OnShowTitle.AddListener(OnShowTitle)`
5. `OnGameStart()`で`_remainingTime = GameConstants.GAME_DURATION; _isRunning = true;`
6. `OnShowTitle()`で`_isRunning = false; _remainingTime = 0f;`（タイマーリセット）
7. `Update()`で残り時間を減算、表示更新（0未満にならないようClamp）
8. フォーマット関数：`FormatTime(float seconds)` → "45s"形式（整数秒）
9. `OnDestroy()`でイベント購読解除
10. UnityエディタでTextMeshProコンポーネントをアタッチ
11. 動作確認

---

### 4. フェーズ表示(TextMeshPro)

**現状分析**：
- フェーズ情報を表示するUIクラスが存在しない
- `PhaseChangeData`には`phaseType`（NotePhase等）と`phaseIndex`が含まれる

**修正方針（CodeArchitecture.md準拠）**：
1. 新規UIクラス`PhaseDisplay.cs`を作成（`Assets/Scripts/UI/`）
2. `ScoreDisplay.cs`をテンプレートとして活用
3. 責務：現在のフェーズ名をTextMeshProで表示
4. PhaseManager.OnPhaseChangedを購読
5. フェーズタイプに応じた表示名を定義（"♪ 音符フェーズ", "💤 休符フェーズ", "🔥 ラストスパート"等）

**実装内容**：
- ファイル：`Assets/Scripts/UI/PhaseDisplay.cs`（新規作成）
- 主要機能：
  - `[SerializeField] private TextMeshProUGUI _phaseText;`
  - `PhaseManager.OnPhaseChanged`を購読
  - フェーズタイプごとの表示名マッピング
  - StringBuilderでGC削減

**実装手順**：
1. `Assets/Scripts/UI/PhaseDisplay.cs`を新規作成
2. `ScoreDisplay.cs`を参考に基本構造をコピー
3. フィールド：`_phaseText`
4. `OnPhaseChanged(PhaseChangeData data)`でフェーズ名を取得
5. フェーズタイプマッピング関数：
   ```csharp
   private string GetPhaseName(Phase phase)
   {
       switch (phase)
       {
           case Phase.NotePhase: return "♪ 音符フェーズ";
           case Phase.RestPhase: return "💤 休符フェーズ";
           case Phase.LastSprintPhase: return "🔥 ラストスパート";
           default: return "不明";
       }
   }
   ```
6. UnityエディタでTextMeshProコンポーネントをアタッチ
7. 動作確認

---

### 5. 最終スコア表示の実装

**現状分析**：
- `ScoreDisplay.cs`は既に存在し、リアルタイムスコアを表示
- `PanelController.cs`でリザルトパネルを表示（GameManager.OnGameOverで発火）
- **問題点**：リザルトパネル内に最終スコアを表示するUIクラスが存在しない

**修正方針（CodeArchitecture.md準拠）**：
1. 新規UIクラス`ResultScoreDisplay.cs`を作成（`Assets/Scripts/UI/`）
2. `ScoreDisplay.cs`と類似だが、購読イベントが異なる
3. 責務：ゲーム終了時の最終スコアを表示（GameManager.OnGameOverで取得）
4. ScoreManager.GetScore()で最終スコアを取得
5. TextMeshProに"Final Score: 123"形式で表示

**実装内容**：
- ファイル：`Assets/Scripts/UI/ResultScoreDisplay.cs`（新規作成）
- 主要機能：
  - `[SerializeField] private TextMeshProUGUI _finalScoreText;`
  - `GameManager.OnGameOver`を購読
  - `ScoreManager.GetScore()`で最終スコアを取得
  - 表示形式："Final Score: 123"
  - StringBuilderでGC削減

**実装手順**：
1. `Assets/Scripts/UI/ResultScoreDisplay.cs`を新規作成
2. `ScoreDisplay.cs`を参考に基本構造をコピー
3. フィールド：`_finalScoreText`, `_prefix = "Final Score: "`
4. `Start()`で`GameManager.OnGameOver.AddListener(OnGameOver)`
5. `OnGameOver()`で`ScoreManager.Instance.GetScore()`を取得
6. StringBuilderで"Final Score: 123"を構築して表示
7. `OnDestroy()`でイベント購読解除（メモリリーク防止）
8. Unityエディタでリザルトパネル内にTextMeshProを配置
9. InspectorでResultScoreDisplayをアタッチ
10. 動作確認

**プレイ中スコアとの違い**：
- `ScoreDisplay`：ScoreManager.OnScoreChangedを購読（リアルタイム更新）
- `ResultScoreDisplay`：GameManager.OnGameOverを購読（1回だけ表示）
- 重なる部分：StringBuilderの使い方、TextMeshProへの反映方法
- 独立性：2つのクラスは完全に独立（疎結合）

**注意事項**：
- リザルトパネル内のTextMeshProコンポーネントは、ゲーム開始時は非表示（PanelControllerが管理）
- `OnGameOver`イベント発火時にパネルが表示され、同時にスコアが更新される
- タイトル復帰時は自動的にリザルトパネルが非表示になるため、特別なリセット処理は不要

---

### 実装優先順位

1. **修正1（休符表示）**：最優先（ゲームプレイの視覚的正確性に直結）
2. **修正2（フリーズ有効化）**：高優先（ゲームバランスに影響）
3. **修正3（タイマー表示）**：中優先（ユーザビリティ向上）
4. **修正4（フェーズ表示）**：中優先（ユーザビリティ向上）
5. **修正5（最終スコア表示）**：低優先（機能完全性）

---

## 修正計画の設計整合性チェック（2025-11-19精査完了）

### ✅ 確認済み事項

#### 1. イベント購読の整合性
- **修正1（NoteSpawner）**: 既に`PhaseManager.OnPhaseChanged`を購読中 → フェーズ情報をローカル保持
- **修正2（FreezeManager）**: コード削除のみ、イベント購読変更なし
- **修正3（TimerDisplay）**: `GameManager.OnGameStart`と`OnShowTitle`を購読 → 適切
- **修正4（PhaseDisplay）**: `PhaseManager.OnPhaseChanged`を購読 → 適切
- **修正5（ResultScoreDisplay）**: `GameManager.OnGameOver`を購読 → 適切

#### 2. 責務分離の確認
- **ゲーム終了判定**: PhaseManagerが全フェーズ完了時に`GameManager.EndGame()`を呼ぶ（既存実装）
- **TimerDisplay**: 表示専用に徹し、ゲーム終了判定は行わない（責務分離）
- **NoteSpawner**: フェーズ情報をローカル保持し、PhaseManagerへの依存を最小化（疎結合）

#### 3. メモリリーク防止
- 全UIクラスで`OnDestroy()`にイベント購読解除を実装
- StringBuilderの再利用でGC削減

#### 4. タイトル復帰時のリセット
- **修正3（TimerDisplay）**: `OnShowTitle`を購読してタイマーリセット
- **修正5（ResultScoreDisplay）**: パネル非表示で自動的にリセット（追加処理不要）
- **修正1（NoteSpawner）**: 既に`OnShowTitle`でスポーン停止（フェーズ情報もリセット不要、次回OnPhaseChangedで更新）

#### 5. CodeArchitecture.md準拠
- すべての修正が以下の設計原則に準拠：
  - イベント駆動設計
  - 責務の分離
  - 疎結合
  - パフォーマンス最適化（StringBuilder、ローカルキャッシュ）

### 📝 設計上の重要な決定事項

1. **修正1（休符表示）**: フェーズ情報をローカル保持する方式を採用
   - 理由: 疎結合、パフォーマンス向上、既存イベント購読の活用
   
2. **修正3（タイマー表示）**: ゲーム終了判定を行わない
   - 理由: 責務分離、PhaseManagerが既に終了判定を実装済み
   
3. **全UIクラス**: StringBuilderを使用してGC削減
   - 理由: ScoreDisplayとの一貫性、パフォーマンス最適化

### 🔍 潜在的な注意点

1. **修正1**: `OnPhaseChanged()`内でフェーズ保存は**Coroutine開始前**に実行すること
2. **修正3**: タイマーが0秒になる直前にPhaseManagerがゲームを終了するため、表示上の違和感はない
3. **修正5**: リザルトパネルは`PanelController`が管理するため、ResultScoreDisplayは表示更新のみに集中

---

## 微小修正項目
おそらく小さな変更で反映できる修正項目。後回し。

- スライダは減っていくようにする。フェーズの種類によって色を変える。
- 音符の生成範囲を画面内に。

## 足りない機能・検討項目

余裕が出来たら追加する。

- **エラーハンドリング/ロギングシステム** - ログ出力・デバッグ用ロギングマネージャー（コンソール出力、ファイル保存等）
- **設定管理** - ゲーム難度、ポート番号、キー設定などをJSONまたはScriptableObjectで管理
- **リソース管理・プリロード** - ゲーム開始時にAudioClip、Sprite等を全てメモリ上に確保するPreloaderマネージャー
- **パフォーマンス監視** - フレームレート、メモリ使用量の表示・監視機構（デバッグ用UI）
- **セーブ・ロード機構** - ハイスコア、プレイ履歴等の永続化（PlayerPrefs or ファイルIO）
- **ネットワーク同期（将来対応）** - オンラインランキング、マルチプレイ検討時の基盤設計
- **入力イベント検証** - 受け取ったシェイクデータ（文字列）のバリデーション・パース機能
- **タイミング同期の微調整** - オーディオDSP時刻とゲーム時間のズレ吸収メカニズム
- **ポーズ/ポーズ解除機能** - GameManager側でポーズ状態を持ち、全マネージャーが購読
- **トランジション効果** - フェーズ間・画面間の切り替えアニメーション統一管理（TransitionManager）

- **もっとラグを少なくしたい**
- **音符がはじけるエフェクト**