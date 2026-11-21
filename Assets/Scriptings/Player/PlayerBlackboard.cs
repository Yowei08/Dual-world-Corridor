using UnityEngine;

/// <summary>
/// 用于存储Player的共享数据
/// </summary>
public class PlayerBlackboard : Blackboard
{
    // 共享组件
    public GameObject player2D;
    public GameObject player3D;
    // 玩家数值
    public float moveSpeed = 0;// 玩家当前移动速度
    public float maxSpeed = 5.5f; // 玩家移动速度
    public float acceleration = 0.2f; // 加速度
}
