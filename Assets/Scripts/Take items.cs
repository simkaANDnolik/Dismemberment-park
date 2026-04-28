using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    [Header("Информация о предмете")]
    public string itemName = "Предмет";
    public bool isHeld = false;      // В руке ли сейчас предмет

    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;    // Добавляем сохранение исходного масштаба
    private Rigidbody rb;
    private Collider itemCollider;

    void Start()
    {
        // Сохраняем начальное состояние
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;  // Сохраняем локальный масштаб

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        // Если нет Rigidbody - добавляем (для физики)
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        if (isHeld) return;  // Предотвращаем повторный подбор

        isHeld = true;

        // Отключаем физику и коллайдер
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (itemCollider != null)
            itemCollider.enabled = false;

        // Сохраняем позицию/поворот в мировых координатах
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;

        // Крепим к точке руки
        transform.SetParent(holdPoint, true);  // true - сохраняет мировое положение

        // Сбрасываем локальные трансформации
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Важно: восстанавливаем исходный локальный масштаб предмета
        transform.localScale = originalScale;

        if (Lasso.currentGrabbedObject != null && Lasso.currentGrabbedObject.name == "Battery")
        {
            Lasso.isInHandKey = true;
            Debug.Log(Lasso.isInHandKey);
        }
    }

    public void Drop()
    {
        if (!isHeld) return;  // Предотвращаем повторный бросок

        isHeld = false;

        // Сохраняем мировую позицию перед откреплением
        Vector3 worldPosition = transform.position;

        // Открепляем от руки, сохраняя мировую позицию
        transform.SetParent(null, true);

        // Возвращаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // Добавляем небольшую задержку перед применением скорости
            StartCoroutine(ApplyDropVelocity());
        }

        if (itemCollider != null)
            itemCollider.enabled = true;

        if (Lasso.currentGrabbedObject != null && Lasso.currentGrabbedObject.name == "Battery")
        {
            Lasso.isInHandKey = false;
            Debug.Log(Lasso.isInHandKey);
        }
    }

    private System.Collections.IEnumerator ApplyDropVelocity()
    {
        // Ждём один кадр, чтобы физика успела инициализироваться
        yield return null;

        if (rb != null && Camera.main != null)
        {
            rb.velocity = Camera.main.transform.forward * 3f + Vector3.up * 2f;
            // Добавляем небольшой случайный момент вращения для реализма
            rb.angularVelocity = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            );
        }
    }

    // Метод для сброса предмета на место (если нужно)
    public void ResetPosition()
    {
        if (!isHeld)
        {
            transform.SetParent(originalParent, false);  // false - не сохранять мировое положение
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;  // Восстанавливаем исходный масштаб

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (itemCollider != null)
                itemCollider.enabled = true;
        }
    }

    // Опционально: метод для принудительного обновления исходной позиции
    public void UpdateOriginalTransform()
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;  // Теперь сохраняем локальную позицию
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
    }
}