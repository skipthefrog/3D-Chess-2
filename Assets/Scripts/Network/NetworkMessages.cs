using System;
using UnityEngine;

namespace ChessNetwork
{
    /// <summary>
    /// Represents a move made by a player in network play
    /// </summary>
    [Serializable]
    public class NetworkMoveData
    {
        public int fromX, fromY, fromZ;
        public int toX, toY, toZ;
        public string playerColor;
        public long timestamp;
        public int moveNumber;
        
        public NetworkMoveData() { }
        
        public NetworkMoveData(BoardPosition from, BoardPosition to, PieceColor color, int moveNum)
        {
            fromX = from.x;
            fromY = from.y;
            fromZ = from.z;
            toX = to.x;
            toY = to.y;
            toZ = to.z;
            playerColor = color.ToString();
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            moveNumber = moveNum;
        }
        
        public BoardPosition GetFromPosition()
        {
            return new BoardPosition(fromX, fromY, fromZ);
        }
        
        public BoardPosition GetToPosition()
        {
            return new BoardPosition(toX, toY, toZ);
        }
        
        public PieceColor GetPlayerColor()
        {
            if (Enum.TryParse<PieceColor>(playerColor, out PieceColor color))
            {
                return color;
            }
            return PieceColor.White; // Default fallback
        }
    }
    
    /// <summary>
    /// Information about a player in the network game
    /// </summary>
    [Serializable]
    public class NetworkPlayerInfo
    {
        public string playerId;
        public string playerName;
        public string assignedColor;
        public bool isHost;
        public bool isReady;
        
        public NetworkPlayerInfo() { }
        
        public NetworkPlayerInfo(string name, bool host = false)
        {
            playerName = name;
            isHost = host;
            isReady = false;
        }
        
        public PieceColor GetAssignedColor()
        {
            if (Enum.TryParse<PieceColor>(assignedColor, out PieceColor color))
            {
                return color;
            }
            return PieceColor.White; // Default fallback
        }
    }
    
    /// <summary>
    /// Complete game state for synchronization
    /// </summary>
    [Serializable]
    public class NetworkGameState
    {
        public string currentPlayer;
        public string gamePhase; // "placement", "playing", "ended"
        public NetworkBoardState board;
        public NetworkMoveData[] moveHistory;
        public int totalMoves;
        
        public GameState GetGamePhase()
        {
            return gamePhase switch
            {
                "placement" => GameState.PiecePlacement,
                "playing" => GameState.Playing,
                "ended" => GameState.GameOver,
                _ => GameState.WaitingForConfiguration
            };
        }
        
        public PieceColor GetCurrentPlayer()
        {
            if (Enum.TryParse<PieceColor>(currentPlayer, out PieceColor color))
            {
                return color;
            }
            return PieceColor.White; // Default fallback
        }
    }
    
    /// <summary>
    /// Simplified board state for network transmission
    /// </summary>
    [Serializable]
    public class NetworkBoardState
    {
        // Simple dictionary representation: "x,y,z" -> piece info
        public NetworkPieceData[] pieces;
        
        [Serializable]
        public class NetworkPieceData
        {
            public int x, y, z;
            public string pieceType;
            public string pieceColor;
            public bool hasMoved;
            
            public BoardPosition GetPosition()
            {
                return new BoardPosition(x, y, z);
            }
            
            public ChessPieceType GetPieceType()
            {
                if (Enum.TryParse<ChessPieceType>(pieceType, out ChessPieceType type))
                {
                    return type;
                }
                return ChessPieceType.Pawn; // Default fallback
            }
            
            public PieceColor GetPieceColor()
            {
                if (Enum.TryParse<PieceColor>(pieceColor, out PieceColor color))
                {
                    return color;
                }
                return PieceColor.White; // Default fallback
            }
        }
    }
    
    /// <summary>
    /// Room information for lobby display
    /// </summary>
    [Serializable]
    public class NetworkRoomData
    {
        public string roomCode;
        public int playerCount;
        public int maxPlayers;
        public string gamePhase;
        public NetworkPlayerInfo[] players;
        public bool isJoinable;
        
        public bool CanJoin()
        {
            return isJoinable && playerCount < maxPlayers && gamePhase != "playing";
        }
    }
    
    /// <summary>
    /// Server response when room is created
    /// </summary>
    [Serializable]
    public class RoomCreatedResponse
    {
        public string roomCode;
        public string playerColor;
        public NetworkGameState gameState;
    }
    
    /// <summary>
    /// Server response when joining a room
    /// </summary>
    [Serializable]
    public class RoomJoinedResponse
    {
        public string roomCode;
        public string playerColor;
        public NetworkGameState gameState;
    }
    
    /// <summary>
    /// Server error message
    /// </summary>
    [Serializable]
    public class NetworkErrorMessage
    {
        public string message;
        public string errorCode;
        public long timestamp;
    }
    
    /// <summary>
    /// Move validation result from server
    /// </summary>
    [Serializable]
    public class MoveValidationResponse
    {
        public bool success;
        public string errorMessage;
        public NetworkGameState updatedGameState;
    }
    
    /// <summary>
    /// Player connection events
    /// </summary>
    [Serializable]
    public class PlayerConnectionEvent
    {
        public NetworkPlayerInfo player;
        public string eventType; // "joined", "left", "disconnected", "reconnected"
        public NetworkGameState gameState;
        public long timestamp;
    }
    
    /// <summary>
    /// Game events (start, end, pause, etc.)
    /// </summary>
    [Serializable]
    public class GameEvent
    {
        public string eventType; // "started", "ended", "paused", "resumed"
        public string triggeredBy; // Player ID who triggered the event
        public NetworkGameState gameState;
        public string additionalData; // JSON string for extra data
        public long timestamp;
    }
    
    /// <summary>
    /// Connection status information
    /// </summary>
    [Serializable]
    public class ConnectionStatus
    {
        public bool isConnected;
        public string serverUrl;
        public int pingMs;
        public string connectionId;
        public long lastHeartbeat;
        public string status; // "connecting", "connected", "disconnected", "error"
    }
}