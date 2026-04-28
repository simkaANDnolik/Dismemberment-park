using UnityEngine;

public class Lasso : MonoBehaviour
{
    [Header("Настройки лассо")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private LayerMask grabbableLayers;

    [Header("Руки персонажа")]
    [SerializeField] private Transform lassoHand;      // Левая рука (с лассо)
    [SerializeField] private Transform itemHand;       // Правая рука (для предметов)
    [SerializeField] private float handSwapDistance = 0.5f; // Дистанция для перекладывания
    [SerializeField] private float swapSpeed = 5f;     // Скорость перекладывания

    [Header("Визуализация")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private GameObject lassoVisual;    // Визуал лассо в левой руке

    private Camera playerCamera;
    public static GameObject currentGrabbedObject;
    private Rigidbody grabbedRigidbody;
    private bool isPulling = false;
    private bool isSwapping = false;    // Идёт перекладывание?
    private bool isHolding = false;      // Предмет в правой руке?
    private Vector3 originalScale;
    private Vector3 swapStartPosition;
    private float swapProgress = 0f;
    public  static bool isInHandKey = false;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
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
        // Левая рука (с лассо)
        if (lassoHand == null)
        {
            GameObject leftHand = new GameObject("LassoHand");
            leftHand.transform.parent = playerCamera.transform;
            leftHand.transform.localPosition = new Vector3(-0.3f, -0.2f, 0.5f);
            lassoHand = leftHand.transform;
        }

        // Правая рука (для предметов)
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
        // Бросаем лассо
        if (Input.GetKeyDown(KeyCode.Q) && !isPulling && !isSwapping && !isHolding)
        {
            TryLasso();
        }

        // Отпускаем предмет из правой руки
        if (Input.GetKeyDown(KeyCode.Q) && isHolding)
        {
            DropFromHand();
        }

        // Притягивание предмета
        if (isPulling && currentGrabbedObject != null && !isSwapping)
        {
            PullObject();
        }

        // Перекладывание из левой руки в правую
        if (isSwapping && currentGrabbedObject != null)
        {
            SwapObjectToRightHand();
        }

        // Удержание в правой руке
        if (isHolding && currentGrabbedObject != null)
        {
            HoldObject();
        }

        // Отмена притягивания
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

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green, 1f);

        if (Physics.Raycast(ray, out hit, maxDistance, grabbableLayers))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                currentGrabbedObject = hit.collider.gameObject;
                grabbedRigidbody = rb;
                isPulling = true;

                originalScale = currentGrabbedObject.transform.localScale;

                if (lineRenderer != null)
                    lineRenderer.enabled = true;

                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.freezeRotation = true;
                grabbedRigidbody.velocity = Vector3.zero;

                Debug.Log($"Лассо захватило: {currentGrabbedObject.name} (тянем к левой руке)");
            }
            else
            {
                Debug.Log("Объект не имеет Rigidbody!");
            }
        }
        else
        {
            Debug.Log("Лассо никого не зацепило");
        }
    }

    void PullObject()
    {
        if (currentGrabbedObject == null)
        {
            ReleaseObject();
            return;
        }

        // Сохраняем масштаб
        currentGrabbedObject.transform.localScale = originalScale;

        // ПРИТЯГИВАЕМ к ЛЕВОЙ руке (где лассо)
        Vector3 direction = lassoHand.position - currentGrabbedObject.transform.position;
        float distance = direction.magnitude;

        // Если предмет подлетел к левой руке - начинаем перекладывание в правую
        if (distance < handSwapDistance)
        {
            StartSwapToRightHand();

            return;
        }

        // Движем объект к левой руке
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

        // Запоминаем начальную позицию (левая рука)
        swapStartPosition = currentGrabbedObject.transform.position;

        // Отключаем физику во время перекладывания
        grabbedRigidbody.isKinematic = true;

        // Выключаем визуализацию лассо
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

        // Увеличиваем прогресс перекладывания
        swapProgress += Time.deltaTime * swapSpeed;

        // Плавно перемещаем предмет из левой руки в правую
        Vector3 targetPosition = itemHand.position;
        Vector3 newPosition = Vector3.Lerp(swapStartPosition, targetPosition, swapProgress);

        // Плавный поворот
        Quaternion targetRotation = itemHand.rotation;
        Quaternion newRotation = Quaternion.Lerp(
            currentGrabbedObject.transform.rotation,
            targetRotation,
            swapProgress
        );

        currentGrabbedObject.transform.position = newPosition;
        currentGrabbedObject.transform.rotation = newRotation;

        // Если перекладывание закончено
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

        // Предмет становится дочерним правой руки
        currentGrabbedObject.transform.parent = itemHand;
        currentGrabbedObject.transform.localPosition = Vector3.zero;
        currentGrabbedObject.transform.localRotation = Quaternion.identity;

        // Настройки физики для удержания
        grabbedRigidbody.isKinematic = true;
        grabbedRigidbody.useGravity = false;
    }

    void HoldObject()
    {
        if (currentGrabbedObject == null)
        {
            DropFromHand();
            return;
        }

        // Плавное удержание в правой руке
        currentGrabbedObject.transform.localPosition = Vector3.Lerp(
            currentGrabbedObject.transform.localPosition,
            Vector3.zero,
            Time.deltaTime * 10f
        );
        if (currentGrabbedObject.name == "Battery")
        {
            isInHandKey = true;
            Debug.Log(isInHandKey);
        }
        currentGrabbedObject.transform.localRotation = Quaternion.Lerp(
            currentGrabbedObject.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 10f
        );
    }

    void DropFromHand()
    {
        Debug.Log("Предмет выпущен из правой руки");

        if (currentGrabbedObject != null)
        {
            currentGrabbedObject.transform.parent = null;

            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.isKinematic = false;
                grabbedRigidbody.useGravity = true;
                grabbedRigidbody.freezeRotation = false;
                grabbedRigidbody.velocity = Vector3.zero;
                if (currentGrabbedObject.name == "Battery")
                {
                    isInHandKey = true;
                    Debug.Log(isInHandKey);
                }
            }
        }

        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isHolding = false;
    }

    void ReleaseObject()
    {
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
        // Рисуем верёвку из левой руки к предмету (только во время притягивания)
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

        if (isPulling)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Лассо тянет: {currentGrabbedObject.name} → левая рука", style);
        }
        else if (isSwapping)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Перекладываем предмет в правую руку...", style);
        }
        else if (isHolding)
        {
            GUI.Label(new Rect(10, 10, 450, 25), $"Предмет в правой руке: {currentGrabbedObject.name} (Q - отпустить)", style);
        }
        else
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance, grabbableLayers))
            {
                GUI.Label(new Rect(10, 10, 450, 25), $"Цель: {hit.collider.name} (Q - лассо + перекладывание)", style);
            }
            else
            {
                GUI.Label(new Rect(10, 10, 450, 25), "Наведитесь на предмет и нажмите Q", style);
            }
        }

        // Визуальная подсказка
        GUI.Label(new Rect(10, 50, 450, 20), "Левая рука: лассо → Правая рука: предмет", style);
    }

    // Визуализация в Scene View
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