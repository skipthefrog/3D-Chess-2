#!/bin/bash
echo "🤖 Testing AI Bot connection to room LMF8S2..."
echo "🎮 Starting AI Bot with medium difficulty"
cd "/Users/skip/3D Chess 2/server"
timeout 15s node ai-bot.js LMF8S2 medium
echo "✅ AI Bot test completed"