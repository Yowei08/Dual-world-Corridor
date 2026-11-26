using UnityEngine;

/// <summary>
/// 怪物类抽象类
/// </summary>
public abstract class Monster : MonoBehaviour, GetHit
{
    protected int health; // 生命值
    protected int damage; // 攻击力
    protected FSM<E_MonsterStateType, MonsterBlackboard> monsterFSM; // 怪物状态机
    protected MonsterBlackboard monsterBlackboard;

    // 死亡方法
    public abstract void Die();

    // 受伤方法
    public abstract void TakeDamage(int damage);

    private void Start()
    {
        monsterBlackboard = new MonsterBlackboard();
        monsterFSM = new FSM<E_MonsterStateType, MonsterBlackboard>(monsterBlackboard);
    }
}
