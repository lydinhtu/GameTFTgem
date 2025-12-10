using UnityEngine;

public class Tile : MonoBehaviour
{
    // toạ độ ô trên board (dùng nếu cần)
    public int x;
    public int y;

    // con tướng đang đứng trên ô
    public Unit currentUnit;

    // renderer để đổi màu ô
    public Renderer rend;

    void Awake()
    {
        // nếu chưa gán sẵn thì tự tìm Renderer trong con (ví dụ TileVisual)
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }
    }

    // hàm khởi tạo, BoardManager sẽ gọi
    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // đổi màu ô (dùng cho bàn cờ sáng / tối)
    public void SetColor(Color c)
    {
        if (rend != null)
        {
            rend.material.color = c;
        }
    }
}
