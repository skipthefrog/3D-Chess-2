# Network Test Scene - Setup Guide

## Overview
This guide will help you set up and test the online multiplayer networking system for 3D Chess.

## What We've Built

### ✅ Server-Side (Complete)
- **database.js** - User accounts, friends, match history
- **friendSystem.js** - Friend requests and online status
- **matchmaking.js** - Automatic player matching
- **server-enhanced.js** - Complete API with lobby, matchmaking, friends

### ✅ Unity Client (Core Complete)
- **SocketIOClient.cs** - Native cross-platform Socket.IO client
- **NetworkManager.cs** - Fully integrated with real Socket.IO
- **NetworkTestUI.cs** - Test UI for verifying connection

## Setup Instructions

### Step 1: Start the Server

```bash
cd "3D Chess 2/server"
node server-enhanced.js
```

You should see:
```
📊 Database loaded: X users, X friend lists
🎯 Matchmaking system started
✅ All systems initialized: Database, Friends, Matchmaking
🚀 3D Chess Server listening on port 3000
```

### Step 2: Create the Test Scene in Unity

1. **Create a new scene:**
   - File → New Scene
   - Save as `Assets/Scenes/NetworkTestScene.unity`

2. **Add NetworkManager:**
   - Create Empty GameObject, name it "NetworkManager"
   - Add Component → Scripts → Network → NetworkManager
   - In Inspector, configure:
     - Server URL: `http://localhost:3000`
     - Use Production URL: ✅ (unchecked)
     - Auto Connect: ❌ (unchecked)
     - Enable Debug Logging: ✅ (checked)

3. **Create Test UI Canvas:**
   - Create → UI → Canvas
   - Canvas Scaler → UI Scale Mode: "Scale With Screen Size"
   - Reference Resolution: 1920x1080

4. **Add UI Elements:**

   **Status Panel (Top):**
   - Create UI → Panel, name it "StatusPanel"
   - Anchor: Top
   - Add Text (TextMeshPro): "Status Text"
     - Text: "🔴 DISCONNECTED"
     - Font Size: 36
     - Alignment: Center

   **Control Panel (Center):**
   - Create UI → Panel, name it "ControlPanel"
   - Add these UI elements:

   **Input Fields:**
   ```
   - Server URL Input (default: http://localhost:3000)
   - Player Name Input (default: TestPlayer_####)
   - Room Code Input (for joining rooms)
   ```

   **Buttons:**
   ```
   - Connect Button
   - Disconnect Button
   - Create Room Button
   - Join Room Button
   ```

   **Log Panel (Bottom):**
   - Create UI → Panel, name it "LogPanel"
   - Add Scroll View
   - Inside Scroll View → Viewport → Content:
     - Add Text (TextMeshPro): "Log Text"
     - Font Size: 14
     - Alignment: Top-Left
     - Enable Rich Text

5. **Attach NetworkTestUI Script:**
   - Create Empty GameObject, name it "NetworkTestController"
   - Add Component → Scripts → Network → NetworkTestUI
   - Drag all UI elements to corresponding fields in Inspector:
     - Status Text → statusText
     - Log Text → logText
     - Server URL Input → serverUrlInput
     - Player Name Input → playerNameInput
     - Room Code Input → roomCodeInput
     - Connect Button → connectButton
     - Disconnect Button → disconnectButton
     - Create Room Button → createRoomButton
     - Join Room Button → joinRoomButton
     - Scroll Rect → logScrollRect

### Step 3: Test the Connection

1. **Start the server** (if not already running)
2. **Enter Play Mode** in Unity
3. **Click "Connect"** button
4. **Watch the log panel** for connection messages

**Expected Log Output:**
```
[HH:MM:SS] 🎮 Network Test UI Initialized
[HH:MM:SS] 📡 Server: http://localhost:3000
[HH:MM:SS] 🔌 Connecting to http://localhost:3000...
[HH:MM:SS] ✅ Connected to server!
```

**Server Console Output:**
```
🔌 Player connected: TestPlayer_XXXX (socket_id) from ::1 - Client: Unity
👤 User registered: TestPlayer_XXXX (ID: user_uuid)
```

### Step 4: Test Room Creation

1. **Click "Create Room"** button
2. **Check the log** for room code

**Expected Output:**
```
[HH:MM:SS] 🏠 Creating room as TestPlayer_XXXX...
[HH:MM:SS] ✅ Room Created! Code: ABC123
[HH:MM:SS] 🎨 Assigned Color: White
```

The room code will automatically populate the "Room Code" input field.

### Step 5: Test Room Joining (Two Clients)

1. **Build the game** or open a second Unity Editor instance
2. **Start first client:**
   - Connect
   - Create Room
   - Note the room code

3. **Start second client:**
   - Connect
   - Enter room code
   - Click "Join Room"

**Expected Output (Client 2):**
```
[HH:MM:SS] 🚪 Joining room ABC123 as TestPlayer_YYYY...
[HH:MM:SS] ✅ Joined Room: ABC123
[HH:MM:SS] 🎨 Assigned Color: Black
```

**Expected Output (Client 1):**
```
[HH:MM:SS] 👤 Player Joined: TestPlayer_YYYY (Black)
```

## Troubleshooting

### Connection Failed
**Problem:** "Connection failed: timeout" or "Cannot connect"

**Solutions:**
1. Verify server is running: `curl http://localhost:3000/api/server/status`
2. Check firewall settings
3. Try `http://127.0.0.1:3000` instead of `localhost`
4. Check Unity console for errors

### Room Creation Fails
**Problem:** "Room error: Not connected"

**Solutions:**
1. Ensure you're connected first (green status)
2. Check server console for errors
3. Verify server has no crashes

### Messages Not Appearing
**Problem:** No log messages in UI

**Solutions:**
1. Check that NetworkTestUI script is attached
2. Verify all UI elements are properly linked in Inspector
3. Check Unity console for NullReferenceExceptions

### Server Errors
**Problem:** Server crashes or shows errors

**Solutions:**
1. Restart server: `pkill -f "node server-enhanced" && node server-enhanced.js`
2. Check Node.js version: `node --version` (should be 16+)
3. Reinstall dependencies: `npm install`

## Test Checklist

Once you have the test scene set up, verify these features:

- [ ] Connect to server
- [ ] Disconnect from server
- [ ] Create room (verify room code appears)
- [ ] Join room with code
- [ ] Two players connect to same room
- [ ] See "Player Joined" messages
- [ ] Status panel updates correctly
- [ ] Log panel shows all events
- [ ] Server console shows Unity connections

## Next Steps

After verifying basic connectivity works:

1. **Test matchmaking** (requires MatchmakingManager - coming next)
2. **Test friend system** (requires FriendSystemManager - coming next)
3. **Test lobby browser** (requires LobbyManager - coming next)
4. **Integrate with actual chess game**

## Files Created

### Server
- `server/database.js`
- `server/friendSystem.js`
- `server/matchmaking.js`
- `server/server-enhanced.js` (modified)

### Unity
- `Assets/Scripts/Network/SocketIOClient.cs`
- `Assets/Scripts/Network/NetworkManager.cs` (modified)
- `Assets/Scripts/Network/NetworkTestUI.cs`
- `Assets/Scenes/NetworkTestScene.unity` (you create this)

## Architecture Diagram

```
┌─────────────────────┐
│   Unity Client      │
│  NetworkManager     │
│  SocketIOClient     │
└──────────┬──────────┘
           │
           │ Socket.IO
           │ (WebSocket + Polling)
           │
┌──────────▼──────────┐
│   Node.js Server    │
│  server-enhanced.js │
├─────────────────────┤
│   Database          │
│   Friend System     │
│   Matchmaking       │
└─────────────────────┘
```

## Support

If you encounter issues:
1. Check Unity console for errors
2. Check server console for errors
3. Verify all files are in correct locations
4. Ensure Node.js dependencies are installed
5. Test server endpoints directly: `curl http://localhost:3000/api/server/status`
