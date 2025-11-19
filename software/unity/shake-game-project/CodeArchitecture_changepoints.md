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
