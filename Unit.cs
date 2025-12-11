using System.Collections;
using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class Unit : MonoBehaviour
{
    [Header("Info")]
    public string unitName;
    public Team team;

    // ====== UNIT DATA (config từ ScriptableObject) ======
    [Header("Unit Data (config)")]
    public UnitData data;   // kéo file UnitData vào đây trong Inspector nếu có

    // ====== CHỈ SỐ RUNTIME (dùng trong game) ======
    [Header("Stats (runtime)")]
    public int maxHP;           // máu tối đa
    public int maxMana;         // mana tối đa
    public int attackDamage;    // sát thương vật lý
    public float attackSpeed;   // tốc đánh (chưa dùng thì để dành sau)
    public float armor;         // giáp
    public float magicResist;   // kháng phép

    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMana;

    // ====== Stats cũ để không phá code khác (được sync từ stats runtime) ======
    [Header("Legacy Stats (sync từ runtime)")]
    public int hp = 100;                 // sẽ = maxHP
    public int damage = 10;              // sẽ = attackDamage
    public float attackCooldown = 1f;
    public int attackRangeTiles = 1;     // 1 = melee, >1 = đánh xa
    public float moveSpeed = 4f;         // tốc độ di chuyển

    [Header("Vị trí")]
    public float heightOffsetY = 0.5f;   // nâng model lên khỏi mặt tile
    public Tile currentTile;

    public bool isInBattle = false;

    float lastAttackTime = -999f;

    // Movement state
    bool isMoving = false;
    Tile moveTargetTile = null;
    Vector3 moveStartPos;
    float moveT = 0f;

    void Start()
    {
        // Nếu spawn sẵn trên tile
        if (currentTile != null)
        {
            currentTile.currentUnit = this;
            Vector3 pos = currentTile.transform.position;
            pos.y += heightOffsetY;
            transform.position = pos;
        }

        // Lấy stat từ UnitData (nếu có) hoặc từ giá trị sẵn có
        ApplyDataFromConfig();
    }

    /// <summary>
    /// Lấy tất cả chỉ số từ UnitData (nếu có) đổ vào các biến runtime.
    /// Gọi 1 lần khi spawn hoặc sau khi set data.
    /// </summary>
    public void ApplyDataFromConfig()
    {
        if (data == null)
        {
            // Không có UnitData: dùng các giá trị đang có trong Inspector
            maxHP = hp;
            maxMana = 100;
            attackDamage = damage;
            attackSpeed = 1f;
            armor = 0f;
            magicResist = 0f;
        }
        else
        {
            unitName = data.unitName;
            maxHP = data.maxHP;
            maxMana = data.maxMana;
            attackDamage = data.attackDamage;
            attackSpeed = data.attackSpeed;
            armor = data.armor;
            magicResist = data.magicResist;
        }

        currentHP = maxHP;
        currentMana = 0;

        // Đồng bộ về biến cũ để không làm hỏng chỗ khác
        hp = maxHP;
        damage = attackDamage;
    }

    /// <summary>
    /// Đặt unit lên 1 tile (dùng khi spawn / drop từ bench)
    /// </summary>
    public void SetTile(Tile newTile)
    {
        // Hủy di chuyển & giữ chỗ cũ nếu có
        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
            moveTargetTile.reservedUnit = null;

        isMoving = false;
        moveTargetTile = null;
        moveT = 0f;

        if (currentTile != null)
        {
            if (currentTile.currentUnit == this)
                currentTile.currentUnit = null;
            if (currentTile.reservedUnit == this)
                currentTile.reservedUnit = null;
        }

        currentTile = newTile;

        if (newTile != null)
        {
            newTile.currentUnit = this;

            Vector3 pos = newTile.transform.position;
            pos.y += heightOffsetY;
            transform.position = pos;
        }
    }

    void Update()
    {
        // Chỉ unit được tham chiến mới xử lý AI
        if (!isInBattle) return;
        if (!BattleManager.Instance.isBattleActive) return;

        // đang di chuyển → tiếp tục lerp
        if (isMoving)
        {
            ContinueMove();
            return;
        }

        if (currentTile == null)
            return;

        // 1. Tìm mục tiêu gần nhất
        Unit target = BattleManager.Instance.FindClosestEnemy(this);
        if (target == null || target.currentTile == null) return;

        // 2. Khoảng cách theo ô
        int dx = Mathf.Abs(target.currentTile.x - currentTile.x);
        int dy = Mathf.Abs(target.currentTile.y - currentTile.y);
        int tileDistance = dx + dy;

        if (tileDistance <= attackRangeTiles)
        {
            // 3. Trong tầm → đánh
            TryAttack(target);
        }
        else
        {
            // 4. Ngoài tầm → bước một ô về phía target (có giữ chỗ)
            MoveOneStepTowards(target);
        }
    }

    void MoveOneStepTowards(Unit target)
    {
        if (currentTile == null || target.currentTile == null) return;

        int dx = target.currentTile.x - currentTile.x;
        int dy = target.currentTile.y - currentTile.y;

        int stepX = 0;
        int stepY = 0;

        // Ưu tiên trục xa hơn
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            stepX = dx > 0 ? 1 : -1;
        }
        else if (dy != 0)
        {
            stepY = dy > 0 ? 1 : -1;
        }

        // Nếu dx,dy đều 0 (đề phòng lỗi) thì khỏi đi
        if (stepX == 0 && stepY == 0)
            return;

        int newX = currentTile.x + stepX;
        int newY = currentTile.y + stepY;

        Tile nextTile = BoardManager.Instance.GetTile(newX, newY);
        if (nextTile == null) return;

        // Ô này có cho mình "đặt chỗ" không?
        if (!nextTile.CanReserve(this))
            return;

        // Hủy giữ chỗ cũ nếu có
        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
            moveTargetTile.reservedUnit = null;

        // ĐẶT CHỖ ô mới
        nextTile.reservedUnit = this;

        // Bắt đầu di chuyển
        moveTargetTile = nextTile;
        moveStartPos = transform.position;
        moveT = 0f;
        isMoving = true;
    }

    void ContinueMove()
    {
        if (moveTargetTile == null)
        {
            isMoving = false;
            return;
        }

        Vector3 targetPos = moveTargetTile.transform.position;
        targetPos.y += heightOffsetY;

        moveT += Time.deltaTime * moveSpeed;
        float t = Mathf.Clamp01(moveT);
        transform.position = Vector3.Lerp(moveStartPos, targetPos, t);

        if (t >= 1f)
        {
            // tới nơi → cập nhật tile
            if (currentTile != null)
            {
                if (currentTile.currentUnit == this)
                    currentTile.currentUnit = null;
                if (currentTile.reservedUnit == this)
                    currentTile.reservedUnit = null;
            }

            currentTile = moveTargetTile;
            currentTile.currentUnit = this;

            if (currentTile.reservedUnit == this)
                currentTile.reservedUnit = null;

            moveTargetTile = null;
            isMoving = false;
            moveT = 0f;
        }
    }

    void TryAttack(Unit target)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // dùng attackDamage runtime
        target.TakeDamage(attackDamage);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }

        // sync về hp cũ cho chắc
        hp = currentHP;
    }

    void Die()
    {
        // Xóa khỏi tile & reservation
        if (currentTile != null)
        {
            if (currentTile.currentUnit == this)
                currentTile.currentUnit = null;
            if (currentTile.reservedUnit == this)
                currentTile.reservedUnit = null;
        }

        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
        {
            moveTargetTile.reservedUnit = null;
        }

        BattleManager.Instance.OnUnitDied(this);
        Destroy(gameObject);
    }
}
