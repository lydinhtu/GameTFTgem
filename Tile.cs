using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;

    public Unit currentUnit;

    // renderer để đổi màu ô (gắn vào child có mesh)
    public Renderer rend;

    private void Awake()
    {
        if (rend == null)
        {
            // tự tìm renderer trong con (Visual, Cube, Plane...)
            rend = GetComponentInChildren<Renderer>();
        }
    }

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void SetColor(Color c)
    {
        if (rend != null)
        {
            rend.material.color = c;
        }
    }

    public bool IsEmpty()
    {
        return currentUnit == null;
    }
}
