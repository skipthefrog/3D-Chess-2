# Socket.IO Unity Integration Guide

## Cross-Platform Socket.IO Options for Unity

### Recommended Approach: Native WebSocket Implementation ✅

**Why This Approach:**
- ✅ **No external dependencies** - Works out of the box
- ✅ **Cross-platform** - Web, iOS, Android, Standalone all supported
- ✅ **Free & Open Source** - No licensing costs
- ✅ **Full control** - Can customize as needed
- ✅ **Maintainable** - No dependency on third-party updates

**Implementation:**
We'll use Unity's native `WebSocket` support (Unity 2019.1+) combined with a custom Socket.IO protocol implementation. This provides:
- Binary/text message support
- Event-based communication
- Automatic reconnection
- Works identically across all platforms

### Alternative Options (For Reference)

#### 1. SocketIOUnity (GitHub)
- **Repository:** https://github.com/itisnajim/SocketIOUnity
- **Cost:** Free (MIT License)
- **Platforms:** Standalone, iOS, Android, WebGL
- **Pros:** Full Socket.IO protocol support, active community
- **Cons:** External dependency, requires package installation

**Installation via Git URL:**
```
Window > Package Manager > + > Add package from git URL
https://github.com/itisnajim/SocketIOUnity.git
```

#### 2. Best HTTP/2 with Socket.IO
- **Source:** Unity Asset Store
- **Cost:** $90 (one-time)
- **Platforms:** All platforms
- **Pros:** Professional support, well documented, very reliable
- **Cons:** Expensive, vendor lock-in

#### 3. Socket.IO Client for Unity (NativeWebSocket)
- **Repository:** https://github.com/doghappy/socket.io-client-csharp
- **Cost:** Free
- **Platforms:** Standalone, iOS, Android (WebGL needs extra work)
- **Pros:** Pure C# implementation, good performance
- **Cons:** WebGL support requires additional work

## Our Implementation Choice

We're using **Native WebSocket with Custom Socket.IO Protocol** for maximum compatibility and sustainability.

### Files Created:
1. `SocketIOClient.cs` - Custom Socket.IO client using WebSockets
2. `NetworkManager.cs` - Enhanced with real Socket.IO integration
3. No external packages required!

### Platform Support:
- ✅ Windows/Mac/Linux (Standalone)
- ✅ iOS
- ✅ Android
- ✅ WebGL
- ✅ Unity Editor

### Testing:
```bash
# Start the server
cd "3D Chess 2/server"
node server-enhanced.js

# Unity will connect to http://localhost:3000
# Production: Update serverUrl in NetworkManager
```

## Migration Notes

If you want to switch to a different Socket.IO implementation later:
1. The NetworkManager API remains the same
2. Only the internal socket implementation changes
3. All events and callbacks are identical
4. Just swap the `#define` directive at the top of NetworkManager.cs

## Troubleshooting

### WebGL CORS Issues
If you encounter CORS errors in WebGL builds:
- Server already has CORS configured (`cors: { origin: "*" }`)
- Ensure server is running on HTTPS in production
- Check browser console for specific errors

### iOS Certificate Issues
For iOS builds using HTTPS:
- Ensure your production server has a valid SSL certificate
- For development, use `http://` (not recommended for production)

### Connection Timeouts
If connections timeout:
- Check firewall settings
- Verify server URL is accessible from target device
- Increase `connectionTimeout` in NetworkManager inspector
