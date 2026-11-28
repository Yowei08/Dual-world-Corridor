using UnityEngine;

public class MonsterBlackboard : Blackboard
{
    // 怪物进入战斗
    public bool start;
    // 怪物攻击间隔
    public float CD;
    // 攻击是否结束
    public bool attackEnd;
    // 怪物自己
    public GameObject monster2D;
    public GameObject monster3D;
    // 玩家坐标
    public Transform player2DTransform;
    public Transform player3DTransform;
}
