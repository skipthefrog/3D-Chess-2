# 3D Chess Server

A comprehensive Socket.IO server for 3D Chess multiplayer games, designed specifically for Unity client integration.

## Features

- 🎮 **Real-time Multiplayer**: Socket.IO based real-time communication
- 🏠 **Room Management**: Create, join, and manage game rooms with unique codes
- 🎯 **Move Validation**: Server-side game state management and move processing
- 📊 **Web Dashboard**: Real-time monitoring of active games and server stats
- 🔒 **Security**: Built-in authentication and rate limiting
- 🐳 **Docker Ready**: Complete containerization setup
- 🧪 **Testing Suite**: Comprehensive test client for Unity integration

## Quick Start

### Prerequisites

- Node.js 16+
- npm or yarn

### Installation

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Start the server:**
   ```bash
   npm start
   ```

3. **For development with auto-reload:**
   ```bash
   npm run dev
   ```

4. **Access the web dashboard:**
   Open http://localhost:3000 in your browser

## Server Configuration

### Environment Variables

Copy `.env.example` to `.env` and configure:

```bash
PORT=3000
NODE_ENV=development
UNITY_AUTH_TOKEN=UNITY
LOG_LEVEL=debug
```

### Unity Client Configuration

In your Unity NetworkManager, set:
- **Server URL**: `http://localhost:3000`
- **Auth Token**: `UNITY`

## API Endpoints

### REST API

- `GET /` - Server status and stats
- `GET /api/rooms` - List all active rooms
- `GET /api/stats` - Server statistics

### Socket.IO Events

#### Room Management
```javascript
// Create room
socket.emit('create_room', {}, (response) => {
  // response.roomCode, response.assignedColor
});

// Join room
socket.emit('join_room', { roomCode }, (response) => {
  // response.success, response.assignedColor
});

// Leave room
socket.emit('leave_room', {}, (response) => {
  // response.success
});
```

#### Game Management
```javascript
// Set player ready
socket.emit('player_ready', { isReady: true }, (response) => {
  // response.success
});

// Start game (host only)
socket.emit('start_game', {}, (response) => {
  // response.gameState
});

// Send move
socket.emit('send_move', {
  fromX: 0, fromY: 0, fromZ: 0,
  toX: 1, toY: 0, toZ: 0,
  playerColor: 'White'
}, (response) => {
  // response.success, response.gameState
});
```

#### Event Listeners
```javascript
// Player events
socket.on('player_joined', (data) => { /* New player joined */ });
socket.on('player_left', (data) => { /* Player left */ });
socket.on('player_disconnected', (data) => { /* Player disconnected */ });

// Game events
socket.on('game_started', (data) => { /* Game started */ });
socket.on('move_received', (data) => { /* Move from other player */ });
socket.on('game_state_updated', (data) => { /* Game state changed */ });
```

## Testing

### Automated Tests

Run the test suite to verify server functionality:

```bash
# Run all tests
npm test

# Or run specific test scenarios
node test-client.js basic    # Basic connection test
node test-client.js room     # Room management test
node test-client.js move     # Move handling test
```

### Manual Testing

1. **Start the server:**
   ```bash
   npm run dev
   ```

2. **Open the web dashboard:**
   http://localhost:3000

3. **Test with Unity:**
   - Set NetworkManager server URL to `http://localhost:3000`
   - Enable authentication with token `UNITY`
   - Create or join rooms through Unity UI

## Deployment

### Docker

1. **Build container:**
   ```bash
   npm run docker:build
   ```

2. **Run container:**
   ```bash
   npm run docker:run
   ```

3. **Using Docker Compose:**
   ```bash
   docker-compose up -d
   ```

### Cloud Deployment

#### Heroku
```bash
# Install Heroku CLI and login
heroku create your-chess-server
git push heroku main
```

#### Railway.app
```bash
# Install Railway CLI
railway login
railway init
railway up
```

#### Digital Ocean
```bash
# Deploy to droplet with Docker
docker build -t chess-server .
docker run -d -p 3000:3000 chess-server
```

## Unity Integration

### NetworkManager Setup

1. **Configure your Unity NetworkManager:**
   ```csharp
   // Set server URL
   NetworkManager.Instance.SetServerUrl("http://localhost:3000");
   
   // Connect with authentication
   NetworkManager.Instance.ConnectToServer();
   ```

2. **Handle room creation:**
   ```csharp
   NetworkManager.Instance.CreateRoom(playerName);
   NetworkManager.Instance.OnRoomCreated += (roomCode, color) => {
       Debug.Log($"Room created: {roomCode}, playing as {color}");
   };
   ```

3. **Handle moves:**
   ```csharp
   NetworkManager.Instance.SendMove(fromPosition, toPosition, playerColor);
   NetworkManager.Instance.OnMoveReceived += (moveData) => {
       // Process opponent's move
   };
   ```

### Message Format

The server expects Unity messages in this format:
```json
{
  "fromX": 0, "fromY": 0, "fromZ": 0,
  "toX": 1, "toY": 0, "toZ": 0,
  "playerColor": "White",
  "timestamp": 1640995200000
}
```

## Architecture

### Server Components

- **GameServer**: Core game logic and state management
- **Room Management**: Create/join/leave room functionality  
- **Move Processing**: Validate and broadcast moves
- **Player Management**: Track connected players and assignments
- **Web Dashboard**: Real-time monitoring interface

### Data Structures

```javascript
// Room structure
{
  roomCode: "ABC123",
  hostId: "player_uuid",
  players: Map<playerId, playerData>,
  gameState: {
    phase: "waiting|placement|playing|ended",
    currentPlayer: "White|Black",
    moveHistory: [],
    totalMoves: 0
  }
}

// Player structure
{
  playerId: "unique_id",
  playerName: "PlayerName",
  assignedColor: "White|Black",
  isHost: true|false,
  isReady: true|false,
  socketId: "socket_id"
}
```

## Monitoring

### Web Dashboard

Access the real-time dashboard at http://localhost:3000 to monitor:

- 📊 Server statistics (rooms, players, uptime)
- 🏠 Active game rooms and their status
- 👥 Connected players
- 🎮 Games in progress

### Logging

The server provides comprehensive logging:

```bash
# View logs in development
npm run dev

# Production logs (when deployed)
heroku logs --tail  # For Heroku
docker logs chess-server  # For Docker
```

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Check server URL in Unity NetworkManager
   - Verify server is running on correct port
   - Check authentication token

2. **Room Not Found**
   - Ensure room code is correct
   - Check if room was closed due to inactivity
   - Verify server is maintaining room state

3. **Move Rejected**
   - Check if it's the player's turn
   - Verify game is in correct phase (playing/placement)
   - Ensure move data format is correct

### Debug Mode

Enable debug logging:
```bash
NODE_ENV=development LOG_LEVEL=debug npm start
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues and questions:
- Check the troubleshooting section
- Review server logs
- Test with the included test client
- Open an issue with detailed reproduction steps