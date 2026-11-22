// PressureWeight.cs
// 压在踏板上的“重量标记”：玩家、残影、黄铜配重、重箱都挂这个。
// 踏板只认这个组件，避免被场景里的碎石、粒子误触发。

using UnityEngine;

public class PressureWeight : MonoBehaviour
{
    [Tooltip("留在哪个维度才有重量；残影一般选 Past2D，配重选 Both")]
    public DimensionMode dimension = DimensionMode.Both;

    [Tooltip("需要几份重量才算压下，可用于“两个人同时踩”")]
    public float weight = 1f;

    public bool IsActive
    {
        get { return DimensionRules.ActiveIn(dimension); }
    }
}
