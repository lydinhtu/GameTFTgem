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

    [Header("Stats")]
    public int hp = 100;
    public int damage = 10;
    public float attackCooldown = 1f;
    public int attackRangeTiles = 1;   // 1 = melee, >1 = đánh xa
    public float moveSpeed = 4f;       // tốc độ di chuyển

    [Header("Vị trí")]
    public float heightOffsetY = 0.5f; // nâng model lên khỏi mặt tile
    public Tile currentTile;

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
    public bool isInBattle = false;
    void Update()   
    {
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

        // Ở đây bạn có thể thêm animation, spawn effect...
        target.TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
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

        if (moveTargetTile != null && moveTargetTile.reservedUnit == this)
        {
            moveTargetTile.reservedUnit = null;
        }

        BattleManager.Instance.OnUnitDied(this);
        Destroy(gameObject);
    }
}
