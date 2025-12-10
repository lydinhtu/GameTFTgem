using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;

    // Unit đang đứng trên ô
    public Unit currentUnit;

    // Unit đã "giữ chỗ", đang chuẩn bị bước vào ô này
    public Unit reservedUnit;

    // Để đổi màu ô
    public Renderer rend;

    private void Awake()
    {
        if (rend == null)
        {
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

    /// <summary>
    /// Ô này có cho Unit u đặt chỗ/bước vào không?
    /// </summary>
    public bool CanReserve(Unit u)
    {
        // Trống hoàn toàn
        if (currentUnit == null && reservedUnit == null) return true;

        // Chính nó đang đứng/đã giữ chỗ thì vẫn coi là hợp lệ
        if (currentUnit == u || reservedUnit == u) return true;

        // Có thằng khác rồi → không cho
        return false;
    }
}
