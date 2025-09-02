# AI Aggression Enhancements

## Overview
Enhanced the AI evaluation system to make it more aggressive and break out of defensive loops where AIs make repetitive, non-progressive moves.

## Problem Analysis
From the logs, the AI was stuck in multi-piece defensive loops with consistently negative evaluation scores (-8 to -18), indicating overly defensive behavior where:
- AIs avoided taking risks
- Moves focused on safety rather than progress
- No incentive to attack or create threats
- Evaluation heavily penalized any forward movement

## Solution Implemented

### 1. Enhanced Main Evaluation Function (EvaluatePosition)
Added two new aggressive components:

```csharp
// AGGRESSIVE: Bonus for attacking moves and threats
score += CalculateAggressionBonus(player);
score -= CalculateAggressionBonus(GetOppositeColor(player));

// AGGRESSIVE: King pressure bonus
score += CalculateKingPressure(player);
score -= CalculateKingPressure(GetOppositeColor(player));
```

### 2. Aggression Bonus System (CalculateAggressionBonus)
- **Attack Bonuses**: 80% of target piece value for threatening enemy pieces
- **King Threat Bonus**: +5.0 points for threatening the enemy king
- **Defended Piece Pressure**: +0.5 points for pressuring even defended pieces
- **Territory Advancement**: 30% piece value bonus for advancing into enemy territory

```csharp
// AGGRESSIVE: Big bonus for attacking valuable pieces
float targetValue = GetPieceValue(targetPiece.pieceType);
aggressionScore += targetValue * 0.8f; // 80% of piece value as attack bonus

// AGGRESSIVE: Extra bonus for attacking the king
if (targetPiece.pieceType == ChessPieceType.King)
{
    aggressionScore += 5.0f; // Big king threat bonus
}
```

### 3. King Pressure System (CalculateKingPressure)
- **Proximity Rewards**: Bonuses for pieces within 3 squares of enemy king
- **Area Control**: +0.8 points for controlling squares adjacent to enemy king
- **Distance Scaling**: Rewards inversely proportional to distance from king

```csharp
// AGGRESSIVE: Bonus inversely proportional to distance from king
if (distance < 3.0f) // Only reward close pieces
{
    float proximityBonus = (3.0f - distance) * 0.4f;
    kingPressureScore += proximityBonus * GetPieceValue(piece.pieceType) * 0.2f;
}
```

### 4. Enhanced Quick Move Evaluation
Made the recovery system more aggressive:
- **Capture Bonus**: 150% of piece value (up from 100%)
- **High-Value Target Bonuses**: +3.0 for Queen captures, +2.0 for Rook captures
- **Territory Advancement**: +1.0 for moving into enemy territory
- **Threat Creation**: 30% piece value for creating threats
- **King Pressure**: Distance-based bonuses for approaching enemy king

### 5. Increased Search Depth
Enhanced tactical vision:
- **Easy**: 1 → 2 depth
- **Medium**: 1 → 2 depth  
- **Hard**: 2 → 3 depth

This allows AI to see 1-2 moves deeper to spot tactical opportunities.

## Key Features

### Territory-Based Aggression
```csharp
private bool IsInOpponentTerritory(BoardPosition pos, PieceColor player)
{
    if (player == PieceColor.White)
    {
        // White advancing to upper levels (Y > 2) is aggressive
        return pos.y >= 2;
    }
    else
    {
        // Black advancing to lower levels (Y < 2) is aggressive  
        return pos.y <= 1;
    }
}
```

### Piece Defense Detection
```csharp
private bool IsPieceDefended(ChessPiece piece)
{
    // Check if any friendly piece can attack this position
    // Enables AI to pressure even defended pieces for positional advantage
}
```

### Multi-Layered Attack Rewards
1. **Direct Attacks**: 80% of piece value
2. **King Threats**: Flat +5.0 bonus
3. **Defended Piece Pressure**: +0.5 bonus
4. **Proximity to King**: Distance-scaled bonuses
5. **Territory Control**: 30% piece value bonuses

## Expected Impact

### Before Enhancement
```
MinimaxEngine: Move Rook (2, 0, 2)→(2, 0, 3) scored -9.80
MinimaxEngine: Move Pawn (2, 1, 0)→(1, 0, 0) scored -18.53
MinimaxEngine: Move Queen (3, 0, 1)→(3, 1, 1) scored -10.80
```

### After Enhancement
```
Expected scores closer to 0 or positive for good moves
Aggressive moves (captures, threats) should score higher
King attacks should get significant bonuses
Territory advancement should be rewarded
```

### Behavioral Changes
- **More Captures**: AI will prioritize taking enemy pieces
- **King Hunting**: AI will actively pressure the enemy king
- **Territory Expansion**: AI will advance pieces into enemy territory
- **Threat Creation**: AI will create threats even without immediate captures
- **Breaking Loops**: Aggressive bonuses should break defensive stalemates

## Technical Details

### Performance Considerations
- Added functions are O(n²) where n=64 board positions
- Cached evaluations minimize performance impact
- Enhanced search depth balanced with time limits

### Compatibility
- ✅ Compatible with existing validation system
- ✅ Compatible with anti-repetition system  
- ✅ Works with error recovery mechanisms
- ✅ Maintains move legality checks

## Testing Recommendations

1. **AI vs AI Games**: Monitor for more dynamic, attacking gameplay
2. **Score Analysis**: Verify moves now get positive/neutral scores instead of all negative
3. **Loop Breaking**: Confirm AIs break out of repetitive patterns
4. **Capture Rate**: Measure increase in capture frequency
5. **King Safety**: Ensure AIs still protect their own kings while attacking

## Files Modified

1. **MinimaxEngine.cs**
   - Enhanced `EvaluatePosition()` (lines 378-413)
   - Added `CalculateAggressionBonus()` (lines 895-948)
   - Added `CalculateKingPressure()` (lines 954-1009)
   - Enhanced `EvaluateMoveQuickly()` (lines 871-934)
   - Added utility functions for aggression detection

2. **AIPlayer.cs**
   - Increased search depths (lines 567-573)

## Configuration Options

The aggression system can be tuned by adjusting these values:
- **Attack bonus multiplier**: Currently 0.8f (80% of piece value)
- **King threat bonus**: Currently 5.0f
- **Territory bonus**: Currently 0.3f (30% of piece value)  
- **King pressure radius**: Currently 3.0f squares
- **Area control bonus**: Currently 0.8f per controlled square

## Conclusion

These enhancements should transform the AI from defensive and repetitive to aggressive and dynamic, creating more engaging and progressive gameplay while maintaining strategic soundness.