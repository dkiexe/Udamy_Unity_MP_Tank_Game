using UnityEngine;

public class SpwanOnDestroy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject prefab;

    private void OnDestroy()
    {
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
