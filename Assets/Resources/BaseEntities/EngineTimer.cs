using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EngineTimer
{
    private bool active = false;
    private bool paused = false;

    private float lastTime = 0.0f;

    private float accumulatedTime = 0.0f;

    private bool timeIsBanked = false;
    private float timeBanked = 0.0f;
    public float timerSpeed { get; private set; } = 1.0f;

    public void StartTimer()
    {
        active = true;
        accumulatedTime = 0.0f;
    }
    public void SetSpeed(float spd)
    {
        timeBanked += timerSpeed * (Time.time - lastTime);
        timeIsBanked = true;
        timerSpeed = spd;
        lastTime = Time.time;
    }
    public void UpdateTimer()
    {
        if (active && !paused)
        {
            accumulatedTime += timerSpeed * (Time.time - lastTime);
            if (timeIsBanked) accumulatedTime += timeBanked;
        }
        timeIsBanked = false;
        timeBanked = 0.0f;
        lastTime = Time.time;
    }
    public void PauseTimer(bool _paused = true)
    {
        if (_paused && !paused)
        {
            paused = true;
        }
        else if (!_paused && paused)
        {
            paused = false;
        }
        timeIsBanked = false;
        timeBanked = 0.0f;
        lastTime = Time.time;
    }
    public float GetCurrentTime()
    {
        return accumulatedTime;
    }
    public float GetTimeFrameDiff(bool fixedTime)
    {
        if (!active || paused) return 0.0f;
        if (fixedTime) return Time.fixedDeltaTime * timerSpeed;
        return Time.deltaTime * timerSpeed;
    }

}
