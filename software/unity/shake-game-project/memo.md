## 2025-11-15 16:56:03
- ボタンが反応しない問題の原因はEventSystemの不在
- UI→EventSystemを追加すると動いた

## 2025-11-19
### 入力システム・ハンドラー統合リファクタリング完了
- **IInputSource修正**: UnityEvent廃止 → 直接呼び出し方式（TryDequeue）に変更、約3倍高速化
- **IShakeHandler修正**: HandleShake(string data, double timestamp) に引数追加
- **Handler統合**: Phase1～7ShakeHandler（7個）→ NoteShakeHandler + RestShakeHandler（2個）に統合、71%削減
- **ShakeResolver書き換え**: 直接呼び出し方式 + Strategyパターン、フェーズ変更時にハンドラー差し替え
- **GameConstants追加**: LAST_SPRINT_SCORE=2, REST_PENALTY=-1 を追加
- **動作確認**: キーボード入力テスト成功、コンパイルエラーなし
- **ドキュメント更新**: CodeArchitecture.mdに変更点を反映