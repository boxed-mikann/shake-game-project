// NoteSpawnerV2.cs に追加する場合の例

[Header("Note Sprites")]
[Tooltip("各デバイス専用のノートスプライト（10個）")]
[SerializeField] private Sprite[] deviceNoteSprites; // デバイスごとに固定

[Tooltip("ランダムに使用するノートスプライト配列")]
[SerializeField] private Sprite[] randomNoteSprites; // ランダム用

[SerializeField] private bool useRandomSprites = false; // ランダム使用フラグ

// スプライト取得メソッドを変更
private Sprite GetSpriteForDevice(int deviceId)
{
    if (useRandomSprites && randomNoteSprites != null && randomNoteSprites.Length > 0)
    {
        // ランダムモード: 配列からランダムに選択
        int randomIndex = Random.Range(0, randomNoteSprites.Length);
        return randomNoteSprites[randomIndex];
    }
    else if (deviceNoteSprites != null && deviceId >= 0 && deviceId < deviceNoteSprites.Length)
    {
        // デバイス固定モード: デバイスIDに対応するスプライト
        return deviceNoteSprites[deviceId];
    }
    return null;
}
