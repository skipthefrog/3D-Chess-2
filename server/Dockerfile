# 3D Chess Server Dockerfile
FROM node:18-alpine

# Set working directory
WORKDIR /app

# Install dependencies first (for better Docker layer caching)
COPY package*.json ./
RUN npm ci --only=production

# Copy application code
COPY . .

# Create non-root user for security
RUN addgroup -g 1001 -S nodejs
RUN adduser -S chess -u 1001

# Change ownership and switch to non-root user
RUN chown -R chess:nodejs /app
USER chess

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3000/api/stats || exit 1

# Start the server
CMD ["node", "server-enhanced.js"]