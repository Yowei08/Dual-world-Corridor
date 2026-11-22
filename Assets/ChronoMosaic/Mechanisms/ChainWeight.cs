// ChainWeight.cs
// 悬吊配重：被刀砍断铁链后自由落体，砸中下方踏板。
// 注意：策划案里“仅 3D 可挥砍”和“2D 检修廊砍链”是冲突的，
// 这里用 cuttableIn 参数化，等你和策划拍板后改一个枚举就行。

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class ChainWeight : MonoBehaviour, ISwordHittable
{
    [Header("规则")]
    public DimensionMode cuttableIn = DimensionMode.Both;
    public int hitsToCut = 1;

    [Header("引用")]
    public Rigidbody body;
    public Transform chainVisual;
    [Tooltip("用关节吊着的话拖进来，砍断时断开")]
    public ConfigurableJoint joint;

    [Header("表现")]
    public float cutImpulse = 0.5f;
    public AudioSource audioSource;
    public AudioClip cutClip;
    public UnityEvent onCut;

    public bool IsCut { get; private set; }

    private int remainingHits;

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody>();
        remainingHits = Mathf.Max(1, hitsToCut);
        if (body != null) body.isKinematic = true;
    }

    public void OnSwordHit(int damage, Vector3 point)
    {
        if (IsCut || !DimensionRules.ActiveIn(cuttableIn)) return;

        remainingHits -= Mathf.Max(1, damage);
        if (remainingHits > 0) return;

        Cut(point);
    }

    private void Cut(Vector3 point)
    {
        IsCut = true;
        if (chainVisual != null) chainVisual.gameObject.SetActive(false);
        if (joint != null) joint.connectedBody = null;
        if (body != null)
        {
            body.isKinematic = false;
            body.AddForceAtPosition(Vector3.down * cutImpulse, point, ForceMode.VelocityChange);
        }
        if (audioSource != null && cutClip != null) audioSource.PlayOneShot(cutClip);
        if (onCut != null) onCut.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsCut) return;
        // 落地即视为“砸中”，交给踏板自己判重量
        if (body != null) body.isKinematic = true;
    }
}
