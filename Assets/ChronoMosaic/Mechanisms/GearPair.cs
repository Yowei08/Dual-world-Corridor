// GearPair.cs —— 双墙齿轮（策划案的王牌机制）
// 策划案 §5.1 第 3 段：
//   左墙主动齿轮 Pos(-9.5, 4.5, 30.0)，右墙从动齿轮 Pos(+9.5, 4.5, 33.3)，相隔 19 m。
//   拉下 Z = 27 的拉杆后，左墙齿轮转，右墙齿轮隔 19 m 一动不动；
//   按 Q 切 2D 侧视，两轮在投影上相切咬合，动力"穿过"这 19 m，门开了。
//
// 所以啮合判定不能是"距离小于某个容差"，而必须是【两轮外切】：
//   间隙误差 = |轮心距 - (r1 + r2)| ≤ 齿隙
// 而且"轮心距"在 3D 用真实距离、在 2D 用 Y/Z 投影距离——
// 19 m 的墙间距在 2D 里被压成 0，齿轮才咬得上。这不是特判，是几何。

using UnityEngine;
using UnityEngine.Events;

public class GearPair : MonoBehaviour
{
    [Header("齿轮")]
    public Transform driverGear;
    public Transform drivenGear;
    [Tooltip("主动轮半径（米）")]
    public float driverRadius = 1.65f;
    [Tooltip("从动轮半径（米）")]
    public float drivenRadius = 1.65f;
    [Tooltip("自转轴（贴墙齿轮一般是圆柱的本地 Y）")]
    public Vector3 localAxis = Vector3.up;

    [Header("咬合")]
    [Tooltip("齿隙容差：投影轮心距与 (r1+r2) 的允许误差")]
    public float toothGap = 0.15f;
    [Tooltip("主动轮转速（度/秒）")]
    public float driverSpeed = 60f;
    [Tooltip("从动轮转满多少圈算完成（开门）")]
    public float requiredTurns = 1f;

    [Header("状态")]
    public bool driverRunning;

    [Header("事件")]
    public UnityEvent onDriverStarted = new UnityEvent();
    public UnityEvent onMeshed = new UnityEvent();
    public UnityEvent onUnmeshed = new UnityEvent();
    public UnityEvent onCompleted = new UnityEvent();

    public bool IsMeshed { get; private set; }
    public bool Completed { get; private set; }
    /// <summary>Y/Z 投影轮心距（2D 侧视看到的距离）</summary>
    public float ProjectedDistance { get; private set; }
    /// <summary>投影轮心距与 r1+r2 的差；越小越"严丝合缝"</summary>
    public float GapError { get; private set; }
    /// <summary>从动轮累计转过的角度</summary>
    public float DrivenDegrees { get; private set; }

    private void Update()
    {
        if (driverGear == null || drivenGear == null) return;

        Vector3 delta = driverGear.position - drivenGear.position;
        ProjectedDistance = new Vector2(delta.y, delta.z).magnitude;

        // 3D 用真实距离，2D 用投影距离 —— 这就是"隔 19 m 不动"的来源
        float centerDistance = DimensionRules.Is2D ? ProjectedDistance : delta.magnitude;
        GapError = Mathf.Abs(centerDistance - (driverRadius + drivenRadius));

        bool now = driverRunning && GapError <= toothGap;
        if (now != IsMeshed)
        {
            IsMeshed = now;
            if (now) { if (onMeshed != null) onMeshed.Invoke(); }
            else { if (onUnmeshed != null) onUnmeshed.Invoke(); }
        }

        float dt = Time.deltaTime;
        if (driverRunning)
            driverGear.Rotate(localAxis, driverSpeed * dt, Space.Self);

        if (IsMeshed)
        {
            // 传动比：半径比决定转速，方向相反
            float ratio = driverRadius / Mathf.Max(0.01f, drivenRadius);
            float drivenSpeed = -driverSpeed * ratio;
            drivenGear.Rotate(localAxis, drivenSpeed * dt, Space.Self);
            DrivenDegrees += Mathf.Abs(drivenSpeed) * dt;

            if (!Completed && DrivenDegrees >= 360f * Mathf.Max(0.01f, requiredTurns))
            {
                Completed = true;
                if (onCompleted != null) onCompleted.Invoke();
            }
        }
    }

    /// <summary>拉杆调用。</summary>
    public void SetDriverRunning(bool running)
    {
        if (running && !driverRunning && onDriverStarted != null) onDriverStarted.Invoke();
        driverRunning = running;
    }

    /// <summary>拉杆（按一下切换）。</summary>
    public void ToggleDriver()
    {
        SetDriverRunning(!driverRunning);
    }

    [ContextMenu("调试：清空进度")]
    public void ResetProgress()
    {
        DrivenDegrees = 0f;
        Completed = false;
    }
}
