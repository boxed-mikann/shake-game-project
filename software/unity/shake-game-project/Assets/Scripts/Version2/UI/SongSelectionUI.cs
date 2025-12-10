using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class SongSelectionUI : MonoBehaviour
{
    [System.Serializable]
    private class VideoManifest
    {
        public string[] videos;
    }
    [Header("Carousel UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image jacketImage;
    [SerializeField] private Image decisionGauge; // 決定進捗ゲージ
    [SerializeField] private Animator carouselAnimator; // 回転アニメーション用

    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TMP_InputField jsonInputField;
    [SerializeField] private TMP_InputField videoPathInputField;
    [SerializeField] private TMP_Dropdown videoDropdown; // 動画選択用ドロップダウン
    [SerializeField] private Button debugStartButton;

    private void Start()
    {
        // イベント購読
        if (SongSelectionManager.Instance != null)
        {
            SongSelectionManager.Instance.OnSongChanged += UpdateUI;
            SongSelectionManager.Instance.OnDecisionProgress += UpdateGauge;
            
            // 初期表示のために手動で現在の曲を取得して更新（マネージャーのStartより遅い場合への対策）
            // マネージャー側に現在の曲を取得するプロパティがあれば良いが、
            // ここではイベントが発火済みかもしれないので、マネージャーのStartでの発火に任せるか、
            // 確実に同期するためにマネージャーにCurrentSongプロパティを追加するのがベスト。
            // 今回は簡易的に、マネージャーのStartでのInvokeに期待しつつ、
            // もしUIが後から生成された場合のために、マネージャー側で「現在の曲」を再通知するメソッドがあると良い。
            // 現状はマネージャーのStartでInvokeしているので、シーンロード時なら概ね大丈夫。
        }

        if (debugStartButton != null)
        {
            debugStartButton.onClick.AddListener(OnDebugStartClicked);
        }
        
        // デバッグパネルは最初は非表示
        if (debugPanel != null) debugPanel.SetActive(false);

        // ゲージの初期化
        UpdateGauge(0f);

        // 動画ドロップダウンの初期化
#if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(PopulateVideoDropdownFromManifest());
#else
        PopulateVideoDropdown();
#endif
    }

    /// <summary>
    /// WebGL用: マニフェストファイルから動画リストを取得
    /// </summary>
    private IEnumerator PopulateVideoDropdownFromManifest()
    {
        if (videoDropdown == null) yield break;

        videoDropdown.ClearOptions();
        List<string> options = new List<string> { "Select Video..." };

        string manifestPath = Path.Combine(Application.streamingAssetsPath, "video_manifest.json");
        
        using (UnityWebRequest www = UnityWebRequest.Get(manifestPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SongSelectionUI] Failed to load video manifest: {www.error}");
            }
            else
            {
                try
                {
                    string json = www.downloadHandler.text;
                    VideoManifest manifest = JsonUtility.FromJson<VideoManifest>(json);
                    if (manifest != null && manifest.videos != null)
                    {
                        options.AddRange(manifest.videos);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SongSelectionUI] Failed to parse video manifest: {e.Message}");
                }
            }
        }

        videoDropdown.AddOptions(options);
        videoDropdown.onValueChanged.AddListener(OnVideoDropdownChanged);
    }

    /// <summary>
    /// StreamingAssets/Videos フォルダ内の動画ファイルをドロップダウンに設定
    /// </summary>
    private void PopulateVideoDropdown()
    {
        if (videoDropdown == null) return;

        videoDropdown.ClearOptions();
        List<string> options = new List<string> { "Select Video..." };

        string videoDir = Path.Combine(Application.streamingAssetsPath, "Videos");
        if (Directory.Exists(videoDir))
        {
            // 対応する拡張子（必要に応じて追加）
            string[] extensions = { "*.mp4", "*.webm", "*.mov", "*.avi" };
            List<string> files = new List<string>();

            foreach (var ext in extensions)
            {
                files.AddRange(Directory.GetFiles(videoDir, ext));
            }

            foreach (var file in files)
            {
                options.Add(Path.GetFileName(file));
            }
        }
        else
        {
            Debug.LogWarning($"[SongSelectionUI] Video directory not found: {videoDir}");
        }

        videoDropdown.AddOptions(options);
        videoDropdown.onValueChanged.AddListener(OnVideoDropdownChanged);
    }

    /// <summary>
    /// ドロップダウン変更時にInput Fieldにパスを反映
    /// </summary>
    private void OnVideoDropdownChanged(int index)
    {
        if (videoDropdown == null) return;
        
        // 0番目は "Select Video..." なので無視
        if (index == 0)
        {
            return;
        }

        string fileName = videoDropdown.options[index].text;
        string path = "Videos/" + fileName;
        
        // InputFieldは表示用として更新するが、実際の値はドロップダウンから取得する
        if (videoPathInputField != null)
        {
            videoPathInputField.text = path;
        }
    }

    // Updateメソッド（Dキー判定）は削除

    /// <summary>
    /// デバッグパネルの表示切り替え（ボタンから呼ぶ）
    /// </summary>
    public void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }

    /// <summary>
    /// 強制終了ボタン（デバッグ用）
    /// </summary>
    public void OnForceQuitClicked()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.ForceSkipToResult();
        }
    }

    /// <summary>
    /// デバッグパネル表示時に現在の曲の動画パスをセット
    /// </summary>
    private void UpdateDebugPanelInit()
    {
        // 現在選択中の曲があれば、その動画パスを入力欄にセットしておく
        // これにより、JSONだけ貼り付けてもその曲で再生される
        // SongSelectionManagerから現在の曲を取得できれば良いが...
        // 簡易的に、最後にUpdateUIで受け取った曲を覚えておく必要がある
    }

    private SongData currentSelectedSong;

    private void UpdateUI(SongData song)
    {
        if (song == null) return;
        currentSelectedSong = song;

        if (titleText != null) titleText.text = song.title;
        if (jacketImage != null) jacketImage.sprite = song.jacketImage;
        
        // アニメーショントリガー
        if (carouselAnimator != null) carouselAnimator.SetTrigger("Rotate");
        
        // デバッグパネルの動画パスも更新（パネルが開いている場合、あるいは次回開くとき用）
        // ただし、ユーザーがドロップダウンで選択している場合は上書きしない方が親切かもしれないが、
        // 「曲が変わったらその曲の動画になる」のが基本挙動なので上書きする。
        if (videoPathInputField != null)
        {
            videoPathInputField.text = song.videoPath;
        }
    }

    private void UpdateGauge(float progress)
    {
        if (decisionGauge != null)
        {
            decisionGauge.fillAmount = progress;
        }
    }

    private void OnDebugStartClicked()
    {
        if (jsonInputField == null) return;

        string json = jsonInputField.text;
        string video = "";

        // 動画パスの取得ロジック変更：
        // 1. ドロップダウンが選択されていればそれを使う
        // 2. ドロップダウンが未選択(index 0)なら、InputFieldの値を使う（手入力や自動入力された値）
        if (videoDropdown != null && videoDropdown.value > 0)
        {
            string fileName = videoDropdown.options[videoDropdown.value].text;
            video = "Videos/" + fileName;
        }
        else if (videoPathInputField != null)
        {
            video = videoPathInputField.text;
        }

        // 入力値のクリーニング
        if (!string.IsNullOrEmpty(video))
        {
            // 前後の空白削除、ダブルクォート削除（パスをコピペしたときに付きがち）
            video = video.Trim().Replace("\"", "");
        }
        
        if (!string.IsNullOrEmpty(json))
        {
             // JSONは中身なのでTrimだけしておく
             json = json.Trim();
             // マークダウンのコードブロック記号が含まれていたら削除
             if (json.StartsWith("```json")) json = json.Replace("```json", "").Replace("```", "").Trim();
             else if (json.StartsWith("```")) json = json.Replace("```", "").Trim();
        }

        Debug.Log($"[SongSelectionUI] Debug Start: JSON Length={json.Length}, Video={video}");
        if (json.Length > 0) Debug.Log($"[SongSelectionUI] JSON Start: {json.Substring(0, Mathf.Min(50, json.Length))}");

        if (!string.IsNullOrEmpty(json))
        {
            // 簡易チェック：JSONが { で始まっていない場合、入力欄の間違いの可能性
            if (!json.StartsWith("{"))
            {
                Debug.LogError("[SongSelectionUI] JSON入力欄の内容が '{' で始まっていません。ファイルパスではなく、JSONの中身そのものを貼り付けてください。また、Video入力欄と間違えていないか確認してください。");
            }

            if (SongSelectionManager.Instance != null)
            {
                SongSelectionManager.Instance.StartCustomGame(json, video);
                if (debugPanel != null) debugPanel.SetActive(false);
            }
        }
    }
}
