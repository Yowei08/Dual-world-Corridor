using UnityEngine;

public delegate void OnSwitch();
public delegate bool OnCheck();

// 控制世界转换
public class WorldSwitchManage
{
    private static WorldSwitchManage manage = new WorldSwitchManage();
    public static WorldSwitchManage WorldSwitch
    {
        get
        {
            return manage;
        }
    }

    // 初始默认世界,2d-false, 3d-true
    public bool nowWorld = false;
    // 世界切换事件
    public event OnSwitch switchWorld;
    // 检查事件
    public event OnCheck check;

    public void AddEvent(OnSwitch switchWorld)
    {
        this.switchWorld += switchWorld;
    }

    public void AddCheck(OnCheck check)
    {
        this.check += check;
    }

    public void UseEvent()
    {
        if (check == null || switchWorld == null)
        {
            return;
        }

        // 通过世界转换检查
        if (check())
        {
            nowWorld = !nowWorld;
            switchWorld.Invoke();
        }
    }
}
