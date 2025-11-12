/**
 * Database Module for 3D Chess Server
 * Simple file-based storage for user accounts, friends, and game data
 * Can be easily migrated to SQLite or PostgreSQL later
 */

const fs = require('fs');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

class Database {
  constructor(dataDirectory = './data') {
    this.dataDir = dataDirectory;
    this.usersFile = path.join(dataDirectory, 'users.json');
    this.friendsFile = path.join(dataDirectory, 'friends.json');
    this.matchHistoryFile = path.join(dataDirectory, 'matchHistory.json');

    // In-memory cache for fast access
    this.users = new Map(); // userId -> user data
    this.usersByName = new Map(); // username -> userId
    this.friendRelationships = new Map(); // userId -> Set of friend userIds
    this.friendRequests = new Map(); // userId -> array of pending requests
    this.matchHistory = new Map(); // userId -> array of match records

    this.initializeStorage();
    this.loadData();
  }

  /**
   * Initialize storage directory and files
   */
  initializeStorage() {
    // Create data directory if it doesn't exist
    if (!fs.existsSync(this.dataDir)) {
      fs.mkdirSync(this.dataDir, { recursive: true });
    }

    // Create empty files if they don't exist
    if (!fs.existsSync(this.usersFile)) {
      fs.writeFileSync(this.usersFile, JSON.stringify({}));
    }

    if (!fs.existsSync(this.friendsFile)) {
      fs.writeFileSync(this.friendsFile, JSON.stringify({}));
    }

    if (!fs.existsSync(this.matchHistoryFile)) {
      fs.writeFileSync(this.matchHistoryFile, JSON.stringify({}));
    }
  }

  /**
   * Load all data from disk into memory
   */
  loadData() {
    try {
      // Load users
      const usersData = JSON.parse(fs.readFileSync(this.usersFile, 'utf8'));
      for (const [userId, userData] of Object.entries(usersData)) {
        this.users.set(userId, userData);
        this.usersByName.set(userData.username.toLowerCase(), userId);
      }

      // Load friend relationships
      const friendsData = JSON.parse(fs.readFileSync(this.friendsFile, 'utf8'));
      for (const [userId, data] of Object.entries(friendsData)) {
        this.friendRelationships.set(userId, new Set(data.friends || []));
        this.friendRequests.set(userId, data.requests || []);
      }

      // Load match history
      const historyData = JSON.parse(fs.readFileSync(this.matchHistoryFile, 'utf8'));
      for (const [userId, matches] of Object.entries(historyData)) {
        this.matchHistory.set(userId, matches);
      }

      console.log(`📊 Database loaded: ${this.users.size} users, ${this.friendRelationships.size} friend lists`);
    } catch (error) {
      console.error('Error loading database:', error);
    }
  }

  /**
   * Save data to disk
   */
  saveData() {
    try {
      // Save users
      const usersObj = {};
      for (const [userId, userData] of this.users.entries()) {
        usersObj[userId] = userData;
      }
      fs.writeFileSync(this.usersFile, JSON.stringify(usersObj, null, 2));

      // Save friend relationships
      const friendsObj = {};
      for (const [userId, friends] of this.friendRelationships.entries()) {
        friendsObj[userId] = {
          friends: Array.from(friends),
          requests: this.friendRequests.get(userId) || []
        };
      }
      fs.writeFileSync(this.friendsFile, JSON.stringify(friendsObj, null, 2));

      // Save match history
      const historyObj = {};
      for (const [userId, matches] of this.matchHistory.entries()) {
        historyObj[userId] = matches;
      }
      fs.writeFileSync(this.matchHistoryFile, JSON.stringify(historyObj, null, 2));

    } catch (error) {
      console.error('Error saving database:', error);
    }
  }

  // ============ USER MANAGEMENT ============

  /**
   * Create or get a user account
   * @param {string} username - Display name
   * @param {string} deviceId - Optional device identifier for anonymous users
   * @returns {object} User data
   */
  getOrCreateUser(username, deviceId = null) {
    const normalizedName = username.toLowerCase();

    // Check if user exists by username
    if (this.usersByName.has(normalizedName)) {
      const userId = this.usersByName.get(normalizedName);
      return this.users.get(userId);
    }

    // Create new user
    const userId = uuidv4();
    const user = {
      userId,
      username,
      deviceId,
      createdAt: Date.now(),
      lastSeen: Date.now(),
      gamesPlayed: 0,
      wins: 0,
      losses: 0,
      draws: 0
    };

    this.users.set(userId, user);
    this.usersByName.set(normalizedName, userId);
    this.friendRelationships.set(userId, new Set());
    this.friendRequests.set(userId, []);
    this.matchHistory.set(userId, []);

    this.saveData();

    console.log(`👤 New user created: ${username} (${userId})`);
    return user;
  }

  /**
   * Get user by ID
   */
  getUser(userId) {
    return this.users.get(userId);
  }

  /**
   * Get user by username
   */
  getUserByName(username) {
    const userId = this.usersByName.get(username.toLowerCase());
    return userId ? this.users.get(userId) : null;
  }

  /**
   * Update user's last seen timestamp
   */
  updateLastSeen(userId) {
    const user = this.users.get(userId);
    if (user) {
      user.lastSeen = Date.now();
      this.saveData();
    }
  }

  /**
   * Update user stats after a game
   */
  updateUserStats(userId, result) {
    const user = this.users.get(userId);
    if (!user) return;

    user.gamesPlayed++;
    if (result === 'win') user.wins++;
    else if (result === 'loss') user.losses++;
    else if (result === 'draw') user.draws++;

    this.saveData();
  }

  // ============ FRIEND SYSTEM ============

  /**
   * Send a friend request
   */
  sendFriendRequest(fromUserId, toUsername) {
    const toUser = this.getUserByName(toUsername);
    if (!toUser) {
      return { success: false, error: 'User not found' };
    }

    const toUserId = toUser.userId;

    // Can't friend yourself
    if (fromUserId === toUserId) {
      return { success: false, error: 'Cannot add yourself as a friend' };
    }

    // Check if already friends
    const fromFriends = this.friendRelationships.get(fromUserId);
    if (fromFriends && fromFriends.has(toUserId)) {
      return { success: false, error: 'Already friends' };
    }

    // Check if request already exists
    const toRequests = this.friendRequests.get(toUserId) || [];
    const existingRequest = toRequests.find(req => req.fromUserId === fromUserId);
    if (existingRequest) {
      return { success: false, error: 'Friend request already sent' };
    }

    // Add friend request
    const request = {
      fromUserId,
      fromUsername: this.users.get(fromUserId).username,
      timestamp: Date.now()
    };

    toRequests.push(request);
    this.friendRequests.set(toUserId, toRequests);
    this.saveData();

    console.log(`👥 Friend request: ${request.fromUsername} → ${toUser.username}`);

    return {
      success: true,
      request,
      toUserId: toUserId  // Return this so server can notify the recipient
    };
  }

  /**
   * Accept a friend request
   */
  acceptFriendRequest(userId, fromUserId) {
    const requests = this.friendRequests.get(userId) || [];
    const requestIndex = requests.findIndex(req => req.fromUserId === fromUserId);

    if (requestIndex === -1) {
      return { success: false, error: 'Friend request not found' };
    }

    // Remove the request
    requests.splice(requestIndex, 1);
    this.friendRequests.set(userId, requests);

    // Add to both users' friend lists
    const userFriends = this.friendRelationships.get(userId);
    const fromUserFriends = this.friendRelationships.get(fromUserId);

    userFriends.add(fromUserId);
    fromUserFriends.add(userId);

    this.saveData();

    const user = this.users.get(userId);
    const fromUser = this.users.get(fromUserId);

    console.log(`👥 Friend request accepted: ${user.username} ↔ ${fromUser.username}`);

    return {
      success: true,
      friend: {
        userId: fromUserId,
        username: fromUser.username
      }
    };
  }

  /**
   * Reject a friend request
   */
  rejectFriendRequest(userId, fromUserId) {
    const requests = this.friendRequests.get(userId) || [];
    const requestIndex = requests.findIndex(req => req.fromUserId === fromUserId);

    if (requestIndex === -1) {
      return { success: false, error: 'Friend request not found' };
    }

    requests.splice(requestIndex, 1);
    this.friendRequests.set(userId, requests);
    this.saveData();

    return { success: true };
  }

  /**
   * Remove a friend
   */
  removeFriend(userId, friendUserId) {
    const userFriends = this.friendRelationships.get(userId);
    const friendUserFriends = this.friendRelationships.get(friendUserId);

    if (!userFriends || !friendUserFriends) {
      return { success: false, error: 'User not found' };
    }

    userFriends.delete(friendUserId);
    friendUserFriends.delete(userId);

    this.saveData();

    return { success: true };
  }

  /**
   * Get user's friend list with details
   */
  getFriendList(userId) {
    const friendIds = this.friendRelationships.get(userId);
    if (!friendIds) return [];

    const friends = [];
    for (const friendId of friendIds) {
      const friend = this.users.get(friendId);
      if (friend) {
        friends.push({
          userId: friend.userId,
          username: friend.username,
          lastSeen: friend.lastSeen,
          gamesPlayed: friend.gamesPlayed,
          wins: friend.wins
        });
      }
    }

    return friends;
  }

  /**
   * Get pending friend requests
   */
  getPendingRequests(userId) {
    return this.friendRequests.get(userId) || [];
  }

  /**
   * Check if two users are friends
   */
  areFriends(userId1, userId2) {
    const friends = this.friendRelationships.get(userId1);
    return friends ? friends.has(userId2) : false;
  }

  // ============ MATCH HISTORY ============

  /**
   * Add a match to user's history
   */
  addMatchRecord(userId, matchData) {
    const history = this.matchHistory.get(userId) || [];
    history.unshift({  // Add to beginning
      matchId: matchData.matchId,
      timestamp: Date.now(),
      opponent: matchData.opponent,
      result: matchData.result,
      playerColor: matchData.playerColor,
      moves: matchData.moves || 0,
      duration: matchData.duration || 0
    });

    // Keep only last 50 matches
    if (history.length > 50) {
      history.length = 50;
    }

    this.matchHistory.set(userId, history);
    this.saveData();
  }

  /**
   * Get user's match history
   */
  getMatchHistory(userId, limit = 20) {
    const history = this.matchHistory.get(userId) || [];
    return history.slice(0, limit);
  }
}

module.exports = Database;
