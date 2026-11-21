using UnityEngine;
using System.Collections.Generic;
using System;


/// <summary>
/// 所有状态类父类接口
/// </summary>
public interface I_State
{
    // 进入状态执行方法
    public void OnEnter();
    // 每帧执行方法
    public void OnUpdate();
    // 离开状态执行方法
    public void OnExit();
}

/// <summary>
/// Blackboard作为共享资源
/// </summary>
public class Blackboard
{

}

/// <summary>
/// 状态机
/// </summary>
public class FSM<TStateType, TBlackboard>
{
    // 当前状态类型
    private TStateType currentStateType;
    // 当前状态
    private I_State currentState;
    // 当前状态机共享资源
    private TBlackboard blackboard;
    // 状态存储字典
    private Dictionary<TStateType, I_State> states;
    // 转换规则表
    private TransitionTable<TStateType> table;

    public FSM(TBlackboard blackboard)
    {
        // 初始化blackboard、字典、转换规则表
        this.blackboard = blackboard;
        this.states = new Dictionary<TStateType, I_State>();
        this.table = new TransitionTable<TStateType>();
    }

    // 加入状态
    public void AddState(TStateType type, I_State state)
    {
        // 判断是否与已有状态重复
        if (states.ContainsKey(type) && states.ContainsValue(state))
        {
            Debug.Log("已经添加该状态");
            return;
        }

        states.Add(type, state);
    }

    // 加入转换规则
    public void AddTransition(TStateType from, TStateType to, Func<bool> condition)
    {
        table.Add(from, to, condition);
    }

    // 切换状态
    public void SwitchState(TStateType newStateType)
    {
        // 判断是否有状态
        if (!states.ContainsKey(newStateType))
        {
            Debug.Log("没有找到对应状态");
            return;
        }
        // 判断是否与当前状态相同
        else if (currentState == states[newStateType])
        {
            Debug.Log("已经在对应状态中");
            return;
        }

        // 如果当前状态不为空就退出当前状态
        currentState?.OnExit();
        // 切换当前状态为新状态
        currentStateType = newStateType;
        currentState = states[newStateType];
        // 新状态执行进入逻辑
        currentState.OnEnter();
    }

    private void CheckTransition()
    {
        // 如果转换规则表得到了下一个状态，证明需要转换
        if (table.TryGetNextState(currentStateType, out TStateType nextState))
        {
            SwitchState(nextState);
        }
    }

    // 每帧执行
    public void OnUpdate()
    {
        CheckTransition();
        currentState?.OnUpdate();
    }
}
