using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder : MonoBehaviour
{
    public static GridPathfinder Instance;

    private Tile[,] cachedGrid;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cachedGrid = BoardManager.Instance != null ? BoardManager.Instance.tiles : null;
    }

    public List<Tile> FindPath(Tile start, Tile destination)
    {
        if (start == null || destination == null)
            return null;

        if (BoardManager.Instance == null || BoardManager.Instance.tiles == null)
            return null;

        cachedGrid = BoardManager.Instance.tiles;

        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();
        Queue<Tile> frontier = new Queue<Tile>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            Tile current = frontier.Dequeue();

            if (current == destination)
                break;

            foreach (Tile neighbor in GetNeighbors(current))
            {
                if (neighbor == null)
                    continue;

                if (cameFrom.ContainsKey(neighbor))
                    continue;

                if (!IsTileWalkable(neighbor, destination, start))
                    continue;

                cameFrom[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(destination))
        {
            return null; // không tìm thấy đường
        }

        List<Tile> path = new List<Tile>();
        Tile step = destination;
        while (step != null)
        {
            path.Add(step);
            cameFrom.TryGetValue(step, out step);
        }

        path.Reverse();
        return path;
    }

    IEnumerable<Tile> GetNeighbors(Tile tile)
    {
        if (tile == null || cachedGrid == null)
            yield break;

        int[,] directions = new int[,]
        {
            {1, 0},
            {-1, 0},
            {0, 1},
            {0, -1}
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = tile.x + directions[i, 0];
            int ny = tile.y + directions[i, 1];

            if (nx < 0 || ny < 0 || nx >= BoardManager.Instance.width || ny >= BoardManager.Instance.height)
                continue;

            yield return cachedGrid[nx, ny];
        }
    }

    bool IsTileWalkable(Tile tile, Tile destination, Tile start)
    {
        if (tile == null)
            return false;

        if (!tile.IsWalkable)
            return false;

        if (tile == destination)
            return true;

        if (tile == start)
            return true;

        return !tile.IsOccupied;
    }
}
