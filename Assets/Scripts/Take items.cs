using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    [Header("Информация о предмете")]
    public string itemName = "Предмет";
    public bool isHeld = false;      // В руке ли сейчас предмет

    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Rigidbody rb;
    private Collider itemCollider;

    void Start()
    {
        // Сохраняем начальное состояние
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        // Если нет Rigidbody - добавляем (для физики)
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        isHeld = true;

        // Отключаем физику и коллайдер
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (itemCollider != null)
            itemCollider.enabled = false;

        // Сохраняем мировой масштаб перед сменой родителя
        Vector3 worldScale = transform.lossyScale;

        // Крепим к точке у руки
        transform.SetParent(holdPoint);

        // Восстанавливаем мировой масштаб
        transform.localScale = new Vector3(
            worldScale.x / holdPoint.lossyScale.x,
            worldScale.y / holdPoint.lossyScale.y,
            worldScale.z / holdPoint.lossyScale.z
        );

        // Сбрасываем позицию и поворот
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        isHeld = false;

        // Возвращаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (itemCollider != null)
            itemCollider.enabled = true;

        // Открепляем от руки
        transform.SetParent(null);

        // Можно добавить небольшой выброс вперёд
        if (rb != null)
        {
            rb.velocity = Camera.main.transform.forward * 3f + Vector3.up * 2f;
        }
    }

    // Метод для сброса предмета на место (если нужно)
    public void ResetPosition()                                             
    {
        if (!isHeld)
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
            transform.rotation = originalRotation;

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}