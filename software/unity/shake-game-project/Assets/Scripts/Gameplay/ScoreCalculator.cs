using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 加速度値からスコアを計算
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// 加速度をスコアに変換
    /// </summary>
    public static int AccelerationToScore(float acceleration)
    {
        return (int)(acceleration / Constants.ACCELERATION_TO_SCORE_DIVISOR);
    }
}