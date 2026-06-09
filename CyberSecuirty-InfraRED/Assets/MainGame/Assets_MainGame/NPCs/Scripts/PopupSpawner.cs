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
        if (rt == null) return;

        // Force popup to use centered anchors so anchoredPosition is predictable
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Calculate bounds so the whole popup stays inside spawnArea
        Vector2 areaSize = spawnArea.rect.size;
        Vector2 popupSize = rt.rect.size;

        float halfW = areaSize.x * 0.5f;
        float halfH = areaSize.y * 0.5f;

        float px = popupSize.x * 0.5f;
        float py = popupSize.y * 0.5f;

        float minX = -halfW + px;
        float maxX =  halfW - px;
        float minY = -halfH + py;
        float maxY =  halfH - py;

        // If popup is larger than the area in one axis, just center it on that axis
        float x = (minX > maxX) ? 0f : Random.Range(minX, maxX);
        float y = (minY > maxY) ? 0f : Random.Range(minY, maxY);

        rt.anchoredPosition = new Vector2(x, y);
    }
    
}