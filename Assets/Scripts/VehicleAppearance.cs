using UnityEngine;

public class VehicleAppearance : MonoBehaviour
{
    public SpriteRenderer body;

    public void SetColor(Color color)
    {
        body.color = color;
    }
}