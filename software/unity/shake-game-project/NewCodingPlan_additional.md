## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## ✅ 微小修正項目（完了 - 2025-11-19）
- ~~スライダは減っていくようにする。フェーズの種類によって色を変える。~~ → **完了**: 進行度を1→0に反転、フェーズごとに色分け（青/オレンジ/赤）
- ~~音符の生成範囲を画面内に。~~ → **完了**: カメラのorthographicSizeから動的計算、画面サイズの90%以内に生成

---

## 微小修正項目の実装計画（2025-11-19）

### 修正1: スライダを減っていくように変更 + フェーズごとの色変更

**対象ファイル**: `Assets/Scripts/UI/PhaseProgressBar.cs`

**現状の問題点**:
- スライダーが0→1に「増えていく」表示（残り時間ではなく経過時間）
- フェーズの種類による色分けがない（視覚的にフェーズ区別が困難）

**修正内容**:
1. **進行度計算の反転**
   - 現在: `progress = (total - remaining) / total` (0→1に増える)
   - 修正後: `progress = remaining / total` (1→0に減る)
   - これにより「残り時間」の視覚的表現が直感的になる

2. **フェーズごとの色設定**
   - `OnPhaseChanged()`内でフェーズタイプに応じてスライダーの色を変更
   - 色定義（CodeArchitecture.mdのPhaseDisplay実装を参考）:
     - `Phase.NotePhase`: 青系（例: `new Color(0.3f, 0.5f, 1f)` - 爽やかな青）
     - `Phase.RestPhase`: オレンジ系（例: `new Color(1f, 0.6f, 0.2f)` - 警告的なオレンジ）
     - `Phase.LastSprintPhase`: 赤系（例: `new Color(1f, 0.2f, 0.2f)` - 緊迫感のある赤）
   - `Slider.fillRect.GetComponent<Image>().color` でfill部分の色を変更

**実装方針**:
- CodeArchitecture.mdの「3.6 UI/」の設計原則に準拠
- イベント駆動設計を維持（`PhaseManager.OnPhaseChanged`購読）
- `GameConstants.cs`に色定義を追加する選択肢もあるが、UI表現なのでPhaseProgressBar内で定義
- `[SerializeField]`でInspector設定可能にすることで調整の柔軟性を確保

**修正箇所**:
```csharp
// 1. フィールド追加
[Header("Phase Colors")]
[SerializeField] private Color _notePhaseColor = new Color(0.3f, 0.5f, 1f);
[SerializeField] private Color _restPhaseColor = new Color(1f, 0.6f, 0.2f);
[SerializeField] private Color _lastSprintColor = new Color(1f, 0.2f, 0.2f);

private Image _fillImage;

// 2. Start()でfillImageキャッシュ
void Start() {
    // 既存コード...
    
    // Fill部分のImageコンポーネント取得
    if (_progressSlider != null && _progressSlider.fillRect != null) {
        _fillImage = _progressSlider.fillRect.GetComponent<Image>();
    }
}

// 3. OnPhaseChanged()で色変更
private void OnPhaseChanged(PhaseChangeData data) {
    // 既存コード（duration, remainingTime設定）...
    
    // フェーズに応じた色変更
    if (_fillImage != null) {
        switch (data.phaseType) {
            case Phase.NotePhase:
                _fillImage.color = _notePhaseColor;
                break;
            case Phase.RestPhase:
                _fillImage.color = _restPhaseColor;
                break;
            case Phase.LastSprintPhase:
                _fillImage.color = _lastSprintColor;
                break;
        }
    }
}

// 4. Update()で進行度計算を反転
void Update() {
    // 既存コード...
    
    // ★ 修正: 減っていく方向に変更
    float progress = 0f;
    if (_totalDuration > 0f) {
        progress = _remainingTime / _totalDuration;  // 1→0に減る
    }
    
    _progressSlider.value = progress;
}
```

**設計上の利点**:
- ユーザビリティ向上: 「残り時間」の直感的な把握
- 視認性向上: フェーズの種類が色で即座に判別可能
- 保守性: Inspector設定で色調整が容易
- 疎結合維持: PhaseManagerへの依存は既存イベントのみ

---

### 修正2: 音符の生成範囲を画面内に制限

**対象ファイル**: `Assets/Scripts/Gameplay/NoteSpawner.cs`

**現状の問題点**:
- `spawnRangeX = (-6f, 6f)`, `spawnRangeY = (-4f, 4f)` が固定値
- 画面解像度やアスペクト比によっては画面外に生成される可能性
- カメラのorthographicSizeと連動していない

**修正内容**:
1. **動的な生成範囲計算**
   - カメラの`orthographicSize`から画面の実際の範囲を計算
   - アスペクト比を考慮してX軸範囲を算出
   - 画面端から少し内側にマージンを設定（例: 画面サイズの90%以内）

2. **実装方法**:
   ```csharp
   // カメラ参照を取得
   Camera mainCamera = Camera.main;
   
   // Y軸範囲 = orthographicSize
   float cameraHeight = mainCamera.orthographicSize;
   
   // X軸範囲 = orthographicSize * aspect ratio
   float cameraWidth = cameraHeight * mainCamera.aspect;
   
   // マージンを適用（画面サイズの90%以内）
   float margin = 0.9f;
   float spawnRangeXMin = -cameraWidth * margin;
   float spawnRangeXMax = cameraWidth * margin;
   float spawnRangeYMin = -cameraHeight * margin;
   float spawnRangeYMax = cameraHeight * margin;
   ```

**実装方針**:
- CodeArchitecture.mdの「3.3 Gameplay/」の設計原則に準拠
- カメラ解像度依存の動的計算（実行時に自動調整）
- Inspector設定の`spawnRangeX/Y`は削除せず、バックアップとして残す（カメラ未設定時のフォールバック）
- `GameConstants.cs`に`NOTE_SPAWN_MARGIN`定数を追加（デフォルト0.9f = 90%）

**修正箇所**:

**A. GameConstants.csに定数追加**:
```csharp
// ===== Visuals =====
/// <summary>音符生成範囲のマージン（画面サイズに対する比率）</summary>
public const float NOTE_SPAWN_MARGIN = 0.9f;  // 90%以内
```

**B. NoteSpawner.cs修正**:
```csharp
// 1. フィールド追加
[Header("Spawn Range (Fallback)")]
[SerializeField] private Vector2 spawnRangeX = new Vector2(-6f, 6f);
[SerializeField] private Vector2 spawnRangeY = new Vector2(-4f, 4f);
[Tooltip("自動計算された生成範囲（実行時に設定）")]
[SerializeField] private Vector2 _calculatedRangeX;
[SerializeField] private Vector2 _calculatedRangeY;

private Camera _mainCamera;

// 2. Awake()またはStart()で範囲計算
private void Awake() {
    // 既存コード...
    
    // カメラ取得と生成範囲計算
    _mainCamera = Camera.main;
    if (_mainCamera != null) {
        CalculateSpawnRange();
    } else {
        Debug.LogWarning("[NoteSpawner] Main camera not found, using fallback spawn range");
    }
}

/// <summary>
/// 画面サイズに基づいて生成範囲を動的計算
/// </summary>
private void CalculateSpawnRange() {
    if (_mainCamera == null) return;
    
    float cameraHeight = _mainCamera.orthographicSize;
    float cameraWidth = cameraHeight * _mainCamera.aspect;
    
    float margin = GameConstants.NOTE_SPAWN_MARGIN;
    
    _calculatedRangeX = new Vector2(-cameraWidth * margin, cameraWidth * margin);
    _calculatedRangeY = new Vector2(-cameraHeight * margin, cameraHeight * margin);
    
    if (GameConstants.DEBUG_MODE) {
        Debug.Log($"[NoteSpawner] Calculated spawn range - X: {_calculatedRangeX}, Y: {_calculatedRangeY}");
    }
}

// 3. SpawnOneNote()で計算済み範囲を使用
private void SpawnOneNote() {
    // 既存コード...
    
    // ★ 修正: 動的計算された範囲を使用
    Vector2 rangeX = (_mainCamera != null) ? _calculatedRangeX : spawnRangeX;
    Vector2 rangeY = (_mainCamera != null) ? _calculatedRangeY : spawnRangeY;
    
    Vector3 randomPos = new Vector3(
        Random.Range(rangeX.x, rangeX.y),
        Random.Range(rangeY.x, rangeY.y),
        0f
    );
    
    // 以降は既存コード...
}
```

**設計上の利点**:
- 解像度対応: 任意のアスペクト比で画面内生成を保証
- 保守性: マージン値をGameConstantsで一元管理
- デバッグ性: 計算結果をInspectorで確認可能（`_calculatedRangeX/Y`）
- 後方互換性: カメラ未設定時はInspector設定値をフォールバック
- 拡張性: 将来的にカメラズーム対応も容易（OnPhaseChangedでCalculateSpawnRange再実行）

---

### 実装優先度
1. **修正1（スライダー）**: 高（ユーザビリティ直結）
2. **修正2（生成範囲）**: 中（特定解像度でのみ問題発生の可能性）

### 実装時の注意点
- 両方の修正ともCodeArchitecture.mdの「イベント駆動」「疎結合」の原則を維持
- Inspector設定の柔軟性を確保（調整容易性）
- DEBUG_MODEでのログ出力を追加（動作確認）
- 既存の機能を破壊しない（フォールバック機構）

### テスト項目
**修正1（スライダー）**:
- [ ] スライダーが1→0に減っていくことを確認
- [ ] NotePhaseで青色表示を確認
- [ ] RestPhaseでオレンジ色表示を確認
- [ ] LastSprintPhaseで赤色表示を確認
- [ ] フェーズ切り替え時に色が即座に変わることを確認

**修正2（生成範囲）**:
- [ ] 16:9解像度で音符が画面内に生成されることを確認
- [ ] 4:3解像度で音符が画面内に生成されることを確認
- [ ] DEBUG_MODEで計算された範囲がログ出力されることを確認
- [ ] カメラ未設定時にフォールバック範囲が使用されることを確認

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