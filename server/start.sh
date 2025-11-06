#!/bin/bash

# 3D Chess Server Quick Start Script

echo "🎮 3D Chess Server Setup"
echo "========================"

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 16+ first."
    echo "   Download from: https://nodejs.org/"
    exit 1
fi

# Check Node.js version
NODE_VERSION=$(node --version | cut -d'.' -f1 | tr -d 'v')
if [ "$NODE_VERSION" -lt 16 ]; then
    echo "❌ Node.js version $NODE_VERSION detected. Please upgrade to Node.js 16+."
    exit 1
fi

echo "✅ Node.js $(node --version) detected"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
    if [ $? -ne 0 ]; then
        echo "❌ Failed to install dependencies"
        exit 1
    fi
    echo "✅ Dependencies installed"
else
    echo "✅ Dependencies already installed"
fi

# Copy environment file if it doesn't exist
if [ ! -f ".env" ]; then
    echo "📋 Setting up environment configuration..."
    cp .env.example .env
    echo "✅ Environment file created (.env)"
fi

# Start the server
echo ""
echo "🚀 Starting 3D Chess Server..."
echo "   Server URL: http://localhost:3000"
echo "   Dashboard: http://localhost:3000"
echo "   Press Ctrl+C to stop"
echo ""

# Check if we should run in development mode
if [ "$1" = "dev" ]; then
    echo "🔧 Starting in development mode (auto-reload enabled)"
    npm run dev
else
    npm start
fi