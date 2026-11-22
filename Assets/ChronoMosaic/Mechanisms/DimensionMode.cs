// DimensionMode.cs
// 全项目公用的维度枚举与判定。机关脚本统一用它，别各自写 if。

using UnityEngine;

public enum DimensionMode
{
    Present3D = 0,   // 只在 3D 残破现在里生效
    Past2D = 1,      // 只在 2D 温馨过去里生效
    Both = 2         // 两边都生效
}

public static class DimensionRules
{
    public static bool Is2D
    {
        get { return DimensionTransitionController.Is2D; }
    }

    public static bool Busy
    {
        get { return DimensionTransitionController.IsBusy; }
    }

    public static bool ActiveIn(DimensionMode mode)
    {
        if (mode == DimensionMode.Both) return true;
        return (mode == DimensionMode.Past2D) == Is2D;
    }

    public static DimensionMode FromBool(bool is2D)
    {
        return is2D ? DimensionMode.Past2D : DimensionMode.Present3D;
    }
}
