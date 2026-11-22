using UnityEngine;
//这个放在player身上
public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("Pickup radius around the player.")]
    [SerializeField] private float pickupRange = 2f;

    private bool is3D;

    private void Awake()
    {
        switch (gameObject.name)
        {
            case "Player2D":
                is3D = false;
                break;

            case "Player3D":
                is3D = true;
                break;

            default:
                enabled = false;
                break;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (is3D)
        {
            TryPickup(Physics.OverlapSphere(
                transform.position,
                pickupRange,
                ~0,
                QueryTriggerInteraction.Collide));
        }
        else
        {
            TryPickup(Physics2D.OverlapCircleAll(
                transform.position,
                pickupRange,
                ~0));
        }
    }

//看附件有无Item
    private void TryPickup(Component[] hits)
    {
        PickupItem nearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (Component hit in hits)
        {
            PickupItem item = hit.GetComponentInParent<PickupItem>();

            if (item == null)
            {
                continue;
            }

            float distance = is3D
                ? (hit.transform.position - transform.position).sqrMagnitude
                : ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestItem = item;
            }
        }

        nearestItem?.Pickup(gameObject);
    }
}
