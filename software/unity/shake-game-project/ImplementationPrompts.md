# 実装用プロンプトテンプレート

このドキュメントは CodeArchitecture.md と組み合わせて使用します。
Copilot に各段階の実装を依頼する際、以下のプロンプトを参照してください。

---

## 🔧 UnityMCP 使用ガイド

**重要：ファイル操作は以下のルールで実施**

| 操作 | 方法 | 理由 |
|------|------|------|
| **ファイル作成（.cs）** | ❌ Copilot に `mkdir` 指示 ❌ | Copilot コマンド実行が不安定 |
| **ファイル作成（.cs）** | ✅ **GitHub Copilot - UnityMCP `create_script`** | 安定した Unity 統合 |
| **ファイル編集（内容追加）** | ✅ **通常の `replace_string_in_file`** | IDE ツール安定性 |
| **ファイル削除** | ✅ **GitHub Copilot - UnityMCP `delete_script`** | 安定した Unity 統合 |

### Copilot に指示するときのテンプレート

```
## ファイル作成は UnityMCP で実施してください

以下のファイルを作成する際は、PowerShell コマンド（mkdir, New-Item）を使わず、
GitHub Copilot - UnityMCP の create_script 機能を使ってください。

指示例：
「Assets/Scripts/Data/PhaseChangeData.cs を、以下の内容で作成してください。」

その後、ファイルパスは以下の形式で Copilot に提供：
- Assets/Scripts/Data/PhaseChangeData.cs （ローカルパス）
- または unity://path/Assets/Scripts/Data/PhaseChangeData.cs （UnityMCP URI）
```

### UnityMCP コマンド リファレンス

```csharp
// ファイル作成（Copilot から指示）
mcp_unitymcp_create_script(
    path: "Assets/Scripts/Data/PhaseChangeData.cs",
    contents: "using UnityEngine; [System.Serializable] ...",
    namespace: null  // namespace は使わない
)

// ファイル削除（必要な場合）
mcp_unitymcp_delete_script(
    uri: "Assets/Scripts/Handlers/Phase1ShakeHandler.cs"
)

// ファイル編集（通常の replace_string_in_file を使用）
// IDE 側で実施
```

---

## フォルダ構成戦略

**旧実装を参照可能にするため、新旧コードを明確に分離します：**

```
Assets/Scripts/
├── FormerCodes/           ← 旧実装（参照用に保持）
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── GameConstants.cs
│   │   └── ...
│   ├── Game/
│   │   ├── PhaseController.cs
│   │   ├── NotePrefab.cs
│   │   └── ...
│   ├── Input/
│   ├── UI/
│   └── README.md           ← 旧実装の説明（参照方法記載）
│
├── Managers/              ← 新実装
│   ├── GameManager.cs
│   ├── PhaseManager.cs
│   ├── FreezeManager.cs
│   ├── ScoreManager.cs
│   └── AudioManager.cs
│
├── Gameplay/
│   ├── Note.cs
│   ├── NotePool.cs
│   ├── NoteManager.cs
│   └── NoteSpawner.cs
│
├── Handlers/
│   ├── Phase1ShakeHandler.cs
│   ├── Phase2ShakeHandler.cs
│   └── ...
│
├── Input/
│   ├── SerialPortManager.cs
│   ├── SerialInputReader.cs
│   ├── KeyboardInputReader.cs
│   └── ShakeResolver.cs
│
├── Audio/
│   └── AudioManager.cs
│
├── UI/
│   ├── PanelController.cs
│   ├── ScoreDisplay.cs
│   ├── PhaseProgressBar.cs
│   └── FreezeEffectUI.cs
│
└── Data/
    ├── PhaseChangeData.cs
    ├── IInputSource.cs
    ├── IShakeHandler.cs
    └── GameConstants.cs
```

**メリット：**
- ✅ 旧コードを参照できる（実装時に仕様確認）
- ✅ 新旧が明確に分離（混在の危険なし）
- ✅ デバッグ時に旧実装と比較可能
- ✅ Git で `FormerCodes/` フォルダごと管理できる
- ✅ 将来的に削除も容易

---

## 【全段階共通】背景・参照設定

```
## 背景
我々は Unity シェイクゲームの既存コードベース（Assets/Scripts/FormerCodes/）を、
新しいイベント駆動アーキテクチャに再構築しています。

機能は 100% 保持しながら、以下を実現します：
- イベント駆動設計（UnityEvent<T>）
- 責務の明確な分離
- Object Pool による最適化
- 将来の拡張性向上

## 参照資料
1. **CodeArchitecture.md**（セクション 3 で各クラスの仕様定義）
2. **旧コード**（以下のファイルから学ぶべき仕様）
   - Assets/Scripts/FormerCodes/Core/GameConstants.cs → 定数、フェーズシーケンス定義
   - Assets/Scripts/FormerCodes/Core/GameManager.cs → ゲームライフサイクル、フェーズ管理
   - Assets/Scripts/FormerCodes/Game/PhaseController.cs → フェーズ自動切り替え
   - Assets/Scripts/FormerCodes/Game/NotePrefab.cs → イベント購読、見た目更新

## 実装フロー
- セクション 3.x に記載の仕様に従う
- 旧実装から機能・ロジック（duration, spawnFrequency, フェーズ種別等）を参考
- 新アーキテクチャのイベント構造を採用

## ⚠️ 重要：ファイル作成について
**以下のファイル作成は UnityMCP を使用してください（Copilot の mkdir/New-Item は使わない）**

各ステップで「Assets/Scripts/○○/**.cs」を作成する際：
1. 作成するファイルのパス と コード内容を準備
2. GitHub Copilot - UnityMCP の **create_script** 機能で作成
3. 失敗時は IDE で手動作成し、その後編集コマンドで追加

```

---

## 【ステップ 1】基盤準備（Data/ フォルダ）

### プロンプト
```
## ステップ 1: Data/ フォルダ基盤実装

CodeArchitecture.md セクション 3.0.1～3.0.3 を参照してください。

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**
Copilot の mkdir/New-Item コマンドは使わないでください。

### 作成するファイル（UnityMCP create_script で作成）

#### 1. Assets/Scripts/Data/PhaseChangeData.cs
- 構造体名：PhaseChangeData
- [System.Serializable] 属性付与
- フィールド：
  - phaseType: Phase（enum）
  - duration: float（このフェーズの継続時間）
  - spawnFrequency: float（音符湧き出し頻度・秒間隔）
  - phaseIndex: int（フェーズ番号 0, 1, 2...）
- ToString() メソッド実装推奨（デバッグ用）

#### 2. Assets/Scripts/Data/IInputSource.cs
- インターフェース名：IInputSource
- メンバー：
  - event UnityEvent OnShakeDetected - シェイク検出イベント
  - bool IsConnected { get; } - 接続状態プロパティ
  - void Connect() - 接続メソッド
  - void Disconnect() - 切断メソッド
  - void Update() - 入力ポーリングメソッド

#### 3. Assets/Scripts/Data/IShakeHandler.cs
- インターフェース名：IShakeHandler
- メンバー：
  - void HandleShake() - シェイク処理メソッド（引数なし）

#### 4. Assets/Scripts/Data/GameConstants.cs（改良版）
- 既存の GameConstants を参考に、以下を確認：
  - 参照元：Assets/Scripts/FormerCodes/Core/GameConstants.cs
  - PhaseConfig 構造体（phase, duration）
  - PHASE_SEQUENCE 配列（旧コード GameConstants.cs に従う）
  - SPAWN_RATE_BASE, LAST_SPRINT_MULTIPLIER 定数
  - NOTE_SCORE 定数
  - Serial 通信定数（SERIAL_PORT_NAME, SERIAL_BAUD_RATE）
- 新たに追加：
  - spawnFrequency 計算（1 / SPAWN_RATE_BASE）をプロパティで提供可能

### CodeArchitecture.md 参照箇所
- セクション 3.0.1: PhaseChangeData 構造体の詳細
- セクション 3.0.2: IInputSource インターフェース
- セクション 3.0.3: IShakeHandler インターフェース
- セクション 4: フォルダ構成確認

### 実装時の注意
- namespaces は使わない（既存コードに合わせる）
- [System.Serializable] 属性で Inspector 編集可能に
- PhaseChangeData は struct（値型）で実装
- 旧 GameConstants.cs との整合性確認（定数値の引き継ぎ）
```

---

## 【ステップ 2】マネージャー群（Managers/ フォルダ）

### プロンプト
```
## ステップ 2: Managers/ フォルダ実装

CodeArchitecture.md セクション 3.1 を参照してください。

### 前提条件
- ステップ 1（Data/ フォルダ）が完了していること
- PhaseChangeData, IInputSource, IShakeHandler が定義済み

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**

### 作成するファイル（UnityMCP create_script で作成）

#### 1. Assets/Scripts/Managers/GameManager.cs
参照：CodeArchitecture.md セクション 3.1.1

- 責務：ゲーム全体のライフサイクル管理（開始・終了）
- 主イベント：
  - public UnityEvent OnGameStart - ゲーム開始（PhaseManager開始トリガー）
  - public UnityEvent OnGameOver - ゲーム終了
- 主メソッド：
  - public static void StartGame() - ゲーム開始
  - public static void EndGame() - ゲーム終了
- 機能：
  - Singleton パターン（既存 GameManager と同様）
  - 静的イベント（全システムがアクセス可能）
- 実装時注意：
  - 旧 GameManager.cs を参考に既存ロジック理解（フェーズシーケンス等）
  - 新実装では OnGameStart 発火後、PhaseManager が引き継ぎ
  - フェーズ管理は PhaseManager に移譲

#### 2. Assets/Scripts/Managers/PhaseManager.cs
参照：CodeArchitecture.md セクション 3.1.2

- 責補：ゲームフェーズの時系列管理と切り替え
- 主イベント：
  - public UnityEvent<PhaseChangeData> OnPhaseChanged - フェーズ変更時発行
- 主メソッド：
  - IEnumerator ExecutePhase(PhaseConfig config) - Coroutine で時間管理
  - Phase GetCurrentPhase() { get; } - 現在フェーズを返すプロパティ
- 機能：
  - GameManager.OnGameStart を購読
  - PHASE_SEQUENCE を順に実行
  - 各フェーズを yield return new WaitForSeconds(duration) で待機
  - 切り替え時に PhaseChangeData を構築して OnPhaseChanged.Invoke()
- 実装時注意：
  - 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の InitializePhaseSequence() ロジック
  - PHASE_SEQUENCE の各要素を PhaseConfig として処理
  - LastSprintPhase は PHASE_SEQUENCE に明示的に含まれる
  - spawnFrequency = 1 / SPAWN_RATE_BASE（フェーズに応じて倍率適用）

#### 3. Assets/Scripts/Managers/FreezeManager.cs
参照：CodeArchitecture.md セクション 3.1.3

- 責補：フリーズエフェクト（画面フラッシュ、入力禁止）の管理
- 主イベント：
  - public UnityEvent<bool> OnFreezeChanged - 凍結開始(true)/解除(false)
- 主メソッド：
  - public void StartFreeze(float duration) - フリーズ開始
  - public bool IsFrozen { get; } - 凍結状態判定
- 機能：
  - Phase*ShakeHandler から StartFreeze(duration) で呼ばれる
  - Coroutine で duration 待機後に解除
  - LastSprintPhase 中は StartFreeze() は何もしない（無効）
- 実装時注意：
  - 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の TriggerFreeze() ロジック
  - _isFrozen フラグで状態管理

#### 4. Assets/Scripts/Managers/ScoreManager.cs
参照：CodeArchitecture.md セクション 3.1.4

- 責補：スコア加減
- 主イベント：
  - public UnityEvent<int> OnScoreChanged - スコア更新時発行（現在値）
- 主メソッド：
  - public void AddScore(int points) - スコア加算（負数で減点可）
  - public int GetScore() { get; } - 現在スコア取得
- 機能：
  - Phase*ShakeHandler から AddScore(points) で呼ばれる
  - スコア変更時に常に OnScoreChanged.Invoke(currentScore) を発火
- 実装時注意：
  - 参照元：Assets/Scripts/FormerCodes/Game/ の Note 処理時の加減点ロジック
  - NotePhase: +1, RestPhase: -1（CodeArchitecture.md セクション 3.5 参照）

#### 5. Assets/Scripts/Managers/AudioManager.cs
参照：CodeArchitecture.md セクション 3.3

- 責補：効果音の再生・キャッシング
- 主メソッド：
  - public void PlaySFX(string clipName) - 効果音再生
  - public AudioClip GetClip(string name) - AudioClip キャッシュ取得
- 機能：
  - Dictionary<string, AudioClip> でプリロード済みクリップ管理
  - Resources フォルダから AudioClip をロード・キャッシング
  - Phase*ShakeHandler から PlaySFX("hit") で呼ばれる
- 実装時注意：
  - Resources/Audio/ に "hit.wav" 等の SFX を配置
  - 初回ロード時にキャッシング（GC 削減）
  - 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の PlayBurstSound() ロジック

### CodeArchitecture.md 参照箇所
- セクション 3.1: 全マネージャー仕様
- セクション 7: イベント一覧（イベント型・引数確認）
- セクション 8.6: 責務分離の例（GameManager vs PhaseManager）

### 実装時の全体注意
- static UnityEvent で全クラスがアクセス可能に
- [SerializeField] で Inspector 確認可能に
- Debug.Log() でフローを可視化（GameConstants.DEBUG_MODE チェック）
- 旧実装の定数値・計算ロジックを引き継ぐ
```

---

## 【ステップ 3】ゲームプレイ（Gameplay/ フォルダ）

### プロンプト
```
## ステップ 3: Gameplay/ フォルダ実装

CodeArchitecture.md セクション 3.2 を参照してください。

### 前提条件
- ステップ 1, 2 が完了していること
- Managers/ の全クラスが実装済み

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**

### 作成するファイル（UnityMCP create_script で作成）

#### 1. Assets/Scripts/Gameplay/Note.cs
参照：CodeArchitecture.md セクション 3.2.1

- 責補：音符 GameObject の表現（見た目・状態のみ）
- 主機能：
  - SpriteRenderer で見た目表示
  - 現在フェーズに応じた Sprite 表示（音符 or 休符）
  - PhaseManager.OnPhaseChanged を購読
- 主メソッド：
  - public void SetPhase(Phase phase) - フェーズに応じて Sprite 更新
  - public Phase GetCurrentPhase() - 現在フェーズ取得
- 実装時注意：
  - 参照元：Assets/Scripts/FormerCodes/Game/NotePrefab.cs（OnPhaseChanged ハンドラ）
  - 処理ロジックは含めない（Handlers が担当）
  - [SerializeField] で noteSprite, restSprite を設定可能に

#### 2. Assets/Scripts/Gameplay/NotePool.cs
参照：CodeArchitecture.md セクション 3.2.2

- 責補：音符オブジェクトのプール管理（生成・再利用）
- 主メソッド：
  - public Note GetNote() - プールから Note 取得（ない場合は Instantiate）
  - public void ReturnNote(Note note) - プールに Note 返却（SetActive(false)）
  - private void ExpandPool(int count) - プール拡張
- 機能：
  - Note.prefab から事前に複数生成
  - SetActive() で有効化・無効化管理
  - Queue<Note> でキューイング（返却順）
- 実装時注意：
  - 初期プールサイズを定数で定義（例：100）
  - 不足時は自動拡張
  - prefabPath = "Prefabs/Note" 等で Resources から読み込み

#### 3. Assets/Scripts/Gameplay/NoteManager.cs
参照：CodeArchitecture.md セクション 3.2.3

- 責補：アクティブ音符の時系列管理・最古削除
- 主メソッド：
  - public void AddNote(Note note) - アクティブリストに追加
  - public Note GetOldestNote() - 最古 Note 取得（FIFO）
  - public void DestroyOldestNote() - 最古 Note をプール返却・リストから削除
  - public int GetActiveNoteCount() - アクティブ音符数取得
- 機能：
  - Queue<Note> で FIFO 順序保持
  - Phase*ShakeHandler が DestroyOldestNote() を呼び出す
  - 生成：NoteSpawner から AddNote() で呼び出し
  - 削除：Handler から、または自動削除時に
- 実装時注意：
  - Queue<T> で順序保証
  - Null チェック（GetOldestNote() は Null 可能性あり）

#### 4. Assets/Scripts/Gameplay/NoteSpawner.cs
参照：CodeArchitecture.md セクション 3.2.4

- 責補：フェーズに応じた時間ベース音符生成
- 主機能：
  - PhaseManager.OnPhaseChanged を購読
  - 各フェーズの spawnFrequency に基づいて定期生成
  - LastSprintPhase では spawnFrequency × LAST_SPRINT_MULTIPLIER で生成加速
- 主メソッド：
  - private IEnumerator SpawnLoop(float frequency, float duration) - Coroutine で生成制御
  - private void SpawnOneNote() - 音符を 1 個生成
- 機能：
  - PhaseChangeData から frequency, duration, isLastSprint を取得
  - yield return new WaitForSeconds(frequency) で定期生成
  - NotePool.GetNote(), NoteManager.AddNote() でスポーン登録
- 実装時注意：
  - 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の UpdateNoteSpawning() + SpawnNote() ロジック
  - LastSprintPhase 検出：phaseData.phaseType == Phase.LastSprintPhase
  - 前のフェーズの Coroutine は StopCoroutine() で停止

### CodeArchitecture.md 参照箇所
- セクション 3.2: Gameplay/ クラス詳細
- セクション 3.0.1: PhaseChangeData フィールド（spawnFrequency, phaseType）
- セクション 7: OnPhaseChanged イベント定義

### 実装時の全体注意
- NotePool と NoteManager は依存（NotePool → NoteManager で使用）
- NoteSpawner は PhaseManager.OnPhaseChanged 購読
- 旧実装の生成レート・LastSprint 倍率を引き継ぐ
```

---

## 【ステップ 4】ハンドラー（Handlers/ フォルダ）

### プロンプト
```
## ステップ 4: Handlers/ フォルダ実装

CodeArchitecture.md セクション 3.5 を参照してください。

### 前提条件
- ステップ 1～3 が完了していること
- NoteManager, AudioManager, FreezeManager, ScoreManager が実装済み

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**

### 作成するファイル（UnityMCP create_script で作成）

#### IShakeHandler の実装パターン

各フェーズごとに以下を実装：
- Phase1ShakeHandler.cs, Phase2ShakeHandler.cs, ...

クラス仕様：

```csharp
public class Phase<X>ShakeHandler : IShakeHandler
{
    [SerializeField] private int _scoreValue = 1;        // フェーズ特有のスコア
    [SerializeField] private float _freezeDuration = 0.3f; // フリーズ時間
    
    public void HandleShake()
    {
        // 1. 最古 Note 取得
        Note oldest = NoteManager.GetOldestNote();
        if (oldest == null) return; // Null チェック
        
        // 2. 最古 Note を破棄
        NoteManager.DestroyOldestNote();
        
        // 3. 効果音再生
        AudioManager.PlaySFX("hit");
        
        // 4. スコア加算（フェーズに応じた値）
        ScoreManager.AddScore(_scoreValue);
        
        // 5. 必要に応じてフリーズ開始（NotePhase では不要、RestPhase では必須等）
        if (_freezeDuration > 0)
        {
            FreezeManager.StartFreeze(_freezeDuration);
        }
    }
}
```

#### フェーズごとの実装差分

参照元：Assets/Scripts/FormerCodes/ の旧実装より、フェーズ別の処理を確認
- NotePhase: _scoreValue = +1, _freezeDuration = 0（フリーズなし）
- RestPhase: _scoreValue = -1, _freezeDuration = 長め（フリーズあり）
- LastSprintPhase: _scoreValue = +2（ボーナス），_freezeDuration = 0

#### 作成ファイル一覧

PHASE_SEQUENCE の各要素に対応させる（例：6フェーズなら 6個）

```
Phase1ShakeHandler.cs (NotePhase)
Phase2ShakeHandler.cs (RestPhase)
Phase3ShakeHandler.cs (NotePhase)
Phase4ShakeHandler.cs (RestPhase)
Phase5ShakeHandler.cs (NotePhase)
Phase6ShakeHandler.cs (RestPhase)
Phase7ShakeHandler.cs (LastSprintPhase)
```

### CodeArchitecture.md 参照箇所
- セクション 3.5: Phase*ShakeHandler の処理フロー
- セクション 3.2.3: NoteManager メソッド
- セクション 3.1.3, 3.1.4: FreezeManager, ScoreManager インターフェース

### 実装時の注意
- Null チェック必須（GetOldestNote() の戻り値）
- フェーズ別 _scoreValue, _freezeDuration を Inspector で設定
- ShakeResolver が現在フェーズに応じて実装を切り替える（ステップ 5）
```

---

## 【ステップ 5】入力システム（Input/ フォルダ）

### プロンプト
```
## ステップ 5: Input/ フォルダ実装

CodeArchitecture.md セクション 3.4 を参照してください。

### 前提条件
- ステップ 1～4 が完了していること
- IInputSource, IShakeHandler が定義済み
- Phase*ShakeHandler がすべて実装済み

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**

### 作成するファイル（UnityMCP create_script で作成）

#### 1. Assets/Scripts/Input/SerialPortManager.cs
参照：CodeArchitecture.md セクション 3.4.1

- 責補：シリアルポート接続・管理
- 主機能：
  - SerialPort 接続・切断
  - 再接続ロジック（SERIAL_RECONNECT_INTERVAL で定期試行）
  - 接続状態の監視
- 主メソッド：
  - public bool IsConnected { get; } - 接続状態
  - public void Connect() - 接続試行
  - public void Disconnect() - 切断
  - public string ReadLine() - ポートからデータ読み込み
- 実装時注意：
  - GameConstants.SERIAL_PORT_NAME, SERIAL_BAUD_RATE を使用
  - SerialPort は using System.IO.Ports
  - 例外処理：ポート存在確認等
  - 参照元：Assets/Scripts/FormerCodes/ のシリアル通信実装

#### 2. Assets/Scripts/Input/SerialInputReader.cs
参照：CodeArchitecture.md セクション 3.4.2

- 責補：シリアルポートから入力読み込み（スレッド化）
- 実装：IInputSource インターフェース
- 主機能：
  - バックグラウンドスレッドでシリアル読み込み
  - ConcurrentQueue<string> で入力データをキューイング
  - メインスレッドでの安全な読み取り
- 主メソッド：
  - void Connect() - スレッド開始
  - void Disconnect() - スレッド停止
  - void Update() - メインスレッドで入力処理
  - event UnityEvent OnShakeDetected - シェイク検出イベント
- 実装時注意：
  - Thread を BackgroundWorker で実装
  - ConcurrentQueue<string> で Thread-safe キューイング
  - OnShakeDetected 発火でハンドラ呼び出し

#### 3. Assets/Scripts/Input/KeyboardInputReader.cs
参照：CodeArchitecture.md セクション 3.4.3

- 責補：キーボード入力（デバッグ用）
- 実装：IInputSource インターフェース
- 主機能：
  - Input.GetKeyDown(KeyCode.Space) 等でシェイク検出
  - OnShakeDetected イベント発火
- 実装時注意：
  - SerialInputReader と互換インターフェース
  - デバッグ時に簡単に切り替え可能

#### 4. Assets/Scripts/Input/ShakeResolver.cs
参照：CodeArchitecture.md セクション 3.4.4

- 責補：入力ルーティング（Strategy パターン）
- 主機能：
  - 現在フェーズに応じて IShakeHandler 実装を切り替え
  - IInputSource からの入力を現在の Handler に振り分け
- 主メソッド：
  - public void Initialize(IInputSource inputSource) - 入力ソース設定
  - private void OnInputDetected() - 入力検出時コールバック
  - private void OnPhaseChanged(PhaseChangeData data) - フェーズ変更時に Handler 切り替え
- 機能：
  - PhaseManager.OnPhaseChanged を購読
  - data.phaseIndex 等に基づいて Phase*ShakeHandler を取得・セット
  - IInputSource.OnShakeDetected を購読し、_currentHandler.HandleShake() を呼び出し
- 実装時注意：
  - switch 式で phaseIndex → Handler マッピング
  ```csharp
  _currentHandler = data.phaseIndex switch
  {
      0 => _phase1Handler,
      1 => _phase2Handler,
      // ...
      _ => throw new System.InvalidOperationException()
  };
  ```
  - Handler は [SerializeField] で Inspector にドラッグ&ドロップ設定

### CodeArchitecture.md 参照箇所
- セクション 3.4: 入力システム仕様
- セクション 3.0.2: IInputSource インターフェース
- セクション 3.0.3: IShakeHandler インターフェース
- セクション 8.2: Strategy パターン説明

### 実装時の全体注意
- SerialInputReader は ConcurrentQueue で Thread-safe に
- KeyboardInputReader で簡単なテスト可能
- ShakeResolver が Handler 切り替えを一元管理
```

---

## 【ステップ 6】UI（UI/ フォルダ）

### プロンプト
```
## ステップ 6: UI/ フォルダ実装

CodeArchitecture.md セクション 3.6 を参照してください。

### 前提条件
- ステップ 1～5 が完了していること
- Managers/ の全クラスが実装済み

### ⚠️ ファイル作成方法
**以下のすべてのファイルは GitHub Copilot - UnityMCP の create_script で作成してください。**

### 作成するファイル（UnityMCP create_script で作成）

#### 1. Assets/Scripts/UI/PanelController.cs
参照：CodeArchitecture.md セクション 3.6.1

- 責補：画面パネル表示・非表示（Title/Play/Result）
- 主機能：
  - GameManager.OnGameStart を購読 → Play パネルアクティベート
  - GameManager.OnGameOver を購読 → Result パネルアクティベート
  - CanvasGroup で見た目制御
- 主メソッド：
  - private void OnGameStart() - ゲーム開始ハンドラ
  - private void OnGameOver() - ゲーム終了ハンドラ
- 実装時注意：
  - [SerializeField] で titlePanel, playPanel, resultPanel を設定
  - CanvasGroup.alpha, enabled で制御

#### 2. Assets/Scripts/UI/ScoreDisplay.cs
参照：CodeArchitecture.md セクション 3.6.2

- 責補：スコア数値表示
- 主機能：
  - ScoreManager.OnScoreChanged を購読
  - TextMeshPro で数値表示
  - StringBuilder で GC 削減
- 実装時注意：
  - [SerializeField] で TextMeshProUGUI scoreText を設定
  - OnScoreChanged(int score) で scoreText.text = score.ToString()
  - 参照元：Assets/Scripts/FormerCodes/UI/ のスコア表示実装

#### 3. Assets/Scripts/UI/PhaseProgressBar.cs
参照：CodeArchitecture.md セクション 3.6.3

- 責補：フェーズ進行度バー表示
- 主機能：
  - PhaseManager.OnPhaseChanged を購読
  - フェーズの duration を取得してローカルタイマー開始
  - Slider で進行度を毎フレーム更新
- 主メソッド：
  - private void OnPhaseChanged(PhaseChangeData data) - duration 保存
  - private void Update() - 進行度計算・表示
- 計算式：
  - progress = (totalDuration - remainingTime) / totalDuration
  - remainingTime = Mathf.Max(0, remainingTime - Time.deltaTime)
- 実装時注意：
  - [SerializeField] で Slider progressSlider を設定
  - コンポーネント自身でタイマー管理

#### 4. Assets/Scripts/UI/FreezeEffectUI.cs
参照：CodeArchitecture.md セクション 3.6.4

- 責補：フリーズ凍結ビジュアル表示
- 主機能：
  - FreezeManager.OnFreezeChanged を購読
  - true: 半透明フラッシュ表示
  - false: 非表示
- 主メソッド：
  - private void OnFreezeChanged(bool isFrozen) - フリーズ状態変更ハンドラ
- 実装時注意：
  - CanvasGroup freezeOverlay で透明度制御
  - isFrozen ? freezeOverlay.alpha = 0.5f : 0f

### CodeArchitecture.md 参照箇所
- セクション 3.6: UI/ クラス詳細
- セクション 7: イベント一覧（OnGameStart, OnScoreChanged 等）

### 実装時の全体注意
- UI 系はイベント購読で自動更新
- TextMeshPro の Import 確認（既存プロジェクトに含まれる）
- Inspector で各パネル・スライダーを設定
```

---

## 【最終段階】統合・テスト

### プロンプト
```
## 最終段階：統合・動作確認

### チェックリスト
- [ ] ステップ 1～6 すべてのファイルが Assets/Scripts/ に配置済み
- [ ] フォルダ構成が CodeArchitecture.md セクション 4 に一致
- [ ] Managers/, Data/ の static イベントが正しく定義されている
- [ ] Phase*ShakeHandler が PHASE_SEQUENCE 要素数と一致（7個）
- [ ] ShakeResolver の Handler 配列が Phase*ShakeHandler 個数と一致
- [ ] Note.prefab が NotePool で参照可能（Resources/Prefabs/Note）
- [ ] AudioClip が Resources/Audio/ に配置済み（hit.wav 等）
- [ ] Inspector で全イベント登録が完了している

### シーン・GameManager 設定
- Main Scene に GameManager, PhaseManager, FreezeManager, ScoreManager を Singleton で配置
- Canvas に PanelController, ScoreDisplay, PhaseProgressBar, FreezeEffectUI を配置
- ShakeResolver に SerialInputReader（または KeyboardInputReader）を設定
- ShakeResolver に Phase1～Phase7ShakeHandler を [SerializeField] で割り当て

### 動作確認
1. **ゲーム開始**：Play Button → GameManager.OnGameStart 発火
2. **フェーズ切り替え**：PhaseManager.OnPhaseChanged 発火 → ShakeResolver Handler 切り替え
3. **シェイク入力**：ShakeResolver → 現在 Handler.HandleShake() 呼び出し
4. **スコア表示**：ScoreManager.OnScoreChanged → ScoreDisplay 更新
5. **フリーズエフェクト**：RestPhase シェイク → FreezeManager.StartFreeze() → FreezeEffectUI 表示
6. **音符生成**：NoteSpawner が PhaseChangeData.spawnFrequency でスポーン
7. **音符削除**：Handler が NoteManager.DestroyOldestNote() で削除

### デバッグ
- GameConstants.DEBUG_MODE = true で各マネージャーの Debug.Log() を確認
- Console でイベント発火フロー を可視化
```

---

## 使用方法

### 開始方法

Copilot への指示：

```
# ゲームコード再構築：実装開始

以下のドキュメントを参照し、CodeArchitecture.md に基づいて実装を進めます。

**参照資料：**
1. CodeArchitecture.md（仕様定義）
2. ImplementationPrompts.md（このファイル）

**【ステップ X】** セクションのプロンプトをそのまま使用してください。

例：
「【ステップ 1】基盤準備（Data/ フォルダ）」のプロンプトをコピーして、
Copilot に提供→ Step 1 の全ファイル実装完了

その後、
「【ステップ 2】マネージャー群」のプロンプトをコピーして、
Copilot に提供→ Step 2 の全ファイル実装完了

以降同様に Step 3～6 を順番に実装。

---

## 注意事項

- **順序厳守**：前ステップが完了していないと、後ステップは動作しません
- **参照資料の確認**：各プロンプントには CodeArchitecture.md のセクション番号が記載されています。必ず確認してください
- **旧コードの活用**：プロンプント内に「旧コード参照」と記載されている場合、該当ファイルを確認して仕様を理解してください
- **Inspector 設定**：実装完了後、Unity Editor で Inspector から各種設定を行う必要があります

