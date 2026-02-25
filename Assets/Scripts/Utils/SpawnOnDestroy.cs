using UnityEngine;

public class SpwanOnDestroy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject prefab;

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
