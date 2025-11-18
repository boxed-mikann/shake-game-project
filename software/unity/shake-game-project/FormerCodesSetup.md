# FormerCodes フォルダ セットアップ手順

新アーキテクチャ実装時、旧コードを参照可能に保つための手順です。

---

## 【手順 1】FormerCodes フォルダ作成

```powershell
# PowerShell で実行
mkdir -Path "Assets/Scripts/FormerCodes"
```

## 【手順 2】旧スクリプトを FormerCodes に移動

**以下のファイルを `Assets/Scripts/Core/` から `Assets/Scripts/FormerCodes/Core/` に移動：**

```
Assets/Scripts/Core/GameManager.cs → Assets/Scripts/FormerCodes/Core/GameManager.cs
Assets/Scripts/Core/GameConstants.cs → Assets/Scripts/FormerCodes/Core/GameConstants.cs
Assets/Scripts/Core/... （他のファイルも同様）
```

**以下のファイルを `Assets/Scripts/Game/` から `Assets/Scripts/FormerCodes/Game/` に移動：**

```
Assets/Scripts/Game/PhaseController.cs → Assets/Scripts/FormerCodes/Game/PhaseController.cs
Assets/Scripts/Game/NotePrefab.cs → Assets/Scripts/FormerCodes/Game/NotePrefab.cs
Assets/Scripts/Game/... （他のファイルも同様）
```

**入力・UI も同様に移動：**

```
Assets/Scripts/Input/ → Assets/Scripts/FormerCodes/Input/
Assets/Scripts/UI/ → Assets/Scripts/FormerCodes/UI/
```

## 【手順 3】空のフォルダを削除（オプション）

```powershell
# 移動後、元のフォルダが空になったら削除
Remove-Item -Path "Assets/Scripts/Core" -Force
Remove-Item -Path "Assets/Scripts/Game" -Force
```

## 【手順 4】FormerCodes 用説明ファイル作成

`Assets/Scripts/FormerCodes/README.md` を作成：

```markdown
# FormerCodes - 旧実装参照用

このフォルダは新アーキテクチャ実装時の参照用に保持されています。

## 主要ファイル

### Core/
- **GameManager.cs** - 旧ゲーム管理システム
  - 参照ポイント：フェーズシーケンス初期化ロジック（InitializePhaseSequence）
  - 参照ポイント：フリーズトリガー（TriggerFreeze）
  - 参照ポイント：音符生成ロジック（UpdateNoteSpawning, SpawnNote）

- **GameConstants.cs** - 定数定義（重要）
  - PHASE_SEQUENCE 配列：フェーズ定義と継続時間
  - SPAWN_RATE_BASE：音符生成レート（秒）
  - LAST_SPRINT_MULTIPLIER：ラストスパント倍率
  - NOTE_SCORE：スコア加点値
  - Serial 通信定数

### Game/
- **PhaseController.cs** - フェーズ自動切り替え
  - 参照ポイント：フェーズ切り替えロジック（SwitchPhase）
  - 参照ポイント：短縮倍率適用（PHASE_SHORTENING_RATE）

- **NotePrefab.cs** - 音符オブジェクト
  - 参照ポイント：フェーズ変更イベント購読（OnPhaseChanged）
  - 参照ポイント：見た目更新（SetPhase）

### Input/, UI/
- 旧入力・UI システム（新実装では再設計）

## 参照方法

新クラス実装時に仕様確認が必要な場合：

1. **ImplementationPrompts.md** の「参照元」セクションを確認
2. 該当ファイルを FormerCodes/ から開く
3. ロジック・定数値を新実装に組み込む

例：
```
PhaseManager.cs 実装時：
→ FormerCodes/Core/GameManager.cs の InitializePhaseSequence() を参照
→ PHASE_SEQUENCE の処理方法を学ぶ
```

## 削除時期

新実装が完全に動作確認できた後（テスト完了後）、
このフォルダ全体を削除することを推奨します。

削除方法：
```powershell
Remove-Item -Path "Assets/Scripts/FormerCodes" -Recurse -Force
git add -A
git commit -m "Remove FormerCodes after successful architecture refactor"
```
```

## 【手順 5】Unity エディタで再コンパイル確認

1. Unity エディタを再度開く
2. コンソールにエラーがないか確認（FormerCodes の型参照エラーが出ないこと）

---

## 注意事項

### ⚠️ FormerCodes フォルダ内のスクリプト実行

- **FormerCodes 内のスクリプトはコンパイル対象です**
- 新実装と型名が被る場合（例：`Phase` enum、`GameManager` class）、**コンパイルエラーが発生します**

**対策：**

オプション A：`FormerCodes/` を `Assets/Scripts/` の外に移動
```powershell
# Assets/ の同じレベルに移動
Move-Item -Path "Assets/Scripts/FormerCodes" -Destination "FormerCodes"
```

オプション B：`FormerCodes/` を namespace でラップ
```csharp
// FormerCodes/Core/GameManager.cs の先頭に追加
namespace FormerCodes
{
    public class GameManager : MonoBehaviour { ... }
}

// 参照時は
var oldManager = FormerCodes.GameManager.Instance;
```

**推奨：オプション A（Assets 外に移動）**
- コンパイルエラーなし
- 参照も手軽（ファイルを開く）

---

## セットアップ完了チェックリスト

- [ ] FormerCodes フォルダを作成
- [ ] 旧スクリプトを FormerCodes に移動
- [ ] Assets/Scripts/Core, Game 等の空フォルダを削除
- [ ] Assets/Scripts/FormerCodes/README.md を作成
- [ ] Unity エディタで再コンパイル（エラーなし）
- [ ] ImplementationPrompts.md で参照パスを確認

**これで準備完了です！**
