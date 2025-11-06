# Testing Instructions - Tray Instance Desync Diagnosis

## What Changed
I've added instance ID logging to both AIPlayer.cs and PlacementManager.cs to diagnose why they see different piece counts for the same tray.

## Changes Made
1. **PlacementManager.cs (lines 2364-2390)**: Modified `IsPlayerPlacementCompleted()` to:
   - Use `PieceTrayManager.GetTray()` directly (instead of `PieceTray.GetTrayForColor()`)
   - Log the tray instance ID when checking placement completion
   
2. **AIPlayer.cs (lines 192, 207)**: Added instance ID logging when accessing trays

## How to Test
1. **Recompile Unity**: Open Unity Editor and wait for scripts to compile
2. **Start 4-player AI game**: Configure a 6x6x6 board with 4 AI players
3. **Watch the logs**: Pay special attention to Purple and Green players

## What to Look For in Logs

### Key Log Lines
Look for these patterns in the console output:

```
🔍 AIPlayer.RequestPlacement: Using PieceTrayManager.GetTray(Purple) - tray instance ID: XXXXX
📦 AIPlayer: Purple tray (instance XXXXX) has N pieces available for placement
🔍 IsPlayerPlacementCompleted: Using PieceTrayManager.GetTray(Purple) - tray instance ID: YYYYY
PlacementManager: Purple placement completion check - tray instance YYYYY, pieces in tray: M, completed: ...
```

### What We Need to Determine

**SCENARIO A: Same Instance ID (XXXXX == YYYYY)**
- AIPlayer and PlacementManager are accessing the SAME tray object
- Problem is with how pieces are counted within that single tray
- Next step: Investigate tray's internal `piecesInTray` list management

**SCENARIO B: Different Instance IDs (XXXXX != YYYYY)**
- AIPlayer and PlacementManager are accessing DIFFERENT tray objects
- Problem is with PieceTrayManager registration/retrieval
- Next step: Debug why PieceTrayManager returns different instances

### Expected Behavior (for comparison)
- White player should show SAME instance ID in both AIPlayer and PlacementManager
- White tray count should decrement from 8 to 0 successfully
- Purple/Green should behave the same way but currently fail

## Collect Full Logs
Please run the test and provide:
1. Complete console log output
2. Focus on Purple player's placement sequence
3. Include all instance ID logs from both AIPlayer and PlacementManager

## If Instance IDs Are DIFFERENT
This would explain the desync - somehow Purple/Green are getting different tray instances registered. We'd need to investigate:
- PieceTrayManager.RegisterTray() calls
- When/how trays are created for multi-player games
- Static property initialization order

## If Instance IDs Are SAME
The tray object is correct, but piece tracking is broken. We'd need to investigate:
- RemovePiece() not being called (already know this from previous logs)
- Why isRepositioningPiece was true for Purple/Green
- Whether the repositioning fix actually resolved the issue
