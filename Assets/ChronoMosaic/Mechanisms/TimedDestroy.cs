// TimedDestroy.cs
// 简单的到点销毁，带一个回调（残影归还计数用）。

using System;
using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    private float remaining = -1f;
    private Action onGone;
    private bool fired;

    public void Init(float seconds, Action callback)
    {
        remaining = seconds;
        onGone = callback;
        fired = false;
    }

    private void Update()
    {
        if (remaining < 0f) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f && !fired)
        {
            fired = true;
            if (onGone != null) onGone.Invoke();
            Destroy(gameObject);
        }
    }
}
