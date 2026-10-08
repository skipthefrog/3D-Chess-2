#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Development-only check of every piece's moves and captures, in every direction, for every color.
/// Turn on with `defaults write BomSapo.Chess3D DebugMoveSelfTest -int 1` on the simulator, then
/// start any game: when the board exists it compares each piece's moves against a simple
/// independent rule implementation and logs "MOVE SELF-TEST" with the results.
/// The board is cleared for the test and restored afterwards.
/// </summary>
public class MoveSelfTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (PlayerPrefs.GetInt("DebugMoveSelfTest", 0) != 1) return;
        var go = new GameObject("Move Self Test");
        DontDestroyOnLoad(go);
        go.AddComponent<MoveSelfTest>();
    }

    private readonly List<string> failures = new List<string>();
    private int passed;
    private bool ran;

    private void Update()
    {
        if (ran || ChessBoard.Instance == null || BoardDimensionsManager.Instance == null) return;
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanPlacePieces()) return;
        ran = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(1f);
        Vector3Int dims = BoardDimensionsManager.Instance.GetDimensions();
        var board = ChessBoard.Instance;

        // Clear the board, remembering what was there
        var saved = new Dictionary<BoardPosition, ChessPiece>();
        board.EnableSimulationMode();
        foreach (BoardPosition p in All(dims))
        {
            ChessPiece piece = board.GetPieceAt(p);
            if (piece != null) { saved[p] = piece; board.SetPieceAt(p, null); }
        }

        var colors = new[] { PieceColor.White, PieceColor.Black, PieceColor.Green, PieceColor.Purple, PieceColor.Yellow, PieceColor.Orange };
        var spots = new[] { new BoardPosition(0, 0, 0), new BoardPosition(dims.x / 2 - 1, dims.y / 2, dims.z / 2 - 1), new BoardPosition(dims.x - 1, dims.y - 2, 1) };

        var made = new List<GameObject>();
        ChessPiece Make<T>(PieceColor color) where T : ChessPiece
        {
            var go = new GameObject($"SelfTest {typeof(T).Name}");
            go.SetActive(false);
            go.AddComponent<SphereCollider>();
            T piece = go.AddComponent<T>();
            piece.pieceColor = color;
            go.SetActive(true);
            made.Add(go);
            return piece;
        }

        foreach (PieceColor color in colors)
        {
            PieceColor enemy = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            ChessPiece blocker = Make<Rook>(enemy);

            // Sliding, stepping and jumping pieces on an empty board, and capturing a far piece
            foreach (BoardPosition at in spots)
            {
                Check(Make<King>(color), at, dims, Directions(1, 2, 3), 1, board, null);
                Check(Make<Rook>(color), at, dims, Directions(1), int.MaxValue, board, null);
                Check(Make<Bishop>(color), at, dims, Directions(2, 3), int.MaxValue, board, null);
                Check(Make<Queen>(color), at, dims, Directions(1, 2, 3), int.MaxValue, board, null);
                CheckKnight(Make<Knight>(color), at, dims, board);
            }

            // Every slider captures the far enemy at the end of each line and stops there
            BoardPosition c = spots[1];
            foreach (Vector3Int d in Directions(1, 2, 3))
            {
                BoardPosition far = Last(c, d, dims);
                if (far == c) continue;
                Place(board, blocker, far);
                ChessPiece q = Make<Queen>(color);
                Place(board, q, c);
                List<BoardPosition> moves = q.GetValidMoves();
                Expect(moves.Contains(far), $"{color} Queen at {c} should capture {enemy} at {far}");
                board.SetPieceAt(c, null);
                board.SetPieceAt(far, null);
            }

            // Pawn: one step forward when clear; captures on all 8 forward-diagonal cells; never straight ahead
            ChessPiece pawn = Make<Pawn>(color);
            Vector3Int fwd = Forward(color);
            BoardPosition home = new BoardPosition(dims.x / 2, dims.y / 2, dims.z / 2);
            Place(board, pawn, home);
            List<BoardPosition> quiet = pawn.GetValidMoves();
            BoardPosition ahead = Add(home, fwd);
            Expect(quiet.Count == 1 && quiet[0] == ahead, $"{color} Pawn on an empty board should only step to {ahead}, got [{string.Join(", ", quiet)}]");

            var victims = new List<ChessPiece>();
            foreach (Vector3Int off in PawnCaptureOffsets(fwd))
            {
                BoardPosition target = Add(home, off);
                ChessPiece v = Make<Rook>(enemy);
                Place(board, v, target);
                victims.Add(v);
                List<BoardPosition> moves = pawn.GetValidMoves();
                Expect(moves.Contains(target), $"{color} Pawn at {home} should capture toward {off} ({target})");
                board.SetPieceAt(target, null);
            }
            ChessPiece wall = Make<Rook>(enemy);
            Place(board, wall, ahead);
            Expect(!pawn.GetValidMoves().Contains(ahead), $"{color} Pawn must not capture straight ahead");
            board.SetPieceAt(ahead, null);
            board.SetPieceAt(home, null);
        }

        foreach (GameObject go in made) Destroy(go);

        // Put the real board back
        foreach (BoardPosition p in All(dims)) board.SetPieceAt(p, null);
        foreach (var kv in saved) { board.SetPieceAt(kv.Key, kv.Value); kv.Value.SetCurrentPosition(kv.Key); }
        board.DisableSimulationMode();

        Debug.Log($"MOVE SELF-TEST ({dims.x}x{dims.y}x{dims.z}): {passed} passed, {failures.Count} failed");
        foreach (string f in failures.Take(40)) Debug.Log("MOVE SELF-TEST FAIL: " + f);
    }

    // ───────── independent rules ─────────

    private void Check(ChessPiece piece, BoardPosition at, Vector3Int dims, List<Vector3Int> dirs, int reach, ChessBoard board, ChessPiece _)
    {
        Place(board, piece, at);
        var expected = new HashSet<BoardPosition>();
        foreach (Vector3Int d in dirs)
        {
            BoardPosition p = at;
            for (int step = 1; step <= reach; step++)
            {
                p = Add(p, d);
                if (!Inside(p, dims)) break;
                expected.Add(p);
            }
        }
        Compare(piece, at, expected, piece.GetValidMoves());
        board.SetPieceAt(at, null);
    }

    private void CheckKnight(ChessPiece knight, BoardPosition at, Vector3Int dims, ChessBoard board)
    {
        Place(board, knight, at);
        var expected = new HashSet<BoardPosition>();
        for (int dx = -2; dx <= 2; dx++)
        for (int dy = -2; dy <= 2; dy++)
        for (int dz = -2; dz <= 2; dz++)
        {
            var a = new[] { Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz) }.OrderBy(v => v).ToArray();
            if (a[0] == 0 && a[1] == 1 && a[2] == 2)
            {
                var p = new BoardPosition(at.x + dx, at.y + dy, at.z + dz);
                if (Inside(p, dims)) expected.Add(p);
            }
        }
        Compare(knight, at, expected, knight.GetValidMoves());
        board.SetPieceAt(at, null);
    }

    private void Compare(ChessPiece piece, BoardPosition at, HashSet<BoardPosition> expected, List<BoardPosition> actual)
    {
        var got = new HashSet<BoardPosition>(actual);
        var missing = expected.Where(p => !got.Contains(p)).ToList();
        var extra = got.Where(p => !expected.Contains(p)).ToList();
        string name = $"{piece.pieceColor} {piece.GetType().Name} at {at}";
        Expect(missing.Count == 0, $"{name} is missing {missing.Count} moves, e.g. {string.Join(", ", missing.Take(4))}");
        Expect(extra.Count == 0, $"{name} has {extra.Count} extra moves, e.g. {string.Join(", ", extra.Take(4))}");
    }

    private void Expect(bool ok, string message)
    {
        if (ok) passed++;
        else failures.Add(message);
    }

    private static List<Vector3Int> Directions(params int[] axesChanged)
    {
        var list = new List<Vector3Int>();
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            int n = (x != 0 ? 1 : 0) + (y != 0 ? 1 : 0) + (z != 0 ? 1 : 0);
            if (axesChanged.Contains(n)) list.Add(new Vector3Int(x, y, z));
        }
        return list;
    }

    private static IEnumerable<Vector3Int> PawnCaptureOffsets(Vector3Int fwd)
    {
        foreach (Vector3Int d in Directions(1, 2, 3))
        {
            // One step forward on the pawn's axis, plus any sideways step on the other two axes
            if (Vector3.Dot(d, fwd) == 1 && d != fwd) yield return d;
        }
    }

    private static Vector3Int Forward(PieceColor c)
    {
        switch (c)
        {
            case PieceColor.White: return new Vector3Int(1, 0, 0);
            case PieceColor.Black: return new Vector3Int(-1, 0, 0);
            case PieceColor.Green: return new Vector3Int(0, 0, 1);
            case PieceColor.Purple: return new Vector3Int(0, 0, -1);
            case PieceColor.Yellow: return new Vector3Int(0, 1, 0);
            default: return new Vector3Int(0, -1, 0);
        }
    }

    private static BoardPosition Last(BoardPosition from, Vector3Int d, Vector3Int dims)
    {
        BoardPosition p = from;
        while (Inside(Add(p, d), dims)) p = Add(p, d);
        return p;
    }

    private static void Place(ChessBoard board, ChessPiece piece, BoardPosition at)
    {
        board.SetPieceAt(at, piece);
        piece.SetCurrentPosition(at);
    }

    private static BoardPosition Add(BoardPosition p, Vector3Int d) => new BoardPosition(p.x + d.x, p.y + d.y, p.z + d.z);
    private static bool Inside(BoardPosition p, Vector3Int dims) => p.x >= 0 && p.y >= 0 && p.z >= 0 && p.x < dims.x && p.y < dims.y && p.z < dims.z;

    private static IEnumerable<BoardPosition> All(Vector3Int dims)
    {
        for (int x = 0; x < dims.x; x++)
        for (int y = 0; y < dims.y; y++)
        for (int z = 0; z < dims.z; z++)
            yield return new BoardPosition(x, y, z);
    }
}
#endif
