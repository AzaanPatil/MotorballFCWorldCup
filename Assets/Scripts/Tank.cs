using UnityEngine;

public class Tank : Vehicle
{
    public Transform turret;

    void Update()
    {
        RotateTurret();
    }

    void FixedUpdate()
    {
        // You can still move like normal vehicle
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Move(input);
    }

    void RotateTurret()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - turret.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        turret.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}   