using UnityEngine;

public class ArrowWobbleAnim : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float wobbleMagnitude = 0.15f;
    [SerializeField] private float wobbleSpeed = 2f;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        // Generating a value between -1 and 1 using a sin wave (Mathf.Sin()) and modifiy it by the following :
        // (Time.time * wobbleSpeed) to control the speed of the sine wave.
        // (* wobbleMagnitude ) scales the sine wave output to control how much the arrow wobbles.
        float yOffset = Mathf.Sin(Time.time * wobbleSpeed) * wobbleMagnitude;
        transform.localPosition = initialPosition + new Vector3(0f, yOffset, 0f);
    }
}