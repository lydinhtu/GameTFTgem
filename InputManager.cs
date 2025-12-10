using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private Unit selectedUnit;

    void Awake()
    {
        Instance = this;
    }

    public void OnUnitClicked(Unit unit)
    {
        selectedUnit = unit;
        Debug.Log("Selected unit: " + unit.unitName);
    }

    public void OnTileClicked(Tile tile)
    {
        if (selectedUnit == null) return;

        // Nếu ô trống và có thể đi được thì yêu cầu pathfinding
        if (tile.IsWalkable && !tile.IsOccupied)
        {
            selectedUnit.RequestMove(tile);
            selectedUnit = null;
        }
        else
        {
            Debug.Log("Tile không thể đi tới hoặc đã có unit!");
        }
    }
}
