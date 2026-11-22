// RigidbodyCompat.cs
// Unity 6 把 Rigidbody.velocity 改名为 linearVelocity，老版本又只有 velocity。
// 统一走这里，避免工程升级时满项目改代码。

using UnityEngine;

public static class RigidbodyCompat
{
    public static Vector3 GetVelocity(Rigidbody body)
    {
        if (body == null) return Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    public static void SetVelocity(Rigidbody body, Vector3 value)
    {
        if (body == null) return;
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }
}
