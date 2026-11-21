using System;
using System.Net.NetworkInformation;
using UnityEngine;

/// <summary>
/// 转换规则
/// </summary>
public class Transition<TStateType>
{
    // 从哪个状态出发
    public TStateType from;
    // 转换到哪个状态
    public TStateType to;
    // 转换的条件
    public Func<bool> condition;

    public Transition(TStateType from, TStateType to, Func<bool> condition)
    {
        this.from = from;
        this.to = to;
        this.condition = condition;
    }

    // 检查是否可以转换
    public bool CanTransition()
    {
        return condition();
    }
}
