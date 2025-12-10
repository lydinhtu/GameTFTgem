using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Cấu hình shop")]
    public GameObject unitPrefab;   // prefab tướng
    public Button[] shopButtons;    // 5 ô shop
    public int unitCost = 3;        // giá mỗi tướng

    [Header("UI")]
    public TMP_Text[] costTexts;    // text hiển thị giá trên từng ô (nếu có)

    void Start()
    {
        // gắn event cho từng nút
        for (int i = 0; i < shopButtons.Length; i++)
        {
            int index = i;
            shopButtons[i].onClick.AddListener(() =>
            {
                BuyUnit(index);
            });

            // nếu có cost text thì gán luôn
            if (costTexts != null && i < costTexts.Length && costTexts[i] != null)
            {
                costTexts[i].text = unitCost.ToString();
            }
        }
    }

    void BuyUnit(int index)
    {
        Debug.Log("Bấm mua ở ô shop " + index);

        if (GoldManager.Instance == null)
        {
            Debug.LogError("GoldManager chưa có trong scene!");
            return;
        }

        if (BenchManager.Instance == null)
        {
            Debug.LogError("BenchManager.Instance không tồn tại!");
            return;
        }

        // kiểm tra còn ô trống không
        if (!BenchManager.Instance.HasFreeSlot())
        {
            Debug.Log("Bench đã full, không mua được!");
            return;
        }

        // thử trừ vàng, nếu không đủ thì SpendGold sẽ trả về false
        bool paid = GoldManager.Instance.SpendGold(unitCost);
        if (!paid)
        {
            Debug.Log("Không đủ vàng để mua! Cần " + unitCost);
            return;
        }

        // đủ vàng + bench còn slot → spawn tướng xuống bench
        BenchManager.Instance.SpawnUnitToBench(unitPrefab);
    }
}
