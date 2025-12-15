using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using static Controls;

[CreateAssetMenu(fileName = "new Input Reader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    // IPlayerActions is an INTERFACE that requires you to implement OnMove(), OnPrimaryFire(), etc.

    // For these events, other methods can 'subscribe' to such events
    // Ex: in a testscript (which has InputReader as an SO), in the Start() -> inputReader.MoveEvent += HandleMove -> and the parameter: HandleMove(Vector2 movement) where movement is the value invoked
    // You can also unsubscribe (usually for when u destroy the gameObject of this testscript) -> inputReader.MoveEvent -= HandleMove;
    public event Action<bool> primaryFireEvent; // event = something that is triggered and otehr code can listen for it, Action<bool> = the action = bool -> passed thru the event (Ex: true is start shooting)

    public event Action<UnityEngine.Vector2> moveEvent;
    private Controls controls; // Controls = class from UnityEngine.InputSystem

    public Vector2 aimPosition { get; private set; } // just means that we can get it anywhere but only set it in this script
    private void OnEnable() // it's basically like Start() but for SO
    {
        if (controls == null)
        {
            controls = new Controls();
            controls.Player.SetCallbacks(this); // tells the input system to send all player input events to this class
        }

        controls.Player.Enable(); // turns on the input listening for the player action map 

    }

    private void OnDisable()
    {
        controls?.Player.Disable(); // ?. = if (controls != null) -> controls.Player.Disable()
    }
    

    public void OnMove(InputAction.CallbackContext context)
    {
        moveEvent?.Invoke(context.ReadValue<UnityEngine.Vector2>());
    }

    public void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            primaryFireEvent?.Invoke(true); // ?. is if there is no listener subscribed for primaryFireEvent -> Will NOT Invoke(true)
        }
        else if (context.canceled)
        {
            primaryFireEvent?.Invoke(false);
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        aimPosition = context.ReadValue<Vector2>();
    }
}
