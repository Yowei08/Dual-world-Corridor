using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public GameObject player3D;
    public GameObject player2D;
    private float distance;
    private bool isUsed;

    private void Start()
    {
        distance = 1;
        isUsed = false;
    }

    private void Update()
    {
        UpdateSavePoint();
    }

    private void UpdateSavePoint()
    {
        if (player3D == null || player2D == null || isUsed)
        {
            return;
        }

        float dis = (player3D.transform.position - transform.position).magnitude;
        if (dis <= distance || player2D.transform.position.x >= transform.position.x)
        {
            PlayerSavePointManage.Manage.Save(transform);
            isUsed = true;
        }
    }
}
