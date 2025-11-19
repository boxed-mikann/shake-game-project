## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## 要修正項目

### 1. タイトルに戻るretryボタン(GoTitleButton)の実装 【修正計画確定】

#### 【設計サマリー】
- **追加イベント**: `GameManager.OnShowTitle` （1つだけ）
- **追加メソッド**: `GameManager.ShowTitle()` （1つだけ）
- **設計原則**: DRY原則 - 初期表示とタイトル復帰を統一処理
- **修正対象**: `GameManager.cs`, `PanelController.cs`, `PhaseManager.cs`, `ScoreManager.cs` + その他マネージャー
- **ボタン設定**: ResultPanel内の「GoTitleButton」のOnClickに`GameManager.ShowTitle`を登録

#### 【問題の原因】
- 現在、`GameManager`には`StartGame()`しか存在せず、タイトル画面に戻る機能が実装されていない
- `PanelController`は`OnGameStart`と`OnGameOver`のみ購読しており、タイトル画面に戻るイベントが存在しない
- ゲーム状態のリセット機能が不完全：
  - `ScoreManager`はゲーム開始時にスコアリセット実装済み（`OnGameStart`購読）
  - `PhaseManager`はCoroutineをStopしているが、状態変数のリセットが未実装
  - その他のマネージャー（`FreezeManager`等）のリセット処理が未実装

#### 【設計方針】
- **イベント駆動**: `GameManager.OnShowTitle`イベントで全システムに通知
- **DRY原則**: アプリ起動時とタイトル復帰を同一イベントで処理
- **完全リセット**: 全マネージャーが`OnShowTitle`を購読して状態をリセット

#### 【修正内容詳細】

##### ✅ **Step 1: GameManager.cs の拡張**

**追加するイベント・メソッド**：
1. `public static UnityEvent OnShowTitle = new UnityEvent();` - タイトル画面表示（起動時・復帰時共通）
2. `public static void ShowTitle()` - タイトル画面を表示（両ケースで使用）

**実装コード**：
```csharp
// GameManager.cs - イベント宣言部分に追加
public static UnityEvent OnShowTitle = new UnityEvent();

// GameManager.cs - Start()メソッドに追加
void Start() {
    ShowTitle();  // 起動時に自動表示
}

// GameManager.cs - 新規メソッド追加
public static void ShowTitle() {
    if (Instance == null) return;
    
    Instance._isGameRunning = false;
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[GameManager] 📺 Showing title screen");
    
    OnShowTitle.Invoke();
}
```

##### ✅ **Step 2: PanelController.cs の拡張**
**実装コード**：
```csharp
// PanelController.cs - Start()メソッドを修正
void Start() {
    // イベント購読
    GameManager.OnShowTitle.AddListener(OnShowTitle);  // ★追加
    GameManager.OnGameStart.AddListener(OnGameStart);
    GameManager.OnGameOver.AddListener(OnGameOver);
    
    // ★削除：以下の3行を削除
    // ShowPanel(_titlePanel);
    // HidePanel(_playPanel);
    // HidePanel(_resultPanel);
    
    // ★追加：初期状態は全パネル非表示
    HidePanel(_titlePanel);
    HidePanel(_playPanel);
    HidePanel(_resultPanel);
}

// PanelController.cs - 新規ハンドラー追加
private void OnShowTitle() {
    ShowPanel(_titlePanel);
    HidePanel(_playPanel);
    HidePanel(_resultPanel);
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[PanelController] Showing title panel");
}

// PanelController.cs - OnDestroy()メソッドを修正
void OnDestroy() {
    GameManager.OnShowTitle.RemoveListener(OnShowTitle);  // ★追加
    GameManager.OnGameStart.RemoveListener(OnGameStart);
    GameManager.OnGameOver.RemoveListener(OnGameOver);
}
```

##### ✅ **Step 3: PhaseManager.cs の拡張**

**実装コード**：
```csharp
// PhaseManager.cs - OnEnable()メソッドに追加
private void OnEnable() {
    GameManager.OnGameStart.AddListener(OnGameStart);
    GameManager.OnShowTitle.AddListener(ResetPhaseManager);  // ★追加
}

// PhaseManager.cs - OnDisable()メソッドに追加
private void OnDisable() {
    GameManager.OnGameStart.RemoveListener(OnGameStart);
    GameManager.OnShowTitle.RemoveListener(ResetPhaseManager);  // ★追加
}

// PhaseManager.cs - 新規メソッド追加
private void ResetPhaseManager() {
    // Coroutine停止
    if (_phaseSequenceCoroutine != null) {
        StopCoroutine(_phaseSequenceCoroutine);
        _phaseSequenceCoroutine = null;
    }
    
    // 状態変数リセット
    _currentPhaseIndex = -1;
    _currentPhase = Phase.NotePhase;
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[PhaseManager] Reset to initial state");
}
```

##### ✅ **Step 4: ScoreManager.cs の拡張**

**実装コード**：
```csharp
// ScoreManager.cs - OnEnable()メソッドを修正
private void OnEnable() {
    GameManager.OnGameStart.AddListener(Initialize);
    GameManager.OnShowTitle.AddListener(Initialize);  // ★追加
}

// ScoreManager.cs - OnDisable()メソッドを修正
private void OnDisable() {
    GameManager.OnGameStart.RemoveListener(Initialize);
    GameManager.OnShowTitle.RemoveListener(Initialize);  // ★追加
}

// ※ Initialize()メソッドは既存のものをそのまま使用（変更不要）
```

##### ✅ **Step 5: その他マネージャーのリセット対応（必要に応じて）**
- `FreezeManager`: フリーズ状態を解除（`OnShowTitle`購読）
- `NotePool` / `NoteManager`: アクティブなNoteをすべてプールに返却（`OnShowTitle`購読）
- `NoteSpawner`: Coroutineを停止（`OnShowTitle`購読）
- `ShakeResolver`: 入力キューをクリア、ハンドラーをデフォルト状態に戻す（`OnShowTitle`購読）

##### ✅ **Step 6: UnityEditor側の設定**
- ResultPanel内の「GoTitleButton」のOnClickイベントに`GameManager.ShowTitle`を登録
  - 設定方法：`StartGame()`と同様にInspectorで手動アタッチ
  - または、ボタンスクリプトを作成して`GameManager.ShowTitle()`を呼び出す
  - **統一**: Play/Retry両方とも同じメソッド名パターン（`StartGame()` / `ShowTitle()`）

#### 【実装順序】
1. `GameManager.cs`にイベントとメソッドを追加（`OnShowTitle`, `ShowTitle()`のみ）
2. `GameManager.cs`の`Start()`に`ShowTitle()`呼び出しを追加（初期タイトル表示）
3. `PanelController.cs`の`Start()`から直接呼び出しを削除し、`OnShowTitle`ハンドラーに変更
4. 各マネージャー（`PhaseManager`, `ScoreManager`等）に`OnShowTitle`購読とリセット処理追加
5. UnityEditorでボタンのOnClickイベント設定（`GameManager.ShowTitle`）
6. 動作確認：
   - ✅ アプリ起動 → タイトル画面表示（`OnShowTitle`経由）
   - ✅ Play → ゲーム画面表示（`OnGameStart`経由）
   - ✅ GameOver → リザルト画面表示（`OnGameOver`経由）
   - ✅ GoToTitle → タイトル画面表示（`OnShowTitle`経由・リセット実行）
   - ✅ 再度Play → 正常に動作

#### 【実装チェックリスト】

**GameManager.cs**
- [x] `OnShowTitle`イベントを宣言部分に追加
- [x] `ShowTitle()`メソッドを追加
- [x] `Start()`メソッドに`ShowTitle()`呼び出しを追加

**PanelController.cs**
- [x] `Start()`から直接のパネル表示コードを削除（3行削除）
- [x] `Start()`に全パネル非表示処理を追加（3行追加）
- [x] `OnShowTitle`イベントの購読を追加
- [x] `OnShowTitle()`ハンドラーを実装
- [x] `OnDestroy()`に`OnShowTitle`の購読解除を追加

**PhaseManager.cs**
- [x] `OnEnable()`に`OnShowTitle`の購読を追加
- [x] `OnDisable()`に`OnShowTitle`の購読解除を追加
- [x] `ResetPhaseManager()`メソッドを実装

**ScoreManager.cs**
- [x] `OnEnable()`に`OnShowTitle`の購読を追加
- [x] `OnDisable()`に`OnShowTitle`の購読解除を追加

**その他のマネージャー（追加実装）**
- [x] `FreezeManager.cs`: `OnShowTitle`購読とリセット処理を追加
- [x] `NoteManager.cs`: `OnShowTitle`購読でClearAllNotesを呼び出し
- [x] `NoteSpawner.cs`: `OnShowTitle`購読でCoroutineを停止
- [x] `ShakeResolver.cs`: `OnShowTitle`購読で入力キューとハンドラーをリセット

**UnityEditor**
- [ ] ResultPanel内のGoTitleButtonのOnClickに`GameManager.ShowTitle`を設定

**動作確認**
- [ ] アプリ起動時にタイトル画面が表示される
- [ ] Playボタンでゲーム開始
- [ ] ゲーム終了後、リザルト画面が表示される
- [ ] GoTitleボタンでタイトル画面に復帰
- [ ] 再度Playボタンを押すと、リセットされた状態でゲームが開始される

#### 【GameManagerイベント一覧（参考）】

| イベント名 | 引数 | 発行タイミング | 主な購読者 | 用途 |
|-----------|------|--------------|-----------|------|
| `OnShowTitle` | なし | アプリ起動時、ゲーム終了後のタイトル復帰時 | `PanelController`, `PhaseManager`, `ScoreManager`, `NoteManager`, `FreezeManager`, `ShakeResolver` | タイトル画面表示 + 全システムリセット |
| `OnGameStart` | なし | ゲーム開始ボタン押下時 | `PanelController`, `PhaseManager`, `ScoreManager`, `ShakeResolver` | ゲーム開始 + フェーズシーケンス開始 |
| `OnGameOver` | なし | 全フェーズ終了時 | `PanelController` | リザルト画面表示 |

---

2. 音符の画像のバリエーションを増やす。
  - プリロード？共通スプライト？ってなに？

3. タイマー表示(TMP)
4. フェーズ表示(TMP)

5. 休符モードの時に生成された音符が休符になっていない。

6. 最終スコア表示の実装　(←プレイ中スコア表示の実装と重なる部分は大きいか？)

## 微小修正項目
おそらく小さな変更で反映できる修正項目。後回し。

- スライダは減っていくようにする。フェーズの種類によって色を変える。
- 音符の生成範囲を画面内に自動でできるようにしたい。

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