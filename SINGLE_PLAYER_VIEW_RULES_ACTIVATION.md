# Single-Player View Rules System - Activation Guide

## Overview

The single-player view rules system for online multiplayer 3D Chess is now fully implemented and ready for activation. This system provides:

1. **Mini Tray UI** - Bottom screen overlay showing current player's available pieces during placement
2. **Placement Visibility Rules** - Players only see their own pieces during placement phase in online games
3. **Automatic Game Mode Detection** - System activates only for online games, preserving local gameplay

## System Architecture

### Core Components

1. **GameConfiguration.Instance** - Global singleton for game settings
2. **NetworkManager.Instance** - Handles online game state and player assignments
3. **MiniTrayUI** - Displays piece tray at bottom of screen during placement
4. **PlacementVisibilityManager** - Controls piece visibility during placement phase
5. **PieceSpriteManager** - Provides piece icons for UI elements
6. **UIManager** - Coordinates all UI components

### Integration Points

The system is automatically initialized at these key points:

1. **SceneController.cs** - Sets GameConfiguration.Instance when applying game configurations
2. **GameManager.cs** - Creates fallback configurations for direct scene loading
3. **MainMenuController.cs** - Sets GameConfiguration.Instance when starting games
4. **UIManager.cs** - Creates MiniTrayUI and PieceSpriteManager components

## How It Works

### For Online Games

1. **Game Start**: GameConfiguration.Instance is set with `gameMode = OnlineHost/OnlineClient`
2. **NetworkManager**: Created and configured with appropriate player color assignment
3. **UI Components**: MiniTrayUI and related systems are created by UIManager
4. **Placement Phase**: 
   - MiniTrayUI appears at bottom showing current player's pieces
   - PlacementVisibilityManager hides opponent pieces from view
   - Players can only see and place their own pieces
5. **Gameplay Phase**: All pieces become visible, mini tray disappears

### For Local Games

1. **Game Start**: GameConfiguration.Instance is set with `gameMode = Local`
2. **NetworkManager**: Not created (not needed for local games)
3. **UI Components**: MiniTrayUI exists but remains hidden
4. **All Phases**: Normal gameplay with full piece visibility (no changes to existing behavior)

## Activation Status

✅ **COMPLETE** - The system is fully implemented and ready to use!

### What's Working

- [x] GameConfiguration singleton pattern with SetInstance() method
- [x] NetworkManager integration with proper color assignment
- [x] MiniTrayUI component with canvas overlay and piece icons
- [x] PlacementVisibilityManager with online single-player visibility rules
- [x] PieceSpriteManager with piece sprite caching system
- [x] UIManager integration with proper component lifecycle
- [x] Automatic game mode detection (online vs local)
- [x] Event-driven architecture for real-time updates
- [x] Proper initialization at all entry points (menu, scene loading, direct loading)

### Initialization Points

The system is automatically initialized at these locations:

1. **SceneController.ApplyConfigurationDelayed()** (lines 290-315)
   - Sets GameConfiguration.Instance
   - Initializes NetworkManager for online games

2. **GameManager.InitializeGameDelayed()** (lines 178-200) 
   - Creates fallback GameConfiguration for direct scene loads
   - Initializes fallback NetworkManager if needed

3. **MainMenuController.StartLocalGameFlow()** (lines 2971-2983)
   - Sets GameConfiguration.Instance for local games

4. **MainMenuController.StartOnlineGameFlow()** (lines 2988-2999)
   - Sets GameConfiguration.Instance for online games

5. **UIManager.InitializeUIComponents()** (lines 70-108)
   - Creates PieceSpriteManager and MiniTrayUI components

## Testing

### Manual Testing

1. Add the `SinglePlayerViewRulesTest` component to any GameObject in your scene
2. Check "Run Test On Start" in the inspector
3. Play the scene to see comprehensive test results

### Console Testing

Use these commands in the Unity console:

```csharp
// Test local game
var localConfig = new GameConfiguration { gameMode = GameMode.Local };
GameConfiguration.SetInstance(localConfig);

// Test online game
var onlineConfig = new GameConfiguration { gameMode = GameMode.OnlineHost };
GameConfiguration.SetInstance(onlineConfig);

// Check system status
Debug.Log($"GameConfig: {GameConfiguration.Instance?.gameMode}");
Debug.Log($"NetworkManager: {NetworkManager.Instance?.AssignedColor}");
Debug.Log($"MiniTrayUI: {MiniTrayUI.Instance != null}");
```

### Expected Behavior

**Online Games:**
- Mini tray should appear at bottom during placement phase
- Should show current player's available pieces as clickable icons
- Opponent pieces should be hidden during placement
- System should activate only when GameConfiguration.gameMode is OnlineHost or OnlineClient

**Local Games:**
- No visual changes to existing gameplay
- Mini tray should remain hidden
- All pieces visible during all phases
- System should remain inactive when GameConfiguration.gameMode is Local

## Troubleshooting

### Common Issues

1. **GameConfiguration.Instance is null**
   - Check that GameConfiguration.SetInstance() is being called in menu flows
   - Verify SceneController is properly applying configurations

2. **NetworkManager.Instance is null**
   - Expected for local games
   - For online games, check SceneController.InitializeNetworkManager()

3. **MiniTrayUI not appearing**
   - Check GameStateManager.currentState == GameState.PiecePlacement
   - Verify GameConfiguration.Instance.gameMode is OnlineHost or OnlineClient
   - Check UIManager created MiniTrayUI component

4. **Piece sprites not showing**
   - Verify PieceSpriteManager.Instance exists
   - Check that piece sprites are assigned in Unity Inspector
   - PieceSpriteManager will create default sprites if none assigned

### Debug Logging

The system includes comprehensive debug logging with emojis for easy identification:

- 🌐 NetworkManager and online game logs
- 🏠 Local game logs  
- 🎮 Game state and UI logs
- ✅ Success indicators
- ❌ Error indicators
- ⚠️ Warning indicators

## Next Steps

The system is complete and ready for use! To activate:

1. **No code changes needed** - System is fully implemented
2. **Test with your online multiplayer games** - Start an online game and verify mini tray appears during placement
3. **Assign piece sprites** - In Unity Inspector, assign actual piece sprites to PieceSpriteManager for better visuals
4. **Customize if needed** - Adjust mini tray positioning, colors, or behavior in MiniTrayUI component

## Files Modified/Created

### Enhanced Files
- `SceneController.cs` - Added GameConfiguration.SetInstance() and NetworkManager initialization
- `GameManager.cs` - Added fallback GameConfiguration creation and NetworkManager setup
- `MainMenuController.cs` - Added GameConfiguration.SetInstance() calls in game start flows
- `UIManager.cs` - Already had MiniTrayUI and PieceSpriteManager integration
- `GameConfiguration.cs` - Already had singleton pattern with SetInstance() method

### New Files
- `MiniTrayUI.cs` - Mini tray component for bottom screen overlay
- `PlacementVisibilityManager.cs` - Enhanced with online single-player visibility rules
- `PieceSpriteManager.cs` - Piece sprite management for UI elements
- `SinglePlayerViewRulesTest.cs` - Comprehensive testing component

The system is production-ready! 🎉