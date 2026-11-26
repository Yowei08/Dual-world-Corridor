using UnityEngine;

public class PlayerSavePointManage : MonoBehaviour
{
    public static PlayerSavePointManage Instance { get; private set; }

    // 初始存档点
    public Transform start;

    // 上一个存档点
    private Transform lastSavePoint;

    private void Awake()
    {
        EventManage.Instance.AddDieEvent(ResetLastSavePoint);
    }

    private void Start()
    {
        Instance = this;

        if (start == null)
        {
            PlayerController3D controller = FindFirstObjectByType<PlayerController3D>();
            if (controller != null)
            {
                Transform target = controller.player3D != null
                    ? controller.player3D.transform
                    : controller.transform;
                GameObject savePoint = new GameObject("StartSavePoint");
                savePoint.transform.position = target.position;
                start = savePoint.transform;
            }
        }

        if (start != null)
        {
            lastSavePoint = start;
        }
    }

    // 保存
    public void Save(Transform newSavePoint)
    {
        if (newSavePoint != null)
        {
            lastSavePoint = newSavePoint;
        }
    }

    // 获取
    public Transform Get(bool isStart)
    {
        // 返回到出生点
        if (isStart)
        {
            return start;
        }

        // 返回到上一个存档点
        return lastSavePoint;
    }

    private void ResetLastSavePoint()
    {
        if (start != null)
        {
            lastSavePoint = start;
        }
    }
}
