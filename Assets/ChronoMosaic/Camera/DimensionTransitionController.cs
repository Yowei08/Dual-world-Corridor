// DimensionTransitionController.cs
// 0.65 s 维度转场总控（策划案 2.1 时间轴 + 2.2 落点规则）。
// 负责：冻结输入 → 遮罩 → 0.25 s 峰值切相机/切层/切吸附 → 0.55 s 交还控制。
//
// 场景里挂一个空物体 DimensionSystem，把相机、光圈、遮罩、玩家拖进来。

using System;
using System.Collections;
using UnityEngine;

public class DimensionTransitionController : MonoBehaviour
{
    public static DimensionTransitionController Instance { get; private set; }

    /// <summary>转场开始（参数为目标状态：true = 2D）。</summary>
    public static event Action<bool> OnTransitionBegin;
    /// <summary>已切到新维度（跨过峰值帧）。</summary>
    public static event Action<bool> OnDimensionChanged;
    /// <summary>切换被拒绝（脚下没有安全落点）。</summary>
    public static event Action OnSwitchDenied;

    public static bool Is2D { get { return Instance != null && Instance.is2D; } }
    public static bool IsBusy { get { return Instance != null && Instance.busy; } }

    [Header("场景引用")]
    public Transform player;
    public Rigidbody playerBody;
    public CharacterController playerCharacter;
    public ThirdPersonCameraRig camera3D;
    public SideViewCamera2D camera2D;
    public SafetyFootRing footRing;
    public ScreenWipe wipe;

    [Header("转场期间禁用的脚本（玩家移动、攻击、AI 根节点）")]
    public Behaviour[] freezeDuringTransition;

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip switchClip;
    public AudioClip denyClip;

    [Header("时间轴（秒）")]
    public float totalDuration = 0.65f;
    [Tooltip("峰值帧：真正换相机")]
    public float swapAt = 0.25f;
    [Tooltip("材质从 3D 外观过渡到 2D 外观的起止")]
    public float blendStart = 0.14f;
    public float blendEnd = 0.36f;
    [Tooltip("交还控制权")]
    public float releaseAt = 0.55f;

    [Header("剔除层（名字必须与 Unity Layer 一致）")]
    public bool autoBuildCullingMasks = true;
    public string[] layer3D = { "Present3D_Only", "RightWall_3D", "Shared_Both" };
    public string[] layer2D = { "Past2D_Only", "Shared_Both" };

    [Header("输入")]
    public bool acceptInput = true;
    public bool startIn2D = false;

    [Header("状态（只读）")]
    public bool is2D;
    public bool busy;
    [Range(0f, 1f)] public float blend;

    private Camera cam3;
    private Camera cam2;
    private int mask3;
    private int mask2;
    private RigidbodyConstraints constraints3D;
    private RigidbodyConstraints constraints2D;
    private bool bodyWasKinematic;
    private float lockedX;
    private bool frozen;

    private static readonly int DimBlendId = Shader.PropertyToID("_DimBlend");

    private void Awake()
    {
        Instance = this;
        cam3 = camera3D != null ? camera3D.GetComponent<Camera>() : null;
        cam2 = camera2D != null ? camera2D.GetComponent<Camera>() : null;

        mask3 = cam3 != null ? cam3.cullingMask : ~0;
        mask2 = cam2 != null ? cam2.cullingMask : ~0;
        if (autoBuildCullingMasks)
        {
            int m3 = LayerMask.GetMask(layer3D);
            int m2 = LayerMask.GetMask(layer2D);
            if (m3 != 0) mask3 = m3;
            if (m2 != 0) mask2 = m2;
        }

        if (playerBody != null)
        {
            bodyWasKinematic = playerBody.isKinematic;
            constraints3D = playerBody.constraints;
            constraints2D = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX;
        }

        is2D = startIn2D;
        ApplyDimensionImmediate(is2D);
        Shader.SetGlobalFloat(DimBlendId, is2D ? 1f : 0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!acceptInput || busy) return;
        if (JamInput.SwitchDown) RequestToggle();
    }

    /// <summary>2D 模式下锁住 X（对 CharacterController 也有效）。</summary>
    private void LateUpdate()
    {
        if (!is2D || busy || player == null || playerBody != null) return;
        Vector3 p = player.position;
        if (Mathf.Abs(p.x - lockedX) > 0.0005f)
        {
            p.x = lockedX;
            player.position = p;
        }
    }

    public void RequestToggle()
    {
        if (busy) return;
        RequestSwitch(!is2D);
    }

    public void RequestSwitch(bool to2D)
    {
        if (busy || to2D == is2D) return;
        if (!CanSwitchTo(to2D))
        {
            Deny();
            return;
        }
        StartCoroutine(SwitchRoutine(to2D));
    }

    private bool CanSwitchTo(bool to2D)
    {
        if (player == null) return false;
        if (to2D) return IsGrounded();
        return footRing == null || footRing.CanSwitch;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(player.position + Vector3.up * 0.2f, Vector3.down, 0.5f,
            ~0, QueryTriggerInteraction.Ignore);
    }

    private void Deny()
    {
        if (footRing != null) footRing.FlashDenied();
        if (audioSource != null && denyClip != null) audioSource.PlayOneShot(denyClip, 0.7f);
        OnSwitchDenied?.Invoke();
    }

    private IEnumerator SwitchRoutine(bool to2D)
    {
        busy = true;
        OnTransitionBegin?.Invoke(to2D);
        if (audioSource != null && switchClip != null) audioSource.PlayOneShot(switchClip);
        SetFrozen(true);
        if (wipe != null) wipe.Play(totalDuration);
        if (camera3D != null) camera3D.SetLookEnabled(false);

        float t = 0f;
        bool swapped = false;

        while (t < totalDuration)
        {
            t += Time.deltaTime;

            float b = Mathf.InverseLerp(blendStart, blendEnd, t);
            b = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(b));
            blend = to2D ? b : 1f - b;
            Shader.SetGlobalFloat(DimBlendId, blend);

            if (!swapped && t >= swapAt)
            {
                swapped = true;
                ApplySwap(to2D);
            }
            if (t >= releaseAt) SetFrozen(false);

            yield return null;
        }

        if (!swapped) ApplySwap(to2D);
        blend = to2D ? 1f : 0f;
        Shader.SetGlobalFloat(DimBlendId, blend);
        SetFrozen(false);
        if (camera3D != null) camera3D.SetLookEnabled(true);
        busy = false;
    }

    private void ApplySwap(bool to2D)
    {
        if (to2D)
        {
            lockedX = player != null ? player.position.x : 0f;

            if (camera3D != null) camera3D.gameObject.SetActive(false);
            if (camera2D != null)
            {
                camera2D.gameObject.SetActive(true);
                if (cam2 != null) cam2.cullingMask = mask2;
                camera2D.SnapToTarget();
            }
            if (playerBody != null) playerBody.constraints = constraints2D;
        }
        else
        {
            // 关键：在遮罩最不透明的这一帧完成吸附，玩家看不到瞬移
            if (footRing != null && footRing.CanSwitch && player != null)
            {
                Vector3 p = player.position;
                p.x = footRing.SnapX;
                TeleportTo(p);
            }

            if (camera2D != null) camera2D.gameObject.SetActive(false);
            if (camera3D != null)
            {
                camera3D.gameObject.SetActive(true);
                if (cam3 != null) cam3.cullingMask = mask3;
                camera3D.SnapBehind();
            }
            if (playerBody != null) playerBody.constraints = constraints3D;
        }

        is2D = to2D;
        if (playerBody != null && !playerBody.isKinematic)
        {
            Vector3 v = RigidbodyCompat.GetVelocity(playerBody);
            RigidbodyCompat.SetVelocity(playerBody, new Vector3(0f, v.y, 0f));
        }
        OnDimensionChanged?.Invoke(to2D);
    }

    private void TeleportTo(Vector3 position)
    {
        if (playerCharacter != null) playerCharacter.enabled = false;
        player.position = position;
        if (playerBody != null) playerBody.position = position;
        if (playerCharacter != null) playerCharacter.enabled = true;
    }

    private void ApplyDimensionImmediate(bool to2D)
    {
        if (camera3D != null) camera3D.gameObject.SetActive(!to2D);
        if (camera2D != null)
        {
            camera2D.gameObject.SetActive(to2D);
            if (cam2 != null) cam2.cullingMask = mask2;
            if (to2D) camera2D.SnapToTarget();
        }
        if (cam3 != null) cam3.cullingMask = mask3;
        if (player != null) lockedX = player.position.x;
        if (playerBody != null) playerBody.constraints = to2D ? constraints2D : constraints3D;
    }

    private void SetFrozen(bool value)
    {
        if (frozen == value) return;
        frozen = value;

        if (freezeDuringTransition != null)
        {
            for (int i = 0; i < freezeDuringTransition.Length; i++)
            {
                if (freezeDuringTransition[i] != null) freezeDuringTransition[i].enabled = !value;
            }
        }

        if (playerBody != null && !bodyWasKinematic)
        {
            if (value)
            {
                RigidbodyCompat.SetVelocity(playerBody, Vector3.zero);
                playerBody.angularVelocity = Vector3.zero;
                playerBody.isKinematic = true;
            }
            else
            {
                playerBody.isKinematic = false;
            }
        }
    }

    [ContextMenu("切到 2D")] private void DebugTo2D() { RequestSwitch(true); }
    [ContextMenu("切到 3D")] private void DebugTo3D() { RequestSwitch(false); }
}
