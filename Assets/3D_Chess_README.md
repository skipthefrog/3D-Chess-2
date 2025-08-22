# 3D Chess Game - iPhone Implementation

## Overview
This is a 3D chess game built for iPhone using Unity 6. The game features a unique 4x4x4 cube board where pieces can move in three dimensions, creating fascinating new strategic possibilities.

## Architecture

### Core Components

#### 1. Board System (`Assets/Scripts/Board/`)
- **BoardPosition.cs**: Struct representing 3D coordinates (x, y, z) from 0-3
- **ChessBoard.cs**: Main board manager creating and managing the 4x4x4 grid
- **BoardCell.cs**: Individual cell component for interaction

#### 2. Piece System (`Assets/Scripts/Pieces/`)
- **ChessPiece.cs**: Abstract base class for all pieces
- **TestPiece.cs**: Simple test piece that moves like a 3D king

#### 3. Input System (`Assets/Scripts/Input/`)
- **InputManager.cs**: Handles touch input and piece selection using raycasting

#### 4. Game Management
- **GameManager.cs**: Coordinates all systems and sets up test pieces
- **CameraController.cs**: Touch-based orbit camera for 3D viewing

#### 5. Testing (`Assets/Scripts/Testing/`)
- **ChessBoardTester.cs**: Automated tests for board functionality
- **SceneSetup.cs**: Automatic scene configuration

## Getting Started

### 1. Scene Setup
1. Open the `SampleScene` in Unity
2. Add the `SceneSetup` script to any GameObject
3. The script will automatically configure the scene with all required components

### 2. Manual Setup (Alternative)
1. Create an empty GameObject and add `GameManager` script
2. The GameManager will create the chess board and input manager
3. Add `CameraController` to your main camera
4. Add `ChessBoardTester` to any GameObject to run validation tests

### 3. iOS Build Configuration
- The project is already configured for iOS in Unity 6
- Universal Render Pipeline (URP) is set up for mobile optimization
- Input System package handles touch controls

## Features

### Current Implementation
- ✅ 4x4x4 cube chess board (64 positions)
- ✅ 3D coordinate system with validation
- ✅ Visual board with alternating colors and transparency
- ✅ Abstract piece system with movement validation
- ✅ Touch input with raycasting for piece selection
- ✅ Camera orbit controls (touch drag and pinch zoom)
- ✅ Test pieces with basic 3D movement
- ✅ Automated testing framework

### 3D Movement Rules
The 4x4x4 board allows movement in three dimensions:
- **X-axis**: Left/Right (0-3)
- **Y-axis**: Up/Down (0-3) 
- **Z-axis**: Forward/Back (0-3)

Pieces can move through the cube, creating unique strategic layers where pieces can be blocked by others on intermediate levels.

## Controls

### Touch Controls (iOS)
- **Single Touch Drag**: Rotate camera around the board
- **Pinch**: Zoom in/out
- **Tap Piece**: Select piece (shows valid moves)
- **Tap Square**: Move selected piece or select piece at that position

### Editor Controls (Testing)
- **Mouse Drag**: Rotate camera
- **Scroll Wheel**: Zoom
- **Click**: Select pieces/squares
- **Escape**: Clear selection

## Testing

### Automated Tests
Run `ChessBoardTester.RunAllTests()` to validate:
- Board position validation
- Coordinate conversion
- Board cell generation  
- Piece placement
- Movement validation

### Manual Testing
1. Select a test piece (capsule-shaped objects)
2. Valid moves will be highlighted in yellow
3. Tap a highlighted square to move the piece
4. Test both white and black pieces

## Code Structure

### Key Classes
```csharp
// 3D position on the board
BoardPosition pos = new BoardPosition(x, y, z);

// Get piece at position
ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);

// Move piece
ChessBoard.Instance.MovePiece(fromPos, toPos);

// Get valid moves for a piece
List<BoardPosition> moves = piece.GetValidMoves();
```

### Adding New Piece Types
1. Inherit from `ChessPiece`
2. Override `GetValidMoves()` method
3. Implement 3D movement logic
4. Add visual representation

Example:
```csharp
public class Rook3D : ChessPiece 
{
    public override List<BoardPosition> GetValidMoves() 
    {
        List<BoardPosition> moves = new List<BoardPosition>();
        
        // Move along X, Y, Z axes (3D rook movement)
        moves.AddRange(GetValidMovesInDirection(Vector3Int.right));
        moves.AddRange(GetValidMovesInDirection(Vector3Int.left));
        moves.AddRange(GetValidMovesInDirection(Vector3Int.up));
        moves.AddRange(GetValidMovesInDirection(Vector3Int.down));
        moves.AddRange(GetValidMovesInDirection(Vector3Int.forward));
        moves.AddRange(GetValidMovesInDirection(Vector3Int.back));
        
        return moves;
    }
}
```

## iOS Build Instructions

1. **Build Settings**:
   - Platform: iOS
   - Architecture: ARM64
   - Target minimum iOS version: 13.0

2. **Player Settings** (already configured):
   - Bundle identifier: `com.UnityTechnologies.UniversalMobile3DTemplate`
   - Orientation: All orientations supported
   - Metal rendering API

3. **Build Process**:
   - File → Build Settings → iOS
   - Click "Build" and choose output folder
   - Open generated Xcode project
   - Connect iOS device and build to device

## Performance Notes

- Uses URP for mobile optimization
- Semi-transparent board materials for visual clarity
- Efficient raycasting for touch input
- Object pooling ready for piece animations

## Future Enhancements

Potential additions:
- Traditional chess pieces with 3D movement rules
- AI opponent
- Multiplayer support
- Sound effects and particle systems
- Different board themes
- Save/load game state
- Move history and replay

## Troubleshooting

### Common Issues
1. **Board not appearing**: Check that GameManager is running and ChessBoard.Instance exists
2. **Touch not working**: Verify InputManager is active and camera has correct layers
3. **Pieces not moving**: Ensure pieces have colliders and are on correct layer
4. **Camera not orbiting**: Check that CameraController target is set to board

### Debug Information
Enable verbose logging in ChessBoardTester to see detailed test results and system status.