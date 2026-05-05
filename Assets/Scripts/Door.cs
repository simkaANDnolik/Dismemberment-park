using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isPlayerInCollider = false;
    private bool isMoving = false;
    private bool isAnimating = false;

    public GameObject door1;
    public Transform start;
    public Transform end;
    private float doorSpeed = 2f;

    public GameObject keyObject;
    public Transform insertPoint;
    private float keyAnimationSpeed = 3f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float animationProgress = 0f;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Space"))
        {
            isPlayerInCollider = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Space"))
        {
            isPlayerInCollider = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && isPlayerInCollider && Lasso.isInHandKey && !isAnimating && !isMoving)
        {
            Debug.Log("=== НАЧАЛО АКТИВАЦИИ ДВЕРИ ===");
            StartKeyInsertAnimation();
        }

        if (isAnimating)
        {
            UpdateKeyInsertAnimation();
        }

        if (isMoving)
        {
            door1.transform.position = Vector3.MoveTowards(
                door1.transform.position,
                end.position,
                doorSpeed * Time.deltaTime
            );

            if (Vector3.Distance(door1.transform.position, end.position) < 0.01f)
            {
                isMoving = false;
                Debug.Log("Дверь открыта!");
            }
        }
    }

    void StartKeyInsertAnimation()
    {
        if (keyObject == null || insertPoint == null)
        {
            Debug.LogError("KeyObject или InsertPoint не назначены!");
            return;
        }

        // 1. Очищаем ключ из рук через Lasso
        Lasso lasso = FindObjectOfType<Lasso>();
        if (lasso != null)
        {
            lasso.ClearKey();
            Debug.Log("Ключ очищен из Lasso");
        }

        // 2. Обновляем статическую переменную
        Lasso.isInHandKey = false;

        // 3. Сохраняем начальную и конечную позицию/поворот
        startPosition = keyObject.transform.position;
        startRotation = keyObject.transform.rotation;
        targetPosition = insertPoint.position;
        targetRotation = insertPoint.rotation;

        // 4. Отключаем физику ключа
        Rigidbody rb = keyObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 5. Отключаем коллайдер
        Collider col = keyObject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 6. Отвязываем от родителя (если есть)
        keyObject.transform.SetParent(null);

        // 7. Запускаем анимацию
        isAnimating = true;
        animationProgress = 0f;

        Debug.Log($"Начинаем движение ключа от {startPosition} к {targetPosition}");
    }

    void UpdateKeyInsertAnimation()
    {
        // Увеличиваем прогресс анимации
        animationProgress += Time.deltaTime * keyAnimationSpeed;

        if (animationProgress >= 1f)
        {
            // Анимация завершена
            keyObject.transform.position = targetPosition;
            keyObject.transform.rotation = targetRotation;
            FinishKeyInsert();
        }
        else
        {
            // Плавное движение
            keyObject.transform.position = Vector3.Lerp(startPosition, targetPosition, animationProgress);
            keyObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, animationProgress);
        }
    }

    void FinishKeyInsert()
    {
        isAnimating = false;

        // Фиксируем ключ в конечной позиции
        keyObject.transform.position = insertPoint.position;
        keyObject.transform.rotation = insertPoint.rotation;

        // Ещё раз убеждаемся, что ключ заморожен
        Rigidbody rb = keyObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
        }

        // Отключаем коллайдер
        Collider col = keyObject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Делаем ключ дочерним объектом отсека (чтобы он оставался на месте)
        keyObject.transform.SetParent(insertPoint);
        keyObject.transform.localPosition = Vector3.zero;
        keyObject.transform.localRotation = Quaternion.identity;

        Debug.Log("КЛЮЧ ЗАКРЕПЛЕН В ОТСЕКЕ!");

        // Активируем движение двери
        isMoving = true;
        Debug.Log("Дверь открывается!");
    }
}