using UnityEngine;

/// <summary>
/// 怪物类抽象类
/// </summary>
public abstract class Monster : MonoBehaviour, GetHit
{
    protected int health; // 生命值
    protected int damage; // 攻击力
    protected MonsterBlackboard blackboard;
    protected FSM<E_MonsterStateType, MonsterBlackboard> FSM; // 怪物状态机

    // 死亡方法
    public abstract void Die();

    // 受伤方法
    public abstract void TakeDamage(int damage);
}
