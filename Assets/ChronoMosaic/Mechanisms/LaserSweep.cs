// LaserSweep.cs
// 扫描激光眼：3D 里来回扫，被大理石柱挡住；2D 里完全关闭（过去没有激光）。
// 伤害走 tag + SendMessage，不绑定你们的血量脚本实现。

using UnityEngine;

public class LaserSweep : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Present3D;
    public Transform emitter;
    public float sweepAngle = 70f;
    public float sweepSpeed = 28f;
    public float range = 30f;
    public LayerMask blockerMask = ~0;
    public string playerTag = "Player";

    [Header("伤害")]
    public int damage = 1;
    public float damageCooldown = 0.8f;
    public string damageMessage = "TakeHit";

    [Header("表现")]
    public LineRenderer laserLine;
    public Light laserLight;

    private float phase;
    private float nextDamageTime;
    private Quaternion baseRotation;

    private void Awake()
    {
        if (emitter == null) emitter = transform;
        baseRotation = emitter.localRotation;
        if (laserLine != null) laserLine.useWorldSpace = true;
    }

    private void Update()
    {
        bool active = DimensionRules.ActiveIn(activeIn);
        if (laserLine != null) laserLine.enabled = active;
        if (laserLight != null) laserLight.enabled = active;
        if (emitter == null || !active) return;

        phase += Time.deltaTime;
        float angle = Mathf.Sin(phase * sweepSpeed * Mathf.Deg2Rad) * (sweepAngle * 0.5f);
        emitter.localRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);

        RaycastHit hit;
        Vector3 end = emitter.position + emitter.forward * range;
        if (Physics.Raycast(emitter.position, emitter.forward, out hit, range, blockerMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            if (hit.collider.CompareTag(playerTag) && Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + damageCooldown;
                hit.collider.SendMessage(damageMessage, damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.SetPosition(0, emitter.position);
            laserLine.SetPosition(1, end);
        }
    }
}
