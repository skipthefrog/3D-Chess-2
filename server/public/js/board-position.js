/**
 * BoardPosition class - Manages 3D chess board coordinates
 * Replicates Unity's BoardPosition struct functionality
 */

class BoardPosition {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    // Static validation method
    static isValid(x, y, z, boardSize) {
        const size = this.getBoardDimensions(boardSize);
        return x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size;
    }

    // Get board dimensions from size enum
    static getBoardDimensions(boardSize) {
        switch (boardSize) {
            case 'Small4x4x4': return 4;
            case 'Medium6x6x6': return 6;
            case 'Large8x8x8': return 8;
            default: return 8;
        }
    }

    // Instance validation
    isValid(boardSize) {
        return BoardPosition.isValid(this.x, this.y, this.z, boardSize);
    }

    // Equality check
    equals(other) {
        return this.x === other.x && this.y === other.y && this.z === other.z;
    }

    // Create copy
    clone() {
        return new BoardPosition(this.x, this.y, this.z);
    }

    // Convert to string key for storage
    toString() {
        return `${this.x},${this.y},${this.z}`;
    }

    // Create from string key
    static fromString(str) {
        const [x, y, z] = str.split(',').map(Number);
        return new BoardPosition(x, y, z);
    }

    // Get adjacent positions (26 directions in 3D)
    getAdjacentPositions(boardSize) {
        const positions = [];
        for (let dx = -1; dx <= 1; dx++) {
            for (let dy = -1; dy <= 1; dy++) {
                for (let dz = -1; dz <= 1; dz++) {
                    if (dx === 0 && dy === 0 && dz === 0) continue; // Skip self
                    
                    const newPos = new BoardPosition(
                        this.x + dx,
                        this.y + dy,
                        this.z + dz
                    );
                    
                    if (newPos.isValid(boardSize)) {
                        positions.push(newPos);
                    }
                }
            }
        }
        return positions;
    }

    // Get direction vector to another position
    getDirectionTo(other) {
        return new BoardPosition(
            Math.sign(other.x - this.x),
            Math.sign(other.y - this.y),
            Math.sign(other.z - this.z)
        );
    }

    // Check if position is on board edge
    isOnEdge(boardSize) {
        const size = BoardPosition.getBoardDimensions(boardSize);
        return this.x === 0 || this.x === size - 1 ||
               this.y === 0 || this.y === size - 1 ||
               this.z === 0 || this.z === size - 1;
    }

    // Get placement zone for a color (replicates Unity's placement logic)
    static getPlacementZone(color, boardSize, playerCount) {
        const size = BoardPosition.getBoardDimensions(boardSize);
        const positions = [];

        // For 2-player games
        if (playerCount === 2) {
            if (color === 'White') {
                // White places on X=0 layer
                for (let y = 0; y < size; y++) {
                    for (let z = 0; z < size; z++) {
                        positions.push(new BoardPosition(0, y, z));
                    }
                }
            } else if (color === 'Black') {
                // Black places on X=max layer
                for (let y = 0; y < size; y++) {
                    for (let z = 0; z < size; z++) {
                        positions.push(new BoardPosition(size - 1, y, z));
                    }
                }
            }
        }
        // Add logic for 4 and 6 player games...
        
        return positions;
    }

    // Convert board position to world coordinates (for 3D rendering)
    toWorldPosition(boardSize, squareSize = 1) {
        const size = BoardPosition.getBoardDimensions(boardSize);
        const offset = (size - 1) * squareSize * 0.5;
        
        return {
            x: this.x * squareSize - offset,
            y: this.y * squareSize - offset,
            z: this.z * squareSize - offset
        };
    }

    // Convert world coordinates back to board position
    static fromWorldPosition(worldX, worldY, worldZ, boardSize, squareSize = 1) {
        const size = BoardPosition.getBoardDimensions(boardSize);
        const offset = (size - 1) * squareSize * 0.5;
        
        const x = Math.round((worldX + offset) / squareSize);
        const y = Math.round((worldY + offset) / squareSize);
        const z = Math.round((worldZ + offset) / squareSize);
        
        return new BoardPosition(x, y, z);
    }
}