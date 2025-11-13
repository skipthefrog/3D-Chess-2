# Quick Start - Network Test Scene (5 Minutes!)

## 🚀 Super Fast Setup

I've created an automated scene builder that does all the work for you!

### Step 1: Start the Server (Terminal)

```bash
cd "3D Chess 2/server"
node server-enhanced.js
```

**Expected output:**
```
📊 Database loaded: 0 users, 0 friend lists
🎯 Matchmaking system started
✅ All systems initialized
🚀 3D Chess Server listening on port 3000
```

Leave this terminal running!

---

### Step 2: Create Test Scene in Unity (2 minutes)

1. **Open Unity**
   - Open your 3D Chess project

2. **Create New Scene**
   - File → New Scene
   - Choose "Empty" template
   - Save As: `Assets/Scenes/NetworkTestScene.unity`

3. **Run the Automated Builder**
   - In Hierarchy, right-click → Create Empty
   - Name it: "SceneBuilder"
   - In Inspector: Add Component → Scripts → Network → **Network Test Scene Builder**
   - Check the box: **Build Scene** ✅
   - **The entire UI will build automatically!**

4. **Delete the Builder** (optional)
   - Delete the "SceneBuilder" GameObject
   - You don't need it anymore

5. **Link UI Fields** (important!)
   - Select "NetworkTestController" in Hierarchy
   - In Inspector, you'll see the NetworkTestUI script
   - If any fields show "None", drag the missing objects:
     - StatusText → drag "StatusText" from Hierarchy
     - LogText → drag "LogText" from Hierarchy
     - ServerUrlInput → drag "ServerUrlInput" from Hierarchy
     - PlayerNameInput → drag "PlayerNameInput" from Hierarchy
     - RoomCodeInput → drag "RoomCodeInput" from Hierarchy
     - ConnectButton → drag "ConnectButton" from Hierarchy
     - DisconnectButton → drag "DisconnectButton" from Hierarchy
     - CreateRoomButton → drag "CreateRoomButton" from Hierarchy
     - JoinRoomButton → drag "JoinRoomButton" from Hierarchy
     - LogScrollRect → drag "ScrollView" from Hierarchy

6. **Save the Scene**
   - Ctrl+S (Windows) or Cmd+S (Mac)

---

### Step 3: Test It! (1 minute)

1. **Enter Play Mode**
   - Click the Play button ▶️

2. **Click "Connect to Server"**
   - Should see green "✅ Connected!" in the log
   - Status changes to "🟢 CONNECTED"

3. **Click "Create Room"**
   - You'll get a room code (e.g., "ABC123")
   - Status shows: "🟢 CONNECTED | Room: ABC123"

4. **Success!** 🎉
   - You're now connected to the server
   - You have a room running
   - Ready for multiplayer!

---

## 🧪 Test With Two Players

### Option A: Second Unity Editor Instance

1. **Build the game:**
   - File → Build Settings
   - Click "Build" (or "Build and Run")
   - Save as "3DChess_Test.exe" (or .app on Mac)

2. **Run both:**
   - Unity Editor (Player 1)
   - Built game (Player 2)

3. **Player 1:**
   - Connect → Create Room
   - Note the room code

4. **Player 2:**
   - Connect
   - Enter room code
   - Click "Join Room"

5. **Both players see:**
   - "👤 Player Joined" messages
   - Each other in the room!

---

## 📺 What You Should See

**Unity Console (successful connection):**
```
🌐 NetworkManager: Native Socket.IO client initialized
🔌 Connecting to http://localhost:3000...
✅ Connected to server successfully! Socket ID: xyz123
```

**Unity UI Log Panel:**
```
[12:34:56] 🎮 Network Test UI Initialized
[12:34:57] 🔌 Connecting to http://localhost:3000...
[12:34:58] ✅ Connected to server!
[12:34:59] 🏠 Creating room as TestPlayer_1234...
[12:35:00] ✅ Room Created! Code: ABC123
[12:35:00] 🎨 Assigned Color: White
```

**Server Console:**
```
🔌 Player connected: TestPlayer_1234 (socket_xyz) from ::1
👤 User registered: TestPlayer_1234 (ID: user_123)
📨 Room creation request received
✅ Room ABC123 created by TestPlayer_1234
```

---

## ❌ Troubleshooting

### "Connection failed"
**Check:**
1. Is server running? Look for "listening on port 3000"
2. Try: `curl http://localhost:3000/api/server/status` in terminal
3. Firewall blocking port 3000?

### "NetworkManager not found"
**Fix:**
1. Select "NetworkManager" GameObject in Hierarchy
2. Verify NetworkManager script is attached
3. If missing, re-add: Add Component → NetworkManager

### "UI fields are None"
**Fix:**
1. Select "NetworkTestController" in Hierarchy
2. Manually drag each UI element from Hierarchy to Inspector fields
3. Save scene (Ctrl+S / Cmd+S)

### Scene builder didn't work
**Manual fallback:**
- Follow the detailed guide in NETWORK_TEST_GUIDE.md
- It has step-by-step manual instructions

---

## 🎯 Next Steps

Once the test scene works:

1. **Test room joining:**
   - Build game
   - Test with 2 clients
   - Verify both see each other

2. **Test reconnection:**
   - Disconnect/reconnect
   - Check if status updates

3. **Check server logs:**
   - See what the server records
   - Verify events are being received

4. **Ready for integration!**
   - The networking foundation works
   - Time to connect it to your chess game

---

## 📝 What This Tests

✅ Socket.IO connection (Unity ↔ Server)
✅ Room creation
✅ Room joining by code
✅ Player joining notifications
✅ Real-time event handling
✅ Connection status tracking
✅ Server-side player management

---

## 🔥 Cool Commands to Try

**In terminal (while server running):**

```bash
# Check server status
curl http://localhost:3000/api/server/status

# See all rooms
curl http://localhost:3000/api/rooms

# See public rooms (lobby)
curl http://localhost:3000/api/lobby/rooms

# Check matchmaking queue
curl http://localhost:3000/api/matchmaking/status
```

---

**That's it! You should have a working network test scene in under 5 minutes!** 🚀

If you have any issues, check the detailed NETWORK_TEST_GUIDE.md for more information.
