using UnityEngine;
using TMPro;

public class RainbowTextWhole : MonoBehaviour
{
    public TMP_Text text;
    public float speed = 0.5f;     // 動く虹にしたい時に使う（不要なら0）

    void Update()
    {
        // メッシュ更新
        text.ForceMeshUpdate();
        var textInfo = text.textInfo;

        // 全体の左右端を取得
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            float left = charInfo.bottomLeft.x;
            float right = charInfo.topRight.x;

            if (left < minX) minX = left;
            if (right > maxX) maxX = right;
        }

        float width = maxX - minX;

        // メッシュと頂点カラー書き換え
        var mesh = text.mesh;
        var colors = mesh.colors32;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;

            for (int v = 0; v < 4; v++)
            {
                float x = textInfo.meshInfo[0].vertices[vertexIndex + v].x;
                float t = (x - minX) / width;

                // HSVで虹色
                float hue = Mathf.Repeat(t + Time.time * speed, 1f);
                Color col = Color.HSVToRGB(hue, 1f, 1f);

                colors[vertexIndex + v] = col;
            }
        }

        mesh.colors32 = colors;
        text.canvasRenderer.SetMesh(mesh);
    }
}
