using UnityEngine;

public class UnitDrag : MonoBehaviour
{
    private Camera cam;
    private bool isDragging = false;
    private Unit unit;
    private Vector3 offset;
    private Tile oldTile;

    void Start()
    {
        cam = Camera.main;
        unit = GetComponent<Unit>();
    }

    void OnMouseDown()
    {
        if (unit == null) return;

        isDragging = true;
        oldTile = unit.currentTile;

        // tính độ lệch giữa điểm click và vị trí tướng
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Raycast tìm Tile bên dưới
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                // nếu tile trống thì đặt tướng lên
                if (tile.currentUnit == null)
                {
                    unit.SetTile(tile);
                    return;
                }
            }
        }

        // Nếu tới đây nghĩa là không thả vào ô hợp lệ → trả lại vị trí cũ
        unit.SetTile(oldTile);
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = GetMouseWorldPos();
            transform.position = mouseWorld + offset + Vector3.up * 0.5f;
        }
    }

    Vector3 GetMouseWorldPos()
    {
        var plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
}
