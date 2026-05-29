using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Настройки монстра")]
    public GameObject monsterPrefab;
    public float spawnDistanceBehind = 2f;
    public float spawnHeight = 0f;

    [Header("Условия появления")]
    public int requiredLeversCount = 2; // Нужно 2 рычага

    [Header("Дополнительно")]
    public bool rotateToPlayer = true;
    public float destroyDelay = 10f;
    public AudioClip spawnSound;

    private GameObject player;
    private bool isSpawned = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            Debug.LogError("Игрок не найден! Убедись, что у персонажа есть тег 'Player'");

        if (monsterPrefab == null)
            Debug.LogError("Префаб монстра не назначен в инспекторе!");
    }

    void Update()
    {
        // Проверяем статическую переменную из другого скрипта
        // ЗАМЕНИ "YourScriptName" НА НАЗВАНИЕ ТВОЕГО СКРИПТА
        if (!isSpawned && Lever.openedLeversCount >= requiredLeversCount)
        {
            monsterPrefab.SetActive(true);
        }
    }

    public void ActivateMonster()
    {
        if (isSpawned) return;

        if (player == null || monsterPrefab == null)
            return;

        SpawnMonster();
        isSpawned = true;
    }

    void SpawnMonster()
    {
        // Вычисляем позицию позади игрока
        Vector3 behindPosition = player.transform.position - player.transform.forward * spawnDistanceBehind;
        behindPosition.y += spawnHeight;

        // Создаем монстра
        GameObject monster = Instantiate(monsterPrefab, behindPosition, Quaternion.identity);

        // Поворачиваем монстра лицом к игроку
        if (rotateToPlayer)
        {
            Vector3 directionToPlayer = (player.transform.position - monster.transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                monster.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }
        }

        // Проигрываем звук
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, monster.transform.position);
        }

        // Авто-удаление через время
        if (destroyDelay > 0)
        {
            Destroy(monster, destroyDelay);
        }

        Debug.Log($"МОНСТР ПОЯВИЛСЯ! Активировано рычагов: {Lever.openedLeversCount}/{requiredLeversCount}");
    }
}