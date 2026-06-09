using UnityEngine;

public class VehicleAppearance : MonoBehaviour
{
    public SpriteRenderer body;

    public void ApplyColor(Color color)
    {
        if (body != null)
            body.color = color;
    }
}