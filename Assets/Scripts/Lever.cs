using UnityEngine;
using System.Collections;

public class Lever : MonoBehaviour
{
    [Header("Настройки рычага")]
    [SerializeField] private bool startState = false;
    [SerializeField] private float switchCooldown = 0.5f;
    [SerializeField] private AudioClip switchSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Визуализация")]
    [SerializeField] private Transform leverHandle;
    [SerializeField] private Vector3 activeRotation = new Vector3(0, 0, -45f);
    [SerializeField] private Vector3 inactiveRotation = new Vector3(0, 0, 45f);
    [SerializeField] private float animationSpeed = 5f;

    [Header("Подсветка при наведении")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Renderer leverRenderer;
    private Material originalMaterial;

    [Header("События")]
    [SerializeField] private GameObject[] objectsToToggle;
    [SerializeField] private MonoBehaviour[] scriptsToToggle;

    private bool isActive;
    private bool canInteract = true;
    private AudioSource audioSource;

    // Свойство для проверки состояния рычага
    public bool IsActive => isActive;

    void Start()
    {
        isActive = startState;

        // Настройка AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && switchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Сохраняем оригинальный материал для подсветки
        if (leverRenderer != null && highlightMaterial != null)
        {
            originalMaterial = leverRenderer.material;
        }
    }

    void Update()
    {
        // Анимация плавного поворота ручки
        if (leverHandle != null)
        {
            Vector3 targetRotation = isActive ? activeRotation : inactiveRotation;
            leverHandle.localRotation = Quaternion.Lerp(
                leverHandle.localRotation,
                Quaternion.Euler(targetRotation),
                Time.deltaTime * animationSpeed
            );
        }
    }

    // Метод для переключения рычага (вызывается из лассо или при нажатии)
    public void ToggleLever()
    {
        if (!canInteract) return;

        isActive = !isActive;
        Debug.Log($"Рычаг {gameObject.name} переключен в положение: {(isActive ? "ВКЛ" : "ВЫКЛ")}");

        // Воспроизводим звук
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound, soundVolume);
        }

        // Активируем/деактивируем объекты
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }

        // Включаем/выключаем скрипты
        foreach (MonoBehaviour script in scriptsToToggle)
        {
            if (script != null)
            {
                script.enabled = isActive;
            }
        }

        // Запускаем кулдаун
        StartCoroutine(InteractCooldown());
    }

    // Альтернативный метод для совместимости
    public void Interact()
    {
        ToggleLever();
    }

    private IEnumerator InteractCooldown()
    {
        canInteract = false;
        yield return new WaitForSeconds(switchCooldown);
        canInteract = true;
    }

    // Подсветка при наведении (опционально)
    public void Highlight()
    {
        if (leverRenderer != null && highlightMaterial != null)
        {
            leverRenderer.material = highlightMaterial;
        }
    }

    public void RemoveHighlight()
    {
        if (leverRenderer != null && originalMaterial != null)
        {
            leverRenderer.material = originalMaterial;
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (leverHandle != null)
        {
            Gizmos.color = isActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(leverHandle.position, 0.2f);
        }

        // Рисуем линии к объектам, которые переключает рычаг
        Gizmos.color = Color.yellow;
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                Gizmos.DrawLine(transform.position, obj.transform.position);
            }
        }
    }
}