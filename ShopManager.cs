using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("POOL TƯỚNG")]
    [Tooltip("Danh sách tất cả tướng có thể xuất hiện trong shop")]
    public UnitData[] unitPool;          // danh sách UnitData

    [Tooltip("Prefab tương ứng với từng UnitData. Cùng length với unitPool.")]
    public GameObject[] unitPrefabs;     // prefab tướng (Mage, Warri o...)

    [Tooltip("Nếu phần tử trong unitPrefabs bị trống sẽ dùng prefab này")]
    public GameObject defaultUnitPrefab; // prefab mặc định

    [Header("SHOP UI")]
    [Tooltip("Các Button đại diện cho 5 ô shop")]
    public Button[] shopButtons;         // 5 ô shop

    [Tooltip("Tên tướng trên từng ô shop")]
    public TMP_Text[] nameTexts;         // 5 text tên

    [Tooltip("Giá vàng trên từng ô shop")]
    public TMP_Text[] costTexts;         // 5 text giá

    [Header("Giá roll")]
    public int rollCost = 2;

    // index của tướng đang nằm trong từng ô shop, -1 = trống
    private int[] currentUnitIndices;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (shopButtons == null || shopButtons.Length == 0)
        {
            Debug.LogError("ShopManager: Chưa gán shopButtons trong Inspector!");
            return;
        }

        currentUnitIndices = new int[shopButtons.Length];
        for (int i = 0; i < currentUnitIndices.Length; i++)
            currentUnitIndices[i] = -1;

        // Roll lần đầu miễn phí khi vào game
        RollShopFree();
    }

    /// <summary>
    /// Roll shop KHÔNG tốn vàng (dùng cho lần đầu vào game).
    /// </summary>
    public void RollShopFree()
    {
        RollInternal();
    }

    /// <summary>
    /// OnClick của nút ROLL: trừ vàng rồi roll lại shop.
    /// </summary>
    public void RollShopPaid()
    {
        if (GoldManager.Instance == null)
        {
            Debug.LogError("GoldManager.Instance = null, không roll được!");
            return;
        }

        bool paid = GoldManager.Instance.SpendGold(rollCost);
        if (!paid)
        {
            Debug.Log("Không đủ vàng để roll! Cần " + rollCost);
            return;
        }

        RollInternal();
    }

    /// <summary>
    /// Hàm roll thật sự (random tướng cho từng ô).
    /// </summary>
    private void RollInternal()
    {
        if (unitPool == null || unitPool.Length == 0)
        {
            Debug.LogWarning("ShopManager: unitPool rỗng, không có tướng nào để roll!");
            return;
        }

        for (int i = 0; i < shopButtons.Length; i++)
        {
            int randomIndex = Random.Range(0, unitPool.Length);
            currentUnitIndices[i] = randomIndex;

            UnitData data = unitPool[randomIndex];

            // Cập nhật UI
            if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
                nameTexts[i].text = data != null ? data.unitName : "???";

            if (costTexts != null && i < costTexts.Length && costTexts[i] != null)
                costTexts[i].text = data != null ? data.cost.ToString() : "?";

            if (shopButtons[i] != null)
                shopButtons[i].interactable = true;
        }

        Debug.Log("Roll shop xong.");
    }

    /// <summary>
    /// Mua tướng ở ô shop có index = slotIndex.
    /// (Hàm này sẽ được gọi từ OnClick() của từng Button trong Inspector)
    /// </summary>
    public void BuyUnit(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentUnitIndices.Length)
            return;

        int unitIndex = currentUnitIndices[slotIndex];
        if (unitIndex < 0)
        {
            Debug.Log("Ô shop " + slotIndex + " đang trống, không mua được.");
            return;
        }

        if (GoldManager.Instance == null)
        {
            Debug.LogError("GoldManager.Instance = null, không mua được!");
            return;
        }

        if (BenchManager.Instance == null)
        {
            Debug.LogError("BenchManager.Instance = null, không mua được!");
            return;
        }

        if (!BenchManager.Instance.HasFreeSlot())
        {
            Debug.Log("Bench đã full, không mua được!");
            return;
        }

        UnitData data = unitPool[unitIndex];
        int cost = (data != null) ? data.cost : 3; // default 3 vàng nếu thiếu data

        bool paid = GoldManager.Instance.SpendGold(cost);
        if (!paid)
        {
            Debug.Log("Không đủ vàng để mua! Cần " + cost);
            return;
        }

        // Chọn prefab tương ứng với UnitData
        GameObject prefabToSpawn = defaultUnitPrefab;
        if (unitPrefabs != null && unitIndex < unitPrefabs.Length && unitPrefabs[unitIndex] != null)
        {
            prefabToSpawn = unitPrefabs[unitIndex];
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("ShopManager: prefabToSpawn bị null, không spawn được tướng!");
            return;
        }

        // Spawn tướng xuống bench
        BenchManager.Instance.SpawnUnitToBench(prefabToSpawn);

        // Sau khi mua, clear ô shop đó
        ClearSlot(slotIndex);
    }

    /// <summary>
    /// Xoá thông tin 1 ô shop sau khi đã mua tướng.
    /// </summary>
    private void ClearSlot(int i)
    {
        currentUnitIndices[i] = -1;

        if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
            nameTexts[i].text = "";

        if (costTexts != null && i < costTexts.Length && costTexts[i] != null)
            costTexts[i].text = "";

        if (shopButtons != null && i < shopButtons.Length && shopButtons[i] != null)
            shopButtons[i].interactable = false;
    }
}
