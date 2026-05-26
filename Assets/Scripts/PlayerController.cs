using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Добавляем для работы с UI

public class PlayerController : MonoBehaviour
{
    private int speed = 3;
    private int sspeed = 30;
    private int runSpeed = 6;
    public float vertical;
    public float horizontal;

    public Camera playerCamera; // Перетащите камеру в это поле в инспекторе
    private float xRotation = 0f;

    // Система выносливости
    private float stamina = 100f; // Текущая выносливость
    private float maxStamina = 100f;
    private float staminaDrain = 10f; // Трата выносливости в секунду
    private float staminaRecoveryDelay = 1f; // Задержка перед восстановлением
    private float staminaRecoveryRate = 14.28f; // Восстановление в секунду
    private float recoveryTimer = 0f; // Таймер задержки восстановления
    private bool isRunning = false;

    // UI элемент для отображения выносливости
    public Slider staminaSlider; // Перетащите сюда Slider из инспектора

    void Start()
    {
        // Если камера не назначена, пытаемся найти дочернюю камеру
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // Настраиваем слайдер выносливости
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.minValue = 0;
            staminaSlider.value = stamina;
        }
        else
        {
            Debug.LogWarning("Stamina Slider не назначен в инспекторе!");
        }

        // Блокируем курсор в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        float mousehorizontal = Input.GetAxis("Mouse X");
        float mousevertical = Input.GetAxis("Mouse Y");

        // Проверка нажатия Shift для ускорения
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Можно бежать только если есть выносливость и игрок хочет бежать и двигается
        isRunning = wantsToRun && stamina > 0 && (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        float currentSpeed = isRunning ? runSpeed : speed;

        // Обработка выносливости
        if (isRunning)
        {
            // Тратим выносливость во время бега
            stamina -= staminaDrain * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);

            // Сбрасываем таймер восстановления, так как мы бежим
            recoveryTimer = staminaRecoveryDelay;
        }
        else
        {
            // Если не бежим, управляем восстановлением
            if (recoveryTimer > 0)
            {
                // Уменьшаем таймер задержки
                recoveryTimer -= Time.deltaTime;
            }
            else if (stamina < maxStamina)
            {
                // После задержки восстанавливаем выносливость
                stamina += staminaRecoveryRate * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0f, maxStamina);
            }
        }

        // Обновляем отображение слайдера
        if (staminaSlider != null)
        {
            staminaSlider.value = stamina;
        }

        // Получаем направление взгляда персонажа
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Обнуляем вертикальную составляющую (чтобы не летать)
        forward.y = 0;
        right.y = 0;

        // Нормализуем векторы, чтобы диагональное движение не было быстрее
        forward.Normalize();
        right.Normalize();

        // Движение относительно направления взгляда, но только по горизонтали
        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
        transform.Translate(moveDirection * Time.deltaTime * currentSpeed, Space.World);

        // ГОРИЗОНТАЛЬНЫЙ ПОВОРОТ (вращаем весь персонаж)
        transform.Rotate(Vector3.up * Time.deltaTime * sspeed * mousehorizontal);

        // ВЕРТИКАЛЬНЫЙ ПОВОРОТ (вращаем только камеру)
        xRotation -= mousevertical * Time.deltaTime * sspeed;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ограничиваем угол обзора
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // Метод для получения процента выносливости
    public float GetStaminaPercentage()
    {
        return stamina / maxStamina;
    }

    public float GetCurrentStamina()
    {
        return stamina;
    }
}