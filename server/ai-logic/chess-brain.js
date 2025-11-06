/**
 * Chess Brain - Advanced AI Logic for 3D Chess
 * Shared logic between Unity and web clients for consistent AI behavior
 * Provides sophisticated position evaluation and move selection
 */

const ChessLogic = require('../public/js/chess-logic.js');

class ChessBrain {
    constructor(difficulty = 'medium', personality = 'balanced', enableDebug = false) {
        this.difficulty = difficulty;
        this.personality = personality;
        this.enableDebug = enableDebug;
        
        // Performance settings
        this.maxSearchDepth = this.getMaxDepthForDifficulty(difficulty);
        this.maxThinkingTime = this.getMaxTimeForDifficulty(difficulty);
        
        // Personality weights
        this.personalityWeights = this.getPersonalityWeights(personality);
        
        // Evaluation cache
        this.evaluationCache = new Map();
        this.maxCacheSize = 10000;
        
        // Statistics tracking
        this.stats = {
            movesCalculated: 0,
            nodesEvaluated: 0,
            cacheHits: 0,
            averageThinkingTime: 0
        };
        
        if (enableDebug) {
            console.log(`🧠 ChessBrain initialized: ${difficulty}/${personality}, max depth: ${this.maxSearchDepth}, max time: ${this.maxThinkingTime}s`);
        }
    }
    
    /**
     * Get the best move for the given player color
     */
    async getBestMove(boardState, playerColor, gameConfig = {}) {
        const startTime = Date.now();
        
        if (this.enableDebug) {
            console.log(`🤖 ChessBrain: Finding best move for ${playerColor}`);
        }
        
        try {
            // Validate inputs
            if (!boardState || !playerColor) {
                throw new Error('Invalid input parameters');
            }
            
            // Get all possible moves
            const possibleMoves = this.getAllPossibleMoves(boardState, playerColor);
            
            if (possibleMoves.length === 0) {
                console.error(`❌ ChessBrain: No legal moves for ${playerColor}`);
                return null;
            }
            
            if (possibleMoves.length === 1) {
                // Only one move available
                const move = possibleMoves[0];
                if (this.enableDebug) {
                    console.log(`🎯 ChessBrain: Only one legal move: ${JSON.stringify(move)}`);
                }
                return move;
            }
            
            // Use minimax with alpha-beta pruning
            const bestMove = await this.minimaxSearch(boardState, playerColor, this.maxSearchDepth, possibleMoves);
            
            const thinkingTime = (Date.now() - startTime) / 1000;
            this.updateStatistics(thinkingTime);
            
            if (this.enableDebug) {
                console.log(`✅ ChessBrain: Best move found in ${thinkingTime.toFixed(2)}s: ${JSON.stringify(bestMove)}`);
                console.log(`📊 Stats: ${this.stats.nodesEvaluated} nodes, ${this.stats.cacheHits} cache hits`);
            }
            
            return bestMove;
            
        } catch (error) {
            console.error(`💥 ChessBrain error:`, error);
            // Return random move as fallback
            const possibleMoves = this.getAllPossibleMoves(boardState, playerColor);
            if (possibleMoves.length > 0) {
                const randomMove = possibleMoves[Math.floor(Math.random() * possibleMoves.length)];
                console.log(`🎲 ChessBrain: Returning random fallback move: ${JSON.stringify(randomMove)}`);
                return randomMove;
            }
            return null;
        }
    }
    
    /**
     * Minimax search with alpha-beta pruning
     */
    async minimaxSearch(boardState, playerColor, depth, moves) {
        this.searchStartTime = Date.now(); // Initialize search start time
        
        let bestMove = null;
        let bestScore = -Infinity;
        const alpha = -Infinity;
        const beta = Infinity;
        
        // Sort moves by basic heuristics for better pruning
        const sortedMoves = this.sortMovesByPriority(moves, boardState, playerColor);
        
        for (const move of sortedMoves) {
            // Apply the move temporarily
            const newBoardState = this.applyMove(boardState, move);
            
            // Evaluate this position
            const score = await this.minimax(newBoardState, depth - 1, false, alpha, beta, playerColor);
            
            if (score > bestScore) {
                bestScore = score;
                bestMove = move;
            }
            
            // Check time limit
            if (this.isTimeUp()) {
                if (this.enableDebug) {
                    console.log(`⏰ ChessBrain: Time limit reached, returning current best move`);
                }
                break;
            }
        }
        
        return bestMove;
    }
    
    /**
     * Recursive minimax with alpha-beta pruning
     */
    async minimax(boardState, depth, isMaximizing, alpha, beta, originalPlayer) {
        this.stats.nodesEvaluated++;
        
        // Base case: reached maximum depth or terminal position
        if (depth === 0 || this.isTerminalPosition(boardState)) {
            return this.evaluatePosition(boardState, originalPlayer);
        }
        
        const currentPlayer = isMaximizing ? originalPlayer : this.getNextPlayer(originalPlayer);
        const moves = this.getAllPossibleMoves(boardState, currentPlayer);
        
        if (moves.length === 0) {
            // No moves available - terminal position
            return this.evaluatePosition(boardState, originalPlayer);
        }
        
        if (isMaximizing) {
            let maxEval = -Infinity;
            
            for (const move of moves) {
                const newBoardState = this.applyMove(boardState, move);
                const eval_score = await this.minimax(newBoardState, depth - 1, false, alpha, beta, originalPlayer);
                
                maxEval = Math.max(maxEval, eval_score);
                alpha = Math.max(alpha, eval_score);
                
                if (beta <= alpha) {
                    break; // Alpha-beta pruning
                }
                
                if (this.isTimeUp()) break;
            }
            
            return maxEval;
        } else {
            let minEval = Infinity;
            
            for (const move of moves) {
                const newBoardState = this.applyMove(boardState, move);
                const eval_score = await this.minimax(newBoardState, depth - 1, true, alpha, beta, originalPlayer);
                
                minEval = Math.min(minEval, eval_score);
                beta = Math.min(beta, eval_score);
                
                if (beta <= alpha) {
                    break; // Alpha-beta pruning
                }
                
                if (this.isTimeUp()) break;
            }
            
            return minEval;
        }
    }
    
    /**
     * Evaluate board position for a specific player
     */
    evaluatePosition(boardState, playerColor) {
        // Check cache first
        const positionKey = this.generatePositionKey(boardState, playerColor);
        if (this.evaluationCache.has(positionKey)) {
            this.stats.cacheHits++;
            return this.evaluationCache.get(positionKey);
        }
        
        let totalScore = 0;
        
        // Material evaluation
        const materialScore = this.evaluateMaterial(boardState, playerColor);
        totalScore += materialScore * this.personalityWeights.material;
        
        // Positional evaluation
        const positionalScore = this.evaluatePosition3D(boardState, playerColor);
        totalScore += positionalScore * this.personalityWeights.positional;
        
        // King safety
        const kingSafetyScore = this.evaluateKingSafety(boardState, playerColor);
        totalScore += kingSafetyScore * this.personalityWeights.kingSafety;
        
        // Piece mobility
        const mobilityScore = this.evaluateMobility(boardState, playerColor);
        totalScore += mobilityScore * this.personalityWeights.mobility;
        
        // Center control (3D specific)
        const centerScore = this.evaluateCenterControl(boardState, playerColor);
        totalScore += centerScore * this.personalityWeights.centerControl;
        
        // Layer control (3D specific)
        const layerScore = this.evaluateLayerControl(boardState, playerColor);
        totalScore += layerScore * this.personalityWeights.layerControl;
        
        // Multi-player considerations
        if (this.isMultiPlayerGame(boardState)) {
            const multiPlayerScore = this.evaluateMultiPlayerPosition(boardState, playerColor);
            totalScore += multiPlayerScore * this.personalityWeights.multiPlayer;
        }
        
        // Cache the result
        if (this.evaluationCache.size < this.maxCacheSize) {
            this.evaluationCache.set(positionKey, totalScore);
        }
        
        return totalScore;
    }
    
    /**
     * Evaluate material balance
     */
    evaluateMaterial(boardState, playerColor) {
        const pieceValues = {
            'pawn': 1,
            'knight': 3,
            'bishop': 3,
            'rook': 5,
            'queen': 9,
            'king': 1000
        };
        
        let playerMaterial = 0;
        let opponentMaterial = 0;
        
        for (const position in boardState.pieces) {
            const piece = boardState.pieces[position];
            const value = pieceValues[piece.type] || 0;
            
            if (piece.color === playerColor) {
                playerMaterial += value;
            } else {
                opponentMaterial += value;
            }
        }
        
        return playerMaterial - opponentMaterial;
    }
    
    /**
     * Evaluate 3D positional factors
     */
    evaluatePosition3D(boardState, playerColor) {
        let positionalScore = 0;
        const boardSize = boardState.boardSize || 4;
        const center = Math.floor(boardSize / 2);
        
        for (const position in boardState.pieces) {
            const piece = boardState.pieces[position];
            if (piece.color !== playerColor) continue;
            
            const [x, y, z] = this.parsePosition(position);
            
            // Distance from center bonus
            const distFromCenter = Math.sqrt(
                Math.pow(x - center, 2) + 
                Math.pow(y - center, 2) + 
                Math.pow(z - center, 2)
            );
            
            const centerBonus = Math.max(0, 3 - distFromCenter) * 0.1;
            positionalScore += centerBonus;
            
            // Piece-specific positional bonuses
            switch (piece.type) {
                case 'pawn':
                    // Pawns prefer advancing
                    positionalScore += x * 0.1;
                    break;
                case 'knight':
                    // Knights prefer center positions
                    positionalScore += centerBonus * 2;
                    break;
                case 'bishop':
                    // Bishops prefer long diagonals
                    if (x === y || x + y === boardSize - 1 || y === z || y + z === boardSize - 1) {
                        positionalScore += 0.3;
                    }
                    break;
                case 'rook':
                    // Rooks prefer edges and corners for layer control
                    if (x === 0 || x === boardSize - 1 || y === 0 || y === boardSize - 1 || z === 0 || z === boardSize - 1) {
                        positionalScore += 0.2;
                    }
                    break;
                case 'queen':
                    // Queens prefer central, active positions
                    positionalScore += centerBonus * 1.5;
                    break;
                case 'king':
                    // Kings prefer safety (edges in early game)
                    const edgeDistance = Math.min(Math.min(x, boardSize - 1 - x), 
                                                 Math.min(Math.min(y, boardSize - 1 - y),
                                                         Math.min(z, boardSize - 1 - z)));
                    positionalScore += edgeDistance * -0.1; // Penalty for center positions
                    break;
            }
        }
        
        return positionalScore;
    }
    
    /**
     * Evaluate king safety
     */
    evaluateKingSafety(boardState, playerColor) {
        // Find the king
        let kingPosition = null;
        for (const position in boardState.pieces) {
            const piece = boardState.pieces[position];
            if (piece.type === 'king' && piece.color === playerColor) {
                kingPosition = position;
                break;
            }
        }
        
        if (!kingPosition) {
            return -1000; // King is missing!
        }
        
        let safetyScore = 0;
        const [kx, ky, kz] = this.parsePosition(kingPosition);
        
        // Count friendly pieces around king
        let protectors = 0;
        for (let dx = -1; dx <= 1; dx++) {
            for (let dy = -1; dy <= 1; dy++) {
                for (let dz = -1; dz <= 1; dz++) {
                    if (dx === 0 && dy === 0 && dz === 0) continue;
                    
                    const checkPos = this.formatPosition(kx + dx, ky + dy, kz + dz);
                    const piece = boardState.pieces[checkPos];
                    
                    if (piece && piece.color === playerColor) {
                        protectors++;
                    }
                }
            }
        }
        
        safetyScore += protectors * 0.5;
        
        // Penalty for being in center (dangerous)
        const boardSize = boardState.boardSize || 4;
        const center = Math.floor(boardSize / 2);
        const centerDistance = Math.abs(kx - center) + Math.abs(ky - center) + Math.abs(kz - center);
        safetyScore += centerDistance * 0.1;
        
        return safetyScore;
    }
    
    /**
     * Evaluate piece mobility
     */
    evaluateMobility(boardState, playerColor) {
        let mobilityScore = 0;
        const moves = this.getAllPossibleMoves(boardState, playerColor);
        
        // Basic mobility: number of legal moves
        mobilityScore += moves.length * 0.1;
        
        // TODO: Add more sophisticated mobility evaluation
        // - Piece-specific mobility weights
        // - Quality of moves (captures, center control, etc.)
        
        return mobilityScore;
    }
    
    /**
     * Evaluate center control
     */
    evaluateCenterControl(boardState, playerColor) {
        let centerScore = 0;
        const boardSize = boardState.boardSize || 4;
        const center = Math.floor(boardSize / 2);
        
        // Define center squares
        const centerSquares = [];
        for (let x = center - 1; x <= center; x++) {
            for (let y = center - 1; y <= center; y++) {
                for (let z = center - 1; z <= center; z++) {
                    if (x >= 0 && y >= 0 && z >= 0 && x < boardSize && y < boardSize && z < boardSize) {
                        centerSquares.push(this.formatPosition(x, y, z));
                    }
                }
            }
        }
        
        // Count pieces controlling center
        for (const centerSquare of centerSquares) {
            const piece = boardState.pieces[centerSquare];
            if (piece && piece.color === playerColor) {
                centerScore += 0.5;
            }
            
            // TODO: Add evaluation for squares attacked by pieces (not just occupied)
        }
        
        return centerScore;
    }
    
    /**
     * Evaluate layer control (3D specific)
     */
    evaluateLayerControl(boardState, playerColor) {
        let layerScore = 0;
        const boardSize = boardState.boardSize || 4;
        
        // Count pieces on each layer
        const xLayers = {};
        const yLayers = {};
        const zLayers = {};
        
        for (const position in boardState.pieces) {
            const piece = boardState.pieces[position];
            if (piece.color !== playerColor) continue;
            
            const [x, y, z] = this.parsePosition(position);
            
            xLayers[x] = (xLayers[x] || 0) + 1;
            yLayers[y] = (yLayers[y] || 0) + 1;
            zLayers[z] = (zLayers[z] || 0) + 1;
        }
        
        // Bonus for controlling layers with multiple pieces
        for (const count of Object.values(xLayers)) {
            if (count >= 2) layerScore += count * 0.1;
        }
        for (const count of Object.values(yLayers)) {
            if (count >= 2) layerScore += count * 0.1;
        }
        for (const count of Object.values(zLayers)) {
            if (count >= 2) layerScore += count * 0.1;
        }
        
        return layerScore;
    }
    
    /**
     * Evaluate multi-player specific factors
     */
    evaluateMultiPlayerPosition(boardState, playerColor) {
        // TODO: Implement multi-player evaluation
        // - Threat assessment from multiple opponents
        // - Alliance detection
        // - Resource allocation strategies
        return 0;
    }
    
    // === HELPER METHODS ===
    
    getAllPossibleMoves(boardState, playerColor) {
        const moves = [];
        
        // More efficient: iterate through pieces directly
        for (const position in boardState.pieces) {
            const piece = boardState.pieces[position];
            
            if (piece && piece.color === playerColor) {
                const [fromX, fromY, fromZ] = this.parsePosition(position);
                const pieceMoves = this.getPieceValidMoves(boardState, fromX, fromY, fromZ, piece);
                moves.push(...pieceMoves);
                
                // Limit moves per piece to prevent explosion
                if (moves.length > 100) {
                    if (this.enableDebug) {
                        console.log(`⚠️ Move generation limited to prevent timeout (${moves.length} moves)`);
                    }
                    break;
                }
            }
        }
        
        return moves;
    }
    
    getPieceValidMoves(boardState, fromX, fromY, fromZ, piece) {
        const moves = [];
        const boardSize = boardState.boardSize || 4;
        
        // Test all possible destination squares
        for (let toX = 0; toX < boardSize; toX++) {
            for (let toY = 0; toY < boardSize; toY++) {
                for (let toZ = 0; toZ < boardSize; toZ++) {
                    if (this.isValidMoveForPiece(boardState, piece, fromX, fromY, fromZ, toX, toY, toZ)) {
                        moves.push({
                            from: this.formatPosition(fromX, fromY, fromZ),
                            to: this.formatPosition(toX, toY, toZ),
                            piece: piece.type,
                            player: piece.color
                        });
                        
                        // Safety limit per piece
                        if (moves.length > 20) break;
                    }
                }
                if (moves.length > 20) break;
            }
            if (moves.length > 20) break;
        }
        
        return moves;
    }
    
    isValidMoveForPiece(boardState, piece, fromX, fromY, fromZ, toX, toY, toZ) {
        // Basic validation
        if (fromX === toX && fromY === toY && fromZ === toZ) return false;
        
        const boardSize = boardState.boardSize || 4;
        if (toX < 0 || toX >= boardSize || toY < 0 || toY >= boardSize || toZ < 0 || toZ >= boardSize) {
            return false;
        }
        
        const toPos = this.formatPosition(toX, toY, toZ);
        const targetPiece = boardState.pieces[toPos];
        
        // Can't capture own pieces
        if (targetPiece && targetPiece.color === piece.color) {
            return false;
        }
        
        // Piece-specific movement rules
        return this.isValidPieceMovement(piece.type, fromX, fromY, fromZ, toX, toY, toZ, boardState);
    }
    
    isValidPieceMovement(pieceType, fromX, fromY, fromZ, toX, toY, toZ, boardState) {
        const dx = toX - fromX;
        const dy = toY - fromY;
        const dz = toZ - fromZ;
        
        switch (pieceType) {
            case 'pawn':
                return this.isValidPawnMove(dx, dy, dz, fromX, fromY, fromZ, boardState);
            case 'rook':
                return this.isValidRookMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState);
            case 'bishop':
                return this.isValidBishopMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState);
            case 'queen':
                return this.isValidQueenMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState);
            case 'king':
                return this.isValidKingMove(dx, dy, dz);
            case 'knight':
                return this.isValidKnightMove(dx, dy, dz);
            default:
                return false;
        }
    }
    
    isValidPawnMove(dx, dy, dz, fromX, fromY, fromZ, boardState) {
        // Simplified pawn movement for 3D (pawns move in X direction)
        if (Math.abs(dy) > 1 || Math.abs(dz) > 1 || Math.abs(dx) > 2) return false;
        if (dx === 0 && dy === 0 && dz === 0) return false;
        
        // Forward moves
        if (dy === 0 && dz === 0 && dx > 0 && dx <= (fromX === 0 ? 2 : 1)) {
            const toPos = this.formatPosition(fromX + dx, fromY, fromZ);
            return !boardState.pieces[toPos]; // Must be empty
        }
        
        // Diagonal captures
        if (dx === 1 && (Math.abs(dy) === 1 || Math.abs(dz) === 1)) {
            const toPos = this.formatPosition(fromX + dx, fromY + dy, fromZ + dz);
            return boardState.pieces[toPos] != null; // Must capture
        }
        
        return false;
    }
    
    isValidRookMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState) {
        // Rook moves along axes
        const axisCount = (dx !== 0 ? 1 : 0) + (dy !== 0 ? 1 : 0) + (dz !== 0 ? 1 : 0);
        if (axisCount !== 1) return false;
        
        return this.isPathClear(fromX, fromY, fromZ, toX, toY, toZ, boardState);
    }
    
    isValidBishopMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState) {
        // Bishop moves diagonally
        if (Math.abs(dx) !== Math.abs(dy) && Math.abs(dy) !== Math.abs(dz) && Math.abs(dx) !== Math.abs(dz)) {
            return false;
        }
        
        return this.isPathClear(fromX, fromY, fromZ, toX, toY, toZ, boardState);
    }
    
    isValidQueenMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState) {
        return this.isValidRookMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState) ||
               this.isValidBishopMove(dx, dy, dz, fromX, fromY, fromZ, toX, toY, toZ, boardState);
    }
    
    isValidKingMove(dx, dy, dz) {
        return Math.abs(dx) <= 1 && Math.abs(dy) <= 1 && Math.abs(dz) <= 1 && 
               (dx !== 0 || dy !== 0 || dz !== 0);
    }
    
    isValidKnightMove(dx, dy, dz) {
        // 3D knight moves: (2,1,0), (2,0,1), (1,2,0), (1,0,2), (0,2,1), (0,1,2) and their negatives
        const adx = Math.abs(dx);
        const ady = Math.abs(dy);
        const adz = Math.abs(dz);
        
        const coords = [adx, ady, adz].sort((a, b) => b - a);
        return (coords[0] === 2 && coords[1] === 1 && coords[2] === 0);
    }
    
    isPathClear(fromX, fromY, fromZ, toX, toY, toZ, boardState) {
        const dx = Math.sign(toX - fromX);
        const dy = Math.sign(toY - fromY);
        const dz = Math.sign(toZ - fromZ);
        
        let x = fromX + dx;
        let y = fromY + dy;
        let z = fromZ + dz;
        
        while (x !== toX || y !== toY || z !== toZ) {
            const pos = this.formatPosition(x, y, z);
            if (boardState.pieces[pos]) {
                return false; // Path blocked
            }
            x += dx;
            y += dy;
            z += dz;
        }
        
        return true;
    }
    
    applyMove(boardState, move) {
        // Create a copy of the board state with the move applied
        const newState = JSON.parse(JSON.stringify(boardState));
        
        // Apply the move
        if (newState.pieces[move.to]) {
            // Capture
            delete newState.pieces[move.to];
        }
        
        newState.pieces[move.to] = newState.pieces[move.from];
        delete newState.pieces[move.from];
        
        return newState;
    }
    
    sortMovesByPriority(moves, boardState, playerColor) {
        // Sort moves by priority for better alpha-beta pruning
        return moves.sort((a, b) => {
            // Captures first
            const aCaptures = boardState.pieces[a.to] ? 1 : 0;
            const bCaptures = boardState.pieces[b.to] ? 1 : 0;
            
            return bCaptures - aCaptures;
        });
    }
    
    isTerminalPosition(boardState) {
        // Check for checkmate, stalemate, etc.
        // For now, just check if king is missing
        const colors = ['white', 'black', 'green', 'purple', 'yellow', 'orange'];
        
        for (const color of colors) {
            let hasKing = false;
            for (const position in boardState.pieces) {
                const piece = boardState.pieces[position];
                if (piece.type === 'king' && piece.color === color) {
                    hasKing = true;
                    break;
                }
            }
            if (!hasKing) {
                return true; // Missing king = terminal
            }
        }
        
        return false;
    }
    
    isMultiPlayerGame(boardState) {
        const colors = new Set();
        for (const position in boardState.pieces) {
            colors.add(boardState.pieces[position].color);
        }
        return colors.size > 2;
    }
    
    getNextPlayer(currentPlayer) {
        // For 2-player games, just alternate
        if (currentPlayer === 'white') return 'black';
        if (currentPlayer === 'black') return 'white';
        
        // For multi-player games - simplified rotation
        const players = ['white', 'black', 'green', 'purple', 'yellow', 'orange'];
        const currentIndex = players.indexOf(currentPlayer);
        if (currentIndex === -1) return 'black'; // Fallback
        return players[(currentIndex + 1) % players.length];
    }
    
    parsePosition(position) {
        // Parse position string like "1,2,3" to [1, 2, 3]
        return position.split(',').map(Number);
    }
    
    formatPosition(x, y, z) {
        return `${x},${y},${z}`;
    }
    
    generatePositionKey(boardState, playerColor) {
        // Simple position hash for caching
        return `${JSON.stringify(boardState.pieces)}_${playerColor}`;
    }
    
    isTimeUp() {
        return Date.now() - this.searchStartTime > this.maxThinkingTime * 1000;
    }
    
    getMaxDepthForDifficulty(difficulty) {
        switch (difficulty.toLowerCase()) {
            case 'easy': return 2;
            case 'medium': return 3;
            case 'hard': return 4;
            default: return 3;
        }
    }
    
    getMaxTimeForDifficulty(difficulty) {
        switch (difficulty.toLowerCase()) {
            case 'easy': return 1.0;
            case 'medium': return 2.5;
            case 'hard': return 4.0;
            default: return 2.5;
        }
    }
    
    getPersonalityWeights(personality) {
        const baseWeights = {
            material: 1.0,
            positional: 1.0,
            kingSafety: 1.0,
            mobility: 1.0,
            centerControl: 1.0,
            layerControl: 1.0,
            multiPlayer: 1.0
        };
        
        switch (personality.toLowerCase()) {
            case 'aggressive':
                return {
                    ...baseWeights,
                    material: 0.8,
                    kingSafety: 0.4,
                    mobility: 1.3,
                    centerControl: 1.4
                };
            case 'defensive':
                return {
                    ...baseWeights,
                    material: 1.2,
                    kingSafety: 1.6,
                    positional: 1.3
                };
            case 'tactical':
                return {
                    ...baseWeights,
                    mobility: 1.3,
                    centerControl: 1.2,
                    layerControl: 1.3
                };
            case 'balanced':
            default:
                return baseWeights;
        }
    }
    
    updateStatistics(thinkingTime) {
        this.stats.movesCalculated++;
        this.stats.averageThinkingTime = 
            (this.stats.averageThinkingTime + thinkingTime) / 2;
    }
    
    getStatistics() {
        return { ...this.stats };
    }
    
    clearCache() {
        this.evaluationCache.clear();
        if (this.enableDebug) {
            console.log('🧠 ChessBrain: Cache cleared');
        }
    }
}

module.exports = ChessBrain;