// filepath: Assets/Scripts/Version2/Managers/GameConstantsV2.cs

using UnityEngine;

/// <summary>
/// Version2 向けのゲーム全体設定値と補助ユーティリティを提供する定数クラス。
/// 
/// 方針:
/// - 時刻は <see cref="Time.time"/> を基準として保存し、比較時はゲーム開始時刻 (gameStartTime) を差し引いた相対時刻で <c>VideoPlayer.time</c> と比較します。
/// - <c>dspTime</c> は使用しません（version2_plan.md の方針準拠）。
/// - 一部の値は将来的な ScriptableObject への移行を考慮し <c>public static readonly</c> を使用しています。
/// - Popup は Object Pool 前提で運用します。
/// </summary>
public static class GameConstantsV2
{
    // =============================
    // タイミング・同期
    // =============================

    /// <summary>
    /// シンクロ判定のウィンドウサイズ（秒）。
    /// <para>Video 側の相対時刻とゲーム側の相対時刻の差が、この値以内なら同時判定します。</para>
    /// <para>計画のデフォルト: 0.2 秒。</para>
    /// </summary>
    public static readonly float SYNC_WINDOW_SIZE = 0.20f;

    /// <summary>
    /// 開始直後の判定緩和期間（秒）。
    /// <para>ゲーム開始からこの時間の間は、厳密な同期判定を緩めます。</para>
    /// <para>計画のデフォルト: 0.5 秒。</para>
    /// </summary>
    public static readonly float START_SYNC_GRACE = 0.50f;

    /// <summary>
    /// Video とゲーム時間の許容ずれ（秒）。
    /// <para>この値を超えない範囲は UI 表示などの細かな補正で吸収します。</para>
    /// <para>計画のデフォルト: 0.1 秒。</para>
    /// </summary>
    public static readonly float MAX_ALLOWED_DESYNC = 0.10f;

    /// <summary>
    /// 自動補正を発動する閾値（秒）。
    /// <para>差分がこの値以上なら、強制的な追従やシーク等のハード補正を検討します。</para>
    /// <para>計画のデフォルト: 0.2 秒。</para>
    /// </summary>
    public static readonly float HARD_DESYNC_CORRECTION = 0.20f;

    /// <summary>
    /// ゲーム開始の同時シェイク判定ウィンドウ（秒）。
    /// <para>複数台の同時開始を見なす相対時間幅。</para>
    /// <para>計画のデフォルト: 0.2 秒。</para>
    /// </summary>
    public static readonly float START_SIMULTANEOUS_WINDOW = 0.20f;

    // =============================
    // ボルテージ・スコア（VoltageManager連携）
    // =============================

    /// <summary>
    /// 1 イベント当たりの基本加算ボルテージ。
    /// <para>ゲームイベント（シェイク等）ごとの標準加点。</para>
    /// </summary>
    public static readonly float BASE_VOLTAGE = 5f;

    /// <summary>
    /// ボルテージゲージの上限値。
    /// </summary>
    public static readonly float VOLTAGE_MAX = 100f;

    /// <summary>
    /// ボルテージの毎秒減衰量。
    /// <para>Version2 では減衰なしのため 0。</para>
    /// </summary>
    public static readonly float VOLTAGE_DECAY_PER_SEC = 0f;

    /// <summary>
    /// シンクロ率の指数。<c>pow(syncRate, n)</c> の n。
    /// <para>同期が高いほど加点を強めるための係数。</para>
    /// </summary>
    public static readonly float SYNC_BONUS_POWER = 2f;

    // =============================
    // 登録・開始（DeviceManager連携）
    // =============================

    /// <summary>
    /// 登録に必要な連続シェイク数。
    /// </summary>
    public static readonly int REGISTER_SHAKE_COUNT = 10;

    /// <summary>
    /// 登録カウントのタイムアウト（秒）。
    /// <para>タイムアウトまでに必要回数に満たない場合は登録カウントをリセット。</para>
    /// </summary>
    public static readonly float REGISTER_TIMEOUT = 5f;

    /// <summary>
    /// ゲーム開始に必要な最小登録台数。
    /// </summary>
    public static readonly int MIN_DEVICES_TO_START = 2;

    // =============================
    // シリアル通信（SerialPortManagerV2連携）
    // =============================

    /// <summary>
    /// シリアルポート名（例: "COM3"）。
    /// </summary>
    public const string SERIAL_PORT_NAME = "COM3";

    /// <summary>
    /// シリアルポートのボーレート。
    /// </summary>
    public const int SERIAL_BAUD_RATE = 115200;

    /// <summary>
    /// シリアルポート再接続の間隔（秒）。
    /// </summary>
    public const float SERIAL_RECONNECT_INTERVAL = 5f;

    // =============================
    // UI・演出（VoltageGaugeUI/PopupPool連携）
    // =============================

    /// <summary>
    /// ゲージ値の補間速度。
    /// <para>UI のスムーズな追従に使用。大きいほど速く追従します。</para>
    /// </summary>
    public static readonly float GAUGE_LERP_SPEED = 5f;

    /// <summary>
    /// ポップアップの初期プールサイズ。
    /// <para>Object Pool 前提。ピーク時の表示数に応じて調整。</para>
    /// </summary>
    public static readonly int POPUP_POOL_INITIAL_SIZE = 10;

    /// <summary>
    /// ポップアップを表示する最小シンクロ率（0〜1）。
    /// <para>0.5 は 50% を意味します。</para>
    /// </summary>
    public static readonly float POPUP_MIN_SYNC_TO_SHOW = 0.5f;

    // =============================
    // 動画・メディア（VideoManager連携）
    // =============================

    /// <summary>
    /// 動画ファイルの StreamingAssets 相対パス。
    /// <para>PC 実行時は <c>Application.streamingAssetsPath</c> と結合して使用します。</para>
    /// </summary>
    public const string VIDEO_RELATIVE_PATH = "Videos/test_video.mp4";

    /// <summary>
    /// 開発時の目標フレームレート。
    /// <para>パフォーマンスと UI レスポンスの基準にします。</para>
    /// </summary>
    public static readonly int TARGET_FPS = 60;

    // =============================
    // デバッグ
    // =============================

    /// <summary>
    /// デバッグモード（ログ出力などを有効化）。
    /// </summary>
    public static readonly bool DEBUG_MODE = true;

    /// <summary>
    /// デバッグ用途で 1 台のみでの開始を許可するか。
    /// <para>通常運用では false を推奨。</para>
    /// </summary>
    public static readonly bool ALLOW_SINGLE_DEVICE_START = false;

    // =============================
    // ユーティリティ
    // =============================

    /// <summary>
    /// 指定されたずれ秒数がハード補正を要するクリティカルかどうかを判定します。
    /// </summary>
    /// <param name="diffSeconds">Video 側の相対時刻とゲーム側の相対時刻の差（秒）。</param>
    /// <returns><c>diffSeconds &gt;= HARD_DESYNC_CORRECTION</c> の場合 <c>true</c>。</returns>
    public static bool IsDesyncCritical(float diffSeconds)
    {
        return diffSeconds >= HARD_DESYNC_CORRECTION;
    }

    /// <summary>
    /// ボルテージ値を 0〜<see cref="VOLTAGE_MAX"/> の範囲に収めます。
    /// </summary>
    /// <param name="value">入力ボルテージ値。</param>
    /// <returns>クランプされたボルテージ値。</returns>
    public static float ClampVoltage(float value)
    {
        return Mathf.Clamp(value, 0f, VOLTAGE_MAX);
    }

    /// <summary>
    /// ゲージ表示用にボルテージを 0〜1 の正規化値へ変換します。
    /// </summary>
    /// <param name="voltage">現在のボルテージ。</param>
    /// <returns><c>voltage / VOLTAGE_MAX</c>。</returns>
    public static float NormalizeGauge(float voltage)
    {
        if (VOLTAGE_MAX <= 0f) return 0f;
        return voltage / VOLTAGE_MAX;
    }

    /// <summary>
    /// 絶対時刻からゲーム開始時刻を差し引いて相対時刻へ変換します。
    /// <para>Time.time と VideoPlayer.time の比較前に使用します。</para>
    /// </summary>
    /// <param name="absoluteTime"><see cref="Time.time"/> 等の絶対時刻。</param>
    /// <param name="gameStartTime">ゲーム開始時刻（<see cref="Time.time"/> を保存した値）。</param>
    /// <returns>相対時刻（<c>absoluteTime - gameStartTime</c>）。</returns>
    public static float ToRelativeTime(float absoluteTime, float gameStartTime)
    {
        return absoluteTime - gameStartTime;
    }
}
