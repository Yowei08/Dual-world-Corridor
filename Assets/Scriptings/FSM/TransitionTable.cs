using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 转换规则表
/// </summary>
public class TransitionTable<TStateType>
{
    // 转换规则总表
    private Dictionary<TStateType, List<Transition<TStateType>>> table;

    public TransitionTable()
    {
        table = new Dictionary<TStateType, List<Transition<TStateType>>>();
    }

    public void Add(TStateType from, TStateType to, Func<bool> condition)
    {
        // 如果表没有记录这个起点
        if (!table.ContainsKey(from))
        {
            // 创建新List
            List<Transition<TStateType>> list = new List<Transition<TStateType>>();
            table.Add(from, list);
        }
        // 创建转换规则
        table[from].Add(new Transition<TStateType>(from, to, condition));
    }

    public bool TryGetNextState(TStateType currentState, out TStateType nextState)
    {
        // 获取当前状态的转换规则
        if (table.TryGetValue(currentState, out var transitions))
        {
            foreach (var transition in transitions)
            {
                // 条件成立
                if (transition.CanTransition())
                {
                    nextState = transition.to;
                    return true;
                }
            }
        }
        // 没有找到转换规则
        nextState = default;
        return false;
    }
}
