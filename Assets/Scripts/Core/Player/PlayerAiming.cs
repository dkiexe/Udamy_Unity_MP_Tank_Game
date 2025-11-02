using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private Transform turretTransform;
    [SerializeField] private InputReader inputReader;
    
    private void LateUpdate()
    {
       if (!IsOwner) return;
        Vector2 aimScreenPosition = inputReader.AimPosition;
        Vector3 aimWorldPosition = Camera.main.ScreenToWorldPoint(aimScreenPosition); // converts the screen point to a world point

        turretTransform.up = new Vector2
            (
                aimWorldPosition.x - turretTransform.position.x,
                aimWorldPosition.y - turretTransform.position.y
            );
    }
}
