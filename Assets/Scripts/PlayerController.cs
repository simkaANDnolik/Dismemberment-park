using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private int speed = 3;
    private int sspeed = 30;
    public float vertical;
    public float horizontal;

    public Camera playerCamera; // Перетащите камеру в это поле в инспекторе
    private float xRotation = 0f;

    void Start()
    {
        // Если камера не назначена, пытаемся найти дочернюю камеру
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // Блокируем курсор в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        float mousehorizontal = Input.GetAxis("Mouse X");
        float mousevertical = Input.GetAxis("Mouse Y");

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
        transform.Translate(moveDirection * Time.deltaTime * speed, Space.World);

        // ГОРИЗОНТАЛЬНЫЙ ПОВОРОТ (вращаем весь персонаж)
        transform.Rotate(Vector3.up * Time.deltaTime * sspeed * mousehorizontal);

        // ВЕРТИКАЛЬНЫЙ ПОВОРОТ (вращаем только камеру)
        xRotation -= mousevertical * Time.deltaTime * sspeed;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ограничиваем угол обзора
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
