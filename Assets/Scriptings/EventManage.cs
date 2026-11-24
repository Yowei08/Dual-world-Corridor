using System;
using UnityEngine;

public class EventManage
{
    private static EventManage instance = new EventManage();
    public static EventManage Instance
    {
        get
        {
            return instance;
        }
    }

    // 世界转换前检查事件
    private event Func<bool> checkSwitch;
    // 世界转换事件
    private event Action worldSwitch;
    // 玩家死亡事件
    private event Action playerDie;


    public void AddCheckEvent(Func<bool> func)
    {
        if (func != null)
        {
            checkSwitch += func;
        }
    }
    public bool UseCheckEvent()
    {
        if (checkSwitch != null)
        {
            return checkSwitch();
        }
        return false;
    }

    public void AddSwitchEvent(Action action)
    {
        if (action != null)
        {
            worldSwitch += action;
        }
    }
    public void UseSwitchEvent()
    {
        worldSwitch?.Invoke();
    }

    public void AddDieEvent(Action action)
    {
        if (action != null)
        {
            playerDie += action;
        }
    }
    public void UseDieEvent()
    {
        playerDie?.Invoke();
    }
}
