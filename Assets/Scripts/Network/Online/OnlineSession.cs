/// <summary>
/// Whether the current game is an online game, which side this device plays, and
/// whether a change being applied came from the other player (so it isn't sent back).
/// Game code checks these to block input for the remote side and to report local actions.
/// </summary>
public static class OnlineSession
{
    public static bool IsActive { get; private set; }
    public static PieceColor LocalColor { get; private set; }

    /// <summary>True while applying a move/placement received from the server</summary>
    public static bool ApplyingRemote { get; set; }

    public static void Begin(PieceColor localColor)
    {
        IsActive = true;
        LocalColor = localColor;
        ApplyingRemote = false;
    }

    public static void End()
    {
        IsActive = false;
        ApplyingRemote = false;
    }

    /// <summary>Online: only this device's side may be controlled locally</summary>
    public static bool CanControl(PieceColor color) => !IsActive || ApplyingRemote || color == LocalColor;

    /// <summary>Report this local action to the server?</summary>
    public static bool ShouldSend(PieceColor color) =>
        IsActive && !ApplyingRemote && color == LocalColor && OnlineClient.Instance != null;
}
