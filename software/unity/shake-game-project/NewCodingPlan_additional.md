## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## 要修正項目

1. 音符の画像のバリエーションを増やす。
  - プリロード？共通スプライト？ってなに？

### 修正計画 #1: 音符画像のバリエーション追加（2025-11-19 改訂版）

---

## 🔧 Copilot実装指示書（簡潔版）

### 概要
複数種類の音符・休符画像を使用できるようにする。IDベースで音符⇔休符の対応を保ち、フェーズ切り替え時に自動で画像が変わる仕組みを実装。

### 実装内容

#### 1. SpriteManager.cs を新規作成
**パス**: `Assets/Scripts/Managers/SpriteManager.cs`

```csharp
using UnityEngine;

/// <summary>
/// ゲーム全体の音符・休符画像を管理（共通スプライト・プリロード方式）
/// </summary>
public class SpriteManager : MonoBehaviour
{
    [SerializeField] private Sprite[] noteSprites;     // 音符画像配列
    [SerializeField] private Sprite[] restSprites;     // 休符画像配列
    
    private static SpriteManager _instance;
    public static SpriteManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SpriteManager>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    /// <summary>
    /// 音符種類の総数を取得
    /// </summary>
    public int GetSpriteTypeCount()
    {
        return Mathf.Min(noteSprites.Length, restSprites.Length);
    }
    
    /// <summary>
    /// ランダムな音符種類IDを取得
    /// </summary>
    public int GetRandomSpriteID()
    {
        int count = GetSpriteTypeCount();
        return count > 0 ? Random.Range(0, count) : 0;
    }
    
    /// <summary>
    /// 指定IDの音符画像を取得
    /// </summary>
    public Sprite GetNoteSpriteByID(int id)
    {
        if (id >= 0 && id < noteSprites.Length)
            return noteSprites[id];
        return null;
    }
    
    /// <summary>
    /// 指定IDの休符画像を取得
    /// </summary>
    public Sprite GetRestSpriteByID(int id)
    {
        if (id >= 0 && id < restSprites.Length)
            return restSprites[id];
        return null;
    }
}
```

#### 2. Note.cs を修正
**パス**: `Assets/Scripts/Gameplay/Note.cs`

**追加するフィールド**:
```csharp
private int _spriteID = 0;                  // 音符種類ID
private Sprite _cachedNoteSprite;           // キャッシュされた音符画像
private Sprite _cachedRestSprite;           // キャッシュされた休符画像
```

**追加するメソッド**:
```csharp
/// <summary>
/// 音符種類IDを設定（生成時にNoteSpawnerから呼ばれる）
/// </summary>
public void SetSpriteID(int id)
{
    _spriteID = id;
    
    // ID設定時に画像参照をキャッシュ
    if (SpriteManager.Instance != null)
    {
        _cachedNoteSprite = SpriteManager.Instance.GetNoteSpriteByID(id);
        _cachedRestSprite = SpriteManager.Instance.GetRestSpriteByID(id);
    }
    else
    {
        // フォールバック：Inspector設定の画像を使用
        _cachedNoteSprite = noteSprite;
        _cachedRestSprite = restSprite;
    }
    
    // 現在のフェーズに応じた画像を表示
    UpdateSprite();
}
```

**SetPhase()メソッドを修正**:
```csharp
public void SetPhase(Phase phase)
{
    _currentPhase = phase;
    UpdateSprite();
}

/// <summary>
/// 現在のフェーズに基づいて画像を更新（キャッシュから取得）
/// </summary>
private void UpdateSprite()
{
    if (_spriteRenderer == null) return;
    
    if (_currentPhase == Phase.NotePhase || _currentPhase == Phase.LastSprintPhase)
    {
        if (_cachedNoteSprite != null)
            _spriteRenderer.sprite = _cachedNoteSprite;
    }
    else if (_currentPhase == Phase.RestPhase)
    {
        if (_cachedRestSprite != null)
            _spriteRenderer.sprite = _cachedRestSprite;
    }
}
```

**ResetState()メソッドを修正**:
```csharp
public void ResetState()
{
    transform.localPosition = Vector3.zero;
    transform.localRotation = Quaternion.identity;
    transform.localScale = Vector3.one;
    
    _currentPhase = Phase.NotePhase;
    _spriteID = 0;
    _cachedNoteSprite = null;  // キャッシュもクリア
    _cachedRestSprite = null;
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[Note] State reset");
}
```

#### 3. NoteSpawner.cs を修正
**パス**: `Assets/Scripts/Gameplay/NoteSpawner.cs`

**SpawnOneNote()メソッド内に追加**（ランダムな色設定の直前）:
```csharp
// ランダムな音符種類IDを設定
if (SpriteManager.Instance != null)
{
    int randomID = SpriteManager.Instance.GetRandomSpriteID();
    note.SetSpriteID(randomID);
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log($"[NoteSpawner] Spawned note with sprite ID: {randomID}");
}

// ランダムな色設定（既存コード）
SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
// ...
```

### Unity Editor設定手順

1. **SpriteManagerオブジェクト作成**
   - Hierarchy: `Managers` フォルダ配下に空のGameObject作成
   - 名前を `SpriteManager` に変更
   - `SpriteManager.cs` コンポーネントをアタッチ

2. **画像配列の設定**（Inspector）
   - **Note Sprites** 配列（サイズ3）:
     - [0] `Assets/Media/Sprites/quarter_note.png`
     - [1] `Assets/Media/Sprites/half_note.png`
     - [2] `Assets/Media/Sprites/whole_note.png`
   - **Rest Sprites** 配列（サイズ3）:
     - [0] `Assets/Media/Sprites/quarter_rest.png`
     - [1] `Assets/Media/Sprites/half_rest.png` ※なければquarter_restで代用
     - [2] `Assets/Media/Sprites/whole_rest.png`

### 動作確認
- [ ] 音符生成時に複数種類の画像が表示される
- [ ] フェーズ切り替え時に音符⇔休符が正しく切り替わる（同じ種類のまま）
- [ ] コンソールにエラーが出ない

### 設計のポイント
- **IDベース**: 同じIDで音符⇔休符の画像をペアで管理
- **キャッシュ**: 生成時に画像参照をキャッシュ、フェーズ切り替え時は高速アクセス
- **既存機能維持**: `PhaseManager.OnPhaseChanged`購読機能はそのまま
- **後方互換性**: SpriteManagerがなくても従来の方式で動作

---

### 🗂️ 検討経緯（参考）

#### 問題の原因
現在の実装では、以下の問題がある：
1. **Note.cs**: `noteSprite`と`restSprite`の2つのフィールドしかなく、各Noteインスタンスが固定の1枚の画像しか持たない
2. **NoteSpawner.cs**: 音符生成時に色はランダム化しているが、画像は固定
3. **リソース管理の欠如**: Assets/Media/Spritesに複数の音符画像（half_note.png, quarter_note.png, whole_note.pngなど）があるが、活用されていない
4. **重要な既存機能**: Note.csは`PhaseManager.OnPhaseChanged`を購読して、フェーズ切り替え時に音符⇔休符の画像を自動切り替えしている

#### 「共通スプライト」とは？
CodeArchitecture.mdの「共通仕様」「プリロード」という記述から推測される概念：
- **共通スプライト**: ゲーム全体で共有される画像リソースのこと（各Noteインスタンスが個別に持つのではなく）
- **プリロード**: ゲーム開始時に全画像をメモリ上に読み込んでおき、実行時のロード時間を削減
- **現状**: 実現されていない（各NoteがInspectorで設定された1枚の画像を参照するのみ）

#### CodeArchitecture.mdに則った設計方針

CodeArchitecture.mdには以下の設計が記載されている：
- **Note.cs**: `SetData(NoteData data)` - Sprite、タイプ(8分音符等)を設定
- **リソース管理・プリロード**: ゲーム開始時にSprite等を全てメモリ上に確保するPreloaderマネージャー（将来機能）
- **責務の分離**: Noteは見た目・状態のみ、生成制御はNoteSpawnerが担当
- **イベント駆動**: Note.csは`PhaseManager.OnPhaseChanged`を購読してフェーズ切り替えに対応

#### 修正計画（IDベース画像管理方式）

提案いただいた「音符種類IDで音符⇔休符の対応を保つ」方式を採用します。

##### Phase 1: SpriteManagerの作成（IDベース共通スプライト管理）
**目的**: 複数の音符/休符画像をペアで管理し、ID指定で取得できるようにする

**実装内容**:
```csharp
// Assets/Scripts/Managers/SpriteManager.cs
/// <summary>
/// ゲーム全体の音符・休符画像を管理（共通スプライト・プリロード方式）
/// </summary>
public class SpriteManager : MonoBehaviour {
    [SerializeField] private Sprite[] noteSprites;     // 音符画像配列（Inspector設定）
    [SerializeField] private Sprite[] restSprites;     // 休符画像配列（Inspector設定）
    
    private static SpriteManager _instance;
    public static SpriteManager Instance { get; }
    
    /// <summary>
    /// 音符種類の総数を取得（noteSpritesとrestSpritesの長さは同じ想定）
    /// </summary>
    public int GetSpriteTypeCount() {
        return Mathf.Min(noteSprites.Length, restSprites.Length);
    }
    
    /// <summary>
    /// ランダムな音符種類IDを取得（0 ～ GetSpriteTypeCount()-1）
    /// </summary>
    public int GetRandomSpriteID() {
        int count = GetSpriteTypeCount();
        return count > 0 ? Random.Range(0, count) : 0;
    }
    
    /// <summary>
    /// 指定IDの音符画像を取得
    /// </summary>
    public Sprite GetNoteSpriteByID(int id) {
        if (id >= 0 && id < noteSprites.Length)
            return noteSprites[id];
        return null;
    }
    
    /// <summary>
    /// 指定IDの休符画像を取得
    /// </summary>
    public Sprite GetRestSpriteByID(int id) {
        if (id >= 0 && id < restSprites.Length)
            return restSprites[id];
        return null;
    }
}
```

**設計の根拠**:
- **IDベース管理**: 同じIDで音符と休符の画像をペアで取得（例：ID=0なら`quarter_note.png`と`quarter_rest.png`）
- **配列の対応関係**: `noteSprites[0]`と`restSprites[0]`は対応する音符・休符のペア
- **プリロード**: Awakeで画像を配列に保持（共通スプライト、メモリ上に確保）
- **疎結合**: 他のクラスはSpriteManager経由でのみ画像にアクセス

##### Phase 2: Note.csの修正（最適化版）
**実装内容**:
```csharp
public class Note : MonoBehaviour {
    // ★ Inspector設定のフィールドは削除せず残す（後方互換性のため警告のみ）
    [SerializeField] private Sprite noteSprite;        // 音符の画像（非推奨・SpriteManager使用推奨）
    [SerializeField] private Sprite restSprite;        // 休符の画像（非推奨・SpriteManager使用推奨）
    
    private Phase _currentPhase = Phase.NotePhase;
    private SpriteRenderer _spriteRenderer;
    
    // ★ 新規追加：音符種類ID（生成時にNoteSpawnerから設定される）
    private int _spriteID = 0;
    
    // ★ 新規追加：キャッシュされた画像参照（パフォーマンス最適化）
    private Sprite _cachedNoteSprite;   // この音符の音符画像（参照）
    private Sprite _cachedRestSprite;   // この音符の休符画像（参照）
    
    // ... Awake, OnEnable, OnDisable は既存のまま ...
    
    /// <summary>
    /// 音符種類IDを設定（生成時にNoteSpawnerから呼ばれる）
    /// </summary>
    public void SetSpriteID(int id) {
        _spriteID = id;
        
        // ★ ID設定時に画像参照をキャッシュ（1回だけSpriteManagerにアクセス）
        if (SpriteManager.Instance != null) {
            _cachedNoteSprite = SpriteManager.Instance.GetNoteSpriteByID(id);
            _cachedRestSprite = SpriteManager.Instance.GetRestSpriteByID(id);
        } else {
            // フォールバック：Inspector設定の画像を使用
            _cachedNoteSprite = noteSprite;
            _cachedRestSprite = restSprite;
        }
        
        // IDが設定されたら、現在のフェーズに応じた画像を表示
        UpdateSprite();
    }
    
    /// <summary>
    /// フェーズ変更イベントハンドラ（既存機能を維持）
    /// PhaseManager.OnPhaseChanged から呼び出される
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData phaseData) {
        SetPhase(phaseData.phaseType);
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新（既存機能を維持）
    /// </summary>
    public void SetPhase(Phase phase) {
        _currentPhase = phase;
        UpdateSprite();
    }
    
    /// <summary>
    /// 現在のフェーズに基づいて画像を更新（キャッシュから取得・高速）
    /// </summary>
    private void UpdateSprite() {
        if (_spriteRenderer == null) return;
        
        // ★ キャッシュされた参照から取得（SpriteManagerへのアクセスなし・高速）
        if (_currentPhase == Phase.NotePhase || _currentPhase == Phase.LastSprintPhase) {
            if (_cachedNoteSprite != null) {
                _spriteRenderer.sprite = _cachedNoteSprite;
            }
        } else if (_currentPhase == Phase.RestPhase) {
            if (_cachedRestSprite != null) {
                _spriteRenderer.sprite = _cachedRestSprite;
            }
        }
    }
    
    /// <summary>
    /// 状態をリセット（既存機能を維持）
    /// </summary>
    public void ResetState() {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        _currentPhase = Phase.NotePhase;
        _spriteID = 0;  // ★ IDもリセット
        
        // ★ キャッシュもクリア（プールに戻るとき）
        _cachedNoteSprite = null;
        _cachedRestSprite = null;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[Note] State reset");
    }
}
```

**変更の根拠**:
- **既存機能の維持**: `PhaseManager.OnPhaseChanged`購読機能はそのまま維持
- **IDベース管理**: 生成時に設定されたIDで音符⇔休符の対応を保つ
- **フェーズ切り替え対応**: フェーズが変わるとIDは同じまま、音符⇔休符の画像だけ切り替わる
- **後方互換性**: SpriteManagerがない場合でも従来の方式で動作
- **パフォーマンス最適化**: 
  - 画像参照をキャッシュ（生成時に1回だけSpriteManagerにアクセス）
  - フェーズ切り替え時はキャッシュから取得（高速）
  - `Sprite`は参照型なので、メモリ効率も良好

**Spriteの仕組み（重要）**:
- **Sprite = 参照型**: 画像データの実体はメモリ上の1箇所、複数のオブジェクトから参照可能
- **共通スプライトの意味**: 
  - ❌ 各Noteが画像データのコピーを持つ（メモリ無駄）
  - ✅ 各Noteが共通の画像データへの参照を持つ（メモリ効率的）
- **キャッシュの効果**:
  ```
  [メモリ構造]
  SpriteManager.noteSprites[0] ← quarter_note.png（実体は1つ）
         ↑参照              ↑参照           ↑参照
  Note1._cachedNoteSprite  Note2._cachedNoteSprite  Note3._cachedNoteSprite
  
  → 画像データは1つ、参照だけが複数（合計8バイト×Note数程度）
  ```
- **パフォーマンス比較**:
  - ❌ 毎回アクセス: `SpriteManager.Instance.GetNoteSpriteByID(_spriteID)`
    - シングルトンプロパティアクセス + 配列アクセス + 境界チェック
  - ✅ キャッシュ: `_cachedNoteSprite`
    - フィールドアクセスのみ（1命令、約1 CPU cycle）

##### Phase 3: NoteSpawner.csの修正
**実装内容**:
```csharp
private void SpawnOneNote() {
    Note note = NotePool.Instance.GetNote();
    // ...既存の位置・回転設定...
    
    // ★ ランダムな音符種類IDを設定（新規追加）
    if (SpriteManager.Instance != null) {
        int randomID = SpriteManager.Instance.GetRandomSpriteID();
        note.SetSpriteID(randomID);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteSpawner] Spawned note with sprite ID: {randomID}");
    }
    
    // ランダムな色設定（既存）
    SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
    if (sr != null) {
        sr.color = GetRandomColor();
    }
    
    // NoteManager に登録（既存）
    // ...
}
```

**変更の根拠**:
- **生成時にID決定**: 音符が生成される瞬間にランダムなIDを割り当て
- **フェーズは自動対応**: Noteが`PhaseManager.OnPhaseChanged`を購読しているため、IDだけ設定すればOK
- **シンプル**: NoteSpawner側ではフェーズを意識する必要なし

#### 実装の流れ（最適化版）

**音符の生成時**:
1. `NoteSpawner.SpawnOneNote()` が `SpriteManager.GetRandomSpriteID()` でランダムID取得（例：ID=1）
2. `note.SetSpriteID(1)` でNoteにIDを設定
3. Note内部で画像参照をキャッシュ（**1回だけSpriteManagerにアクセス**）:
   ```
   _cachedNoteSprite = SpriteManager.GetNoteSpriteByID(1)  // → quarter_note.png への参照
   _cachedRestSprite = SpriteManager.GetRestSpriteByID(1)  // → quarter_rest.png への参照
   ```
4. `UpdateSprite()` が呼ばれ、現在のフェーズに応じた画像を表示
   - NotePhaseなら `_cachedNoteSprite` → `quarter_note.png`
   - RestPhaseなら `_cachedRestSprite` → `quarter_rest.png`

**フェーズ切り替え時（高速・最適化）**:
1. `PhaseManager` が `OnPhaseChanged` イベントを発行
2. 各 `Note` が `OnPhaseChanged()` ハンドラで `SetPhase()` を呼び出し
3. `UpdateSprite()` が実行され、**キャッシュから取得**（SpriteManagerアクセスなし）
   - ID=1の音符が NotePhase→RestPhase に切り替わると
   - `_cachedNoteSprite` → `_cachedRestSprite` に切り替え
   - `quarter_note.png` → `quarter_rest.png` に自動変更（実体は参照のみ、コピーなし）

**パフォーマンス特性**:
- 生成時: SpriteManagerへのアクセス **2回のみ**（音符画像1回 + 休符画像1回）
- フェーズ切り替え時: **0回**（キャッシュから取得）
- メモリオーバーヘッド: **16バイト/Note**（参照2つ、各8バイト）
- 画像データ: **0バイト増加**（実体は共有、参照のみ保持）

#### Unity Editor設定

1. **SpriteManagerオブジェクト作成**
   - Hierarchy: `Managers` → 右クリック → Create Empty → 名前を `SpriteManager` に変更
   - Add Component → SpriteManager.cs
   
2. **画像配列の設定**（Inspector上）
   - **Note Sprites** 配列:
     - [0] quarter_note.png（4分音符）
     - [1] half_note.png（2分音符）
     - [2] whole_note.png（全音符）
   - **Rest Sprites** 配列:
     - [0] quarter_rest.png（4分休符）
     - [1] half_rest.png（2分休符）※存在しない場合はquarter_restで代用
     - [2] whole_rest.png（全休符）

3. **対応関係の確認**
   - 同じインデックスが音符と休符のペアになる
   - 例：ID=0なら4分音符⇔4分休符

#### 実装順序
1. **SpriteManager.cs** を作成（IDベース画像管理）
2. **Note.cs** を修正（`_spriteID`フィールド追加、`SetSpriteID()`と`UpdateSprite()`実装）
3. **NoteSpawner.cs** の`SpawnOneNote()`でID設定を追加
4. **Unity Editor**: SpriteManagerオブジェクト作成、Inspector上で画像配列を登録
5. **動作確認**: 
   - 音符の画像がバリエーション豊かに表示されること
   - フェーズ切り替え時に音符⇔休符が正しく切り替わること

#### 設計の利点
- ✅ **フェーズ切り替え対応**: 既存の`PhaseManager.OnPhaseChanged`購読機能を維持
- ✅ **音符⇔休符の対応**: 同じIDで対応する画像を取得可能
- ✅ **バリエーション**: 複数種類の音符画像を使用可能
- ✅ **共通スプライト実現**: SpriteManagerで一元管理、プリロード方式
- ✅ **疎結合**: Note, NoteSpawnerはSpriteManager経由でのみ画像にアクセス
- ✅ **後方互換性**: SpriteManagerがなくても従来の方式で動作
- ✅ **パフォーマンス最適化**: 
  - 画像参照をキャッシュ、フェーズ切り替え時はSpriteManagerへのアクセスなし
  - フィールドアクセスのみ（1命令、約1 CPU cycle）
  - 画像実体は共有、メモリ効率的（参照型の利点）

#### 将来の拡張性
- **ResourcesからのLoad**: `Resources.Load<Sprite>()`で動的ロードも可能
- **ScriptableObject化**: 音符種類データをScriptableObjectで管理し、設定ファイル化も可能
- **重み付けランダム**: 特定の音符種類を出やすくする機能追加可能

#### テスト計画
1. **単体テスト**: 
   - `SpriteManager.GetRandomSpriteID()` が正しい範囲の値を返すこと
   - `GetNoteSpriteByID()` / `GetRestSpriteByID()` が正しい画像を返すこと
2. **統合テスト**: 
   - 音符生成時に複数種類の画像が表示されること
   - フェーズ切り替え時に音符⇔休符が正しく切り替わること（同じ種類のまま）
3. **エッジケース**: 
   - SpriteManagerがない場合でも従来の方式で動作すること

---

2. タイマー表示(TMP)
3. フェーズ表示(TMP)

4. 休符モードの時に生成された音符が休符になっていない。

5. 最終スコア表示の実装　(←プレイ中スコア表示の実装と重なる部分は大きいか？)

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