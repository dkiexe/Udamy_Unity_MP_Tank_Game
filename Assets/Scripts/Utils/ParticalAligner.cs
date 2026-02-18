using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticalAligner : MonoBehaviour
{
    private ParticleSystem.MainModule psMain;

    private void Start()
    {
        psMain = GetComponent<ParticleSystem>().main;
    }

    private void Update()
    {
        /// startRotation is in radians but the transform rotation
        /// is in degrees we convert between them by using "Mathf.Deg2Rad"
        psMain.startRotation = 
            -transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
    }
}
