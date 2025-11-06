# 3D Chess Server Deployment Guide

## Local Development

### Quick Start
```bash
# Make start script executable
chmod +x start.sh

# Start server
./start.sh

# Or start in development mode (auto-reload)
./start.sh dev
```

### Manual Setup
```bash
# Install dependencies
npm install

# Start server
npm start

# Development with auto-reload
npm run dev
```

## Testing the Server

### 1. Basic Server Test
```bash
# Test server functionality
npm test

# Test specific components
npm run test:basic   # Connection test
npm run test:room    # Room management
npm run test:move    # Move handling
```

### 2. Web Dashboard Test
1. Start server: `npm start`
2. Open browser: http://localhost:3000
3. Check server stats and room monitoring

### 3. Unity Integration Test
1. Start server locally
2. In Unity, set NetworkManager server URL to `http://localhost:3000`
3. Test creating/joining rooms through Unity UI

## Cloud Deployment Options

### Option 1: Heroku (Easiest)

1. **Setup:**
   ```bash
   # Install Heroku CLI
   # Sign up at heroku.com
   
   # Login and create app
   heroku login
   heroku create your-chess-server-name
   ```

2. **Deploy:**
   ```bash
   # Push to Heroku
   git add .
   git commit -m "Deploy 3D Chess Server"
   git push heroku main
   ```

3. **Configure:**
   ```bash
   # Set environment variables
   heroku config:set NODE_ENV=production
   heroku config:set PORT=80
   ```

4. **Monitor:**
   ```bash
   # View logs
   heroku logs --tail
   
   # Open app
   heroku open
   ```

### Option 2: Railway.app (Modern & Fast)

1. **Setup:**
   ```bash
   # Install Railway CLI
   npm install -g @railway/cli
   
   # Login
   railway login
   ```

2. **Deploy:**
   ```bash
   # Initialize project
   railway init
   
   # Deploy
   railway up
   ```

3. **Get URL:**
   ```bash
   # Get deployment URL
   railway domain
   ```

### Option 3: Digital Ocean Droplet (Full Control)

1. **Create Droplet:**
   - Choose Ubuntu 22.04
   - At least 1GB RAM
   - Enable monitoring

2. **Setup Server:**
   ```bash
   # SSH into droplet
   ssh root@your-droplet-ip
   
   # Install Node.js
   curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
   sudo apt-get install -y nodejs
   
   # Install PM2 for process management
   npm install -g pm2
   ```

3. **Deploy Application:**
   ```bash
   # Clone your code
   git clone your-repo-url
   cd 3d-chess-server/server
   
   # Install dependencies
   npm install --production
   
   # Start with PM2
   pm2 start server-enhanced.js --name "chess-server"
   pm2 startup
   pm2 save
   ```

4. **Setup Nginx (Optional):**
   ```bash
   sudo apt install nginx
   
   # Configure nginx reverse proxy
   sudo nano /etc/nginx/sites-available/chess-server
   ```

### Option 4: Docker Deployment

1. **Build Image:**
   ```bash
   docker build -t 3d-chess-server .
   ```

2. **Run Container:**
   ```bash
   docker run -d -p 3000:3000 --name chess-server 3d-chess-server
   ```

3. **Using Docker Compose:**
   ```bash
   docker-compose up -d
   ```

4. **Deploy to Cloud:**
   ```bash
   # Push to Docker Hub
   docker tag 3d-chess-server your-username/3d-chess-server
   docker push your-username/3d-chess-server
   
   # Deploy on any Docker-compatible platform
   ```

## Environment Configuration

### Production Environment Variables

Create `.env` file or set in your deployment platform:

```bash
# Server
PORT=3000
NODE_ENV=production

# Security
JWT_SECRET=your-very-secure-secret-key
ALLOWED_ORIGINS=https://yourdomain.com

# Unity
UNITY_AUTH_TOKEN=UNITY
ENABLE_UNITY_AUTH=true

# Logging
LOG_LEVEL=info
LOG_TO_FILE=true

# Game Settings
MAX_ROOMS=1000
MAX_PLAYERS_PER_ROOM=6
```

### SSL/HTTPS Setup

For production, you'll need HTTPS:

1. **Using Cloudflare (Recommended):**
   - Point your domain to your server
   - Enable Cloudflare proxy
   - SSL automatically handled

2. **Using Let's Encrypt:**
   ```bash
   # Install certbot
   sudo apt install certbot python3-certbot-nginx
   
   # Get certificate
   sudo certbot --nginx -d yourdomain.com
   ```

## Monitoring & Maintenance

### Health Checks

```bash
# Check server status
curl http://your-server-url/api/stats

# Monitor logs
pm2 logs chess-server  # For PM2
heroku logs --tail     # For Heroku
docker logs chess-server  # For Docker
```

### Performance Monitoring

1. **Built-in Dashboard:**
   - Access at `http://your-server-url`
   - Monitor active rooms, players, uptime

2. **Server Metrics:**
   ```bash
   # Check resource usage
   pm2 monit  # For PM2
   docker stats  # For Docker
   ```

### Scaling

1. **Vertical Scaling:**
   - Increase server resources (RAM, CPU)
   - Upgrade hosting plan

2. **Horizontal Scaling:**
   - Use Redis for session storage
   - Load balance multiple server instances
   - Use clustering:
   ```javascript
   // Add to server-enhanced.js for clustering
   const cluster = require('cluster');
   const numCPUs = require('os').cpus().length;
   
   if (cluster.isMaster) {
     for (let i = 0; i < numCPUs; i++) {
       cluster.fork();
     }
   } else {
     // Your server code here
   }
   ```

## Troubleshooting

### Common Issues

1. **Port Already in Use:**
   ```bash
   # Find process using port 3000
   lsof -i :3000
   
   # Kill process
   kill -9 PID
   ```

2. **Dependencies Issues:**
   ```bash
   # Clear npm cache
   npm cache clean --force
   
   # Remove node_modules and reinstall
   rm -rf node_modules package-lock.json
   npm install
   ```

3. **Memory Issues:**
   ```bash
   # Increase Node.js memory limit
   node --max-old-space-size=4096 server-enhanced.js
   ```

### Logs & Debugging

```bash
# Development debugging
DEBUG=* npm run dev

# Production logs
npm start > server.log 2>&1 &

# Monitor logs
tail -f server.log
```

## Unity Integration After Deployment

Once deployed, update your Unity NetworkManager:

```csharp
// Set your deployed server URL
NetworkManager.Instance.SetServerUrl("https://your-deployed-server.herokuapp.com");

// Or for custom domain
NetworkManager.Instance.SetServerUrl("https://chess.yourdomain.com");
```

## Security Checklist

- [ ] Use HTTPS in production
- [ ] Set strong JWT_SECRET
- [ ] Configure CORS properly
- [ ] Enable rate limiting
- [ ] Use secure authentication tokens
- [ ] Regular security updates
- [ ] Monitor for unusual activity

## Cost Optimization

### Free Tier Options:
- **Heroku**: 1000 free hours/month
- **Railway**: $5/month with generous limits  
- **Vercel**: Free for hobby projects
- **Netlify**: Free tier available

### Paid Recommendations:
- **Small Scale**: Railway.app ($5-10/month)
- **Medium Scale**: DigitalOcean droplet ($10-20/month)
- **Large Scale**: AWS/GCP with auto-scaling