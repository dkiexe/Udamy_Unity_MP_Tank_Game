using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject prefab;

    private void OnDestroy()
    {
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
