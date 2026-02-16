using static System.Math;
using System.Collections;
using TMPro;
using UnityEngine;

public static class TimeUtils
{
    public static IEnumerator QueueTimer(TMP_Text queueTimerText)
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime;

            int seconds = Mathf.FloorToInt(timer);
            float milliseconds = timer - seconds;

            queueTimerText.SetText("{0:0.00}", timer);

            yield return null;
        }
    }
}