using UnityEngine;

// 控制世界转换
public class WorldSwitchManage
{
    private static WorldSwitchManage instance = new WorldSwitchManage();
    public static WorldSwitchManage Instance
    {
        get
        {
            return instance;
        }
    }

    // 初始默认世界,2d-false, 3d-true
    public bool nowWorld = false;

    public void UseEvent()
    {
        // 通过检查
        if (EventManage.Instance.UseCheckEvent())
        {
            nowWorld = !nowWorld;
            EventManage.Instance.UseSwitchEvent();
        }
    }
}
