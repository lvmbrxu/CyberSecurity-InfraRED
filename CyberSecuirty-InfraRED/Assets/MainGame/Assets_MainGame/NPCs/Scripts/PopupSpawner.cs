using System.Collections.Generic;
using UnityEngine;

public class PopupSpawner : MonoBehaviour
{
    public GameObject popupPrefab;
    public RectTransform spawnArea;

    [HideInInspector] public float spawnIntervalSeconds = 1f;
    [HideInInspector] public int maxPopupsOnScreen = 6;

    private float timer;
    private readonly List<GameObject> alive = new();

    private void Start()
    {
        timer = spawnIntervalSeconds;
    }

    private void Update()
    {
        if (popupPrefab == null || spawnArea == null) return;

        alive.RemoveAll(x => x == null);

        if (alive.Count >= maxPopupsOnScreen) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Spawn();
        timer = spawnIntervalSeconds;
    }

    private void Spawn()
    {
        var go = Instantiate(popupPrefab, spawnArea);
        alive.Add(go);

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector2 size = spawnArea.rect.size;
            rt.anchoredPosition = new Vector2(
                Random.Range(-size.x * 0.5f, size.x * 0.5f),
                Random.Range(-size.y * 0.5f, size.y * 0.5f)
            );
        }
    }
}