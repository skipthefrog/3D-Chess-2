/**
 * Debug AI Test - Find the issue with move generation
 */

const ChessBrain = require('./ai-logic/chess-brain.js');

function debugMoveGeneration() {
    console.log('🐛 Debug Move Generation...\n');
    
    const brain = new ChessBrain('easy', 'balanced', true);
    
    // Very simple test board
    const boardState = {
        boardSize: 2, // Even smaller board
        pieces: {
            '0,0,0': { type: 'king', color: 'white' },
            '1,1,1': { type: 'king', color: 'black' }
        }
    };
    
    console.log('📋 Board state:', JSON.stringify(boardState, null, 2));
    
    console.log('\n🔍 Testing move generation for white king...');
    
    try {
        const moves = brain.getAllPossibleMoves(boardState, 'white');
        console.log(`✅ Found ${moves.length} moves:`, moves);
        
        if (moves.length === 0) {
            console.log('❌ No moves found - debugging piece validation...');
            
            // Debug individual piece
            const piece = boardState.pieces['0,0,0'];
            console.log('🔧 Piece at 0,0,0:', piece);
            
            const pieceMoves = brain.getPieceValidMoves(boardState, 0, 0, 0, piece);
            console.log('🔧 Piece moves:', pieceMoves);
            
            // Test specific move validation
            console.log('🔧 Testing move (0,0,0) -> (0,0,1)...');
            const isValid = brain.isValidMoveForPiece(boardState, piece, 0, 0, 0, 0, 0, 1);
            console.log('🔧 Is valid:', isValid);
            
            console.log('🔧 Testing king movement validation...');
            const kingValid = brain.isValidKingMove(0, 0, 1);
            console.log('🔧 King move valid:', kingValid);
        }
        
    } catch (error) {
        console.error(`💥 Error in move generation:`, error.message);
        console.error(error.stack);
    }
    
    console.log('\n🏁 Debug Complete!');
}

if (require.main === module) {
    debugMoveGeneration();
}

module.exports = { debugMoveGeneration };