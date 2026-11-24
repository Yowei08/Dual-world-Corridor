using UnityEngine;

public class PlayerSavePointManage : MonoBehaviour
{
    public static PlayerSavePointManage Manage { get; private set; }

    // 初始存档点
    public Transform start;

    // 上一个存档点
    private Transform lastSavePoint;

    private void Start()
    {
        Manage = this;
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
        if (lastSavePoint == null)
        {
            return null;
        }
        if (isStart)
        {
            return start;
        }
        return lastSavePoint;
    }
}
