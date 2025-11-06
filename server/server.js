'use strict';

const http = require('http');
const express = require('express');
const socket = require('socket.io');
const path = require('path');

const app = express();
const server = http.createServer(app);
const port = 3000;

// Serve static files from public directory
app.use(express.static(path.join(__dirname, 'public')));

// Default route to lobby
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'lobby.html'));
});

var io = socket(server, {
    pingInterval: 10000,
    pingTimeout: 5000,
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    }
});

console.log('🎮 3D Chess Server Starting...');

// Authentication for both Unity and web clients
io.use((socket, next) => {
    const token = socket.handshake.query.token;
    
    // Allow Unity clients with UNITY token
    if (token === "UNITY") {
        console.log('✅ Unity client authenticated');
        next();
    } 
    // Allow web clients (no token required for web browser connections)
    else if (!token || token === "" || socket.handshake.headers.origin) {
        console.log('✅ Web client connected');
        next();
    } 
    else {
        console.log('❌ Authentication failed for:', socket.handshake.query);
        next(new Error("Authentication error"));
    }
});

io.on('connection', socket => {
    console.log('🔌 New connection:', socket.id);

    // Send welcome message
    setTimeout(() => {
        socket.emit('connection', {
            date: new Date().getTime(), 
            data: "3D Chess Server Connected",
            message: "Socket.IO is working!"
        });
    }, 1000);

    // Test message handler
    socket.on('hello', (data) => {
        console.log('👋 Hello message received:', data);
        socket.emit('hello', {
            date: new Date().getTime(), 
            data: data,
            serverResponse: "Hello from 3D Chess Server!"
        });
    });

    // Test connection handler
    socket.on('test_connection', (data) => {
        console.log('🧪 Test connection:', data);
        socket.emit('test_response', {
            date: new Date().getTime(),
            success: true,
            message: "Test successful - Socket.IO is working!",
            receivedData: data
        });
    });

    // Socket.IO test handlers
    socket.on('spin', (data) => {
        console.log('🌀 Spin event:', data);
        socket.emit('spin', {date: new Date().getTime(), data: data});
    });

    socket.on('class', (data) => {
        console.log('📚 Class event:', data);
        socket.emit('class', {date: new Date().getTime(), data: data});
    });

    socket.on('disconnect', () => {
        console.log('🔌 Client disconnected:', socket.id);
    });
});

server.listen(port, () => {
    console.log('🚀 3D Chess Server listening on port:', port);
    console.log('🌐 Server URL: http://localhost:' + port);
    console.log('💡 Ready for Unity Socket.IO connections');
});