# 実装指示書 - 入力システム改善（3項目）

## 📋 実装概要

以下の3つの改善を実装してください。すべて `CodeArchitecture.md` の設計思想に基づいています。

---

## 🎯 修正1: 入力ソースの統一キュー化

### 目的
- SerialとKeyboardの両方から同時に入力を受け取れるようにする
- DEBUG_MODEによる切り替えを廃止し、コードを簡潔化

### 実装内容

#### ShakeResolver.cs（修正1と修正2を統合）
```csharp
public class ShakeResolver : MonoBehaviour {
    // ★ 統一された入力キュー（static）
    private static ConcurrentQueue<(string data, double timestamp)> _sharedInputQueue 
        = new ConcurrentQueue<(string data, double timestamp)>();
    
    // ★ 外部から入力を追加するメソッド
    public static void EnqueueInput(string data, double timestamp) {
        _sharedInputQueue.Enqueue((data, timestamp));
    }
    
    // ★ 削除：[Header("Input Sources")] セクション
    // ★ 削除：_serialInput, _keyboardInput フィールド
    // ★ 削除：_activeInputSource フィールド
    
    [Header("Freeze & Phase Handlers")]
    [SerializeField] private NullShakeHandler _nullHandler;
    [SerializeField] private NoteShakeHandler _noteHandler;
    [SerializeField] private RestShakeHandler _restHandler;
    
    private IShakeHandler _currentHandler;   // Update()で呼ばれる最終ハンドラー
    private IShakeHandler _activeHandler;    // 通常時のハンドラー（変数のみ）
    
    void Start() {
        // ★ 削除：DEBUG_MODE による入力ソース選択
        // ★ 削除：_activeInputSource?.Connect();
        
        // 初期状態：nullに設定（OnPhaseChangedで設定される）
        _currentHandler = null;
        
        // イベント購読
        FreezeManager.OnFreezeChanged.AddListener(OnFreezeChanged);
        PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        GameManager.OnShowTitle.AddListener(ResetResolver);
    }
    
    void Update() {
        // ★ 統一キューから取り出して処理
        while (_sharedInputQueue.TryDequeue(out var input)) {
            _currentHandler?.HandleShake(input.data, input.timestamp);
        }
    }
    
    void OnFreezeChanged(bool isFrozen) {
        // フリーズ層の切り替え
        _currentHandler = isFrozen ? _nullHandler : _activeHandler;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Freeze: {isFrozen}, Handler: {_currentHandler?.GetType().Name}");
    }
    
    void OnPhaseChanged(PhaseChangeData data) {
        // フェーズ層の切り替え（_activeHandlerを変更）
        switch (data.phaseType) {
            case Phase.NotePhase:
                _activeHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
                break;
            case Phase.LastSprintPhase:
                _activeHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
                break;
            case Phase.RestPhase:
                _activeHandler = _restHandler;
                break;
        }
        
        // ★ 重要：フリーズ中でない場合のみ_currentHandlerを更新
        // （フリーズ中は_nullHandlerのまま、解除時にOnFreezeChangedで更新される）
        if (FreezeManager.Instance != null && !FreezeManager.Instance.IsFrozen) {
            _currentHandler = _activeHandler;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Phase changed, active: {_activeHandler?.GetType().Name}");
    }
    
    private void ResetResolver() {
        // ★ 統一キューをクリア
        while (_sharedInputQueue.TryDequeue(out _)) { }
        // ハンドラーはOnPhaseChangedで再設定される
    }
    
    void OnDestroy() {
        // ★ 削除：_activeInputSource?.Disconnect();
        
        FreezeManager.OnFreezeChanged.RemoveListener(OnFreezeChanged);
        PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        GameManager.OnShowTitle.RemoveListener(ResetResolver);
    }
}
```

#### SerialInputReader.cs
```csharp
// ★ 削除：`IInputSource`インターフェースの実装
// ★ 削除：`_inputQueue`フィールド
// ★ 削除：`TryDequeue()`メソッド
// ★ 削除：`Connect()`メソッド（GameManagerイベント購読も削除）
// ★ 削除：`Disconnect()`メソッド（GameManagerイベント購読解除も削除）

// ★ Start()メソッドを簡素化
void Start() {
    // ゲーム開始時にスレッド開始
    StartReadThread();
}

// ★ 新規メソッド：スレッド開始
private void StartReadThread() {
    if (_isRunning) return;
    
    _isRunning = true;
    _readThread = new Thread(ReadThreadLoop);
    _readThread.IsBackground = true;
    _readThread.Start();
}

// ★ 新規メソッド：スレッド停止
private void StopReadThread() {
    _isRunning = false;
    
    // ReadLine()のブロックを解除するためポート切断
    if (SerialPortManager.Instance != null) {
        SerialPortManager.Instance.Disconnect();
    }
    
    if (_readThread != null && _readThread.IsAlive) {
        _readThread.Join(2000);  // 最大2秒待機
    }
}

private void ReadThreadLoop() {
    while (_isRunning) {
        try {
            if (SerialPortManager.Instance != null && SerialPortManager.Instance.IsConnected) {
                string data = SerialPortManager.Instance.ReadLine();
                if (!string.IsNullOrEmpty(data)) {
                    double timestamp = AudioSettings.dspTime;
                    ShakeResolver.EnqueueInput(data.Trim(), timestamp);  // ★ 統一キューに追加
                }
            }
            // ★ Thread.Sleep(100)を削除（修正3で実施）
        }
        catch (System.Exception ex) {
            Debug.LogError($"[SerialInputReader] Thread error: {ex.Message}");
            Thread.Sleep(500);
        }
    }
}

void OnDestroy() {
    StopReadThread();
}

void OnDisable() {
    StopReadThread();
}

void OnApplicationQuit() {
    StopReadThread();
}
```

#### KeyboardInputReader.cs
```csharp
// ★ 削除：`IInputSource`インターフェースの実装
// ★ 削除：`_inputQueue`フィールド
// ★ 削除：`TryDequeue()`メソッド
// ★ 削除：`Connect()`メソッド（GameManagerイベント購読も削除）
// ★ 削除：`Disconnect()`メソッド（GameManagerイベント購読も削除）
// ★ 削除：Start()メソッド全体

[SerializeField] private KeyCode _shakeKey = KeyCode.Space;

void Update() {
    // ★ 削除：フリーズチェック（修正2で対応）
    
    if (Input.GetKeyDown(_shakeKey)) {
        double timestamp = AudioSettings.dspTime;
        ShakeResolver.EnqueueInput("shake", timestamp);  // ★ 統一キューに追加
    }
}

// ★ OnDestroy()メソッドを削除
```
- `IInputSource`インターフェースの実装を削除
- `_inputQueue`フィールドを削除
- `TryDequeue()`メソッドを削除
- `FreezeManager.IsFrozen`のチェックを削除（フリーズ処理は修正2で対応）
- Update内で`ShakeResolver.EnqueueInput("shake", timestamp);`を呼び出す

#### 削除するファイル
- `Assets/Scripts/Data/IInputSource.cs`

#### GameConstants.cs
- `DEBUG_MODE`は他の用途（ログ制御等）で使用されているため、**変更不要**

---

## 🎯 修正2: フリーズ処理の2段階ハンドラー切り替え

### 目的
- 入力層からフリーズチェックを削除（責務分離）
- フリーズ中のフェーズ切り替えに対応

### 実装内容

#### 新規作成: NullShakeHandler.cs
```csharp
using UnityEngine;

/// <summary>
/// フリーズ中用ハンドラー（何もしない）
/// </summary>
public class NullShakeHandler : MonoBehaviour, IShakeHandler {
    public void HandleShake(string data, double timestamp) {
        // 何もしない（フリーズ中の入力を無視）
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NullShakeHandler] Input ignored during freeze");
    }
}
```

#### ShakeResolver.cs
```csharp
[Header("Freeze & Phase Handlers")]
[SerializeField] private NullShakeHandler _nullHandler;        // フリーズ中用
[SerializeField] private NoteShakeHandler _noteHandler;        // 音符処理
[SerializeField] private RestShakeHandler _restHandler;        // 休符処理

private IShakeHandler _currentHandler;   // Update()で呼ばれる最終ハンドラー
private IShakeHandler _activeHandler;    // 通常時のハンドラー（フェーズに応じて変わる）

void Start() {
    // 初期状態：通常時ハンドラー（最初のフェーズで設定される）
    _currentHandler = null;  // OnPhaseChangedで設定される
    
    // イベント購読
    FreezeManager.OnFreezeChanged.AddListener(OnFreezeChanged);
    PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
    GameManager.OnShowTitle.AddListener(ResetResolver);
}

void OnFreezeChanged(bool isFrozen) {
    // フリーズ層の切り替え
    if (isFrozen) {
        _currentHandler = _nullHandler;  // フリーズ中は何もしない
    } else {
        _currentHandler = _activeHandler;  // 通常時は現在のフェーズハンドラー
    }
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log($"[ShakeResolver] Freeze changed: {isFrozen}, Handler: {_currentHandler?.GetType().Name}");
}

void OnPhaseChanged(PhaseChangeData data) {
    // フェーズ層の切り替え（_activeHandlerを変更）
    switch (data.phaseType) {
        case Phase.NotePhase:
            _activeHandler = _noteHandler;
            _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
            break;
        case Phase.LastSprintPhase:
            _activeHandler = _noteHandler;
            _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
            break;
        case Phase.RestPhase:
            _activeHandler = _restHandler;
            break;
    }
    
    // ★ 重要：フリーズ中でない場合のみ_currentHandlerを更新
    // （フリーズ中は_nullHandlerのまま、解除時にOnFreezeChangedで更新される）
    if (FreezeManager.Instance != null && !FreezeManager.Instance.IsFrozen) {
        _currentHandler = _activeHandler;
    }
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log($"[ShakeResolver] Phase changed, active handler: {_activeHandler?.GetType().Name}");
}

void OnDestroy() {
    FreezeManager.OnFreezeChanged.RemoveListener(OnFreezeChanged);
    PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
    GameManager.OnShowTitle.RemoveListener(ResetResolver);
}
```

---

## 🎯 修正3: SerialPort.ReadLine()のブロッキング特性活用

### 目的
- CPU使用率削減
- 入力レイテンシの劇的改善（最大200ms遅延 → 0ms）
- Thread.Sleep(100)の削除

### 実装内容

#### SerialPortManager.cs
```csharp
public void Connect() {
    // ... 既存の接続処理 ...
    
    _serialPort = new SerialPort(GameConstants.SERIAL_PORT_NAME, GameConstants.SERIAL_BAUD_RATE);
    _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;  // ★ 変更：ブロッキング待機
    _serialPort.WriteTimeout = 100;
    _serialPort.Open();
    
    // ... 残りの処理 ...
}
```

#### SerialInputReader.cs
```csharp
private void ReadThreadLoop() {
    while (_isRunning) {
        try {
            if (SerialPortManager.Instance != null && SerialPortManager.Instance.IsConnected) {
                string data = SerialPortManager.Instance.ReadLine();
                if (!string.IsNullOrEmpty(data)) {
                    double timestamp = AudioSettings.dspTime;
                    ShakeResolver.EnqueueInput(data.Trim(), timestamp);  // ★ 修正1の変更を適用
                }
            }
            // ★ Thread.Sleep(100)を削除
        }
        catch (System.Exception ex) {
            Debug.LogError($"[SerialInputReader] Thread error: {ex.Message}");
            Thread.Sleep(500);  // エラー時のみ待機
        }
    }
}

public void Disconnect() {
    _isRunning = false;
    
    // ★ 追加：ReadLine()のブロックを解除するためポート切断
    if (SerialPortManager.Instance != null) {
        SerialPortManager.Instance.Disconnect();
    }
    
    if (_readThread != null && _readThread.IsAlive) {
        _readThread.Join(2000);  // ★ 変更：最大2秒待機（余裕を持たせる）
    }
}
```

---

## 📁 修正対象ファイル一覧

### 修正
- `Assets/Scripts/Input/ShakeResolver.cs`（大幅修正）
- `Assets/Scripts/Input/SerialInputReader.cs`（大幅修正）
- `Assets/Scripts/Input/KeyboardInputReader.cs`（大幅簡素化）
- `Assets/Scripts/Input/SerialPortManager.cs`（軽微修正）

### 新規作成
- `Assets/Scripts/Handlers/NullShakeHandler.cs`

### 削除
- `Assets/Scripts/Data/IInputSource.cs`

---

## ⚠️ 重要な注意事項

### 1. Inspector設定の変更が必要
**ShakeResolver.cs**:
- ❌ 削除：`Serial Input Reader`フィールド
- ❌ 削除：`Keyboard Input Reader`フィールド
- ✅ 追加：`Null Handler`（新規作成したNullShakeHandlerをアタッチ）
- ✅ 既存：`Note Handler`
- ✅ 既存：`Rest Handler`
- ℹ️ `_activeHandler`は変数のみ（Inspectorフィールド不要）

### 2. 入力ソースの自動起動
- SerialInputReader: Start()で自動的にスレッド開始
- KeyboardInputReader: 常時有効（Update()で監視）
- **両方とも常に動作**（DEBUG_MODEによる切り替え不要）

### 3. 修正の依存関係
修正1と修正2は**相互依存**しているため、以下の順序で実装すること：
1. まず新しいHandlerを作成（NullShakeHandler）
2. ShakeResolverを修正（修正1と修正2を同時適用）
3. SerialInputReader, KeyboardInputReaderを修正
4. 最後に修正3（SerialPortManager）

### 4. 動作確認項目

#### 基本動作
- ✅ キーボード入力が常に有効（Unity起動後すぐに動作）
- ✅ シリアル入力が常に有効（接続時、Unity起動後すぐに動作）
- ✅ キーボードとシリアルを同時に使用しても正常動作

#### フリーズ機能
- ✅ フリーズ中は両方の入力が無視される
- ✅ フリーズ中にフェーズが切り替わっても、解除後に正しいハンドラーで処理
- ✅ RestPhaseでシェイク→フリーズ→NotePhaseに変更→フリーズ解除→音符処理が実行される

#### 初期化・リセット
- ✅ ゲーム開始前の入力が処理されない（_currentHandler = nullのため）
- ✅ 最初のOnPhaseChangedで正しくハンドラーが設定される
- ✅ タイトル復帰時に入力キューがクリアされる
- ✅ タイトル復帰後、次のゲームで正常に動作する

#### エッジケース
- ✅ OnPhaseChangedよりOnFreezeChangedが先に呼ばれても安全
- ✅ FreezeManager.Instanceがnullでもクラッシュしない

### 5. パフォーマンス改善の検証
- Thread.Sleep(100)削除により**最大200ms**の遅延改善が期待される
- CPU使用率の削減（ポーリング→ブロッキング待機）

---

## 🎯 期待される効果

- **コード削減**: IInputSourceインターフェース削除、キュー統一
- **機能向上**: Serial/Keyboard同時受け取り、フリーズ中のフェーズ切り替え対応
- **パフォーマンス**: 入力レイテンシ最大200ms改善、CPU使用率削減
- **保守性**: 責務分離、Strategyパターンの一貫した利用
