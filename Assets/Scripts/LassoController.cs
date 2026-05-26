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

    [Header("Визуализация лассо")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private Color rayColorActive = Color.red;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Material lineMaterial;

    [Header("Визуальный эффект лассо")]
    [SerializeField] private GameObject lassoVisual;
    [SerializeField] private ParticleSystem lassoParticles;
    [SerializeField] private Light lassoLight;
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private AnimationCurve lightPulseCurve;
    [SerializeField] private float lightPulseSpeed = 2f;

    [Header("Эффект захвата")]
    [SerializeField] private ParticleSystem captureEffect;
    [SerializeField] private AudioClip lassoThrowSound;
    [SerializeField] private AudioClip lassoCaptureSound;
    [SerializeField] private AudioClip lassoPullSound;
    [SerializeField] private float soundVolume = 0.7f;

    [Header("Эффект на объекте")]
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private float highlightPulseSpeed = 2f;

    private Camera playerCamera;
    private GameObject currentGrabbedObject;
    private Rigidbody grabbedRigidbody;
    private bool isPulling = false;
    private bool isSwapping = false;
    private bool isHolding = false;
    private Vector3 originalScale;
    private Vector3 swapStartPosition;
    private float swapProgress = 0f;
    private AudioSource audioSource;
    private Material originalObjectMaterial;
    private Renderer objectRenderer;
    private float lightPulseTimer = 0f;
    private float highlightTimer = 0f;
    private bool isHighlighting = false;

    // Публичные статические переменные для проверки из других скриптов
    public static bool isInHandKey = false;
    public static bool isInHandAdrenaline = false;

    void Start()
    {
        playerCamera = Camera.main;

        // Настройка AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupHands();
        SetupLineRenderer();
        SetupVisualEffects();

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

    void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (lineMaterial != null)
            lineRenderer.material = lineMaterial;
        else
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.enabled = false;
    }

    void SetupVisualEffects()
    {
        // Создаем свет, если его нет
        if (lassoLight == null)
        {
            lassoLight = GetComponentInChildren<Light>();
            if (lassoLight == null)
            {
                GameObject lightObj = new GameObject("LassoLight");
                lightObj.transform.parent = lassoHand;
                lightObj.transform.localPosition = Vector3.zero;
                lassoLight = lightObj.AddComponent<Light>();
                lassoLight.type = LightType.Point;
                lassoLight.range = 3f;
                lassoLight.intensity = 0f;
            }
        }

        // Создаем партиклы, если их нет
        if (lassoParticles == null)
        {
            lassoParticles = GetComponentInChildren<ParticleSystem>();
            if (lassoParticles == null)
            {
                GameObject particlesObj = new GameObject("LassoParticles");
                particlesObj.transform.parent = lassoHand;
                particlesObj.transform.localPosition = Vector3.zero;
                lassoParticles = particlesObj.AddComponent<ParticleSystem>();

                var main = lassoParticles.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 2f;
                main.startSize = 0.1f;
                main.maxParticles = 50;

                var emission = lassoParticles.emission;
                emission.rateOverTime = 20;

                var shape = lassoParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                shape.radius = 0.2f;
            }
        }
    }

    void Update()
    {
        // Обновляем визуальные эффекты
        UpdateVisualEffects();

        // Подсветка потенциальной цели
        if (!isPulling && !isSwapping && !isHolding)
        {
            HighlightTarget();
        }
        else
        {
            RemoveTargetHighlight();
        }

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

    void UpdateVisualEffects()
    {
        // Пульсирующий свет
        if (lassoLight != null)
        {
            lightPulseTimer += Time.deltaTime * lightPulseSpeed;
            float pulseValue = lightPulseCurve.Evaluate(lightPulseTimer);

            if (isPulling || isHolding)
            {
                lassoLight.intensity = lightIntensity * pulseValue;
            }
            else
            {
                lassoLight.intensity = Mathf.Lerp(lassoLight.intensity, 0, Time.deltaTime * 5f);
            }
        }

        // Эффект партиклов
        if (lassoParticles != null)
        {
            if (isPulling || isHolding)
            {
                if (!lassoParticles.isPlaying)
                    lassoParticles.Play();
            }
            else
            {
                if (lassoParticles.isPlaying)
                    lassoParticles.Stop();
            }
        }
    }

    void HighlightTarget()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grabbableLayers))
        {
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            if (hitRenderer != null && !isHighlighting)
            {
                RemoveTargetHighlight();

                if (highlightedMaterial != null)
                {
                    originalObjectMaterial = hitRenderer.material;
                    hitRenderer.material = highlightedMaterial;
                    isHighlighting = true;
                    objectRenderer = hitRenderer;
                }
            }

            // Обновляем пульсацию подсветки
            if (isHighlighting && objectRenderer != null && highlightedMaterial != null)
            {
                highlightTimer += Time.deltaTime * highlightPulseSpeed;
                float pulse = (Mathf.Sin(highlightTimer) + 1f) / 2f;
                highlightedMaterial.SetFloat("_GlowStrength", pulse);
            }
        }
        else
        {
            RemoveTargetHighlight();
        }
    }

    void RemoveTargetHighlight()
    {
        if (isHighlighting && objectRenderer != null && originalObjectMaterial != null)
        {
            objectRenderer.material = originalObjectMaterial;
            isHighlighting = false;
            objectRenderer = null;
        }
    }

    void TryLasso()
    {
        if (playerCamera == null) return;

        // Воспроизводим звук броска лассо
        PlaySound(lassoThrowSound);

        // Эффект при броске
        if (captureEffect != null)
        {
            Instantiate(captureEffect, lassoHand.position, Quaternion.identity);
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grabbableLayers))
        {
            // Проверка на рычаг
            Lever lever = hit.collider.GetComponent<Lever>();
            if (lever != null)
            {
                lever.ToggleLever();
                Debug.Log($"Лассо переключило рычаг: {lever.name}");

                // Эффект при взаимодействии с рычагом
                if (captureEffect != null)
                {
                    Instantiate(captureEffect, hit.point, Quaternion.identity);
                }
                PlaySound(lassoCaptureSound);
                return;
            }

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
                {
                    lineRenderer.enabled = true;
                    lineRenderer.startColor = rayColorActive;
                    lineRenderer.endColor = rayColorActive;
                }

                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.freezeRotation = true;
                grabbedRigidbody.velocity = Vector3.zero;

                if (item != null)
                    item.BlockPickup();

                // Эффект захвата
                if (captureEffect != null)
                {
                    Instantiate(captureEffect, hit.point, Quaternion.identity);
                }
                PlaySound(lassoCaptureSound);

                Debug.Log($"Лассо захватило: {currentGrabbedObject.name}");
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
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

        // Воспроизводим звук натяжения
        if (distance > handSwapDistance && !audioSource.isPlaying && lassoPullSound != null)
        {
            audioSource.PlayOneShot(lassoPullSound, soundVolume * 0.5f);
        }

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

        // Эффект при перекладывании
        if (captureEffect != null)
        {
            Instantiate(captureEffect, currentGrabbedObject.transform.position, Quaternion.identity);
        }
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

        // Проверка на батарейку/ключ по имени
        if (currentGrabbedObject != null && currentGrabbedObject.name == "Battery")
        {
            isInHandKey = true;
        }
        else
        {
            isInHandKey = false;
        }

        // Проверка на адреналин по тегу
        if (currentGrabbedObject != null && currentGrabbedObject.CompareTag("Adrenaline"))
        {
            isInHandAdrenaline = true;
        }
        else
        {
            isInHandAdrenaline = false;
        }

        currentGrabbedObject.transform.localRotation = Quaternion.Lerp(
            currentGrabbedObject.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 10f
        );
    }

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

        // Сбрасываем оба статических флага
        isInHandKey = false;
        isInHandAdrenaline = false;

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        RemoveTargetHighlight();
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

            // Сбрасываем флаги в зависимости от предмета
            if (currentGrabbedObject.name == "Battery")
            {
                isInHandKey = false;
            }

            if (currentGrabbedObject.CompareTag("Adrenaline"))
            {
                isInHandAdrenaline = false;
            }
        }

        currentGrabbedObject = null;
        grabbedRigidbody = null;
        isHolding = false;

        // Возвращаем обычный цвет линии
        if (lineRenderer != null)
        {
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
        }
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
        {
            lineRenderer.enabled = false;
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
        }

        RemoveTargetHighlight();
    }

    void DrawLassoVisual()
    {
        if (isPulling && lineRenderer != null && currentGrabbedObject != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lassoHand.position);
            lineRenderer.SetPosition(1, currentGrabbedObject.transform.position);

            // Анимируем толщину линии
            float pulse = (Mathf.Sin(Time.time * 10f) + 1f) / 2f;
            float animatedWidth = lineWidth * (0.8f + pulse * 0.4f);
            lineRenderer.startWidth = animatedWidth;
            lineRenderer.endWidth = animatedWidth;
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

        // Рисуем луч для визуализации дальности лассо
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Gizmos.DrawRay(ray.origin, ray.direction * maxDistance);
        }
    }
}