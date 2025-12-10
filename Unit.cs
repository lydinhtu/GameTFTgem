using System.Collections;
using System.Collections.Generic;
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

    [Header("Stats cơ bản")]
    public float moveSpeed = 4f;        // tốc độ di chuyển
    public int attackRangeTiles = 1;    // tầm đánh tính theo ô (1 = melee)
    public float attackCooldown = 1f;   // thời gian giữa 2 đòn đánh

    [Header("Vị trí / chiều cao")]
    public float heightOffsetY = 0.5f;  // ✅ chỉnh trong Inspector cho tới khi đứng vừa mặt bàn

    [Header("Tham chiếu")]
    public Tile currentTile;

    // combat đơn giản
    public int hp = 100;
    public int damage = 10;

    float lastAttackTime = -999f;

    // movement
    bool isMoving = false;
    Tile moveTargetTile = null;

    void Start()
    {
        // đảm bảo unit đứng đúng trên tile khi spawn
        if (currentTile != null)
        {
            Vector3 pos = currentTile.transform.position;
            pos.y += heightOffsetY;                // ✅ nâng lên theo offset
            transform.position = pos;

            currentTile.currentUnit = this;
        }
    }

    public void SetTile(Tile newTile)
    {
        if (currentTile != null && currentTile.currentUnit == this)
            currentTile.currentUnit = null;

        currentTile = newTile;

        if (newTile != null)
        {
            newTile.currentUnit = this;

            // ✅ luôn set đúng Y (tile + offset)
            Vector3 pos = newTile.transform.position;
            pos.y += heightOffsetY;
            transform.position = pos;
        }
    }

    void Update()
    {
        if (!BattleManager.Instance.isBattleActive)
            return;

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
        {
            TryAttack(target);
        }
        else
        {
            Tile bestAttackTile = BoardManager.Instance.GetBestAttackTile(this, target);
            if (bestAttackTile == null)
                return;

            List<Tile> path = BoardManager.Instance.FindPath(currentTile, bestAttackTile, this);
            if (path == null || path.Count < 2)
                return;

            Tile nextTile = path[1];

            if (nextTile.currentUnit != null && nextTile != currentTile)
                return;

            moveTargetTile = nextTile;
            isMoving = true;
        }
    }

    void ContinueMove()
    {
        if (moveTargetTile == null)
        {
            isMoving = false;
            return;
        }

        // ✅ di chuyển tới vị trí tile + offset Y
        Vector3 targetPos = moveTargetTile.transform.position;
        targetPos.y += heightOffsetY;

        Vector3 currentPos = transform.position;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(currentPos, targetPos, step);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            SetTile(moveTargetTile);
            moveTargetTile = null;
            isMoving = false;
        }
    }

    void TryAttack(Unit target)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

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
        if (currentTile != null && currentTile.currentUnit == this)
            currentTile.currentUnit = null;

        BattleManager.Instance.OnUnitDied(this);
        Destroy(gameObject);
    }
}
