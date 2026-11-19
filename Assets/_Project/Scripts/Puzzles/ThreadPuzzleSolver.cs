using System.Collections.Generic;
using UnityEngine;

// Логіка пазла ниток без UI: які сторони з'єднує плитка і куди доходить кожна нитка.
// Перевірка — пошук у ширину (BFS) від голки: беремо клітинку з черги, дивимось, куди з неї
// виходить нитка, і додаємо в чергу сусідні клітинки, які приймають нитку з цього боку.
// Сторони кодуються бітами: верх = 1, право = 2, низ = 4, ліво = 8.
public static class ThreadPuzzleSolver
{
    // Результат перевірки
    public class Result
    {
        public bool solved;
        // для кожної нитки: які канали плиток вона пройшла (ключ = індекс клітинки * 16 + маска каналу)
        public List<HashSet<int>> lit = new List<HashSet<int>>();
        // для кожної нитки: чи дійшла до своєї котушки
        public List<bool> reachedSpool = new List<bool>();
    }

    // Сторони плитки до повороту
    public static int BaseMask(ThreadTileType type)
    {
        switch (type)
        {
            case ThreadTileType.Straight: return 1 | 4;           // верх — низ
            case ThreadTileType.Corner: return 1 | 2;             // верх — право
            case ThreadTileType.Tee: return 1 | 2 | 4;            // верх — право — низ
            case ThreadTileType.Cross: return 1 | 2 | 4 | 8;
            default: return 0;
        }
    }

    // Поворот на 90° за годинниковою стрілкою = зсув бітів: верх → право → низ → ліво → верх
    public static int Rotate(int mask, int rotation)
    {
        rotation &= 3;
        return ((mask << rotation) | (mask >> (4 - rotation))) & 15;
    }

    // Канали плитки: окремі «трубки», по яких іде нитка. У хреста їх два, і вони не з'єднані
    public static int[] Channels(ThreadTileType type, int rotation)
    {
        if (type == ThreadTileType.Empty) return new int[0];
        if (type == ThreadTileType.Cross) return new[] { 1 | 4, 2 | 8 };
        return new[] { Rotate(BaseMask(type), rotation) };
    }

    // Через скільки поворотів плитка виглядає так само: пряма — 2, хрест — 1, кут і Т — 4
    public static int Period(ThreadTileType type)
    {
        switch (type)
        {
            case ThreadTileType.Straight: return 2;
            case ThreadTileType.Corner:
            case ThreadTileType.Tee: return 4;
            default: return 1;
        }
    }

    // Скільки кліків, щоб з одного повороту дійти до іншого
    public static int TurnsNeeded(ThreadTileType type, int from, int to)
    {
        int p = Period(type);
        return ((to - from) % p + p) % p;
    }

    // Зсув до сусідньої клітинки (y росте вниз)
    public static Vector2Int Step(int side)
    {
        switch (side)
        {
            case 0: return new Vector2Int(0, -1);
            case 1: return new Vector2Int(1, 0);
            case 2: return new Vector2Int(0, 1);
            default: return new Vector2Int(-1, 0);
        }
    }

    public static int Opposite(int side) => (side + 2) % 4;

    // Край клітинки як одне число (для голки й котушки)
    private static int Edge(ThreadPuzzleData data, Vector2Int cell, int side) => (cell.y * data.Width + cell.x) * 4 + side;

    // Перевірити поточні повороти. Розв'язано, коли кожна нитка дійшла до своєї котушки
    // і не торкнулась іншої нитки (у хресті вони проходять окремими каналами, це дозволено)
    public static Result Solve(ThreadPuzzleData data, int[] rotations)
    {
        var result = new Result();
        var exits = new List<HashSet<int>>();

        foreach (ThreadLine line in data.Lines)
        {
            Trace(data, rotations, line, out HashSet<int> visited, out HashSet<int> lineExits);
            result.lit.Add(visited);
            exits.Add(lineExits);
            result.reachedSpool.Add(lineExits.Contains(Edge(data, line.spoolCell, (int)line.spoolSide)));
        }

        result.solved = true;
        for (int a = 0; a < data.Lines.Length; a++)
        {
            if (!result.reachedSpool[a]) result.solved = false;

            for (int b = 0; b < data.Lines.Length; b++)
            {
                if (a == b) continue;
                ThreadLine other = data.Lines[b];
                bool touches = result.lit[a].Overlaps(result.lit[b])
                               || exits[a].Contains(Edge(data, other.needleCell, (int)other.needleSide))
                               || exits[a].Contains(Edge(data, other.spoolCell, (int)other.spoolSide));
                if (touches) result.solved = false;
            }
        }
        return result;
    }

    // Пошук у ширину від голки однієї нитки
    private static void Trace(ThreadPuzzleData data, int[] rotations, ThreadLine line,
                              out HashSet<int> visited, out HashSet<int> exits)
    {
        visited = new HashSet<int>();
        exits = new HashSet<int>();
        var queue = new Queue<int>();

        // нитка входить у клітинку біля голки з боку голки
        Enter(data, rotations, line.needleCell, (int)line.needleSide, visited, queue);

        while (queue.Count > 0)
        {
            int key = queue.Dequeue();
            int index = key / 16;
            int channel = key % 16;
            var cell = new Vector2Int(index % data.Width, index / data.Width);

            for (int side = 0; side < 4; side++)
            {
                if ((channel >> side & 1) == 0) continue;     // з цього боку нитка не виходить

                Vector2Int next = cell + Step(side);
                if (Inside(data, next)) Enter(data, rotations, next, Opposite(side), visited, queue);
                else exits.Add(index * 4 + side);           // вийшла за край сітки (може, там котушка)
            }
        }
    }

    // Нитка заходить у клітинку з боку side: шукаємо канал плитки, який має цю сторону
    private static void Enter(ThreadPuzzleData data, int[] rotations, Vector2Int cell, int side,
                              HashSet<int> visited, Queue<int> queue)
    {
        if (!Inside(data, cell)) return;

        int index = cell.y * data.Width + cell.x;
        foreach (int channel in Channels(data.Tile(index).type, rotations[index]))
        {
            if ((channel >> side & 1) == 0) continue;
            int key = index * 16 + channel;
            if (visited.Add(key)) queue.Enqueue(key);
            return;
        }
    }

    private static bool Inside(ThreadPuzzleData data, Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < data.Width && c.y < data.Height;

    // Для підказки Марки (рівень 3): перша плитка на шляху, яка повернута не так. -1 — таких немає
    public static int FindWrongTile(ThreadPuzzleData data, int[] rotations)
    {
        for (int i = 0; i < data.TileCount; i++)
        {
            ThreadTile t = data.Tile(i);
            if (t.solutionRotation >= 0 && TurnsNeeded(t.type, rotations[i], t.solutionRotation) != 0) return i;
        }
        return -1;
    }
}
