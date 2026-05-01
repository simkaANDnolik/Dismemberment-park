using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    public float interactRange = 2f;
    public Transform holdPoint;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI подсказка")]
    public GameObject hintPanel;
    public Text hintText;

    private InteractableItem currentItem;
    private GameObject heldItem = null;

    void Update()
    {
        FindNearestInteractable();

        if (currentItem != null)
        {
            ShowHint($"E - взять {currentItem.itemName}");

            if (Input.GetKeyDown(interactKey) && heldItem == null)
            {
                PickUpItem(currentItem);
            }
        }
        else
        {
            HideHint();
        }

        if (Input.GetKeyDown(KeyCode.G) && heldItem != null)
        {
            DropItem();
        }
    }

    void FindNearestInteractable()
    {
        InteractableItem[] items = FindObjectsOfType<InteractableItem>();
        InteractableItem nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (InteractableItem item in items)
        {
            if (item == null) continue;
            if (item.isHeldByPlayer) continue;
            if (item.isHeldByLasso) continue;
            if (item.IsPickupBlocked()) continue;

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < minDistance && dist <= interactRange)
            {
                minDistance = dist;
                nearest = item;
            }
        }

        currentItem = nearest;
    }

    void PickUpItem(InteractableItem item)
    {
        if (item == null) return;
        if (item.isHeldByPlayer) return;
        if (item.isHeldByLasso) return;

        item.PickUp(holdPoint);
        heldItem = item.gameObject;
        currentItem = null;
        HideHint();

        Debug.Log($"Взят предмет (E): {item.itemName}");
    }

    void DropItem()
    {
        if (heldItem == null) return;

        InteractableItem item = heldItem.GetComponent<InteractableItem>();
        if (item != null && item.isHeldByPlayer)
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}