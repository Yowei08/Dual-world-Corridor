using UnityEngine;

public class CameraCenter : MonoBehaviour
{
    public Camera camera2D;
    public Camera camera3D;

    private bool nowWorld;

    private void Start()
    {
        // 加入到switchWorld事件
        WorldSwitchManage.WorldSwitch.AddEvent(SwitchWorld);
        nowWorld = WorldSwitchManage.WorldSwitch.nowWorld;

        UpdateWorld();
    }

    private void Update()
    {
        
    }

    private void UpdateWorld()
    {
        if (camera2D != null)
        {
            camera2D.enabled = !nowWorld;
        }
        if (camera3D != null)
        {
            camera3D.enabled = nowWorld;
        }
    }

    private void SwitchWorld()
    {
        nowWorld = WorldSwitchManage.WorldSwitch.nowWorld;
        UpdateWorld();
    }
}
