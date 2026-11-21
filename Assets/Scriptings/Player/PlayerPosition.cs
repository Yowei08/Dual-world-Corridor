using UnityEngine;

// 2D世界为XY平面，3D世界为XYZ空间，XY坐标固定

/// <summary>
/// 存储玩家坐标
/// </summary>
public struct PlayerPosition
{
    public float X;
    public float Y;
    public float Z;

    // 写入XY
    public void WritePosition(float X, float Y)
    {
        this.X = X;
        this.Y = Y;
    }

    // 独立写入Z
    public void WritePosition(float Z)
    {
        this.Z = Z;
    }

    // 获取2D空间坐标
    public Vector2 GetPosition2D()
    {
        return new Vector2(X, Y);
    }

    // 获取3D空间坐标
    public Vector3 GetPosition3D()
    {
        return new Vector3(X, Y, Z);
    }
}

/// <summary>
/// 静态类独立封装玩家坐标，全局统一
/// </summary>
public static class PlayerPositionManage
{
    public static PlayerPosition playerPosition = new PlayerPosition();
}