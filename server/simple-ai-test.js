/**
 * Simple AI Test - Quick validation of the AI system
 */

const ChessBrain = require('./ai-logic/chess-brain.js');

async function simpleAITest() {
    console.log('🧪 Simple AI Test...\n');
    
    // Very simple test board - just a few pieces
    const simpleBoardState = {
        boardSize: 4,
        pieces: {
            '0,0,0': { type: 'king', color: 'white' },
            '1,0,0': { type: 'pawn', color: 'white' },
            '2,3,3': { type: 'king', color: 'black' },
            '1,3,3': { type: 'pawn', color: 'black' }
        }
    };
    
    console.log('📋 Board state:', JSON.stringify(simpleBoardState, null, 2));
    
    const brain = new ChessBrain('easy', 'balanced', true);
    
    try {
        console.log('🤖 AI thinking...');
        const startTime = Date.now();
        const bestMove = await brain.getBestMove(simpleBoardState, 'white');
        const endTime = Date.now();
        
        if (bestMove) {
            console.log(`✅ AI found move: ${JSON.stringify(bestMove)}`);
            console.log(`⏱️  Thinking time: ${endTime - startTime}ms`);
            
            const stats = brain.getStatistics();
            console.log(`📊 Nodes evaluated: ${stats.nodesEvaluated}`);
            console.log(`💾 Cache hits: ${stats.cacheHits}`);
        } else {
            console.log(`❌ No move found`);
        }
        
    } catch (error) {
        console.error(`💥 Error:`, error.message);
        console.error(error.stack);
    }
    
    console.log('\n🏁 Simple AI Test Complete!');
}

if (require.main === module) {
    simpleAITest().catch(error => {
        console.error('Test failed:', error);
        process.exit(1);
    });
}

module.exports = { simpleAITest };