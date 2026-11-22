// Interactable3D.cs
// 所有“按 E”的东西（拉杆、手轮、控制台、浇水花洒）都挂这个，事件在 Inspector 里接。
// 需要一个 Collider 才能被 PlayerInteractor 找到。

using UnityEngine;
using UnityEngine.Events;

public class Interactable3D : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Both;
    [Tooltip("显示在屏幕上的提示，留空则不提示")]
    public string prompt = "按 E 互动";
    [Tooltip("是否只能用一次")]
    public bool oneShot = false;

    [Header("事件")]
    // 一定要给默认值：别的脚本 AddComponent 后立刻接事件时，null 的 UnityEvent 会 NRE
    public UnityEvent onInteract = new UnityEvent();

    public bool Used { get; private set; }

    public bool CanInteract
    {
        get { return !(oneShot && Used) && DimensionRules.ActiveIn(activeIn) && !DimensionRules.Busy; }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        Used = true;
        if (onInteract != null) onInteract.Invoke();
    }
}
