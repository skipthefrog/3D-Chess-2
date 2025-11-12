/**
 * Friend System Module for 3D Chess Server
 * Handles friend requests, friend lists, online status, and invitations
 */

class FriendSystem {
  constructor(database, io) {
    this.db = database;
    this.io = io;
    this.onlineUsers = new Map(); // userId -> socketId
    this.userSockets = new Map(); // socketId -> userId
  }

  /**
   * Register a user as online
   */
  registerOnlineUser(socketId, userId) {
    this.onlineUsers.set(userId, socketId);
    this.userSockets.set(socketId, userId);

    // Notify friends that this user is online
    this.notifyFriendsOfStatus(userId, 'online');

    console.log(`✅ User ${userId} is now online (socket: ${socketId})`);
  }

  /**
   * Unregister a user (when they disconnect)
   */
  unregisterOnlineUser(socketId) {
    const userId = this.userSockets.get(socketId);
    if (userId) {
      this.onlineUsers.delete(userId);
      this.userSockets.delete(socketId);

      // Notify friends that this user is offline
      this.notifyFriendsOfStatus(userId, 'offline');

      console.log(`❌ User ${userId} is now offline`);
    }
  }

  /**
   * Check if a user is online
   */
  isUserOnline(userId) {
    return this.onlineUsers.has(userId);
  }

  /**
   * Get socket ID for a user
   */
  getUserSocket(userId) {
    return this.onlineUsers.get(userId);
  }

  /**
   * Get user ID from socket ID
   */
  getUserId(socketId) {
    return this.userSockets.get(socketId);
  }

  /**
   * Notify friends when user's status changes
   */
  notifyFriendsOfStatus(userId, status) {
    const friends = this.db.getFriendList(userId);
    const user = this.db.getUser(userId);

    if (!user) return;

    friends.forEach(friend => {
      const friendSocketId = this.onlineUsers.get(friend.userId);
      if (friendSocketId) {
        this.io.to(friendSocketId).emit('friend_status_changed', {
          userId: userId,
          username: user.username,
          status: status,
          timestamp: Date.now()
        });
      }
    });
  }

  /**
   * Initialize Socket.IO event handlers for this socket
   */
  setupSocketHandlers(socket, userId) {
    // Send friend request
    socket.on('send_friend_request', (data, callback) => {
      this.handleSendFriendRequest(socket, userId, data, callback);
    });

    // Accept friend request
    socket.on('accept_friend_request', (data, callback) => {
      this.handleAcceptFriendRequest(socket, userId, data, callback);
    });

    // Reject friend request
    socket.on('reject_friend_request', (data, callback) => {
      this.handleRejectFriendRequest(socket, userId, data, callback);
    });

    // Remove friend
    socket.on('remove_friend', (data, callback) => {
      this.handleRemoveFriend(socket, userId, data, callback);
    });

    // Get friend list
    socket.on('get_friend_list', (callback) => {
      this.handleGetFriendList(socket, userId, callback);
    });

    // Get pending friend requests
    socket.on('get_friend_requests', (callback) => {
      this.handleGetFriendRequests(socket, userId, callback);
    });

    // Invite friend to room
    socket.on('invite_friend_to_room', (data, callback) => {
      this.handleInviteFriendToRoom(socket, userId, data, callback);
    });
  }

  /**
   * Handle send friend request
   */
  handleSendFriendRequest(socket, userId, data, callback) {
    const { targetUsername } = data;

    if (!targetUsername) {
      return callback && callback({
        success: false,
        error: 'Target username is required'
      });
    }

    const result = this.db.sendFriendRequest(userId, targetUsername);

    if (result.success) {
      // Notify the recipient if they're online
      const recipientSocketId = this.onlineUsers.get(result.toUserId);
      if (recipientSocketId) {
        this.io.to(recipientSocketId).emit('friend_request_received', {
          fromUserId: userId,
          fromUsername: this.db.getUser(userId).username,
          timestamp: Date.now()
        });
      }
    }

    callback && callback(result);
  }

  /**
   * Handle accept friend request
   */
  handleAcceptFriendRequest(socket, userId, data, callback) {
    const { fromUserId } = data;

    if (!fromUserId) {
      return callback && callback({
        success: false,
        error: 'fromUserId is required'
      });
    }

    const result = this.db.acceptFriendRequest(userId, fromUserId);

    if (result.success) {
      const user = this.db.getUser(userId);
      const fromUser = this.db.getUser(fromUserId);

      // Notify the requester that their request was accepted
      const requesterSocketId = this.onlineUsers.get(fromUserId);
      if (requesterSocketId) {
        this.io.to(requesterSocketId).emit('friend_request_accepted', {
          userId: userId,
          username: user.username,
          isOnline: this.isUserOnline(userId),
          timestamp: Date.now()
        });
      }

      // Send updated friend list to both users
      this.sendFriendListUpdate(userId);
      this.sendFriendListUpdate(fromUserId);
    }

    callback && callback(result);
  }

  /**
   * Handle reject friend request
   */
  handleRejectFriendRequest(socket, userId, data, callback) {
    const { fromUserId } = data;

    if (!fromUserId) {
      return callback && callback({
        success: false,
        error: 'fromUserId is required'
      });
    }

    const result = this.db.rejectFriendRequest(userId, fromUserId);
    callback && callback(result);
  }

  /**
   * Handle remove friend
   */
  handleRemoveFriend(socket, userId, data, callback) {
    const { friendUserId } = data;

    if (!friendUserId) {
      return callback && callback({
        success: false,
        error: 'friendUserId is required'
      });
    }

    const result = this.db.removeFriend(userId, friendUserId);

    if (result.success) {
      // Notify the ex-friend if they're online
      const exFriendSocketId = this.onlineUsers.get(friendUserId);
      if (exFriendSocketId) {
        this.io.to(exFriendSocketId).emit('friend_removed', {
          userId: userId,
          username: this.db.getUser(userId).username,
          timestamp: Date.now()
        });
      }

      // Send updated friend lists
      this.sendFriendListUpdate(userId);
      this.sendFriendListUpdate(friendUserId);
    }

    callback && callback(result);
  }

  /**
   * Handle get friend list
   */
  handleGetFriendList(socket, userId, callback) {
    const friends = this.db.getFriendList(userId);

    // Add online status to each friend
    const friendsWithStatus = friends.map(friend => ({
      ...friend,
      isOnline: this.isUserOnline(friend.userId)
    }));

    callback && callback({
      success: true,
      friends: friendsWithStatus
    });
  }

  /**
   * Handle get friend requests
   */
  handleGetFriendRequests(socket, userId, callback) {
    const requests = this.db.getPendingRequests(userId);

    callback && callback({
      success: true,
      requests: requests
    });
  }

  /**
   * Handle invite friend to room
   */
  handleInviteFriendToRoom(socket, userId, data, callback) {
    const { friendUserId, roomCode, roomSettings } = data;

    if (!friendUserId || !roomCode) {
      return callback && callback({
        success: false,
        error: 'friendUserId and roomCode are required'
      });
    }

    // Check if they're actually friends
    if (!this.db.areFriends(userId, friendUserId)) {
      return callback && callback({
        success: false,
        error: 'Not friends with this user'
      });
    }

    // Check if friend is online
    const friendSocketId = this.onlineUsers.get(friendUserId);
    if (!friendSocketId) {
      return callback && callback({
        success: false,
        error: 'Friend is not online'
      });
    }

    // Send invitation to friend
    const user = this.db.getUser(userId);
    this.io.to(friendSocketId).emit('room_invitation_received', {
      fromUserId: userId,
      fromUsername: user.username,
      roomCode: roomCode,
      roomSettings: roomSettings,
      timestamp: Date.now()
    });

    callback && callback({
      success: true,
      message: 'Invitation sent'
    });

    console.log(`📨 Room invitation: ${user.username} invited friend ${friendUserId} to room ${roomCode}`);
  }

  /**
   * Send friend list update to a user
   */
  sendFriendListUpdate(userId) {
    const socketId = this.onlineUsers.get(userId);
    if (socketId) {
      const friends = this.db.getFriendList(userId);
      const friendsWithStatus = friends.map(friend => ({
        ...friend,
        isOnline: this.isUserOnline(friend.userId)
      }));

      this.io.to(socketId).emit('friend_list_updated', {
        friends: friendsWithStatus,
        timestamp: Date.now()
      });
    }
  }

  /**
   * Get online friends for a user
   */
  getOnlineFriends(userId) {
    const friends = this.db.getFriendList(userId);
    return friends.filter(friend => this.isUserOnline(friend.userId));
  }

  /**
   * Search for users by username (for adding friends)
   */
  searchUsers(searchQuery, currentUserId, limit = 10) {
    const results = [];
    const query = searchQuery.toLowerCase();

    for (const [userId, user] of this.db.users.entries()) {
      // Don't include current user
      if (userId === currentUserId) continue;

      // Check if username matches
      if (user.username.toLowerCase().includes(query)) {
        results.push({
          userId: user.userId,
          username: user.username,
          isOnline: this.isUserOnline(userId),
          isFriend: this.db.areFriends(currentUserId, userId)
        });

        if (results.length >= limit) break;
      }
    }

    return results;
  }
}

module.exports = FriendSystem;
