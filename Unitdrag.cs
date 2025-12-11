using UnityEngine;

public class UnitDrag : MonoBehaviour
{
    private Camera cam;
    private bool isDragging = false;
    private Unit unit;
    private Vector3 offset;
    private Tile oldTile;

    private void Start()
    {
        cam = Camera.main;
        unit = GetComponent<Unit>();
    }

    private void OnMouseDown()
    {
        if (unit == null) return;

        // ❌ KHÔNG CHO KÉO QUÁI (ENEMY) BAO GIỜ
        if (unit.team == Team.Enemy)
        {
            Debug.Log("Enemy unit: chỉ xem, không được kéo.");
            return;
        }

        // ❌ ĐANG TRONG TRẬN THÌ KHÔNG CHO KÉO (kể cả tướng mình)
        if (BattleManager.Instance != null && BattleManager.Instance.isBattleActive)
        {
            Debug.Log("Battle is active -> cannot drag unit.");
            return;
        }

        isDragging = true;
        oldTile = unit.currentTile;

        // tính độ lệch giữa điểm click và vị trí tướng
        offset = transform.position - GetMouseWorldPos();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
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

        // Nếu không thả được vào ô hợp lệ → trả lại vị trí cũ
        unit.SetTile(oldTile);
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = GetMouseWorldPos();
            transform.position = mouseWorld + offset + Vector3.up * 0.5f;
        }
    }

    private Vector3 GetMouseWorldPos()
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
