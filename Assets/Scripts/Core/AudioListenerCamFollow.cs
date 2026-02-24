using UnityEngine;

public class AudioListenerCamFollow : MonoBehaviour
{
    /// <summary>
    /// In a 2D game if we want to create special sounds we must use 3D sound settings, 
    /// but since the default audio listener component is attactched to the camera the Z value of the
    /// sound listner is in the same Z position of the camera ( -10Z ) this interupts the 3D special sound calculation 
    /// and makes the sound not work as intended, This script is created to follow a position of the main camera, 
    /// so using this script on a gameobject with Z =0, and attacthing a audioListner effectivly solves this problem and allows us to use 3D sound settings in a 2D game.
    /// </summary>
    [SerializeField] private Transform cameraTransform;
    void LateUpdate()
    {
        transform.position = new Vector3(
            cameraTransform.position.x,
            cameraTransform.position.y,
            0f
        );
    }
}
