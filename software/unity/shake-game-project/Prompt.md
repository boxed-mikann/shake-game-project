# ゲームコード再構築：新アーキテクチャ実装

## 【背景】
旧コードベース（GameManager, PhaseController等）を新アーキテクチャに再構築します。
機能は100%保持し、構造のみ改善します。

## 【旧実装から学ぶべき仕様】
### フェーズシステム
- **フェーズ種別**：NotePhase（音符をはじく→加点）、RestPhase（休符→ペナルティ+凍結）、LastSprintPhase（最後10秒、生成速度2倍）
- **フェーズシーケンス**：GameConstants.PHASE_SEQUENCE配列で定義
  - 例：[Note 10s, Rest 5s, Note 10s, Rest 5s, ..., LastSprint 15s]
  - 合計がGAME_DURATION
- **フェーズ切り替え**：時間に基づき自動切り替え
- **イベント**：フェーズ変更時にOnPhaseChangedを発火

### スコアシステム
- NotePhase中の音符をはじく → +1点
- RestPhase中の音符をはじく → -1点 + FreezeEffect開始

### フリーズシステム
- RestPhase中の音符はじき時に発動
- 画面が半透明フラッシュ、操作不可時間発生
- LastSprintPhase中は無効

### 音符生成
- SPAWN_RATE_BASE（秒/個）で継続生成
- LastSprintPhaseでは LAST_SPRINT_MULTIPLIER (2倍) 適用
- Object Pool使用で最適化

## 【新アーキテクチャ参照】
CodeArchitecture.mdで以下を定義済み：
- 15個のクラス（Managers, Input, Gameplay, Handlers, Audio, UI, Data）
- PhaseChangeData構造体（phaseType, duration, spawnFrequency, phaseIndex）
- IInputSource, IShakeHandler インターフェース
- イベント駆動で全システム連携

## 【実装要件】

### 優先度順
1. **Data/ フォルダ基盤**
   - PhaseChangeData.cs（構造体）
   - IInputSource.cs（インターフェース）
   - IShakeHandler.cs（インターフェース）
   - GameConstants.cs（既存を改良）

2. **Managers/ マネージャー群**
   - GameManager.cs（ゲーム開始・終了、全体ライフサイクル）
   - PhaseManager.cs（フェーズ時系列管理、OnPhaseChanged発行）
   - FreezeManager.cs（凍結状態と効果音）
   - ScoreManager.cs（スコア加算・イベント発行）

3. **Gameplay/ ゲームプレイ**
   - NotePool.cs（音符のプール管理）
   - NoteManager.cs（アクティブ音符のQueue管理、DestroyOldestNote()）
   - NoteSpawner.cs（Coroutineで時間ベース生成制御）
   - Note.cs（見た目・フェーズ表示のみ）

4. **Handlers/ フェーズ別処理**
   - Phase1ShakeHandler.cs（最初の音符フェーズ用）
   - Phase*ShakeHandler.cs（以下同様）
   - 処理フロー：GetOldestNote→Nullチェック→Destroy→PlaySFX→AddScore→Freeze

5. **Input/ 入力システム**
   - SerialPortManager.cs, SerialInputReader.cs（シリアル読み込み、スレッド安全）
   - KeyboardInputReader.cs（デバッグ用）
   - ShakeResolver.cs（Strategy パターンで現在のHandlerに入力振り分け）

6. **Audio/ & UI/**
   - AudioManager.cs（AudioClip辞書キャッシング）
   - UI*.cs各種（ScoreDisplay, PhaseProgressBar, FreezeEffectUI等）

## 【旧コード参照】
以下のファイルから学ぶ：
- Assets/Scripts/Core/GameConstants.cs → PHASE_SEQUENCE, spawnRate定数
- Assets/Scripts/Core/GameManager.cs → フェーズシーケンス初期化ロジック
- Assets/Scripts/Game/PhaseController.cs → フェーズ自動切り替え、短縮倍率適用
- Assets/Scripts/Game/NotePrefab.cs → フェーズ変更イベント購読、見た目更新

## 【実装時の注意】
1. **PhaseChangeData** にspawnFrequency追加（旧実装では計算で求めていたが、新実装では渡す）
2. **PhaseManager** が OnPhaseChanged を発行（責務分離）
3. **NoteManager.DestroyOldestNote()** はQueue.Dequeue()で最古を取得→返却
4. **Phase*ShakeHandler** はNullチェック必須（アクティブNoteゼロ時用）
5. **AudioSettings.dspTime** 使用でネットワーク遅延対応可能性保留