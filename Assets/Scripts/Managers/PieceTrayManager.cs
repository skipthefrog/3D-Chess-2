using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages piece trays for all player colors in scalable 3D chess.
/// Provides centralized access to trays for any number of players (2-6).
/// </summary>
public class PieceTrayManager : MonoBehaviour
{
    public static PieceTrayManager Instance { get; private set; }

    [Header("Tray Registry")]
    private Dictionary<PieceColor, PieceTray> trays = new Dictionary<PieceColor, PieceTray>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PieceTrayManager: Instance created");
        }
        else
        {
            Debug.LogWarning("PieceTrayManager: Duplicate instance detected, destroying");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Register a tray for a specific player color
    /// Called by PieceTray.Awake() when trays are created
    /// </summary>
    public void RegisterTray(PieceColor color, PieceTray tray)
    {
        if (tray == null)
        {
            Debug.LogError($"PieceTrayManager: Attempted to register null tray for {color}");
            return;
        }

        if (trays.ContainsKey(color))
        {
            Debug.LogWarning($"PieceTrayManager: Tray for {color} already registered, replacing");
            trays[color] = tray;
        }
        else
        {
            trays[color] = tray;
            Debug.Log($"PieceTrayManager: Registered {color} tray");
        }
    }

    /// <summary>
    /// Unregister a tray for a specific player color
    /// Called by PieceTray.OnDestroy() when trays are destroyed
    /// </summary>
    public void UnregisterTray(PieceColor color)
    {
        if (trays.ContainsKey(color))
        {
            trays.Remove(color);
            Debug.Log($"PieceTrayManager: Unregistered {color} tray");
        }
    }

    /// <summary>
    /// Get the tray for a specific player color
    /// </summary>
    public PieceTray GetTray(PieceColor color)
    {
        if (trays.ContainsKey(color))
        {
            return trays[color];
        }

        Debug.LogWarning($"PieceTrayManager: No tray registered for {color}");
        return null;
    }

    /// <summary>
    /// Check if a tray is registered for a specific color
    /// </summary>
    public bool HasTray(PieceColor color)
    {
        return trays.ContainsKey(color) && trays[color] != null;
    }

    /// <summary>
    /// Get all registered trays
    /// </summary>
    public Dictionary<PieceColor, PieceTray> GetAllTrays()
    {
        return new Dictionary<PieceColor, PieceTray>(trays);
    }

    /// <summary>
    /// Get the number of registered trays
    /// </summary>
    public int GetTrayCount()
    {
        return trays.Count;
    }

    /// <summary>
    /// Check if all trays for active players are registered
    /// </summary>
    public bool AllTraysRegistered()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogWarning("PieceTrayManager: Cannot check trays - PlayerManager not available");
            return false;
        }

        List<PieceColor> activePlayers = PlayerManager.Instance.GetAllPlayers();

        foreach (PieceColor color in activePlayers)
        {
            if (!HasTray(color))
            {
                Debug.LogWarning($"PieceTrayManager: Missing tray for active player {color}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get total piece count across all registered trays
    /// </summary>
    public int GetTotalPieceCount()
    {
        int total = 0;
        foreach (var tray in trays.Values)
        {
            if (tray != null)
            {
                total += tray.GetPieceCount();
            }
        }
        return total;
    }

    /// <summary>
    /// Check if all trays are empty
    /// </summary>
    public bool AllTraysEmpty()
    {
        foreach (var tray in trays.Values)
        {
            if (tray != null && tray.GetPieceCount() > 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Clear all tray registrations (for cleanup/reset)
    /// </summary>
    public void ClearTrays()
    {
        trays.Clear();
        Debug.Log("PieceTrayManager: All tray registrations cleared");
    }

    /// <summary>
    /// Get debug information about registered trays
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"PieceTrayManager: {trays.Count} trays registered\n";

        foreach (var kvp in trays)
        {
            PieceColor color = kvp.Key;
            PieceTray tray = kvp.Value;

            if (tray != null)
            {
                int pieceCount = tray.GetPieceCount();
                info += $"  {color}: {pieceCount} pieces\n";
            }
            else
            {
                info += $"  {color}: NULL tray\n";
            }
        }

        return info;
    }
}
