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
    public string unitName;
    public Team team;
    public float moveSpeed = 3f;

    public Tile currentTile;

    private Coroutine moveRoutine;

    void OnMouseDown()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnUnitClicked(this);
        }
    }

    public void SetTile(Tile tile)
    {
        if (tile == null)
            return;

        if (currentTile != null)
        {
            currentTile.ClearUnit(this);
        }

        currentTile = tile;
        tile.SetUnit(this);

        Vector3 targetPos = tile.transform.position + Vector3.up * 0.5f;
        transform.position = targetPos;
    }

    public void RequestMove(Tile destination)
    {
        if (destination == null || GridPathfinder.Instance == null || currentTile == null)
            return;

        List<Tile> path = GridPathfinder.Instance.FindPath(currentTile, destination);

        if (path == null || path.Count == 0)
        {
            Debug.Log("[Unit] Không tìm thấy đường đi tới tile đích");
            return;
        }

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveAlongPath(path));
    }

    IEnumerator MoveAlongPath(List<Tile> path)
    {
        if (path == null || path.Count == 0)
            yield break;

        Tile startTile = currentTile;
        if (startTile != null)
        {
            startTile.ClearUnit(this);
            currentTile = null;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Tile tile = path[i];
            if (tile == null)
                continue;

            if (tile == startTile)
                continue;

            Vector3 targetPos = tile.transform.position + Vector3.up * 0.5f;

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
        }

        Tile finalTile = path[path.Count - 1];
        currentTile = finalTile;
        finalTile.SetUnit(this);
    }

    public void Die()
    {
        if (currentTile != null)
        {
            currentTile.ClearUnit(this);
        }

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnUnitDied(this);
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (currentTile != null)
        {
            currentTile.ClearUnit(this);
        }
    }
}
