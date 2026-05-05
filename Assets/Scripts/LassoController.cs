using UnityEngine;

public class Lasso : MonoBehaviour
{
    [Header("Настройки лассо")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private LayerMask grabbableLayers;

    [Header("Руки персонажа")]
    [SerializeField] private Transform lassoHand;
    [SerializeField] private Transform itemHand;
    [SerializeField] private float handSwapDistance = 0.5f;
    [SerializeField] private float swapSpeed = 5f;

    [Header("Визуализация")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private GameObject lassoVisual;

    private Camera playerCamera;
    private GameObject currentGrabbedObject;
    private Rigidbody grabbedRigidbody;
    private bool isPulling = false;
    private bool isSwapping = false;
    private bool isHolding = false;
    private Vector3 originalScale;
    private Vector3 swapStartPosition;
    private float swapProgress = 0f;
    public static bool isInHandKey = false;

    void Start()
    {
        playerCamera = Camera.main;

        SetupHands();

        if (lineRenderer != null)
        {
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
            lineRenderer.enabled = false;
        }

        if (lassoVisual != null)
        {
            lassoVisual.transform.parent = lassoHand;
            lassoVisual.transform.localPosition = Vector3.zero;
            lassoVisual.transform.localRotation = Quaternion.identity;
        }
    }

    void SetupHands()
    {
        if (lassoHand == null)
        {
            GameObject leftHand = new GameObject("LassoHand");
            leftHand.transform.parent = playerCamera.transform;
            leftHand.transform.localPosition = new Vector3(-0.3f, -0.2f, 0.5f);
            lassoHand = leftHand.transform;
        }

        if (itemHand == null)
        {
            GameObject rightHand = new GameObject("ItemHand");
            rightHand.transform.parent = playerCamera.transform;
            rightHand.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            itemHand = rightHand.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isPulling && !isSwapping && !isHolding)
        {
            TryLasso();
        }

        if (Input.GetKeyDown(KeyCode.Q) && isHolding)
        {
            DropFromHand();
        }

        if (isPulling && currentGrabbedObject != null && !isSwapping)
        {
            PullObject();
        }

        if (isSwapping && currentGrabbedObject != null)
        {
            SwapObjectToRightHand();
        }

        if (isHolding && currentGrabbedObject != null)
        {
            HoldObject();
        }

        if (Input.GetKeyUp(KeyCode.Q) && isPulling && !isSwapping)
        {
            ReleaseObject();
        }

        DrawLassoVisual();
    }

    void TryLasso()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grabbableLayers))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                InteractableItem item = hit.collider.GetComponent<InteractableItem>();
                if (item != null && item.isHeldByPlayer)
                {
                    Debug.Log("Предмет уже в руке (обычный пикап), лассо не работает");
                    return;
                }

                currentGrabbedObject = hit.collider.gameObject;
                grabbedRigidbody = rb;
                isPulling = true;

                originalScale = currentGrabbedObject.transform.localScale;

                if (lineRenderer != null)
                    lineRenderer.enabled = true;

                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.freezeRotation = true;
                grabbedRigidbody.velocity = Vector3.zero;

                if (item != null)
                    item.BlockPickup();

                Debug.Log($"Лассо захватило: {currentGrabbedObject.name}");
            }
        }
    }

    void PullObject()
    {
        if (currentGrabbedObject == null)
        {
            ReleaseObject();
            return;
        }

        currentGrabbedObject.transform.localScale = originalScale;

        Vector3 direction = lassoHand.position - currentGrabbedObject.transform.position;
        float distance = direction.magnitude;

        if (distance < handSwapDistance)
        {
            StartSwapToRightHand();
            return;
        }

        Vector3 newPosition = Vector3.MoveTowards(
            currentGrabbedObject.transform.position,
            lassoHand.position,
            pullSpeed * Time.deltaTime
        );

        grabbedRigidbody.MovePosition(newPosition);
    }

    void StartSwapToRightHand()
    {
        Debug.Log("Предмет достиг лассо! Перекладываем в правую руку...");

        isPulling = false;
        isSwapping = true;
        swapProgress = 0f;

        swapStartPosition = currentGrabbedObject.transform.position;

        grabbedRigidbody.isKinematic = true;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    void SwapObjectToRightHand()
    {
        if (currentGrabbedObject == null)
        {
            isSwapping = false;
            return;
        }

        swapProgress += Time.deltaTime * swapSpeed;

        Vector3 targetPosition = itemHand.position;
        Vector3 newPosition = Vector3.Lerp(swapStartPosition, targetPosition, swapProgress);
        Quaternion targetRotation = itemHand.rotation;
        Quaternion newRotation = Quaternion.Lerp(currentGrabbedObject.transform.rotation, targetRotation, swapProgress);

        currentGrabbedObject.transform.position = newPosition;
        currentGrabbedObject.transform.rotation = newRotation;

        if (swapProgress >= 1f)
        {
            FinishSwapToRightHand();
        }
    }

    void FinishSwapToRightHand()
    {
        Debug.Log("Предмет переложен в правую руку!");

        isSwapping = false;
        isHolding = true;

        currentGrabbedObject.transform.parent = itemHand;
        currentGrabbedObject.transform.localPosition = Vector3.zero;
        currentGrabbedObject.transform.localRotation = Quaternion.identity;

        grabbedRigidbody.isKinematic = true;
        grabbedRigidbody.useGravity = false;

        InteractableItem item = currentGrabbedObject.GetComponent<InteractableItem>();
        if (item != null)
        {
            item.isHeldByLasso = true;
        }
    }

    void HoldObject()
    {
        if (currentGrabbedObject == null)
        {
            DropFromHand();
            return;
        }

        currentGrabbedObject.transform.localPosition = Vector3.Lerp(
            currentGrabbedObject.transform.localPosition,
            Vector3.zero,
            Time.deltaTime * 10f
        );

        // ДОБАВЛЕННАЯ СТРОКА:
        // Проверяем, является ли предмет батарейкой/ключом
        if (currentGrabbedObject != null && currentGrabbedObject.name == "Battery")
        {
            isInHandKey = true;
        }

        currentGrabbedObject.transform.localRotation = Quaternion.Lerp(
            currentGrabbedObject.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 10f
        );
    }

    // НОВЫЙ МЕТОД: принудительно очищает ключ из рук
    // В скрипте Lasso, обновите метод ClearKey:
    public void ClearKey()
    {
        Debug.Log("Принудительная очистка ключа из рук");

        if (currentGrabbedObject != null)
        {
            // Отвязываем от руки
            currentGrabbedObject.transform.parent = null;

            // Отключаем компоненты
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.isKinematic = true;
                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.velocity = Vector3.zero;
                grabbedRigidbody.angularVelocity = Vector3.zero;
            }

            // Очищаем ссылки
            currentGrabbedObject = null;
            grabbedRigidbody = null;
        }

        isHolding = false;
        isSwapping = false;
        isPulling = false;
        isInHandKey = false;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
    public void DropFromHand()
    {
        Debug.Log("Предмет выпущен из правой руки (лассо)");

        if (currentGrabbedObject != null)
        {
            InteractableItem item = currentGrabbedObject.GetComponent<InteractableItem>();
            if (item != null)
            {
                item.isHeldByLasso = false;
                item.UnblockPickup();
            }

            currentGrabbedObject.transform.parent = null;

            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.isKinematic = false;
                grabbedRigidbody.useGravity = true;
                grabbedRigidbody.freezeRotation = false;
                grabbedRigidbody.velocity = Vector3.zero;

                if (Camera.main != null)
                {
                    grabbedRigidbody.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 2f, ForceMode.Impulse);
                }
            }

            if (currentGrabbedObject.name == "Battery")
            {
                isInHandKey = false;
            }
        }

        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isHolding = false;
    }

    void ReleaseObject()
    {
        if (currentGrabbedObject != null)
        {
            InteractableItem item = currentGrabbedObject.GetComponent<InteractableItem>();
            if (item != null)
            {
                item.UnblockPickup();
            }
        }

        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.useGravity = true;
            grabbedRigidbody.freezeRotation = false;
            grabbedRigidbody.velocity = Vector3.zero;
        }

        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isPulling = false;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    void DrawLassoVisual()
    {
        if (isPulling && lineRenderer != null && currentGrabbedObject != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lassoHand.position);
            lineRenderer.SetPosition(1, currentGrabbedObject.transform.position);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        if (isPulling && currentGrabbedObject != null)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Лассо тянет: {currentGrabbedObject.name}", style);
        }
        else if (isSwapping)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Перекладываем предмет...", style);
        }
        else if (isHolding && currentGrabbedObject != null)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Предмет в правой руке (Q - бросить)", style);
        }

        GUI.Label(new Rect(10, 50, 450, 20), "Q - лассо | G - бросить (обычный пикап)", style);
    }

    void OnDrawGizmos()
    {
        if (lassoHand != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lassoHand.position, handSwapDistance);
        }

        if (itemHand != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(itemHand.position, 0.2f);
        }
    }
}