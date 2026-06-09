using UnityEngine;

public class PlayerVehicle : Vehicle
{
    private Vector2 input;

    protected override void Update()
    {
        base.Update();

        // Only accept player input when this object is the currently active player.
        if (IsActivePlayer())
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }
        else
        {
            input = Vector2.zero;
        }

        // Automatically switch to this player when the ball is within the detection radius.
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