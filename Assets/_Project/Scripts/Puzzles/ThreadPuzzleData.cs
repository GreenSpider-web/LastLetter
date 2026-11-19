using System;
using UnityEngine;

// Типи плиток ниток (GDD: 4 типи) + порожня клітинка
public enum ThreadTileType
{
    Empty,      // порожньо, нитка не проходить
    Straight,   // пряма: верх — низ
    Corner,     // кут: верх — право
    Tee,        // Т: верх — право — низ
    Cross       // хрест: верх — низ і ліво — право, дві нитки не з'єднуються
}

// Сторони клітинки. Номер = скільки разів повернути «верх» за годинниковою стрілкою
public enum ThreadSide { Up = 0, Right = 1, Down = 2, Left = 3 }

public enum ThreadColor { Red, White }

// Одна плитка сітки. Повороти — за годинниковою стрілкою, 0–3 (по 90°)
[Serializable]
public class ThreadTile
{
    public ThreadTileType type = ThreadTileType.Straight;
    [Range(0, 3)] public int startRotation;
    [Tooltip("Поворот у розв'язку. -1 — плитка не на шляху нитки (будь-який поворот)")]
    [Range(-1, 3)] public int solutionRotation = -1;
}

// Одна нитка: голка (звідки) і котушка (куди). Обидві стоять за краєм сітки, біля вказаної сторони клітинки
[Serializable]
public class ThreadLine
{
    public ThreadColor color = ThreadColor.Red;
    [Tooltip("Клітинка біля голки: x — зліва направо, y — згори вниз, від 0")]
    public Vector2Int needleCell;
    public ThreadSide needleSide = ThreadSide.Left;
    public Vector2Int spoolCell;
    public ThreadSide spoolSide = ThreadSide.Right;
}

// Візерунок пазла ниток (GDD: «Хвіртка» 3×3, «Коридор» 4×4). Дизайн — Софія (задача 24), код — задача 23.
// Плитки йдуть рядками: індекс = y * width + x.
[CreateAssetMenu(fileName = "Puzzle_New", menuName = "LastLetter/Thread Puzzle Data")]
public class ThreadPuzzleData : ScriptableObject
{
    [SerializeField] private string title = "Хвіртка";

    [Tooltip("Прапорець GameState, який ставиться після розв'язку (щоб після «Продовжити» блокер не повертався)")]
    [SerializeField] private string solvedFlag = "puzzle_gate_solved";

    [Min(1)] [SerializeField] private int width = 3;
    [Min(1)] [SerializeField] private int height = 3;

    [Tooltip("Рядками згори вниз: індекс = y * width + x")]
    [SerializeField] private ThreadTile[] tiles = new ThreadTile[9];

    [SerializeField] private ThreadLine[] lines = new ThreadLine[1];

    public string Title => title;
    public string SolvedFlag => solvedFlag;
    public int Width => width;
    public int Height => height;
    public ThreadLine[] Lines => lines;

    public ThreadTile Tile(int x, int y) => tiles[y * width + x];
    public ThreadTile Tile(int index) => tiles[index];
    public int TileCount => width * height;

    // Повороти на старті — з них починає гравець
    public int[] StartRotations()
    {
        var result = new int[TileCount];
        for (int i = 0; i < result.Length; i++) result[i] = tiles[i].startRotation;
        return result;
    }

    // Мінімум кліків до розв'язку (записати в GDD, задача 24)
    public int MinMoves()
    {
        int moves = 0;
        foreach (ThreadTile t in tiles)
        {
            if (t.solutionRotation < 0) continue;
            moves += ThreadPuzzleSolver.TurnsNeeded(t.type, t.startRotation, t.solutionRotation);
        }
        return moves;
    }

    // Кількість плиток завжди = width × height
    private void OnValidate()
    {
        int count = width * height;
        if (tiles == null || tiles.Length == count) return;

        var resized = new ThreadTile[count];
        for (int i = 0; i < count; i++)
            resized[i] = (tiles != null && i < tiles.Length && tiles[i] != null) ? tiles[i] : new ThreadTile();
        tiles = resized;
    }

    // ПКМ по асету в Inspector → перевірити, що розв'язок справді розв'язує пазл, і порахувати ходи
    [ContextMenu("Перевірити розв'язок і порахувати ходи")]
    private void CheckSolution()
    {
        var rotations = new int[TileCount];
        for (int i = 0; i < rotations.Length; i++)
            rotations[i] = tiles[i].solutionRotation >= 0 ? tiles[i].solutionRotation : tiles[i].startRotation;

        bool solutionWorks = ThreadPuzzleSolver.Solve(this, rotations).solved;
        bool startSolved = ThreadPuzzleSolver.Solve(this, StartRotations()).solved;

        Debug.Log($"Пазл «{title}»: розв'язок {(solutionWorks ? "працює" : "НЕ працює")}, " +
                  $"старт {(startSolved ? "уже розв'язаний — зміни стартові повороти" : "не розв'язаний")}, " +
                  $"мінімум ходів: {MinMoves()}");
    }
}
