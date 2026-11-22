// PlayerInteractor.cs
// 挂在玩家身上：找最近的 Interactable3D，按 E 触发。
// 不依赖 UI，想显示提示就订阅 OnTargetChanged。

using System;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float radius = 2.2f;
    public LayerMask mask = ~0;
    [Tooltip("优先选择正前方 120° 内的目标")]
    public float forwardBias = 0.35f;

    public event Action<Interactable3D> OnTargetChanged;

    public Interactable3D Current { get; private set; }

    private readonly Collider[] buffer = new Collider[24];

    private void Update()
    {
        Interactable3D best = FindBest();
        if (best != Current)
        {
            Current = best;
            OnTargetChanged?.Invoke(Current);
        }

        if (Current != null && JamInput.InteractDown) Current.Interact();
    }

    private Interactable3D FindBest()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, mask, QueryTriggerInteraction.Collide);
        Interactable3D best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Interactable3D candidate = buffer[i].GetComponentInParent<Interactable3D>();
            if (candidate == null || !candidate.CanInteract) continue;

            Vector3 to = candidate.transform.position - transform.position;
            float score = to.magnitude - Vector3.Dot(transform.forward, to.normalized) * forwardBias;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }
}
