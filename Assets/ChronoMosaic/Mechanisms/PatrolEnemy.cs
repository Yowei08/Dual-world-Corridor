// PatrolEnemy.cs
// 【静滞巡卫】唯一小怪：巡逻 → 发现玩家 → 红光预警 0.8 s → 直线冲撞。
// 2D 模式下自动定格为雕塑（不移动、不攻击），这是“跨维绕背”的前提。
// 正面盾变体只是 hasShield = true，正面吃到伤害会弹刀。

using UnityEngine;
using UnityEngine.Events;

public class PatrolEnemy : MonoBehaviour, ISwordHittable
{
    [Header("维度")]
    public DimensionMode activeIn = DimensionMode.Present3D;

    [Header("巡逻")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 1.2f;

    [Header("战斗")]
    public int maxHp = 2;
    public bool hasShield = false;
    public float detectRadius = 7f;
    public float telegraphTime = 0.8f;
    public float chargeSpeed = 5.5f;
    public float chargeDuration = 1.1f;
    public float recoverTime = 0.9f;
    public int contactDamage = 1;

    [Header("表现")]
    public Transform bodyVisual;
    public Renderer coreRenderer;
    public Color coreIdle = new Color(0.45f, 0.1f, 0.1f);
    public Color coreAlert = new Color(1f, 0.15f, 0.1f);
    public float spinSpeed = 60f;

    [Header("事件")]
    public UnityEvent onDeath;
    public UnityEvent onBlocked;

    private int hp;
    private float stateTimer;
    private float nextContactDamageTime;
    private int state; // 0 巡逻 1 预警 2 冲锋 3 硬直
    private bool goingToB = true;
    private Transform player;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        hp = maxHp;
        mpb = new MaterialPropertyBlock();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        SetCore(coreIdle);
    }

    private void Update()
    {
        bool active = DimensionRules.ActiveIn(activeIn) && !DimensionRules.Busy;

        if (bodyVisual != null)
        {
            bodyVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }

        if (!active)
        {
            // 2D：定格成雕塑
            state = 0;
            SetCore(coreIdle);
            return;
        }

        if (player == null) return;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        switch (state)
        {
            case 0:
                Patrol();
                if (toPlayer.magnitude <= detectRadius)
                {
                    state = 1;
                    stateTimer = 0f;
                    SetCore(coreAlert);
                }
                break;

            case 1:
                stateTimer += Time.deltaTime;
                FaceDirection(toPlayer);
                if (stateTimer >= telegraphTime)
                {
                    state = 2;
                    stateTimer = 0f;
                }
                break;

            case 2:
                stateTimer += Time.deltaTime;
                transform.position += transform.forward * chargeSpeed * Time.deltaTime;
                if (stateTimer >= chargeDuration)
                {
                    state = 3;
                    stateTimer = 0f;
                }
                break;

            case 3:
                stateTimer += Time.deltaTime;
                if (stateTimer >= recoverTime)
                {
                    state = 0;
                    stateTimer = 0f;
                    SetCore(coreIdle);
                }
                break;
        }
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null) return;
        Transform goal = goingToB ? pointB : pointA;
        Vector3 dir = goal.position - transform.position;
        dir.y = 0f;
        if (dir.magnitude < 0.15f)
        {
            goingToB = !goingToB;
            return;
        }
        transform.position += dir.normalized * patrolSpeed * Time.deltaTime;
        FaceDirection(dir);
    }

    private void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    public void OnSwordHit(int damage, Vector3 point)
    {
        if (hp <= 0) return;

        if (hasShield)
        {
            Vector3 toAttacker = point - transform.position;
            toAttacker.y = 0f;
            if (Vector3.Dot(transform.forward, toAttacker.normalized) > 0.15f)
            {
                if (onBlocked != null) onBlocked.Invoke();
                return;
            }
        }

        hp -= Mathf.Max(1, damage);
        if (hp <= 0) Die();
    }

    private void Die()
    {
        state = 3;
        stateTimer = -999f;
        if (onDeath != null) onDeath.Invoke();
        gameObject.SetActive(false);
    }

    private void SetCore(Color c)
    {
        if (coreRenderer == null) return;
        coreRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        mpb.SetColor(EmissionId, c * 2f);
        coreRenderer.SetPropertyBlock(mpb);
    }

    private void OnTriggerStay(Collider other)
    {
        if (state != 2 || !other.CompareTag("Player")) return;
        if (Time.time < nextContactDamageTime) return;
        nextContactDamageTime = Time.time + 0.8f;
        other.SendMessage("TakeHit", contactDamage, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
