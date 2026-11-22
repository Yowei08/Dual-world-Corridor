// PastEcho.cs
// 【过去残影】：2D 模式下按右键，在原地留下一个带碰撞的金色分身，持续 15 s。
// 它挂了 PressureWeight，所以能替你踩住踏板；也能被小怪当成仇恨目标（看你的 AI 实现）。

using UnityEngine;

public class PastEcho : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode usableIn = DimensionMode.Past2D;
    public GameObject echoPrefab;
    public float lifetime = 15f;
    public float cooldown = 3f;
    [Tooltip("同时只能存在几个残影")]
    public int maxAlive = 1;

    [Header("生成点")]
    public Transform spawnPoint;

    private float nextReadyTime;
    private int alive;

    private void Update()
    {
        if (!DimensionRules.ActiveIn(usableIn) || DimensionRules.Busy) return;
        if (!JamInput.EchoDown || Time.time < nextReadyTime) return;
        Spawn();
    }

    public void Spawn()
    {
        // 维度/转场校验放在这里，别只放在 Update —— 外部调用也会走这条路
        if (!DimensionRules.ActiveIn(usableIn) || DimensionRules.Busy) return;
        if (echoPrefab == null || alive >= maxAlive) return;

        Transform t = spawnPoint != null ? spawnPoint : transform;
        GameObject echo = Instantiate(echoPrefab, t.position, t.rotation);
        alive++;
        nextReadyTime = Time.time + cooldown;

        TimedDestroy timer = echo.GetComponent<TimedDestroy>();
        if (timer == null) timer = echo.AddComponent<TimedDestroy>();
        timer.Init(lifetime, OnEchoGone);
    }

    private void OnEchoGone()
    {
        alive = Mathf.Max(0, alive - 1);
    }
}
