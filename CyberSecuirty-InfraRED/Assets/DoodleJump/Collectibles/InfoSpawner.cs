// CircleSpawner_WaitsForFinish.cs
using UnityEngine;

public class InfoSpawner : MonoBehaviour
{
    public PlatformSpawner spawner; // assign
    public Transform player;
    public GameObject circlePrefab;

    public int count = 5;
    public float minX = -6f;
    public float maxX = 6f;
    public float fixedZ = 0f;
    public float minAbovePlayer = 10f;
    public float marginBelowFinish = 8f;

    bool spawned;

    void Update()
    {
        if (spawned) return;
        if (!spawner || !player || !circlePrefab) return;
        if (!spawner.finishTransform) return;

        spawned = true;

        float startY = player.position.y + minAbovePlayer;
        float endY = spawner.finishTransform.position.y - marginBelowFinish;
        if (endY <= startY) endY = startY + 20f;

        float step = (endY - startY) / Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            float y = startY + step * (i + 0.5f) + Random.Range(-step * 0.25f, step * 0.25f);
            float x = Random.Range(minX, maxX);

            var go = Instantiate(circlePrefab, new Vector3(x, y, fixedZ), Quaternion.identity, transform);
            var cc = go.GetComponent<InfoCollectible>();
            if (cc) cc.id = i + 1; // 1..5
        }
    }
}