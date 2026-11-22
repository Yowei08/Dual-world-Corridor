using UnityEngine;

/// <summary>
/// 2D摄像机：始终跟随2D玩家
/// 只跟X/Y，Z保持原样（2D世界在Z轴上有偏移，别把它带跑）
/// </summary>
public class Camera2D : MonoBehaviour
{
    public GameObject player2D; // 跟随目标，不拖就自动找场景里的Player2D
    public float offsetY = 2f; // 跟随位置上抬多少（画面里玩家靠下，前路看得多）
    public float smoothTime = 0.1f; // 跟随的平滑时间，0就是硬跟

    private Transform camTransform; // 真正要摆的相机（挂在子物体上也能用）
    private Vector3 followVelocity;

    private void Awake()
    {
        SetCameraTransform();
    }

    /// <summary>
    /// 找到真正带Camera的物体：脚本可能挂在相机的父物体上
    /// </summary>
    private void SetCameraTransform()
    {
        Camera cam = GetComponent<Camera>();

        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }

        camTransform = cam != null ? cam.transform : transform;
    }

    private void LateUpdate()
    {
        // 玩家没拖就自己找（包含未激活的物体，切换世界时也能找到）
        if (player2D == null)
        {
            PlayerController2D controller = FindFirstObjectByType<PlayerController2D>(FindObjectsInactive.Include);
            player2D = controller != null ? controller.gameObject : null;
        }

        if (player2D == null || camTransform == null)
        {
            return;
        }

        Vector3 target = new Vector3(player2D.transform.position.x, player2D.transform.position.y + offsetY, camTransform.position.z);
        camTransform.position = smoothTime <= 0f
            ? target
            : Vector3.SmoothDamp(camTransform.position, target, ref followVelocity, smoothTime);
    }
}