/**
 * 3D Chess Logic Engine
 * Handles all chess rules, piece movements, and game state validation
 */

class ChessLogic {
    constructor() {
        this.board = null;
        this.currentPlayer = 'white';
        this.gamePhase = 'waiting'; // waiting, placement, playing, ended
        this.boardSize = 'Large8x8x8';
        this.playerCount = 2;
        this.moveHistory = [];
        this.capturedPieces = { 
            white: [], black: [], green: [], 
            purple: [], yellow: [], orange: [] 
        };
        this.kingPositions = { 
            white: null, black: null, green: null,
            purple: null, yellow: null, orange: null 
        };
        this.castlingRights = {
            white: { kingside: true, queenside: true },
            black: { kingside: true, queenside: true },
            green: { kingside: true, queenside: true },
            purple: { kingside: true, queenside: true },
            yellow: { kingside: true, queenside: true },
            orange: { kingside: true, queenside: true }
        };
        this.enPassantTarget = null;
        this.halfmoveClock = 0;
        this.fullmoveNumber = 1;
        
        this.initializeBoard();
    }

    initializeBoard(gamePhase = 'placement') {
        // Initialize dynamic 3D chess board based on boardSize
        const size = BoardPosition.getBoardDimensions(this.boardSize);
        
        this.board = [];
        for (let x = 0; x < size; x++) {
            this.board[x] = [];
            for (let y = 0; y < size; y++) {
                this.board[x][y] = [];
                for (let z = 0; z < size; z++) {
                    this.board[x][y][z] = null;
                }
            }
        }
        
        // Set the current game phase
        this.gamePhase = gamePhase;
        
        // Initialize piece pools for placement phase
        this.piecePool = {
            white: this.createPiecePool(gamePhase),
            black: this.createPiecePool(gamePhase),
            green: this.createPiecePool(gamePhase),
            purple: this.createPiecePool(gamePhase),
            yellow: this.createPiecePool(gamePhase),
            orange: this.createPiecePool(gamePhase)
        };
        
        // Track placement progress
        this.placementComplete = {
            white: false,
            black: false,
            green: false,
            purple: false,
            yellow: false,
            orange: false
        };
        
        // Only setup pieces for playing phase, keep board empty for placement/waiting
        if (gamePhase === 'playing') {
            this.setupInitialPosition();
        }
        // For 'placement' and 'waiting' phases, board starts empty
        
        console.log(`Board initialized for phase: ${gamePhase}, board will be ${gamePhase === 'playing' ? 'populated' : 'empty'}`);
    }
    
    createPiecePool(phase = 'placement') {
        if (phase === 'placement') {
            // Phase 1: Only starting pieces (no pawns)
            return {
                king: 1,
                queen: 1,
                rook: 2,
                bishop: 2,
                knight: 2
            };
        } else if (phase === 'pawn_placement') {
            // Phase 2: Only pawns for automatic placement
            return {
                pawn: 8
            };
        } else {
            // Full piece set for other phases
            return {
                king: 1,
                queen: 1,
                rook: 2,
                bishop: 2,
                knight: 2,
                pawn: 8
            };
        }
    }

    setupInitialPosition() {
        // Standard chess setup on the middle level (z = 3)
        const backRow = ['rook', 'knight', 'bishop', 'queen', 'king', 'bishop', 'knight', 'rook'];
        
        // White pieces (y = 0, 1)
        for (let x = 0; x < 8; x++) {
            this.board[x][0][3] = { type: backRow[x], color: 'white' };
            this.board[x][1][3] = { type: 'pawn', color: 'white' };
        }
        
        // Black pieces (y = 6, 7)
        for (let x = 0; x < 8; x++) {
            this.board[x][7][3] = { type: backRow[x], color: 'black' };
            this.board[x][6][3] = { type: 'pawn', color: 'black' };
        }
        
        // Set king positions
        this.kingPositions.white = { x: 4, y: 0, z: 3 };
        this.kingPositions.black = { x: 4, y: 7, z: 3 };
    }
    
    // Piece placement methods
    canPlacePiece(pieceType, color, x, y, z) {
        // Check if position is valid
        if (!this.isValidPosition(x, y, z)) {
            return { valid: false, reason: 'Invalid position' };
        }
        
        // Check if position is empty
        if (this.board[x][y][z] !== null) {
            return { valid: false, reason: 'Position occupied' };
        }
        
        // Check if player has pieces of this type left
        if (this.piecePool[color][pieceType] <= 0) {
            return { valid: false, reason: 'No pieces of this type remaining' };
        }
        
        // Check placement zone restrictions
        if (!this.isValidPlacementZone(color, x, y, z)) {
            return { valid: false, reason: 'Invalid placement zone' };
        }
        
        return { valid: true };
    }
    
    isValidPlacementZone(color, x, y, z) {
        const size = BoardPosition.getBoardDimensions(this.boardSize);
        
        // Define placement zones based on Unity's system
        // Each color gets specific layers to place on
        switch (color) {
            case 'white':
                // White places on X=0 layer (left side)
                return x === 0;
            case 'black':
                // Black places on X=max layer (right side)
                return x === size - 1;
            case 'green':
                // Green places on Z=0 layer (front side)
                return z === 0;
            case 'purple':
                // Purple places on Z=max layer (back side)
                return z === size - 1;
            case 'yellow':
                // Yellow places on Y=0 layer (bottom)
                return y === 0;
            case 'orange':
                // Orange places on Y=max layer (top)
                return y === size - 1;
            default:
                return false;
        }
    }
    
    placePiece(pieceType, color, x, y, z) {
        const validation = this.canPlacePiece(pieceType, color, x, y, z);
        if (!validation.valid) {
            return validation;
        }
        
        // Place the piece
        this.board[x][y][z] = { type: pieceType, color: color };
        
        // Remove from piece pool
        this.piecePool[color][pieceType]--;
        
        // Update king position if king was placed
        if (pieceType === 'king') {
            this.kingPositions[color] = { x, y, z };
        }
        
        // Check if placement is complete for this color
        this.checkPlacementComplete(color);
        
        return { valid: true, placed: true };
    }
    
    checkPlacementComplete(color) {
        // Check if all pieces have been placed
        const pool = this.piecePool[color];
        const totalRemaining = Object.values(pool).reduce((sum, count) => sum + count, 0);
        
        if (totalRemaining === 0) {
            this.placementComplete[color] = true;
        }
    }
    
    /**
     * Get active player colors based on player count
     */
    getActivePlayerColors() {
        switch (this.playerCount) {
            case 2:
                return ['white', 'black'];
            case 4:
                return ['white', 'black', 'green', 'purple'];
            case 6:
                return ['white', 'black', 'green', 'purple', 'yellow', 'orange'];
            default:
                return ['white', 'black'];
        }
    }
    
    isPlacementPhaseComplete() {
        const activeColors = this.getActivePlayerColors();
        return activeColors.every(color => this.placementComplete[color]);
    }
    
    getPiecePool(color) {
        return { ...this.piecePool[color] };
    }

    // Position validation
    isValidPosition(x, y, z) {
        const size = BoardPosition.getBoardDimensions(this.boardSize);
        return x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size;
    }

    getPiece(x, y, z) {
        if (!this.isValidPosition(x, y, z)) return null;
        return this.board[x][y][z];
    }

    setPiece(x, y, z, piece) {
        if (!this.isValidPosition(x, y, z)) return false;
        this.board[x][y][z] = piece;
        return true;
    }

    // Move validation
    isValidMove(fromX, fromY, fromZ, toX, toY, toZ) {
        // Basic position validation
        if (!this.isValidPosition(fromX, fromY, fromZ) || !this.isValidPosition(toX, toY, toZ)) {
            return false;
        }

        const piece = this.getPiece(fromX, fromY, fromZ);
        if (!piece) return false;

        // Can't move opponent's pieces
        if (piece.color !== this.currentPlayer) return false;

        // Can't capture own pieces
        const targetPiece = this.getPiece(toX, toY, toZ);
        if (targetPiece && targetPiece.color === piece.color) return false;

        // Check piece-specific movement rules
        if (!this.isValidPieceMove(piece.type, fromX, fromY, fromZ, toX, toY, toZ)) {
            return false;
        }

        // Check if path is clear (for sliding pieces)
        if (this.isSlidingPiece(piece.type) && !this.isPathClear(fromX, fromY, fromZ, toX, toY, toZ)) {
            return false;
        }

        // Test if move leaves king in check
        if (this.wouldLeaveKingInCheck(fromX, fromY, fromZ, toX, toY, toZ)) {
            return false;
        }

        return true;
    }

    isValidPieceMove(pieceType, fromX, fromY, fromZ, toX, toY, toZ) {
        const dx = toX - fromX;
        const dy = toY - fromY;
        const dz = toZ - fromZ;

        switch (pieceType) {
            case 'pawn':
                return this.isValidPawnMove(fromX, fromY, fromZ, toX, toY, toZ);
            case 'rook':
                return this.isValidRookMove(dx, dy, dz);
            case 'knight':
                return this.isValidKnightMove(dx, dy, dz);
            case 'bishop':
                return this.isValidBishopMove(dx, dy, dz);
            case 'queen':
                return this.isValidQueenMove(dx, dy, dz);
            case 'king':
                return this.isValidKingMove(dx, dy, dz);
            default:
                return false;
        }
    }

    isValidPawnMove(fromX, fromY, fromZ, toX, toY, toZ) {
        const piece = this.getPiece(fromX, fromY, fromZ);
        const targetPiece = this.getPiece(toX, toY, toZ);
        const direction = piece.color === 'white' ? 1 : -1;
        
        const dx = toX - fromX;
        const dy = toY - fromY;
        const dz = toZ - fromZ;

        // Forward move (no capture)
        if (dx === 0 && dz === 0 && !targetPiece) {
            // Single step forward
            if (dy === direction) return true;
            
            // Double step from starting position
            if (dy === 2 * direction && this.isStartingPawnPosition(fromX, fromY, fromZ, piece.color)) {
                return true;
            }
        }

        // Diagonal capture (including 3D diagonals)
        if (Math.abs(dx) <= 1 && dy === direction && Math.abs(dz) <= 1 && targetPiece) {
            // Standard capture or 3D diagonal capture
            if ((Math.abs(dx) === 1 && dz === 0) || (dx === 0 && Math.abs(dz) === 1) || (Math.abs(dx) === 1 && Math.abs(dz) === 1)) {
                return targetPiece.color !== piece.color;
            }
        }

        // En passant (simplified for 3D)
        if (this.enPassantTarget && toX === this.enPassantTarget.x && toY === this.enPassantTarget.y && toZ === this.enPassantTarget.z) {
            return Math.abs(dx) === 1 && dy === direction && dz === 0;
        }

        return false;
    }

    isValidRookMove(dx, dy, dz) {
        // Rook moves in straight lines along one axis
        const nonZeroCount = (dx !== 0 ? 1 : 0) + (dy !== 0 ? 1 : 0) + (dz !== 0 ? 1 : 0);
        return nonZeroCount === 1;
    }

    isValidKnightMove(dx, dy, dz) {
        // Knight moves in L-shapes, including 3D L-shapes
        const absDx = Math.abs(dx);
        const absDy = Math.abs(dy);
        const absDz = Math.abs(dz);

        // Traditional 2D knight moves on any plane
        if (dz === 0 && ((absDx === 2 && absDy === 1) || (absDx === 1 && absDy === 2))) return true;
        if (dy === 0 && ((absDx === 2 && absDz === 1) || (absDx === 1 && absDz === 2))) return true;
        if (dx === 0 && ((absDy === 2 && absDz === 1) || (absDy === 1 && absDz === 2))) return true;

        // 3D knight moves
        if ((absDx === 2 && absDy === 1 && absDz === 1) || 
            (absDx === 1 && absDy === 2 && absDz === 1) || 
            (absDx === 1 && absDy === 1 && absDz === 2)) return true;

        return false;
    }

    isValidBishopMove(dx, dy, dz) {
        // Bishop moves diagonally, including 3D diagonals
        const absDx = Math.abs(dx);
        const absDy = Math.abs(dy);
        const absDz = Math.abs(dz);

        // 2D diagonal moves on any plane
        if (dz === 0 && absDx === absDy && absDx > 0) return true;
        if (dy === 0 && absDx === absDz && absDx > 0) return true;
        if (dx === 0 && absDy === absDz && absDy > 0) return true;

        // 3D diagonal moves
        if (absDx === absDy && absDy === absDz && absDx > 0) return true;

        return false;
    }

    isValidQueenMove(dx, dy, dz) {
        // Queen combines rook and bishop moves
        return this.isValidRookMove(dx, dy, dz) || this.isValidBishopMove(dx, dy, dz);
    }

    isValidKingMove(dx, dy, dz) {
        // King moves one square in any direction (including 3D)
        return Math.abs(dx) <= 1 && Math.abs(dy) <= 1 && Math.abs(dz) <= 1 && (dx !== 0 || dy !== 0 || dz !== 0);
    }

    isSlidingPiece(pieceType) {
        return ['rook', 'bishop', 'queen'].includes(pieceType);
    }

    isPathClear(fromX, fromY, fromZ, toX, toY, toZ) {
        const dx = toX - fromX;
        const dy = toY - fromY;
        const dz = toZ - fromZ;
        
        const steps = Math.max(Math.abs(dx), Math.abs(dy), Math.abs(dz));
        const stepX = dx === 0 ? 0 : dx / Math.abs(dx);
        const stepY = dy === 0 ? 0 : dy / Math.abs(dy);
        const stepZ = dz === 0 ? 0 : dz / Math.abs(dz);

        for (let i = 1; i < steps; i++) {
            const checkX = fromX + Math.round(stepX * i);
            const checkY = fromY + Math.round(stepY * i);
            const checkZ = fromZ + Math.round(stepZ * i);
            
            if (this.getPiece(checkX, checkY, checkZ)) {
                return false;
            }
        }

        return true;
    }

    isStartingPawnPosition(x, y, z, color) {
        if (z !== 3) return false; // Standard level
        return (color === 'white' && y === 1) || (color === 'black' && y === 6);
    }

    // Check and checkmate detection
    isInCheck(color) {
        const kingPos = this.kingPositions[color];
        if (!kingPos) return false;

        return this.isSquareAttacked(kingPos.x, kingPos.y, kingPos.z, color);
    }

    isSquareAttacked(x, y, z, byColor) {
        const attackingColor = byColor === 'white' ? 'black' : 'white';
        const size = BoardPosition.getBoardDimensions(this.boardSize);

        for (let fx = 0; fx < size; fx++) {
            for (let fy = 0; fy < size; fy++) {
                for (let fz = 0; fz < size; fz++) {
                    const piece = this.getPiece(fx, fy, fz);
                    if (piece && piece.color === attackingColor) {
                        if (this.canPieceAttackSquare(piece.type, fx, fy, fz, x, y, z)) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    canPieceAttackSquare(pieceType, fromX, fromY, fromZ, toX, toY, toZ) {
        if (pieceType === 'pawn') {
            return this.canPawnAttackSquare(fromX, fromY, fromZ, toX, toY, toZ);
        }

        const isValidMovement = this.isValidPieceMove(pieceType, fromX, fromY, fromZ, toX, toY, toZ);
        const isPathClear = this.isSlidingPiece(pieceType) ? 
            this.isPathClear(fromX, fromY, fromZ, toX, toY, toZ) : true;

        return isValidMovement && isPathClear;
    }

    canPawnAttackSquare(fromX, fromY, fromZ, toX, toY, toZ) {
        const piece = this.getPiece(fromX, fromY, fromZ);
        const direction = piece.color === 'white' ? 1 : -1;
        
        const dx = toX - fromX;
        const dy = toY - fromY;
        const dz = toZ - fromZ;

        // Pawn attacks diagonally forward (including 3D diagonals)
        if (dy === direction && Math.abs(dx) <= 1 && Math.abs(dz) <= 1) {
            return (Math.abs(dx) === 1 && dz === 0) || (dx === 0 && Math.abs(dz) === 1) || (Math.abs(dx) === 1 && Math.abs(dz) === 1);
        }

        return false;
    }

    wouldLeaveKingInCheck(fromX, fromY, fromZ, toX, toY, toZ) {
        // Temporarily make the move
        const piece = this.getPiece(fromX, fromY, fromZ);
        const capturedPiece = this.getPiece(toX, toY, toZ);
        
        this.setPiece(toX, toY, toZ, piece);
        this.setPiece(fromX, fromY, fromZ, null);

        // Update king position if moving king
        const originalKingPos = { ...this.kingPositions[piece.color] };
        if (piece.type === 'king') {
            this.kingPositions[piece.color] = { x: toX, y: toY, z: toZ };
        }

        const inCheck = this.isInCheck(piece.color);

        // Restore board state
        this.setPiece(fromX, fromY, fromZ, piece);
        this.setPiece(toX, toY, toZ, capturedPiece);
        this.kingPositions[piece.color] = originalKingPos;

        return inCheck;
    }

    isCheckmate(color) {
        if (!this.isInCheck(color)) return false;
        return this.getAllValidMoves(color).length === 0;
    }

    isStalemate(color) {
        if (this.isInCheck(color)) return false;
        return this.getAllValidMoves(color).length === 0;
    }

    getAllValidMoves(color) {
        const moves = [];
        const size = BoardPosition.getBoardDimensions(this.boardSize);

        for (let fromX = 0; fromX < size; fromX++) {
            for (let fromY = 0; fromY < size; fromY++) {
                for (let fromZ = 0; fromZ < size; fromZ++) {
                    const piece = this.getPiece(fromX, fromY, fromZ);
                    if (piece && piece.color === color) {
                        for (let toX = 0; toX < size; toX++) {
                            for (let toY = 0; toY < size; toY++) {
                                for (let toZ = 0; toZ < size; toZ++) {
                                    if (this.isValidMove(fromX, fromY, fromZ, toX, toY, toZ)) {
                                        moves.push({ fromX, fromY, fromZ, toX, toY, toZ });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return moves;
    }

    // Move execution
    makeMove(fromX, fromY, fromZ, toX, toY, toZ, promotion = null) {
        if (!this.isValidMove(fromX, fromY, fromZ, toX, toY, toZ)) {
            return { success: false, error: 'Invalid move' };
        }

        const piece = this.getPiece(fromX, fromY, fromZ);
        const capturedPiece = this.getPiece(toX, toY, toZ);

        // Handle captures
        if (capturedPiece) {
            this.capturedPieces[capturedPiece.color].push(capturedPiece);
        }

        // Move the piece
        this.setPiece(toX, toY, toZ, piece);
        this.setPiece(fromX, fromY, fromZ, null);

        // Handle special moves
        this.handleSpecialMoves(piece, fromX, fromY, fromZ, toX, toY, toZ, promotion);

        // Update game state
        this.updateGameState(piece, fromX, fromY, fromZ, toX, toY, toZ, capturedPiece);

        // Record move
        const moveNotation = this.generateMoveNotation(piece, fromX, fromY, fromZ, toX, toY, toZ, capturedPiece, promotion);
        this.moveHistory.push({
            from: { x: fromX, y: fromY, z: fromZ },
            to: { x: toX, y: toY, z: toZ },
            piece: piece.type,
            color: piece.color,
            captured: capturedPiece,
            notation: moveNotation,
            timestamp: Date.now()
        });

        // Switch players based on player count
        this.currentPlayer = this.getNextPlayer(this.currentPlayer);

        // Check for game end
        const gameStatus = this.checkGameEnd();

        return { 
            success: true, 
            moveNotation,
            gameStatus,
            isCheck: this.isInCheck(this.currentPlayer),
            capturedPiece 
        };
    }

    handleSpecialMoves(piece, fromX, fromY, fromZ, toX, toY, toZ, promotion) {
        // Pawn promotion
        if (piece.type === 'pawn' && this.isPromotionMove(toY, piece.color)) {
            const promotionPiece = promotion || 'queen';
            this.setPiece(toX, toY, toZ, { type: promotionPiece, color: piece.color });
        }

        // Update king position
        if (piece.type === 'king') {
            this.kingPositions[piece.color] = { x: toX, y: toY, z: toZ };
        }

        // Update castling rights
        if (piece.type === 'king' || piece.type === 'rook') {
            this.updateCastlingRights(piece, fromX, fromY, fromZ);
        }

        // Set en passant target for pawn double moves
        if (piece.type === 'pawn' && Math.abs(toY - fromY) === 2) {
            this.enPassantTarget = { 
                x: toX, 
                y: fromY + (piece.color === 'white' ? 1 : -1), 
                z: toZ 
            };
        } else {
            this.enPassantTarget = null;
        }
    }

    isPromotionMove(y, color) {
        return (color === 'white' && y === 7) || (color === 'black' && y === 0);
    }

    updateCastlingRights(piece, x, y, z) {
        const color = piece.color;
        if (piece.type === 'king') {
            this.castlingRights[color].kingside = false;
            this.castlingRights[color].queenside = false;
        } else if (piece.type === 'rook') {
            if (x === 0) this.castlingRights[color].queenside = false;
            if (x === 7) this.castlingRights[color].kingside = false;
        }
    }

    updateGameState(piece, fromX, fromY, fromZ, toX, toY, toZ, capturedPiece) {
        // Update halfmove clock
        if (piece.type === 'pawn' || capturedPiece) {
            this.halfmoveClock = 0;
        } else {
            this.halfmoveClock++;
        }

        // Update fullmove number
        if (piece.color === 'black') {
            this.fullmoveNumber++;
        }
    }

    checkGameEnd() {
        const currentPlayerInCheck = this.isInCheck(this.currentPlayer);
        const hasValidMoves = this.getAllValidMoves(this.currentPlayer).length > 0;

        if (!hasValidMoves) {
            if (currentPlayerInCheck) {
                return {
                    ended: true,
                    result: this.currentPlayer === 'white' ? 'black_wins' : 'white_wins',
                    reason: 'checkmate'
                };
            } else {
                return {
                    ended: true,
                    result: 'draw',
                    reason: 'stalemate'
                };
            }
        }

        // Draw by repetition, 50-move rule, etc.
        if (this.halfmoveClock >= 50) {
            return {
                ended: true,
                result: 'draw',
                reason: 'fifty_move_rule'
            };
        }

        return { ended: false };
    }

    generateMoveNotation(piece, fromX, fromY, fromZ, toX, toY, toZ, capturedPiece, promotion) {
        // Simplified 3D notation: PieceFile(x,y,z)-File(x,y,z)
        const fromNotation = `${String.fromCharCode(97 + fromX)}${fromY + 1}${fromZ + 1}`;
        const toNotation = `${String.fromCharCode(97 + toX)}${toY + 1}${toZ + 1}`;
        
        let notation = '';
        
        if (piece.type !== 'pawn') {
            notation += piece.type.charAt(0).toUpperCase();
        }
        
        notation += fromNotation;
        
        if (capturedPiece) {
            notation += 'x';
        } else {
            notation += '-';
        }
        
        notation += toNotation;
        
        if (promotion) {
            notation += '=' + promotion.charAt(0).toUpperCase();
        }

        return notation;
    }

    // Utility methods
    getGameState() {
        return {
            board: this.board,
            currentPlayer: this.currentPlayer,
            gamePhase: this.gamePhase,
            moveHistory: this.moveHistory,
            capturedPieces: this.capturedPieces,
            kingPositions: this.kingPositions,
            isCheck: {
                white: this.isInCheck('white'),
                black: this.isInCheck('black')
            },
            gameStatus: this.checkGameEnd()
        };
    }

    loadGameState(state) {
        this.board = state.board || this.board;
        this.currentPlayer = state.currentPlayer || 'white';
        this.gamePhase = state.gamePhase || 'playing';
        this.moveHistory = state.moveHistory || [];
        this.capturedPieces = state.capturedPieces || { white: [], black: [] };
        this.kingPositions = state.kingPositions || this.kingPositions;
        this.castlingRights = state.castlingRights || this.castlingRights;
        this.enPassantTarget = state.enPassantTarget || null;
        this.halfmoveClock = state.halfmoveClock || 0;
        this.fullmoveNumber = state.fullmoveNumber || 1;
    }

    getNextPlayer(currentPlayer) {
        const playerOrder = this.getPlayerOrder();
        const currentIndex = playerOrder.indexOf(currentPlayer);
        const nextIndex = (currentIndex + 1) % playerOrder.length;
        return playerOrder[nextIndex];
    }

    getPlayerOrder() {
        switch (this.playerCount) {
            case 2:
                return ['white', 'black'];
            case 4:
                return ['white', 'black', 'green', 'purple'];
            case 6:
                return ['white', 'black', 'green', 'purple', 'yellow', 'orange'];
            default:
                return ['white', 'black'];
        }
    }

    reset() {
        this.initializeBoard();
        this.currentPlayer = 'white';
        this.gamePhase = 'placement';
        this.moveHistory = [];
        this.capturedPieces = { white: [], black: [], green: [], purple: [], yellow: [], orange: [] };
        this.castlingRights = {
            white: { kingside: true, queenside: true },
            black: { kingside: true, queenside: true },
            green: { kingside: true, queenside: true },
            purple: { kingside: true, queenside: true },
            yellow: { kingside: true, queenside: true },
            orange: { kingside: true, queenside: true }
        };
        this.enPassantTarget = null;
        this.halfmoveClock = 0;
        this.fullmoveNumber = 1;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ChessLogic;
}