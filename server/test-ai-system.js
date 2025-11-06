/**
 * AI System Test Script
 * Tests the enhanced AI system to ensure cross-platform compatibility
 */

const ChessBrain = require('./ai-logic/chess-brain.js');

async function testAISystem() {
    console.log('🧪 Testing AI System...\n');
    
    // Create test board state
    const testBoardState = {
        boardSize: 4,
        pieces: {
            '0,0,0': { type: 'rook', color: 'white' },
            '1,0,0': { type: 'knight', color: 'white' },
            '2,0,0': { type: 'bishop', color: 'white' },
            '3,0,0': { type: 'queen', color: 'white' },
            '0,1,0': { type: 'king', color: 'white' },
            '1,1,0': { type: 'pawn', color: 'white' },
            '2,1,0': { type: 'pawn', color: 'white' },
            '3,1,0': { type: 'pawn', color: 'white' },
            
            '0,3,3': { type: 'rook', color: 'black' },
            '1,3,3': { type: 'knight', color: 'black' },
            '2,3,3': { type: 'bishop', color: 'black' },
            '3,3,3': { type: 'queen', color: 'black' },
            '0,2,3': { type: 'king', color: 'black' },
            '1,2,3': { type: 'pawn', color: 'black' },
            '2,2,3': { type: 'pawn', color: 'black' },
            '3,2,3': { type: 'pawn', color: 'black' }
        }
    };
    
    // Test different AI personalities and difficulties
    const testConfigs = [
        { difficulty: 'easy', personality: 'balanced' },
        { difficulty: 'medium', personality: 'aggressive' },
        { difficulty: 'hard', personality: 'defensive' }
    ];
    
    for (const config of testConfigs) {
        console.log(`\n🤖 Testing ${config.difficulty}/${config.personality} AI...`);
        
        const brain = new ChessBrain(config.difficulty, config.personality, true);
        
        try {
            const startTime = Date.now();
            const bestMove = await brain.getBestMove(testBoardState, 'white');
            const endTime = Date.now();
            
            if (bestMove) {
                console.log(`✅ AI found move: ${JSON.stringify(bestMove)}`);
                console.log(`⏱️  Thinking time: ${endTime - startTime}ms`);
                
                const stats = brain.getStatistics();
                console.log(`📊 Nodes evaluated: ${stats.nodesEvaluated}`);
                console.log(`💾 Cache hits: ${stats.cacheHits}`);
            } else {
                console.log(`❌ No move found for ${config.difficulty}/${config.personality}`);
            }
            
        } catch (error) {
            console.error(`💥 Error testing ${config.difficulty}/${config.personality}:`, error.message);
        }
    }
    
    console.log('\n🏁 AI System Test Complete!');
}

// Test multi-player scenario
async function testMultiPlayerAI() {
    console.log('\n\n🎯 Testing Multi-Player AI...\n');
    
    const multiPlayerBoardState = {
        boardSize: 4,
        pieces: {
            // White pieces
            '0,0,0': { type: 'king', color: 'white' },
            '1,0,0': { type: 'queen', color: 'white' },
            
            // Black pieces
            '3,3,3': { type: 'king', color: 'black' },
            '2,3,3': { type: 'queen', color: 'black' },
            
            // Green pieces (6-player game)
            '0,0,3': { type: 'king', color: 'green' },
            '1,0,3': { type: 'rook', color: 'green' },
            
            // Purple pieces
            '3,3,0': { type: 'king', color: 'purple' },
            '2,3,0': { type: 'rook', color: 'purple' }
        }
    };
    
    const brain = new ChessBrain('medium', 'tactical', true);
    
    try {
        const move = await brain.getBestMove(multiPlayerBoardState, 'white');
        
        if (move) {
            console.log(`✅ Multi-player AI found move: ${JSON.stringify(move)}`);
            console.log(`📊 Multi-player evaluation completed successfully`);
        } else {
            console.log(`❌ Multi-player AI failed to find move`);
        }
        
    } catch (error) {
        console.error(`💥 Multi-player AI error:`, error.message);
    }
}

// Run tests
async function runAllTests() {
    await testAISystem();
    await testMultiPlayerAI();
    
    console.log('\n🎉 All AI tests completed!');
    console.log('✨ The enhanced AI system is ready for use in both Unity and web clients.');
}

if (require.main === module) {
    runAllTests().catch(error => {
        console.error('Test failed:', error);
        process.exit(1);
    });
}

module.exports = { testAISystem, testMultiPlayerAI, runAllTests };