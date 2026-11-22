// TestPlayerMotor.cs
// 只用于“看相机效果”的最小移动脚本：CharacterController + 相机相对移动 + 跳跃。
// 正式项目里请换成你们自己的移动组件，然后把本文件删掉。

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestPlayerMotor : MonoBehaviour, IJumpPadReceiver
{
    public float speed = 5.5f;
    public float jumpHeight = 1.6f;
    public float gravity = -22f;
    public Transform camera3D;
    public float turnSpeed = 14f;

    private CharacterController cc;
    private float verticalVelocity;

    /// <summary>弹起次数（调试/自动化测试用）。</summary>
    public int LaunchCount { get; private set; }
    public float VerticalVelocity { get { return verticalVelocity; } }

    /// <summary>被弹簧板之类的东西弹起。</summary>
    public void Launch(float heightMeters)
    {
        LaunchCount++;
        verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.2f, heightMeters));
    }

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (DimensionTransitionController.IsBusy) return;

        Vector2 input = JamInput.Move;
        bool is2D = DimensionTransitionController.Is2D;
        Vector3 move;

        if (is2D)
        {
            // 2D：A/D 直接映射到 +Z / -Z，屏幕右 = +Z
            move = new Vector3(0f, 0f, input.x);
        }
        else
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (camera3D != null)
            {
                forward = Vector3.ProjectOnPlane(camera3D.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(camera3D.right, Vector3.up).normalized;
            }
            move = forward * input.y + right * input.x;
        }

        if (move.sqrMagnitude > 1f) move.Normalize();

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (cc.isGrounded && JamInput.JumpDown) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        cc.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (!is2D && move.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(move), 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
    }
}
