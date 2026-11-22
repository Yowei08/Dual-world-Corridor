using UnityEngine;
public class PickupItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private string itemName = "Health Potion";

    /// 被player拾取的时候触发下面的
    public void Pickup(GameObject picker)
    {
        Debug.Log($"[Pickup] {picker.name} picked up {itemName}.");
        Destroy(gameObject);
    }
}
