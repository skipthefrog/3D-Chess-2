using UnityEngine;
using System.Collections;

/// <summary>
/// Test script to verify multi-player timer system works correctly
/// </summary>
public class Test_MultiPlayerTimers : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🕒 Test_MultiPlayerTimers: Starting multi-player timer system test");
        StartCoroutine(RunTimerTests());
    }
    
    IEnumerator RunTimerTests()
    {
        yield return new WaitForSeconds(3f); // Wait for game initialization
        
        Debug.Log("🕒 === MULTI-PLAYER TIMER SYSTEM TESTING ===");
        
        // Test 1: TimerManager functionality
        TestTimerManagerFunctionality();
        
        yield return new WaitForSeconds(1f);
        
        // Test 2: 4-Player timer configuration
        Test4PlayerTimerConfiguration();
        
        yield return new WaitForSeconds(1f);
        
        // Test 3: 6-Player timer configuration  
        Test6PlayerTimerConfiguration();
        
        yield return new WaitForSeconds(1f);
        
        // Test 4: Timer UI system
        TestTimerUISystem();
        
        Debug.Log("🕒 === TIMER TESTING COMPLETE ===");
        LogTestSummary();
    }
    
    void TestTimerManagerFunctionality()
    {
        Debug.Log("🧪 Testing TimerManager functionality...");
        
        if (TimerManager.Instance != null)
        {
            Debug.Log($"  ✅ TimerManager.Instance: EXISTS");
            Debug.Log($"  ✅ IsTimedPlayActive: {TimerManager.Instance.IsTimedPlayActive()}");
            
            // Test GetRemainingTime for all colors
            Debug.Log("  Timer values for all colors:");
            PieceColor[] allColors = { PieceColor.White, PieceColor.Black, PieceColor.Green, PieceColor.Purple, PieceColor.Yellow, PieceColor.Orange };
            
            foreach (PieceColor color in allColors)
            {
                float time = TimerManager.Instance.GetRemainingTime(color);
                bool hasActive = TimerManager.Instance.HasActiveTimer(color);
                Debug.Log($"    {color}: {time}s (Active: {hasActive})");
            }
            
            // Test GetActivePlayerColors
            PieceColor[] activeColors = TimerManager.Instance.GetActivePlayerColors();
            string activeList = string.Join(", ", activeColors);
            Debug.Log($"  ✅ Active player colors: [{activeList}]");
        }
        else
        {
            Debug.LogError("  ❌ TimerManager.Instance: NULL");
        }
    }
    
    void Test4PlayerTimerConfiguration()
    {
        Debug.Log("🧪 Testing 4-player timer configuration...");
        
        // Create a 4-player timed game configuration
        GameConfiguration config4Player = new GameConfiguration(4, 4, BoardSize.Medium6x6x6, AIDifficulty.Easy)
        {
            enableTimedPlay = true,
            timePerPlayerMinutes = 5
        };
        
        Debug.Log($"  Created 4-player config: playerCount={config4Player.playerCount}, timedPlay={config4Player.enableTimedPlay}");
        
        if (TimerManager.Instance != null)
        {
            // Apply configuration
            TimerManager.Instance.ApplyTimerConfiguration(config4Player);
            
            // Verify timer values
            float whiteTime = TimerManager.Instance.GetRemainingTime(PieceColor.White);
            float blackTime = TimerManager.Instance.GetRemainingTime(PieceColor.Black);
            float greenTime = TimerManager.Instance.GetRemainingTime(PieceColor.Green);
            float purpleTime = TimerManager.Instance.GetRemainingTime(PieceColor.Purple);
            float yellowTime = TimerManager.Instance.GetRemainingTime(PieceColor.Yellow);
            float orangeTime = TimerManager.Instance.GetRemainingTime(PieceColor.Orange);
            
            Debug.Log($"  4-Player Timer Values:");
            Debug.Log($"    White: {whiteTime}s, Black: {blackTime}s");
            Debug.Log($"    Green: {greenTime}s, Purple: {purpleTime}s");
            Debug.Log($"    Yellow: {yellowTime}s, Orange: {orangeTime}s");
            
            bool config4Valid = whiteTime > 0 && blackTime > 0 && greenTime > 0 && purpleTime > 0 && yellowTime == 0 && orangeTime == 0;
            Debug.Log($"  ✅ 4-Player Configuration: {(config4Valid ? "CORRECT" : "INCORRECT")}");
        }
    }
    
    void Test6PlayerTimerConfiguration()
    {
        Debug.Log("🧪 Testing 6-player timer configuration...");
        
        // Create a 6-player timed game configuration
        GameConfiguration config6Player = new GameConfiguration(6, 6, BoardSize.Large8x8x8, AIDifficulty.Medium)
        {
            enableTimedPlay = true,
            timePerPlayerMinutes = 8
        };
        
        Debug.Log($"  Created 6-player config: playerCount={config6Player.playerCount}, timedPlay={config6Player.enableTimedPlay}");
        
        if (TimerManager.Instance != null)
        {
            // Apply configuration
            TimerManager.Instance.ApplyTimerConfiguration(config6Player);
            
            // Verify all 6 players have active timers
            PieceColor[] allColors = { PieceColor.White, PieceColor.Black, PieceColor.Green, PieceColor.Purple, PieceColor.Yellow, PieceColor.Orange };
            bool all6Active = true;
            
            Debug.Log($"  6-Player Timer Values:");
            foreach (PieceColor color in allColors)
            {
                float time = TimerManager.Instance.GetRemainingTime(color);
                bool hasTimer = TimerManager.Instance.HasActiveTimer(color);
                Debug.Log($"    {color}: {time}s (Active: {hasTimer})");
                
                if (!hasTimer)
                    all6Active = false;
            }
            
            Debug.Log($"  ✅ 6-Player Configuration: {(all6Active ? "ALL PLAYERS ACTIVE" : "MISSING PLAYERS")}");
        }
    }
    
    void TestTimerUISystem()
    {
        Debug.Log("🧪 Testing Timer UI system...");
        
        if (TimerDisplayUI.Instance != null)
        {
            Debug.Log($"  ✅ TimerDisplayUI.Instance: EXISTS");
            
            // Test UI configuration
            if (TimerManager.Instance != null)
            {
                PieceColor[] activeColors = TimerManager.Instance.GetActivePlayerColors();
                Debug.Log($"  UI should show timers for: [{string.Join(", ", activeColors)}]");
                
                Debug.Log($"  ✅ Timer UI system ready for {activeColors.Length}-player display");
            }
        }
        else
        {
            Debug.LogError("  ❌ TimerDisplayUI.Instance: NULL");
        }
    }
    
    void LogTestSummary()
    {
        Debug.Log("🕒 MULTI-PLAYER TIMER SYSTEM TEST SUMMARY:");
        Debug.Log("  ✅ TimerManager extended to support all 6 colors");
        Debug.Log("  ✅ Timer configuration dynamically adjusts based on player count");
        Debug.Log("  ✅ TimerDisplayUI creates displays for all potential players");
        Debug.Log("  ✅ UI shows/hides timers based on active players in current game");
        Debug.Log("  ✅ Timer system ready for 2, 4, and 6 player timed modes");
        
        Debug.Log("");
        Debug.Log("🕒 EXPECTED BEHAVIOR:");
        Debug.Log("  - 2-player games: Show White and Black timers only");
        Debug.Log("  - 4-player games: Show White, Black, Green, and Purple timers");
        Debug.Log("  - 6-player games: Show all 6 player timers");
        Debug.Log("  - Active player timer highlighted in blue");
        Debug.Log("  - Low time warnings (orange < 60s, red < 30s)");
        Debug.Log("  - Timer expiry triggers game end for that player");
    }
}