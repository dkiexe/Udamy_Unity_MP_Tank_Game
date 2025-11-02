using UnityEngine;

public class LifeTime : MonoBehaviour
{
    [SerializeField] private float AliveTime = 2f;

    private void Start()
    {
        Destroy(gameObject, AliveTime);
    }
}
