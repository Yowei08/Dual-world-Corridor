using UnityEngine;

public class CameraCenter : MonoBehaviour
{
    public static CameraCenter Instance { get; private set; }

    public Camera camera2D;
    public Camera camera3D;

    private bool nowWorld;

    private void Awake()
    {
        Instance = this;

        EventManage.Instance.AddSwitchEvent(SwitchWorld);
    }

    private void Start()
    {
        nowWorld = WorldSwitchManage.Instance.nowWorld;

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
        nowWorld = WorldSwitchManage.Instance.nowWorld;
        UpdateWorld();
    }
}
