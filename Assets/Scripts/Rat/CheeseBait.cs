using System.Collections;
using UnityEngine;

public class CheeseBait : MonoBehaviour
{
    public RatEater ratPrefab;

    [Tooltip("Pause before the rat shows up.")]
    public float spawnDelay = 0.5f;

    [Tooltip("Pick a random spawn point instead of the nearest one.")]
    public bool chooseRandomSpawn;

    [Tooltip("Used only if there are no RatSpawnPoints in the scene: the rat appears this far from the cheese.")]
    public Vector3 fallbackSpawnOffset = new(3f, 0f, 0f);

    private IEnumerator Start()
    {
        if (ratPrefab == null)
        {
            Debug.LogError("CheeseBait: 'ratPrefab' isn't assigned.", this);
            yield break;
        }

        yield return new WaitForSeconds(spawnDelay);

        RatSpawnPoint point = chooseRandomSpawn ? RatSpawnPoint.PickRandom() : RatSpawnPoint.Nearest(transform.position);

        Vector3 pos;
        Quaternion rot;
        if (point != null)
        {
            pos = point.transform.position;
            rot = point.transform.rotation;
        }
        else
        {
            Debug.LogWarning("CheeseBait: no RatSpawnPoint in the scene, using the fallback offset.", this);
            pos = transform.position + fallbackSpawnOffset;
            rot = Quaternion.identity;
        }

        RatEater rat = Instantiate(ratPrefab, pos, rot);
        rat.GoEat(gameObject);
    }
}
