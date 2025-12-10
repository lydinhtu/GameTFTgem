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

        // Nếu ô trống thì cho unit đi sang
        if (tile.currentUnit == null)
        {
            selectedUnit.SetTile(tile);
            selectedUnit = null;
        }
        else
        {
            Debug.Log("Tile has unit already!");
        }
    }
}
