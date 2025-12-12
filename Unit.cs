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

    // ====== UNIT DATA ======
    [Header("Unit Data (ScriptableObject)")]
    public UnitData data;

    // ====== RUNTIME STATS ======
    [Header("Runtime Stats (auto lấy từ UnitData)")]
    public int maxHP;
    public int maxMana;
    public int attackDamage;
    public float attackSpeed;       // attack per second
    public float armor;
    public float magicResist;
    public int attackRangeTiles;
    public float moveSpeed;

    [Header("Attack / Skill")]
    public bool isRanged = false;           // true = bắn xa, false = đánh gần
    public GameObject basicProjectilePrefab;
    public GameObject skillProjectilePrefab;
    public float projectileSpeed = 8f;

    public int manaPerAttack = 10;         // cộng mana mỗi lần đánh
    public int manaPerHitTaken = 5;        // cộng mana khi bị đánh

    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMana;
    [HideInInspector] public float attackCooldown;
    [Header("Skill System")]
    public SkillData activeSkill;
    [Header("Vị trí")]
    // offset thêm nếu muốn cho model nổi/chìm so với mặt tile
    public float heightOffsetY = 0f;
    public Tile currentTile;

    // offset từ pivot -> chân model (tự tính)
    float modelFootOffsetY = 0f;

    public bool isInBattle = false;

    float lastAttackTime = -999f;

    // move system
    bool isMoving = false;
    Tile moveTargetTile = null;
    Vector3 moveStartPos;
    float moveT = 0f;

    // ============================================

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyDataFromConfig();
    }

    void Start()
    {
        ApplyDataFromConfig();

        // Tính khoảng cách từ pivot -> chân model 1 lần
        RecalculateModelFootOffset();

        // Nếu đã có currentTile gán sẵn, đặt unit đúng vị trí
        if (currentTile != null)
        {
            SetTile(currentTile);
        }
    }

    public void GainMana(int amount)
    {
        if (maxMana <= 0) return;
        currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);
        // TODO: update thanh mana UI sau này
    }

    // Tính khoảng cách từ pivot -> chân mesh (dùng bounds)
    void RecalculateModelFootOffset()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            float minY = r.bounds.min.y;         // chân thấp nhất của mesh (world)
            float pivotY = transform.position.y; // pivot hiện tại (world)
            modelFootOffsetY = pivotY - minY;   // khoảng cách pivot -> chân
        }
        else
        {
            modelFootOffsetY = 0f;
        }
    }

    // Lấy tọa độ Y của mặt trên tile (top surface)
    float GetTileTopY(Tile tile)
    {
        if (tile == null) return 0f;

        Renderer r = tile.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            return r.bounds.max.y;   // mặt trên của mesh tile (world Y)
        }

        // fallback: không có renderer thì dùng transform Y
        return tile.transform.position.y;
    }

    // Đặt unit đứng đúng trên tile (không đụng đến currentUnit / reservedUnit)
    void PlaceOnTile(Tile tile)
    {
        if (tile == null) return;

        float tileTopY = GetTileTopY(tile);

        Vector3 pos = transform.position;
        pos.x = tile.transform.position.x;
        pos.z = tile.transform.position.z;

        // Chân model = mặt tile + heightOffsetY
        pos.y = tileTopY + modelFootOffsetY + heightOffsetY;

        transform.position = pos;
    }

    // ============================================
    // LOAD STATS FROM UNITDATA
    // ============================================
    public void ApplyDataFromConfig()
    {
        if (data == null)
        {
            Debug.LogWarning($"Unit '{name}' không có UnitData!");
            return;
        }

        unitName = data.unitName;
        maxHP = data.maxHP;
        maxMana = data.maxMana;
        attackDamage = data.attackDamage;
        attackSpeed = data.attackSpeed;
        armor = data.armor;
        magicResist = data.magicResist;
        moveSpeed = data.moveSpeed;
        attackRangeTiles = data.attackRangeTiles;

        currentHP = maxHP;
        currentMana = 0;

        attackCooldown = attackSpeed > 0 ? 1f / attackSpeed : 999f;
    }

    // ============================================

    public void SetTile(Tile newTile)
    {
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
            PlaceOnTile(newTile);
        }
    }

    // ============================================

    void Update()
    {
        if (!isInBattle) return;
        if (!BattleManager.Instance.isBattleActive) return;

        if (isMoving)
        {
            ContinueMove();
            return;
        }

        if (currentTile == null) return;

        Unit target = BattleManager.Instance.FindClosestEnemy(this);
        if (target == null || target.currentTile == null) return;

        int dx = Mathf.Abs(target.currentTile.x - currentTile.x);
        int dy = Mathf.Abs(target.currentTile.y - currentTile.y);
        int tileDistance = dx + dy;

        if (tileDistance <= attackRangeTiles)
            TryAttack(target);
        else
            MoveOneStepTowards(target);
    }

    // ============================================
    // MOVEMENT
    // ============================================

    void MoveOneStepTowards(Unit target)
    {
        if (currentTile == null || target.currentTile == null) return;

        int dx = target.currentTile.x - currentTile.x;
        int dy = target.currentTile.y - currentTile.y;

        int stepX = 0;
        int stepY = 0;

        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            stepX = dx > 0 ? 1 : -1;
        else if (dy != 0)
            stepY = dy > 0 ? 1 : -1;

        if (stepX == 0 && stepY == 0) return;

        Tile nextTile = BoardManager.Instance.GetTile(currentTile.x + stepX, currentTile.y + stepY);
        if (nextTile == null) return;
        if (!nextTile.CanReserve(this)) return;

        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
            moveTargetTile.reservedUnit = null;

        nextTile.reservedUnit = this;
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

        // Tính targetPos dựa trên mặt tile + chân model
        float tileTopY = GetTileTopY(moveTargetTile);
        Vector3 targetPos = new Vector3(
            moveTargetTile.transform.position.x,
            tileTopY + modelFootOffsetY + heightOffsetY,
            moveTargetTile.transform.position.z
        );

        moveT += Time.deltaTime * moveSpeed;
        float t = Mathf.Clamp01(moveT);

        transform.position = Vector3.Lerp(moveStartPos, targetPos, t);

        if (t >= 1f)
        {
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

    // ============================================
    // COMBAT
    // ============================================

    void TryAttack(Unit target)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        // Nếu đủ mana -> xả skill, rồi reset mana
        if (currentMana >= maxMana && maxMana > 0)
        {
            CastSkill(target);
            currentMana = 0;
        }
        else
        {
            BasicAttack(target);
            GainMana(manaPerAttack);
        }
    }

    // ĐÁNH THƯỜNG
    void BasicAttack(Unit target)
    {
        if (target == null) return;

        if (isRanged && basicProjectilePrefab != null)
        {
            // vị trí spawn đạn (khoảng tầm ngực)
            Vector3 spawnPos = transform.position + Vector3.up * 1.2f;

            GameObject go = Instantiate(basicProjectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj = go.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Init(target, attackDamage, projectileSpeed);
            }
        }
        else
        {
            // melee: trừ máu trực tiếp
            target.TakeDamage(attackDamage);
        }
    }

    // SKILL KHI ĐỦ MANA
    void CastSkill(Unit target)
    {
        if (activeSkill == null)
        {
            Debug.LogWarning($"{unitName} không có skill!");
            return;
        }

        activeSkill.Execute(this, target);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;

        // nhận dame thì được cộng mana
        GainMana(manaPerHitTaken);

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
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
        // Rơi gem (chỉ quái mới rơi)
        if (team == Team.Enemy)
            GetComponent<LootOnDeath>()?.TryDrop();
        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
        {
            moveTargetTile.reservedUnit = null;
        }

        // Báo cho BattleManager biết unit này đã chết
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnUnitDied(this);

        if (team == Team.Enemy)
        {
            // QUÁI: xoá hẳn object
            Destroy(gameObject);
        }
        else
        {
            // LÍNH MÌNH: chỉ ẩn đi, KHÔNG destroy, để round sau còn reset lại
            gameObject.SetActive(false);
        }
    }
}
