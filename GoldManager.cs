using TMPro;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    public int gold = 10;
    public TextMeshProUGUI goldText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;

        gold -= amount;
        UpdateGoldUI();
        return true;
    }

    void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = "Gold: " + gold.ToString();
    }
}
