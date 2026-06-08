using UnityEngine;

public class PlayerVehicle : Vehicle
{
    private Vector2 input;

    protected override void Update()
    {
        base.Update();

        // Only read input if this is the active player
        if (IsActivePlayer())
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }
        else
        {
            input = Vector2.zero;
        }

        // Auto-switch control when near ball
        if (IsNearBall() && gameManager != null)
        {
            gameManager.SetActivePlayer(this);
        }
    }

    void FixedUpdate()
    {
        Move(input);
    }
}