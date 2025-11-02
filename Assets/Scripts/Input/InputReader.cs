using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerControls;

// This allows the creation of a Input Reader Asset creation.
[CreateAssetMenu(fileName = "New Input Reader", menuName = "Inputs/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event Action<bool> PrimaryFireEvent;
    public event Action<Vector2> MoveEvent;
    
    public Vector2 AimPosition { get; private set; }


    private PlayerControls controls;
    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Player.SetCallbacks(this); // assigning this objects functions as callback
        }
        controls.Player.Enable(); // turn on player control reading
    }
    private void OnDisable()
    {
        if (controls.Player.enabled) controls.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimPosition = context.ReadValue<Vector2>();
    }

    public void OnPrimaryFire(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }
}
