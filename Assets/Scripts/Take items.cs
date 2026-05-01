using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    [Header("Информация о предмете")]
    public string itemName = "Предмет";
    public bool isHeldByPlayer = false;
    public bool isHeldByLasso = false;

    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Rigidbody rb;
    private Collider itemCollider;

    private bool isPickupBlocked = false;

    void Start()
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        if (isHeldByLasso || isPickupBlocked) return;
        if (isHeldByPlayer) return;

        isHeldByPlayer = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (itemCollider != null)
            itemCollider.enabled = false;

        // Сохраняем МИРОВОЙ масштаб до смены родителя
        Vector3 worldScale = transform.lossyScale;

        transform.SetParent(holdPoint, false);

        // Пересчитываем локальный масштаб, чтобы сохранить мировой
        transform.localScale = new Vector3(
            worldScale.x / holdPoint.lossyScale.x,
            worldScale.y / holdPoint.lossyScale.y,
            worldScale.z / holdPoint.lossyScale.z
        );

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Дополнительная страховка - принудительно замораживаем масштаб через 1 кадр
        StartCoroutine(FreezeScale());

        Debug.Log($"Обычный пикап: {itemName}");
    }

    // Добавьте этот метод в конец класса InteractableItem
    private System.Collections.IEnumerator FreezeScale()
    {
        Vector3 fixedScale = transform.localScale;
        yield return null; // ждем один кадр
        transform.localScale = fixedScale;
    }

    public void Drop()
    {
        if (!isHeldByPlayer) return;

        isHeldByPlayer = false;

        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;

        transform.SetParent(null, true);
        transform.position = worldPosition;
        transform.rotation = worldRotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;

            if (Camera.main != null)
            {
                rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 2f, ForceMode.Impulse);
            }
        }

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    public void BlockPickup()
    {
        isPickupBlocked = true;
    }

    public void UnblockPickup()
    {
        isPickupBlocked = false;
    }

    public bool IsPickupBlocked()
    {
        return isPickupBlocked;
    }

    public void ResetPosition()
    {
        if (!isHeldByPlayer && !isHeldByLasso)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
            }

            if (itemCollider != null)
                itemCollider.enabled = true;
        }
    }

    public void UpdateOriginalTransform()
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
    }
}