using UnityEngine;

/// <summary>
/// 3D摄像机：第三人称自由视角，始终跟在3D玩家身后
/// 鼠标滑动直接转视角，上下有角度限制；点左键锁住鼠标，Esc松开
/// </summary>
public class Camera3D : MonoBehaviour
{
    public GameObject player3D; // 跟随目标，不拖就自动找场景里的Player3D
    public float distance = 6f; // 相机离玩家多远
    public float focusHeight = 1.2f; // 观察点比玩家原点高多少
    public float sensitivity = 3f; // 鼠标灵敏度（度/屏幕单位）
    public float minPitch = -25f; // 最低俯仰角（相机能压到多低）
    public float maxPitch = 70f; // 最高俯仰角（相机能抬多高，俯视）

    private float yaw; // 水平角
    private float pitch; // 俯仰角
    private Transform camTransform; // 真正要摆的相机（挂在子物体上也能用）

    private void Awake()
    {
        SetCameraTransform();

        // 开局沿用当前朝向，避免一上来镜头乱跳
        yaw = camTransform.eulerAngles.y;
        pitch = Mathf.Clamp(camTransform.eulerAngles.x, minPitch, maxPitch);
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

    private void Update()
    {
        // 点左键锁住鼠标，不然滑到窗口外面就转不动了；Esc放开
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 鼠标滑动直接转视角，不用按着右键
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;

        // 上下有极限：每帧都夹一次，鼠标再猛也转不出去
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        // 玩家没拖就自己找（包含未激活的物体，切换世界时也能找到）
        if (player3D == null)
        {
            PlayerController3D controller = FindFirstObjectByType<PlayerController3D>(FindObjectsInactive.Include);
            player3D = controller != null ? controller.gameObject : null;
        }

        if (player3D == null || camTransform == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = player3D.transform.position + Vector3.up * focusHeight;
        camTransform.SetPositionAndRotation(focus + rotation * Vector3.back * distance, rotation);
    }
}