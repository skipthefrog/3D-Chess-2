using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimax algorithm with alpha-beta pruning for 3D chess AI.
/// This engine evaluates board positions and finds the best moves for the AI player.
/// </summary>
public class MinimaxEngine
{
    private const float POSITIVE_INFINITY = 99999f;
    private const float NEGATIVE_INFINITY = -99999f;
    private const float CHECKMATE_SCORE = 10000f;
    
    private int nodesEvaluated = 0;
    private bool enableDebugLogging = false;
    
    public MinimaxEngine(bool debug = false)
    {
        enableDebugLogging = debug;
    }
    
    /// <summary>
    /// Find the best move for the given player using minimax with alpha-beta pruning
    /// </summary>
    public AIMove FindBestMove(PieceColor player, int depth, float timeLimit = 5.0f)
    {
        nodesEvaluated = 0;
        float startTime = Time.time;
        
        if (enableDebugLogging)
            Debug.Log($"MinimaxEngine: Starting search for {player} at depth {depth}");
        
        List<AIMove> possibleMoves = GetAllPossibleMoves(player);
        
        if (possibleMoves.Count == 0)
        {
            Debug.LogWarning($"MinimaxEngine: No moves available for {player}");
            return null;
        }
        
        AIMove bestMove = null;
        float bestScore = NEGATIVE_INFINITY;
        
        // Evaluate each possible move
        foreach (AIMove move in possibleMoves)
        {
            // Check time limit
            if (Time.time - startTime > timeLimit)
            {
                Debug.LogWarning($"MinimaxEngine: Time limit exceeded ({timeLimit}s), stopping search");
                break;
            }
            
            // Make the move temporarily
            ChessPiece capturedPiece = MakeMove(move);
            
            // Evaluate this position
            float score = Minimax(depth - 1, NEGATIVE_INFINITY, POSITIVE_INFINITY, false, GetOppositeColor(player));
            
            // Undo the move
            UndoMove(move, capturedPiece);
            
            // Update best move if this is better
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                bestMove.evaluationScore = score;
            }
            
            if (enableDebugLogging)
                Debug.Log($"MinimaxEngine: Move {move.piece.pieceType} {move.fromPosition}→{move.toPosition} scored {score:F2}");
        }
        
        float searchTime = Time.time - startTime;
        
        if (enableDebugLogging)
        {
            Debug.Log($"MinimaxEngine: Search completed in {searchTime:F2}s");
            Debug.Log($"MinimaxEngine: Evaluated {nodesEvaluated} nodes");
            Debug.Log($"MinimaxEngine: Best move: {bestMove} (score: {bestScore:F2})");
        }
        
        return bestMove;
    }
    
    /// <summary>
    /// Minimax algorithm with alpha-beta pruning
    /// </summary>
    private float Minimax(int depth, float alpha, float beta, bool isMaximizing, PieceColor currentPlayer)
    {
        nodesEvaluated++;
        
        // Base case: depth reached or game over
        if (depth == 0)
        {
            return EvaluatePosition(currentPlayer);
        }
        
        // Check for checkmate/stalemate
        List<AIMove> moves = GetAllPossibleMoves(currentPlayer);
        if (moves.Count == 0)
        {
            // No legal moves - could be checkmate or stalemate
            if (IsPlayerInCheck(currentPlayer))
            {
                // Checkmate - return a score based on depth to prefer faster checkmates
                return isMaximizing ? -CHECKMATE_SCORE + depth : CHECKMATE_SCORE - depth;
            }
            else
            {
                // Stalemate
                return 0;
            }
        }
        
        if (isMaximizing)
        {
            float maxEval = NEGATIVE_INFINITY;
            
            foreach (AIMove move in moves)
            {
                ChessPiece capturedPiece = MakeMove(move);
                float eval = Minimax(depth - 1, alpha, beta, false, GetOppositeColor(currentPlayer));
                UndoMove(move, capturedPiece);
                
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                
                // Alpha-beta pruning
                if (beta <= alpha)
                    break;
            }
            
            return maxEval;
        }
        else
        {
            float minEval = POSITIVE_INFINITY;
            
            foreach (AIMove move in moves)
            {
                ChessPiece capturedPiece = MakeMove(move);
                float eval = Minimax(depth - 1, alpha, beta, true, GetOppositeColor(currentPlayer));
                UndoMove(move, capturedPiece);
                
                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                
                // Alpha-beta pruning
                if (beta <= alpha)
                    break;
            }
            
            return minEval;
        }
    }
    
    /// <summary>
    /// Get all possible legal moves for a player
    /// </summary>
    private List<AIMove> GetAllPossibleMoves(PieceColor player)
    {
        List<AIMove> moves = new List<AIMove>();
        
        if (ChessBoard.Instance == null)
            return moves;
        
        // Scan all board positions for pieces of the given color
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition position = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(position);
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        List<BoardPosition> legalMoves = piece.GetLegalMoves();
                        
                        foreach (BoardPosition movePos in legalMoves)
                        {
                            moves.Add(new AIMove
                            {
                                piece = piece,
                                fromPosition = position,
                                toPosition = movePos,
                                evaluationScore = 0
                            });
                        }
                    }
                }
            }
        }
        
        return moves;
    }
    
    /// <summary>
    /// Make a move temporarily for evaluation (returns captured piece if any)
    /// </summary>
    private ChessPiece MakeMove(AIMove move)
    {
        if (ChessBoard.Instance == null)
            return null;
        
        ChessPiece capturedPiece = ChessBoard.Instance.GetPieceAt(move.toPosition);
        
        // Remove piece from original position
        ChessBoard.Instance.SetPieceAt(move.fromPosition, null);
        
        // Place piece at new position
        ChessBoard.Instance.SetPieceAt(move.toPosition, move.piece);
        
        // Update piece's internal position
        move.piece.SetCurrentPosition(move.toPosition);
        
        return capturedPiece;
    }
    
    /// <summary>
    /// Undo a move (restore previous board state)
    /// </summary>
    private void UndoMove(AIMove move, ChessPiece capturedPiece)
    {
        if (ChessBoard.Instance == null)
            return;
        
        // Remove piece from new position
        ChessBoard.Instance.SetPieceAt(move.toPosition, capturedPiece);
        
        // Restore piece to original position
        ChessBoard.Instance.SetPieceAt(move.fromPosition, move.piece);
        
        // Update piece's internal position
        move.piece.SetCurrentPosition(move.fromPosition);
    }
    
    /// <summary>
    /// Evaluate the current board position for the given player
    /// Positive scores favor the player, negative scores favor the opponent
    /// </summary>
    private float EvaluatePosition(PieceColor player)
    {
        if (ChessBoard.Instance == null)
            return 0;
        
        float score = 0;
        
        // Material evaluation (piece values)
        score += CalculateMaterialScore(player);
        score -= CalculateMaterialScore(GetOppositeColor(player));
        
        // Positional evaluation
        score += CalculatePositionalScore(player);
        score -= CalculatePositionalScore(GetOppositeColor(player));
        
        // King safety evaluation
        score += CalculateKingSafety(player);
        score -= CalculateKingSafety(GetOppositeColor(player));
        
        // Mobility evaluation (number of legal moves)
        score += CalculateMobility(player) * 0.1f;
        score -= CalculateMobility(GetOppositeColor(player)) * 0.1f;
        
        return score;
    }
    
    /// <summary>
    /// Calculate material score based on piece values
    /// </summary>
    private float CalculateMaterialScore(PieceColor player)
    {
        float materialScore = 0;
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        materialScore += GetPieceValue(piece.pieceType);
                    }
                }
            }
        }
        
        return materialScore;
    }
    
    /// <summary>
    /// Get the material value of a piece type
    /// </summary>
    private float GetPieceValue(ChessPieceType pieceType)
    {
        switch (pieceType)
        {
            case ChessPieceType.Pawn:   return 1.0f;
            case ChessPieceType.Knight: return 3.0f;
            case ChessPieceType.Bishop: return 3.0f;
            case ChessPieceType.Rook:   return 5.0f;
            case ChessPieceType.Queen:  return 9.0f;
            case ChessPieceType.King:   return 0.0f; // King safety handled separately
            default: return 0.0f;
        }
    }
    
    /// <summary>
    /// Calculate positional score (piece placement quality)
    /// </summary>
    private float CalculatePositionalScore(PieceColor player)
    {
        float positionalScore = 0;
        
        // 3D Center control - pieces closer to center of 4x4x4 cube are more valuable
        positionalScore += CalculateCenterControl(player);
        
        // Piece coordination - pieces that support each other
        positionalScore += CalculatePieceCoordination(player);
        
        // Vertical layer control - controlling different Y levels
        positionalScore += CalculateLayerControl(player);
        
        return positionalScore;
    }
    
    /// <summary>
    /// Calculate how well pieces control the center of the 3D board
    /// In a 4x4x4 cube, center squares are more valuable
    /// </summary>
    private float CalculateCenterControl(PieceColor player)
    {
        float centerScore = 0;
        
        // Define center and near-center positions in 4x4x4 board
        // True center would be between (1.5, 1.5, 1.5) - not possible on discrete grid
        // So we value positions closer to center more highly
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        // Calculate distance from center (1.5, 1.5, 1.5)
                        float distanceFromCenter = Mathf.Sqrt(
                            Mathf.Pow(x - 1.5f, 2) + 
                            Mathf.Pow(y - 1.5f, 2) + 
                            Mathf.Pow(z - 1.5f, 2)
                        );
                        
                        // Closer to center = higher score (inverse relationship)
                        float centerValue = (3.0f - distanceFromCenter) * 0.1f;
                        centerScore += centerValue;
                    }
                }
            }
        }
        
        return centerScore;
    }
    
    /// <summary>
    /// Calculate how well pieces coordinate and support each other
    /// </summary>
    private float CalculatePieceCoordination(PieceColor player)
    {
        float coordinationScore = 0;
        
        // Count how many friendly pieces each piece can "see" (attack squares)
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        List<BoardPosition> attackSquares = piece.GetAttackSquares();
                        
                        foreach (BoardPosition attackPos in attackSquares)
                        {
                            ChessPiece targetPiece = ChessBoard.Instance.GetPieceAt(attackPos);
                            
                            // Bonus for defending friendly pieces
                            if (targetPiece != null && targetPiece.pieceColor == player)
                            {
                                coordinationScore += 0.1f;
                            }
                        }
                    }
                }
            }
        }
        
        return coordinationScore;
    }
    
    /// <summary>
    /// Calculate control over different vertical layers (Y levels)
    /// Having pieces spread across different layers provides strategic advantage
    /// </summary>
    private float CalculateLayerControl(PieceColor player)
    {
        float layerScore = 0;
        int[] piecesPerLayer = new int[4]; // Count pieces on each Y level
        
        // Count pieces on each layer
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        piecesPerLayer[y]++;
                    }
                }
            }
        }
        
        // Bonus for having pieces on multiple layers (diversity)
        int layersOccupied = 0;
        for (int i = 0; i < 4; i++)
        {
            if (piecesPerLayer[i] > 0) layersOccupied++;
        }
        
        layerScore += layersOccupied * 0.2f; // Bonus for layer diversity
        
        return layerScore;
    }
    
    /// <summary>
    /// Calculate king safety score
    /// </summary>
    private float CalculateKingSafety(PieceColor player)
    {
        float kingSafetyScore = 0;
        
        // Find the king
        ChessPiece king = FindKing(player);
        if (king == null) return -1000; // King missing = game over
        
        BoardPosition kingPos = king.CurrentPosition;
        
        // Count escape squares (empty squares king can move to)
        int escapeSquares = 0;
        List<BoardPosition> kingMoves = king.GetValidMoves();
        
        foreach (BoardPosition move in kingMoves)
        {
            ChessPiece pieceAtMove = ChessBoard.Instance.GetPieceAt(move);
            if (pieceAtMove == null || pieceAtMove.pieceColor != player)
            {
                escapeSquares++;
            }
        }
        
        kingSafetyScore += escapeSquares * 0.1f; // More escape squares = safer
        
        // Penalty for being near edges (easier to checkmate in corners)
        float distanceFromEdge = Mathf.Min(
            Mathf.Min(kingPos.x, 3 - kingPos.x),
            Mathf.Min(kingPos.y, 3 - kingPos.y),
            Mathf.Min(kingPos.z, 3 - kingPos.z)
        );
        
        kingSafetyScore += distanceFromEdge * 0.2f; // Penalty for being near edges
        
        // Count friendly pieces near king (protection)
        int protectors = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue; // Skip king's own square
                    
                    BoardPosition nearPos = new BoardPosition(
                        kingPos.x + dx, kingPos.y + dy, kingPos.z + dz
                    );
                    
                    if (nearPos.IsValid())
                    {
                        ChessPiece nearPiece = ChessBoard.Instance.GetPieceAt(nearPos);
                        if (nearPiece != null && nearPiece.pieceColor == player)
                        {
                            protectors++;
                        }
                    }
                }
            }
        }
        
        kingSafetyScore += protectors * 0.15f; // Bonus for friendly pieces nearby
        
        return kingSafetyScore;
    }
    
    /// <summary>
    /// Find the king of the specified color
    /// </summary>
    private ChessPiece FindKing(PieceColor color)
    {
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    
                    if (piece != null && piece.pieceColor == color && piece.pieceType == ChessPieceType.King)
                    {
                        return piece;
                    }
                }
            }
        }
        
        return null; // King not found (should not happen in normal game)
    }
    
    /// <summary>
    /// Calculate mobility score (number of legal moves)
    /// </summary>
    private float CalculateMobility(PieceColor player)
    {
        return GetAllPossibleMoves(player).Count;
    }
    
    /// <summary>
    /// Check if a player is currently in check
    /// </summary>
    private bool IsPlayerInCheck(PieceColor player)
    {
        if (CheckDetectionManager.Instance == null)
            return false;
        
        return CheckDetectionManager.Instance.IsKingInCheck(player);
    }
    
    /// <summary>
    /// Get the opposite color
    /// </summary>
    private PieceColor GetOppositeColor(PieceColor color)
    {
        return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
    
    /// <summary>
    /// Get statistics about the last search
    /// </summary>
    public int GetNodesEvaluated()
    {
        return nodesEvaluated;
    }
}