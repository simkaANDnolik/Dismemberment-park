using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    public float interactRange = 2f;          // Радиус для поиска предметов
    public Transform holdPoint;               // Точка, куда будет крепиться предмет (например, пустой GameObject у руки)
    public KeyCode interactKey = KeyCode.E;   // Клавиша для взятия

    [Header("UI подсказка")]
    public GameObject hintPanel;              // Панель с подсказкой (Text или целый объект)
    public Text hintText;                     // Текст подсказки (если используешь Text)

    private InteractableItem currentItem;     // Предмет, на который смотрим
    private GameObject heldItem = null;        // Предмет в руке

    void Update()
    {
        // Поиск ближайшего интерактивного предмета в радиусе
        FindNearestInteractable();

        // Показываем подсказку, если есть предмет рядом
        if (currentItem != null)
        {
            ShowHint($"E - взять {currentItem.itemName}");

            // Если нажали E и предмет ещё не в руке
            if (Input.GetKeyDown(interactKey) && heldItem == null)
            {
                PickUpItem(currentItem);
            }
        }
        else
        {
            HideHint();
        }

        // Доп. логика: выбросить предмет (нажми G)
        if (Input.GetKeyDown(KeyCode.G) && heldItem != null)
        {
            DropItem();
        }
    }

    void FindNearestInteractable()
    {
        // Находим все предметы с компонентом InteractableItem
        InteractableItem[] items = FindObjectsOfType<InteractableItem>();
        InteractableItem nearest = null;
        float minDistance = interactRange;

        foreach (InteractableItem item in items)
        {
            // Если предмет уже в руке - пропускаем (не показываем подсказку)
            if (item.isHeld) continue;

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = item;
            }
        }

        currentItem = nearest;
    }

    void PickUpItem(InteractableItem item)
    {
        if (item == null || item.isHeld) return;

        // Запоминаем, что предмет взят
        item.PickUp(holdPoint);
        heldItem = item.gameObject;
        currentItem = null; // Скрываем подсказку сразу
        HideHint();

        Debug.Log($"Взят предмет: {item.itemName}");
    }

    void DropItem()
    {
        if (heldItem == null) return;

        InteractableItem item = heldItem.GetComponent<InteractableItem>();
        if (item != null)
        {
            item.Drop();
            heldItem = null;
        }
    }

    void ShowHint(string message)
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            if (hintText != null)
                hintText.text = message;
        }
    }

    void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    // Рисуем в редакторе радиус для наглядности
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
