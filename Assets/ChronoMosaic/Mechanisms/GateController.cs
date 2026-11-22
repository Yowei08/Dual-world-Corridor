// GateController.cs
// 门 / 闸 / 电梯：所有 required 踏板同时按下才开。
// 直接挂在大门上，拦路的碰撞体放 blocker。

using UnityEngine;
using UnityEngine.Events;

public class GateController : MonoBehaviour
{
    [Header("条件")]
    public PressurePlate[] requiredPlates;
    [Tooltip("全满足才开；否则任意满足即可")]
    public bool requireAll = true;
    [Tooltip("开启后不再关闭")]
    public bool stayOpen = false;

    [Header("运动")]
    public Transform gateVisual;
    public Vector3 openOffset = new Vector3(0f, 5f, 0f);
    public float openSpeed = 2.5f;
    [Tooltip("拦路碰撞体，开启时关闭，避免视觉开了但人过不去")]
    public Collider blocker;

    [Header("事件")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool latched;

    private void Awake()
    {
        if (gateVisual != null)
        {
            closedPos = gateVisual.localPosition;
            openPos = closedPos + openOffset;
        }
    }

    private void Update()
    {
        bool condition = Evaluate();
        if (condition && !IsOpen) Open();
        else if (!condition && IsOpen && !stayOpen && !latched) Close();

        if (gateVisual != null)
        {
            Vector3 target = IsOpen ? openPos : closedPos;
            gateVisual.localPosition = Vector3.MoveTowards(gateVisual.localPosition, target,
                openSpeed * Time.deltaTime);
        }
    }

    private bool Evaluate()
    {
        if (requiredPlates == null || requiredPlates.Length == 0) return false;

        int pressed = 0;
        for (int i = 0; i < requiredPlates.Length; i++)
        {
            if (requiredPlates[i] != null && requiredPlates[i].IsPressed) pressed++;
        }
        return requireAll ? pressed == requiredPlates.Length : pressed > 0;
    }

    private void Open()
    {
        IsOpen = true;
        if (blocker != null) blocker.enabled = false;
        if (onOpened != null) onOpened.Invoke();
    }

    private void Close()
    {
        IsOpen = false;
        if (blocker != null) blocker.enabled = true;
        if (onClosed != null) onClosed.Invoke();
    }

    [ContextMenu("强制开启")]
    public void ForceOpen()
    {
        latched = true;
        Open();
    }
}
