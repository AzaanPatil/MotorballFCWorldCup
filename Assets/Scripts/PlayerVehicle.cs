using UnityEngine;

public class PlayerVehicle : Vehicle
{
    private Vector2 input;

    void Update()
    {
        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
        Move(input);
    }
}