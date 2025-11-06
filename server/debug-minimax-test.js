/**
 * Debug Minimax Test - Find the issue with minimax search
 */

const ChessBrain = require('./ai-logic/chess-brain.js');

async function debugMinimaxSearch() {
    console.log('🐛 Debug Minimax Search...\n');
    
    const brain = new ChessBrain('easy', 'balanced', true);
    
    // Very simple test board
    const boardState = {
        boardSize: 2, // Small board
        pieces: {
            '0,0,0': { type: 'king', color: 'white' },
            '1,1,1': { type: 'king', color: 'black' }
        }
    };
    
    console.log('📋 Board state:', JSON.stringify(boardState, null, 2));
    
    try {
        console.log('🔍 Getting possible moves...');
        const moves = brain.getAllPossibleMoves(boardState, 'white');
        console.log(`✅ Found ${moves.length} moves`);
        
        if (moves.length > 0) {
            console.log('🔍 Testing minimax search...');
            
            // Manually call minimaxSearch with reduced depth
            console.log('🔧 Calling minimaxSearch with depth 1...');
            
            brain.searchStartTime = Date.now();
            const bestMove = await brain.minimaxSearch(boardState, 'white', 1, moves.slice(0, 3)); // Just first 3 moves
            
            console.log('✅ Minimax search completed:', bestMove);
        }
        
    } catch (error) {
        console.error(`💥 Error in minimax search:`, error.message);
        console.error(error.stack);
    }
    
    console.log('\n🏁 Debug Minimax Complete!');
}

if (require.main === module) {
    debugMinimaxSearch().catch(error => {
        console.error('Debug failed:', error);
        process.exit(1);
    });
}

module.exports = { debugMinimaxSearch };