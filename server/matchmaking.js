/**
 * Matchmaking System for 3D Chess Server
 * Handles automatic player matching based on preferences
 * Uses progressive time-window matching to balance wait times with match quality
 */

const { v4: uuidv4 } = require('uuid');

class MatchmakingSystem {
  constructor(gameServer, io) {
    this.gameServer = gameServer;
    this.io = io;
    this.queue = new Map(); // socketId -> player data
    this.matchingInterval = null;
    this.MATCH_CHECK_INTERVAL = 5000; // Check for matches every 5 seconds
    this.TIME_WINDOWS = [10, 30, 60, 120]; // Progressive time windows in seconds

    // Start the matching loop
    this.startMatchingLoop();
  }

  /**
   * Start the automatic matching loop
   */
  startMatchingLoop() {
    this.matchingInterval = setInterval(() => {
      this.processMatchmaking();
    }, this.MATCH_CHECK_INTERVAL);

    console.log('🎯 Matchmaking system started');
  }

  /**
   * Stop the matching loop
   */
  stopMatchingLoop() {
    if (this.matchingInterval) {
      clearInterval(this.matchingInterval);
      this.matchingInterval = null;
    }
  }

  /**
   * Add a player to the matchmaking queue
   */
  joinQueue(socket, userId, preferences) {
    const player = {
      socketId: socket.id,
      userId: userId,
      username: preferences.username || 'Player',
      joinedAt: Date.now(),
      preferences: {
        boardSize: preferences.boardSize || 8, // 4, 6, or 8
        playerCount: preferences.playerCount || 2, // 2-6
        chaosMode: preferences.chaosMode || false,
        timedPlay: preferences.timedPlay || false,
        timeLimit: preferences.timeLimit || 600, // seconds
        skillLevel: preferences.skillLevel || 'any', // beginner, intermediate, advanced, any
        includeAI: preferences.includeAI || false // Allow AI to fill empty slots
      }
    };

    this.queue.set(socket.id, player);

    console.log(`🎮 Player ${player.username} joined matchmaking queue (${this.queue.size} in queue)`);
    console.log(`   Preferences: ${player.preferences.playerCount}P, ${player.preferences.boardSize}x${player.preferences.boardSize}x${player.preferences.boardSize}`);

    // Notify player they're in queue
    socket.emit('matchmaking_queue_joined', {
      success: true,
      queuePosition: this.queue.size,
      estimatedWaitTime: this.estimateWaitTime(player.preferences)
    });

    // Try to find a match immediately
    this.processMatchmaking();

    return { success: true };
  }

  /**
   * Remove a player from the queue
   */
  leaveQueue(socketId) {
    const player = this.queue.get(socketId);
    if (player) {
      this.queue.delete(socketId);
      console.log(`🚪 Player ${player.username} left matchmaking queue (${this.queue.size} remaining)`);
      return { success: true };
    }
    return { success: false, error: 'Not in queue' };
  }

  /**
   * Process matchmaking - find compatible players and create rooms
   */
  processMatchmaking() {
    if (this.queue.size < 2) return; // Need at least 2 players

    const players = Array.from(this.queue.values());
    const matched = new Set(); // Track which players have been matched

    // Group players by preferences
    const groups = this.groupPlayersByPreferences(players);

    // Try to match players in each group
    for (const group of groups) {
      if (group.length < 2) continue;

      // Try to create matches for this group
      this.createMatchesForGroup(group, matched);
    }
  }

  /**
   * Group players by similar preferences
   */
  groupPlayersByPreferences(players) {
    const groups = new Map();

    for (const player of players) {
      const waitTime = (Date.now() - player.joinedAt) / 1000; // seconds
      const timeWindow = this.getTimeWindow(waitTime);

      // Create a key for grouping - preferences relax over time
      let groupKey;
      if (timeWindow === 0) {
        // First 10 seconds: exact match required
        groupKey = `${player.preferences.playerCount}|${player.preferences.boardSize}|${player.preferences.chaosMode}|${player.preferences.timedPlay}`;
      } else if (timeWindow === 1) {
        // 10-30 seconds: relax chaos/timed requirements
        groupKey = `${player.preferences.playerCount}|${player.preferences.boardSize}`;
      } else if (timeWindow === 2) {
        // 30-60 seconds: relax board size
        groupKey = `${player.preferences.playerCount}`;
      } else {
        // 60+ seconds: match any preferences (just need same player count)
        groupKey = `${player.preferences.playerCount}`;
      }

      if (!groups.has(groupKey)) {
        groups.set(groupKey, []);
      }
      groups.get(groupKey).push(player);
    }

    return Array.from(groups.values());
  }

  /**
   * Get time window index based on wait time
   */
  getTimeWindow(waitTimeSeconds) {
    for (let i = 0; i < this.TIME_WINDOWS.length; i++) {
      if (waitTimeSeconds < this.TIME_WINDOWS[i]) {
        return i;
      }
    }
    return this.TIME_WINDOWS.length - 1;
  }

  /**
   * Create matches for a group of compatible players
   */
  createMatchesForGroup(group, matched) {
    // Sort by join time (fairness)
    group.sort((a, b) => a.joinedAt - b.joinedAt);

    while (group.length >= 2) {
      const playerCount = group[0].preferences.playerCount;

      // Try to form a complete match
      const matchPlayers = [];
      for (let i = 0; i < group.length && matchPlayers.length < playerCount; i++) {
        const player = group[i];
        if (!matched.has(player.socketId)) {
          matchPlayers.push(player);
        }
      }

      // If we have enough players, create a room
      if (matchPlayers.length >= 2) {
        this.createMatchRoom(matchPlayers);

        // Mark these players as matched
        matchPlayers.forEach(p => {
          matched.add(p.socketId);
          this.queue.delete(p.socketId);
          group.splice(group.indexOf(p), 1);
        });
      } else {
        break; // Not enough players
      }
    }
  }

  /**
   * Create a room for matched players
   */
  createMatchRoom(players) {
    const roomCode = this.gameServer.generateRoomCode();

    // Use preferences from first player as base, merge with others
    const prefs = players[0].preferences;

    const room = {
      roomCode: roomCode,
      hostId: players[0].userId,
      maxPlayers: prefs.playerCount,
      players: new Map(),
      gameState: {
        phase: 'waiting',
        currentPlayer: 'White',
        moveHistory: [],
        totalMoves: 0,
        boardSize: prefs.boardSize,
        chaosMode: prefs.chaosMode,
        timedPlay: prefs.timedPlay,
        timeLimit: prefs.timeLimit
      },
      settings: {
        isPublic: false, // Matchmade games are private
        allowSpectators: false,
        boardSize: prefs.boardSize,
        chaosMode: prefs.chaosMode,
        timedPlay: prefs.timedPlay
      },
      createdAt: Date.now(),
      isMatchmade: true
    };

    // Add players to the room
    const colors = ['White', 'Black', 'Green', 'Purple', 'Yellow', 'Orange'];
    players.forEach((player, index) => {
      const color = colors[index];

      const playerData = {
        playerId: player.userId,
        playerName: player.username,
        assignedColor: color,
        isHost: index === 0,
        isReady: true, // Matchmade players are auto-ready
        isAI: false,
        socketId: player.socketId
      };

      room.players.set(player.userId, playerData);

      // Join the socket to the room
      const socket = this.io.sockets.sockets.get(player.socketId);
      if (socket) {
        socket.join(roomCode);
      }
    });

    // Store the room
    this.gameServer.rooms.set(roomCode, room);

    // Notify all matched players
    players.forEach(player => {
      const socket = this.io.sockets.sockets.get(player.socketId);
      if (socket) {
        const playerData = room.players.get(player.userId);

        socket.emit('match_found', {
          roomCode: roomCode,
          assignedColor: playerData.assignedColor,
          isHost: playerData.isHost,
          room: {
            maxPlayers: room.maxPlayers,
            playerCount: room.players.size,
            gameState: room.gameState,
            settings: room.settings
          },
          opponents: Array.from(room.players.values())
            .filter(p => p.playerId !== player.userId)
            .map(p => ({
              username: p.playerName,
              color: p.assignedColor
            }))
        });

        console.log(`✅ Match found for ${player.username}: Room ${roomCode} as ${playerData.assignedColor}`);
      }
    });

    // Broadcast to room that all players are ready
    this.io.to(roomCode).emit('all_players_ready', {
      playerCount: room.players.size,
      players: Array.from(room.players.values()).map(p => ({
        username: p.playerName,
        color: p.assignedColor,
        isReady: true
      }))
    });

    console.log(`🎮 Matchmade room created: ${roomCode} with ${players.length} players`);
  }

  /**
   * Estimate wait time for given preferences
   */
  estimateWaitTime(preferences) {
    // Count players in queue with similar preferences
    const similar = Array.from(this.queue.values()).filter(p => {
      return p.preferences.playerCount === preferences.playerCount &&
             p.preferences.boardSize === preferences.boardSize;
    });

    const needed = preferences.playerCount;
    const current = similar.length + 1; // +1 for this player

    if (current >= needed) {
      return 5; // Should match very soon
    } else if (current >= needed / 2) {
      return 30; // Halfway there
    } else {
      return 60; // May take a while
    }
  }

  /**
   * Get current queue status
   */
  getQueueStatus() {
    return {
      totalInQueue: this.queue.size,
      byPlayerCount: this.getQueueBreakdown('playerCount'),
      byBoardSize: this.getQueueBreakdown('boardSize')
    };
  }

  /**
   * Get queue breakdown by preference
   */
  getQueueBreakdown(preferenceKey) {
    const breakdown = {};
    for (const player of this.queue.values()) {
      const value = player.preferences[preferenceKey];
      breakdown[value] = (breakdown[value] || 0) + 1;
    }
    return breakdown;
  }

  /**
   * Setup socket handlers for matchmaking
   */
  setupSocketHandlers(socket, userId) {
    // Join matchmaking queue
    socket.on('join_matchmaking', (preferences, callback) => {
      const result = this.joinQueue(socket, userId, preferences);
      callback && callback(result);
    });

    // Leave matchmaking queue
    socket.on('leave_matchmaking', (callback) => {
      const result = this.leaveQueue(socket.id);
      callback && callback(result);
    });

    // Get queue status
    socket.on('get_queue_status', (callback) => {
      callback && callback({
        success: true,
        ...this.getQueueStatus()
      });
    });

    // Handle disconnection - remove from queue
    socket.on('disconnect', () => {
      this.leaveQueue(socket.id);
    });
  }
}

module.exports = MatchmakingSystem;
