/**
 * 3D Chess Engine with Three.js
 * Handles 3D rendering, piece models, animations, and user interactions
 */

class Chess3DEngine {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        
        // Game objects
        this.boardMesh = null;
        this.pieceMeshes = new Map(); // position key -> mesh
        this.highlightMeshes = [];
        this.selectedPiece = null;
        this.validMoves = [];
        
        // Interaction
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        this.isInteractionEnabled = true;
        
        // Animation
        this.animationMixer = null;
        this.clock = new THREE.Clock();
        
        // Settings
        this.settings = {
            boardOpacity: 0.8,
            enableShadows: true,
            enableAntialiasing: true,
            enableAnimations: true,
            cameraSpeed: 1.0
        };
        
        // Materials
        this.materials = {};
        
        // Initialize the engine
        this.init();
    }

    init() {
        this.initScene();
        this.initCamera();
        this.initRenderer();
        this.initLights();
        this.initMaterials();
        // Don't create board immediately - wait for configuration
        this.setupEventListeners();
        this.startRenderLoop();
    }

    initScene() {
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x1a1a2e);
        
        // Add fog for depth
        this.scene.fog = new THREE.Fog(0x1a1a2e, 10, 50);
    }

    initCamera() {
        this.camera = new THREE.PerspectiveCamera(
            45, 
            this.canvas.clientWidth / this.canvas.clientHeight, 
            0.1, 
            1000
        );
        this.camera.position.set(12, 12, 12);
        this.camera.lookAt(0, 0, 0);
        
        // Initialize orbit controls for camera movement
        this.initControls();
    }
    
    initControls() {
        if (typeof THREE.OrbitControls !== 'undefined') {
            this.controls = new THREE.OrbitControls(this.camera, this.canvas);
            
            // Configure controls
            this.controls.target.set(0, 2, 0); // Focus on center of board
            this.controls.enableDamping = true; // Smooth movement
            this.controls.dampingFactor = 0.05;
            this.controls.enableZoom = true;
            this.controls.enableRotate = true;
            this.controls.enablePan = true;
            
            // Set zoom limits
            this.controls.minDistance = 5;
            this.controls.maxDistance = 30;
            
            // Set rotation limits
            this.controls.maxPolarAngle = Math.PI * 0.8; // Prevent going below board
            this.controls.minPolarAngle = Math.PI * 0.1; // Prevent going too high
            
            // Update controls in render loop
            this.controls.update();
            
            console.log('✅ Orbit controls initialized - Use mouse to move camera!');
        } else {
            console.warn('⚠️ OrbitControls not available - using basic camera');
        }
    }

    initRenderer() {
        this.renderer = new THREE.WebGLRenderer({ 
            canvas: this.canvas,
            antialias: this.settings.enableAntialiasing,
            alpha: true
        });
        this.renderer.setSize(this.canvas.clientWidth, this.canvas.clientHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        
        if (this.settings.enableShadows) {
            this.renderer.shadowMap.enabled = true;
            this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        }
        
        this.renderer.outputEncoding = THREE.sRGBEncoding;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.2;
    }

    initLights() {
        // Ambient light
        const ambientLight = new THREE.AmbientLight(0x404040, 0.4);
        this.scene.add(ambientLight);

        // Main directional light
        const directionalLight = new THREE.DirectionalLight(0xffffff, 1);
        directionalLight.position.set(10, 15, 5);
        directionalLight.castShadow = this.settings.enableShadows;
        directionalLight.shadow.mapSize.width = 2048;
        directionalLight.shadow.mapSize.height = 2048;
        directionalLight.shadow.camera.near = 0.5;
        directionalLight.shadow.camera.far = 50;
        directionalLight.shadow.camera.left = -20;
        directionalLight.shadow.camera.right = 20;
        directionalLight.shadow.camera.top = 20;
        directionalLight.shadow.camera.bottom = -20;
        this.scene.add(directionalLight);

        // Secondary fill light
        const fillLight = new THREE.DirectionalLight(0x4080ff, 0.3);
        fillLight.position.set(-5, 10, -5);
        this.scene.add(fillLight);

        // Point lights for dramatic effect
        const pointLight1 = new THREE.PointLight(0xff6b6b, 0.5, 20);
        pointLight1.position.set(8, 6, 8);
        this.scene.add(pointLight1);

        const pointLight2 = new THREE.PointLight(0x4ecdc4, 0.5, 20);
        pointLight2.position.set(-8, 6, -8);
        this.scene.add(pointLight2);
    }

    initMaterials() {
        // Board materials
        this.materials.lightSquare = new THREE.MeshLambertMaterial({ 
            color: 0xf0d9b5,
            transparent: true,
            opacity: this.settings.boardOpacity
        });
        
        this.materials.darkSquare = new THREE.MeshLambertMaterial({ 
            color: 0xb58863,
            transparent: true,
            opacity: this.settings.boardOpacity
        });

        // Piece materials for all 6 colors
        this.materials.whitePiece = new THREE.MeshPhongMaterial({ 
            color: 0xffffff,
            shininess: 30,
            specular: 0x111111
        });
        
        this.materials.blackPiece = new THREE.MeshPhongMaterial({ 
            color: 0x222222,
            shininess: 30,
            specular: 0x444444
        });
        
        this.materials.greenPiece = new THREE.MeshPhongMaterial({ 
            color: 0x22aa22,
            shininess: 30,
            specular: 0x114411
        });
        
        this.materials.purplePiece = new THREE.MeshPhongMaterial({ 
            color: 0x8844cc,
            shininess: 30,
            specular: 0x442266
        });
        
        this.materials.yellowPiece = new THREE.MeshPhongMaterial({ 
            color: 0xdddd22,
            shininess: 30,
            specular: 0x666611
        });
        
        this.materials.orangePiece = new THREE.MeshPhongMaterial({ 
            color: 0xff8844,
            shininess: 30,
            specular: 0x664422
        });

        // Highlight materials
        this.materials.highlight = new THREE.MeshBasicMaterial({ 
            color: 0x00ff00,
            transparent: true,
            opacity: 0.5
        });
        
        this.materials.validMove = new THREE.MeshBasicMaterial({ 
            color: 0x0080ff,
            transparent: true,
            opacity: 0.3
        });
        
        this.materials.attack = new THREE.MeshBasicMaterial({ 
            color: 0xff4444,
            transparent: true,
            opacity: 0.4
        });

        this.materials.check = new THREE.MeshBasicMaterial({ 
            color: 0xff0000,
            transparent: true,
            opacity: 0.6
        });
    }

    createBoard(boardSize = 'Large8x8x8') {
        console.log(`Chess3DEngine.createBoard called with: ${boardSize}`);
        
        // Clear existing board
        if (this.boardGroup) {
            this.scene.remove(this.boardGroup);
        }
        
        this.boardGroup = new THREE.Group();
        this.boardSize = boardSize;
        
        const size = BoardPosition.getBoardDimensions(boardSize);
        const squareSize = 1;
        
        console.log(`Creating board with dimensions: ${size}x${size}x${size} (${boardSize})`);
        
        if (size === 8 && boardSize !== 'Large8x8x8') {
            console.warn(`Board size ${boardSize} mapped to 8x8x8, this might be unexpected`);
        }
        
        // Create floor planes for each level
        this.createFloorPlanes(size, squareSize);
        
        // Create wireframe grid structure
        this.createWireframeGrid(size, squareSize);
        
        // Create board boundaries
        this.createBoardBoundaries(size, squareSize);
        
        this.scene.add(this.boardGroup);
        this.boardMesh = this.boardGroup; // Keep compatibility
    }
    
    createFloorPlanes(size, squareSize) {
        for (let y = 0; y < size; y++) {
            for (let x = 0; x < size; x++) {
                for (let z = 0; z < size; z++) {
                    // Create floor for all squares to show proper checkerboard
                    this.createSquareFloor(x, y, z, size, squareSize);
                }
            }
        }
    }
    
    createSquareFloor(x, y, z, size, squareSize) {
        // Use 2D checkerboard pattern on each level
        const isLight = (x + z) % 2 === 0;
        // Make squares larger for better click detection while keeping visual gap
        const geometry = new THREE.PlaneGeometry(squareSize * 0.95, squareSize * 0.95);
        const material = isLight ? this.materials.lightSquare : this.materials.darkSquare;
        
        const square = new THREE.Mesh(geometry, material);
        square.rotation.x = -Math.PI / 2;
        
        const boardPosition = new BoardPosition(x, y, z);
        const worldPos = boardPosition.toWorldPosition(this.boardSize, squareSize);
        square.position.set(worldPos.x, worldPos.y, worldPos.z);
        
        // Add invisible collision box for better click detection
        const collisionGeometry = new THREE.BoxGeometry(squareSize, 0.1, squareSize);
        const collisionMaterial = new THREE.MeshBasicMaterial({ 
            transparent: true, 
            opacity: 0,
            visible: false 
        });
        const collisionBox = new THREE.Mesh(collisionGeometry, collisionMaterial);
        collisionBox.position.set(worldPos.x, worldPos.y + 0.05, worldPos.z);
        
        // Set userData on both the visual square and collision box
        const userData = { 
            type: 'board', 
            boardPosition: boardPosition
        };
        square.userData = userData;
        collisionBox.userData = userData;
        
        this.boardGroup.add(square);
        this.boardGroup.add(collisionBox);
    }
    
    createWireframeGrid(size, squareSize) {
        const material = new THREE.LineBasicMaterial({ 
            color: 0x404040, 
            transparent: true, 
            opacity: 0.3 
        });
        
        // Create grid lines for each axis
        this.createGridLines(size, squareSize, material, 'x'); // Vertical lines along X
        this.createGridLines(size, squareSize, material, 'y'); // Horizontal lines along Y  
        this.createGridLines(size, squareSize, material, 'z'); // Depth lines along Z
    }
    
    createGridLines(size, squareSize, material, axis) {
        const offset = (size - 1) * squareSize * 0.5;
        
        if (axis === 'x') {
            // Lines parallel to X-axis
            for (let y = 0; y < size; y++) {
                for (let z = 0; z < size; z++) {
                    const geometry = new THREE.BufferGeometry().setFromPoints([
                        new THREE.Vector3(-offset, y * squareSize - offset, z * squareSize - offset),
                        new THREE.Vector3(offset, y * squareSize - offset, z * squareSize - offset)
                    ]);
                    const line = new THREE.Line(geometry, material);
                    this.boardGroup.add(line);
                }
            }
        } else if (axis === 'y') {
            // Lines parallel to Y-axis
            for (let x = 0; x < size; x++) {
                for (let z = 0; z < size; z++) {
                    const geometry = new THREE.BufferGeometry().setFromPoints([
                        new THREE.Vector3(x * squareSize - offset, -offset, z * squareSize - offset),
                        new THREE.Vector3(x * squareSize - offset, offset, z * squareSize - offset)
                    ]);
                    const line = new THREE.Line(geometry, material);
                    this.boardGroup.add(line);
                }
            }
        } else if (axis === 'z') {
            // Lines parallel to Z-axis
            for (let x = 0; x < size; x++) {
                for (let y = 0; y < size; y++) {
                    const geometry = new THREE.BufferGeometry().setFromPoints([
                        new THREE.Vector3(x * squareSize - offset, y * squareSize - offset, -offset),
                        new THREE.Vector3(x * squareSize - offset, y * squareSize - offset, offset)
                    ]);
                    const line = new THREE.Line(geometry, material);
                    this.boardGroup.add(line);
                }
            }
        }
    }
    
    createBoardBoundaries(size, squareSize) {
        const material = new THREE.LineBasicMaterial({ 
            color: 0x888888, 
            linewidth: 2 
        });
        
        const offset = (size - 1) * squareSize * 0.5;
        
        // Create boundary box
        const points = [
            // Bottom face
            new THREE.Vector3(-offset, -offset, -offset),
            new THREE.Vector3(offset, -offset, -offset),
            new THREE.Vector3(offset, -offset, offset),
            new THREE.Vector3(-offset, -offset, offset),
            new THREE.Vector3(-offset, -offset, -offset),
            
            // Top face
            new THREE.Vector3(-offset, offset, -offset),
            new THREE.Vector3(offset, offset, -offset),
            new THREE.Vector3(offset, offset, offset),
            new THREE.Vector3(-offset, offset, offset),
            new THREE.Vector3(-offset, offset, -offset),
        ];
        
        // Vertical edges
        const verticalLines = [
            [new THREE.Vector3(-offset, -offset, -offset), new THREE.Vector3(-offset, offset, -offset)],
            [new THREE.Vector3(offset, -offset, -offset), new THREE.Vector3(offset, offset, -offset)],
            [new THREE.Vector3(offset, -offset, offset), new THREE.Vector3(offset, offset, offset)],
            [new THREE.Vector3(-offset, -offset, offset), new THREE.Vector3(-offset, offset, offset)],
        ];
        
        // Add boundary lines
        const boundaryGeometry = new THREE.BufferGeometry().setFromPoints(points);
        const boundaryLine = new THREE.Line(boundaryGeometry, material);
        this.boardGroup.add(boundaryLine);
        
        // Add vertical edges
        verticalLines.forEach(([start, end]) => {
            const geometry = new THREE.BufferGeometry().setFromPoints([start, end]);
            const line = new THREE.Line(geometry, material);
            this.boardGroup.add(line);
        });
    }

    createPieceGeometry(pieceType) {
        switch (pieceType) {
            case 'pawn':
                return this.createPawnGeometry();
            case 'rook':
                return this.createRookGeometry();
            case 'knight':
                return this.createKnightGeometry();
            case 'bishop':
                return this.createBishopGeometry();
            case 'queen':
                return this.createQueenGeometry();
            case 'king':
                return this.createKingGeometry();
            default:
                return new THREE.CylinderGeometry(0.3, 0.4, 0.8, 8);
        }
    }

    createPawnGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.3, 0.4, 0.1, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.05;
        geometry.add(baseMesh);
        
        // Body
        const body = new THREE.CylinderGeometry(0.2, 0.3, 0.5, 8);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.35;
        geometry.add(bodyMesh);
        
        // Head
        const head = new THREE.SphereGeometry(0.15, 8, 6);
        const headMesh = new THREE.Mesh(head);
        headMesh.position.y = 0.65;
        geometry.add(headMesh);
        
        return geometry;
    }

    createRookGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.35, 0.4, 0.15, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.075;
        geometry.add(baseMesh);
        
        // Body
        const body = new THREE.CylinderGeometry(0.25, 0.3, 0.6, 8);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.45;
        geometry.add(bodyMesh);
        
        // Top
        const top = new THREE.CylinderGeometry(0.3, 0.25, 0.2, 8);
        const topMesh = new THREE.Mesh(top);
        topMesh.position.y = 0.85;
        geometry.add(topMesh);
        
        return geometry;
    }

    createKnightGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.35, 0.4, 0.15, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.075;
        geometry.add(baseMesh);
        
        // Body (horse-like shape)
        const body = new THREE.CylinderGeometry(0.2, 0.3, 0.5, 6);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.4;
        geometry.add(bodyMesh);
        
        // Head (elongated)
        const head = new THREE.BoxGeometry(0.3, 0.4, 0.2);
        const headMesh = new THREE.Mesh(head);
        headMesh.position.set(0, 0.8, 0.1);
        headMesh.rotation.x = Math.PI / 6;
        geometry.add(headMesh);
        
        return geometry;
    }

    createBishopGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.35, 0.4, 0.15, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.075;
        geometry.add(baseMesh);
        
        // Body
        const body = new THREE.CylinderGeometry(0.15, 0.3, 0.6, 8);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.45;
        geometry.add(bodyMesh);
        
        // Head
        const head = new THREE.SphereGeometry(0.2, 8, 6);
        const headMesh = new THREE.Mesh(head);
        headMesh.position.y = 0.8;
        geometry.add(headMesh);
        
        // Top point
        const point = new THREE.ConeGeometry(0.05, 0.2, 6);
        const pointMesh = new THREE.Mesh(point);
        pointMesh.position.y = 1.05;
        geometry.add(pointMesh);
        
        return geometry;
    }

    createQueenGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.4, 0.45, 0.15, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.075;
        geometry.add(baseMesh);
        
        // Body
        const body = new THREE.CylinderGeometry(0.2, 0.35, 0.7, 8);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.5;
        geometry.add(bodyMesh);
        
        // Crown base
        const crown = new THREE.CylinderGeometry(0.25, 0.2, 0.2, 8);
        const crownMesh = new THREE.Mesh(crown);
        crownMesh.position.y = 0.95;
        geometry.add(crownMesh);
        
        // Crown points
        for (let i = 0; i < 8; i++) {
            const angle = (i / 8) * Math.PI * 2;
            const point = new THREE.ConeGeometry(0.03, 0.15, 4);
            const pointMesh = new THREE.Mesh(point);
            pointMesh.position.set(
                Math.cos(angle) * 0.2,
                1.15,
                Math.sin(angle) * 0.2
            );
            geometry.add(pointMesh);
        }
        
        return geometry;
    }

    createKingGeometry() {
        const geometry = new THREE.Group();
        
        // Base
        const base = new THREE.CylinderGeometry(0.4, 0.45, 0.15, 8);
        const baseMesh = new THREE.Mesh(base);
        baseMesh.position.y = 0.075;
        geometry.add(baseMesh);
        
        // Body
        const body = new THREE.CylinderGeometry(0.25, 0.35, 0.7, 8);
        const bodyMesh = new THREE.Mesh(body);
        bodyMesh.position.y = 0.5;
        geometry.add(bodyMesh);
        
        // Crown
        const crown = new THREE.CylinderGeometry(0.3, 0.25, 0.25, 8);
        const crownMesh = new THREE.Mesh(crown);
        crownMesh.position.y = 0.975;
        geometry.add(crownMesh);
        
        // Cross on top
        const crossV = new THREE.BoxGeometry(0.05, 0.3, 0.05);
        const crossVMesh = new THREE.Mesh(crossV);
        crossVMesh.position.y = 1.25;
        geometry.add(crossVMesh);
        
        const crossH = new THREE.BoxGeometry(0.2, 0.05, 0.05);
        const crossHMesh = new THREE.Mesh(crossH);
        crossHMesh.position.y = 1.2;
        geometry.add(crossHMesh);
        
        return geometry;
    }

    createPiece(pieceType, color, x, y, z) {
        const geometry = this.createPieceGeometry(pieceType);
        const material = this.materials[color + 'Piece'] || this.materials.whitePiece;
        
        const piece = new THREE.Group();
        
        // Apply material to all meshes in the geometry
        geometry.traverse((child) => {
            if (child instanceof THREE.Mesh) {
                child.material = material.clone();
                child.castShadow = this.settings.enableShadows;
                child.receiveShadow = this.settings.enableShadows;
            }
        });
        
        // Scale piece based on board size
        const scaleFactor = this.getPieceScaleFactor();
        geometry.scale.set(scaleFactor, scaleFactor, scaleFactor);
        
        piece.add(geometry);
        piece.userData = { 
            x, y, z, 
            type: 'piece', 
            pieceType, 
            color,
            originalPosition: { x, y, z }
        };
        
        this.updatePiecePosition(piece, x, y, z);
        this.scene.add(piece);
        
        const positionKey = `${x},${y},${z}`;
        this.pieceMeshes.set(positionKey, piece);
        
        return piece;
    }

    getPieceScaleFactor() {
        // Scale pieces based on board size to fit better
        switch (this.boardSize) {
            case 'Small4x4x4':
                return 0.6; // Smaller pieces for 4x4x4 board
            case 'Medium6x6x6':
                return 0.75; // Medium pieces for 6x6x6 board
            case 'Large8x8x8':
            default:
                return 0.8; // Slightly smaller than original for 8x8x8 board
        }
    }

    updatePiecePosition(piece, x, y, z) {
        const squareSize = 1;
        const boardPosition = new BoardPosition(x, y, z);
        const worldPos = boardPosition.toWorldPosition(this.boardSize || 'Large8x8x8', squareSize);
        
        // Position piece on the floor of the square, elevated slightly
        piece.position.set(
            worldPos.x,
            worldPos.y + 0.5, // Elevate piece above the floor
            worldPos.z
        );
        
        // Store board position in userData
        piece.userData.boardPosition = boardPosition;
        piece.userData.x = x; // Keep for compatibility
        piece.userData.y = y;
        piece.userData.z = z;
    }

    movePiece(fromX, fromY, fromZ, toX, toY, toZ, animate = true) {
        const fromKey = `${fromX},${fromY},${fromZ}`;
        const toKey = `${toX},${toY},${toZ}`;
        
        const piece = this.pieceMeshes.get(fromKey);
        if (!piece) return false;

        // Remove piece from old position
        this.pieceMeshes.delete(fromKey);
        
        // Handle capture
        const capturedPiece = this.pieceMeshes.get(toKey);
        if (capturedPiece) {
            this.removePiece(toX, toY, toZ, animate);
        }
        
        if (animate && this.settings.enableAnimations) {
            this.animatePieceMove(piece, toX, toY, toZ);
        } else {
            this.updatePiecePosition(piece, toX, toY, toZ);
        }
        
        // Add piece to new position
        this.pieceMeshes.set(toKey, piece);
        
        return true;
    }

    animatePieceMove(piece, toX, toY, toZ) {
        const squareSize = 1;
        const boardSize = 8;
        
        const targetPosition = new THREE.Vector3(
            (toX - boardSize/2 + 0.5) * squareSize,
            (toY - boardSize/2 + 0.5) * squareSize + 0.5,
            (toZ - boardSize/2 + 0.5) * squareSize
        );
        
        // Create animation
        const startPosition = piece.position.clone();
        const duration = 500; // milliseconds
        const startTime = Date.now();
        
        const animate = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            
            // Easing function (ease-out)
            const easedProgress = 1 - Math.pow(1 - progress, 3);
            
            piece.position.lerpVectors(startPosition, targetPosition, easedProgress);
            
            // Add slight arc to the movement
            const arcHeight = 1;
            piece.position.y += Math.sin(progress * Math.PI) * arcHeight;
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                piece.position.copy(targetPosition);
                piece.userData.x = toX;
                piece.userData.y = toY;
                piece.userData.z = toZ;
            }
        };
        
        animate();
    }

    removePiece(x, y, z, animate = true) {
        const positionKey = `${x},${y},${z}`;
        const piece = this.pieceMeshes.get(positionKey);
        
        if (!piece) return false;
        
        this.pieceMeshes.delete(positionKey);
        
        if (animate && this.settings.enableAnimations) {
            // Animate piece disappearing
            const startScale = piece.scale.clone();
            const duration = 300;
            const startTime = Date.now();
            
            const animate = () => {
                const elapsed = Date.now() - startTime;
                const progress = Math.min(elapsed / duration, 1);
                
                const scale = 1 - progress;
                piece.scale.set(scale, scale, scale);
                piece.rotation.y += 0.1;
                
                if (progress < 1) {
                    requestAnimationFrame(animate);
                } else {
                    this.scene.remove(piece);
                }
            };
            
            animate();
        } else {
            this.scene.remove(piece);
        }
        
        return true;
    }

    highlightSquare(x, y, z, type = 'highlight') {
        this.clearHighlights();
        
        const squareSize = 1;
        const boardSize = 8;
        
        const geometry = new THREE.BoxGeometry(squareSize, 0.02, squareSize);
        const material = this.materials[type] || this.materials.highlight;
        
        const highlight = new THREE.Mesh(geometry, material);
        highlight.position.set(
            (x - boardSize/2 + 0.5) * squareSize,
            (y - boardSize/2 + 0.5) * squareSize + 0.51,
            (z - boardSize/2 + 0.5) * squareSize
        );
        
        this.scene.add(highlight);
        this.highlightMeshes.push(highlight);
    }

    highlightValidMoves(moves) {
        this.clearHighlights();
        
        moves.forEach(move => {
            const squareSize = 1;
            const boardSize = 8;
            
            const geometry = new THREE.RingGeometry(0.3, 0.4, 8);
            const material = this.materials.validMove;
            
            const highlight = new THREE.Mesh(geometry, material);
            highlight.position.set(
                (move.toX - boardSize/2 + 0.5) * squareSize,
                (move.toY - boardSize/2 + 0.5) * squareSize + 0.51,
                (move.toZ - boardSize/2 + 0.5) * squareSize
            );
            highlight.rotation.x = -Math.PI / 2;
            
            this.scene.add(highlight);
            this.highlightMeshes.push(highlight);
        });
    }

    clearHighlights() {
        this.highlightMeshes.forEach(highlight => {
            this.scene.remove(highlight);
        });
        this.highlightMeshes = [];
    }

    setupEventListeners() {
        // Mouse events
        this.canvas.addEventListener('mousemove', (event) => this.onMouseMove(event));
        this.canvas.addEventListener('click', (event) => this.onMouseClick(event));
        
        // Window resize
        window.addEventListener('resize', () => this.onWindowResize());
        
        // Touch events for mobile
        this.canvas.addEventListener('touchstart', (event) => this.onTouchStart(event));
        this.canvas.addEventListener('touchend', (event) => this.onTouchEnd(event));
    }

    onMouseMove(event) {
        const rect = this.canvas.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    }

    onMouseClick(event) {
        if (!this.isInteractionEnabled) return;
        
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersects = this.raycaster.intersectObjects(this.scene.children, true);
        
        if (intersects.length > 0) {
            const clickedObject = this.findClickableObject(intersects[0].object);
            if (clickedObject) {
                this.handleObjectClick(clickedObject);
            }
        }
    }

    findClickableObject(object) {
        // Traverse up the object hierarchy to find a clickable object
        let current = object;
        while (current) {
            if (current.userData && (current.userData.type === 'piece' || current.userData.type === 'board')) {
                return current;
            }
            current = current.parent;
        }
        return null;
    }

    handleObjectClick(object) {
        const { type, boardPosition } = object.userData;
        
        if (type === 'piece') {
            // For pieces, use the stored board position
            const pos = object.userData.boardPosition || new BoardPosition(object.userData.x, object.userData.y, object.userData.z);
            this.onPieceClick(pos.x, pos.y, pos.z, object);
        } else if (type === 'board') {
            // For board squares, validate the position before allowing interaction
            if (boardPosition && boardPosition.isValid(this.boardSize)) {
                this.onSquareClick(boardPosition.x, boardPosition.y, boardPosition.z);
            } else {
                console.log('Click on invalid board position, ignoring');
            }
        }
    }

    onPieceClick(x, y, z, piece) {
        if (this.selectedPiece === piece) {
            // Deselect piece
            this.selectedPiece = null;
            this.clearHighlights();
            this.onPieceDeselected();
        } else {
            // Select piece
            this.selectedPiece = piece;
            this.highlightSquare(x, y, z, 'highlight');
            this.onPieceSelected(x, y, z, piece.userData.pieceType, piece.userData.color);
        }
    }

    onSquareClick(x, y, z) {
        if (this.selectedPiece) {
            const fromPos = this.selectedPiece.userData.boardPosition || 
                           new BoardPosition(this.selectedPiece.userData.x, this.selectedPiece.userData.y, this.selectedPiece.userData.z);
            
            this.onMoveAttempt(fromPos.x, fromPos.y, fromPos.z, x, y, z);
        } else {
            // Handle placement phase clicks or empty square selections
            this.onSquareSelected(x, y, z);
        }
    }

    onTouchStart(event) {
        event.preventDefault();
        if (event.touches.length === 1) {
            const touch = event.touches[0];
            const rect = this.canvas.getBoundingClientRect();
            this.mouse.x = ((touch.clientX - rect.left) / rect.width) * 2 - 1;
            this.mouse.y = -((touch.clientY - rect.top) / rect.height) * 2 + 1;
        }
    }

    onTouchEnd(event) {
        event.preventDefault();
        if (event.changedTouches.length === 1) {
            this.onMouseClick(event.changedTouches[0]);
        }
    }

    onWindowResize() {
        this.camera.aspect = this.canvas.clientWidth / this.canvas.clientHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(this.canvas.clientWidth, this.canvas.clientHeight);
    }

    // Callback methods to be overridden by game implementation
    onPieceSelected(x, y, z, pieceType, color) {
        console.log(`Piece selected: ${color} ${pieceType} at (${x},${y},${z})`);
    }

    onPieceDeselected() {
        console.log('Piece deselected');
    }

    onMoveAttempt(fromX, fromY, fromZ, toX, toY, toZ) {
        console.log(`Move attempt: (${fromX},${fromY},${fromZ}) -> (${toX},${toY},${toZ})`);
    }

    onSquareSelected(x, y, z) {
        console.log(`Square selected: (${x},${y},${z})`);
    }

    // Camera controls
    setCameraView(viewType) {
        const distance = 15;
        
        switch (viewType) {
            case 'top':
                this.camera.position.set(0, distance, 0);
                this.camera.lookAt(0, 0, 0);
                break;
            case 'side':
                this.camera.position.set(distance, 5, 0);
                this.camera.lookAt(0, 0, 0);
                break;
            case 'perspective':
                this.camera.position.set(12, 12, 12);
                this.camera.lookAt(0, 0, 0);
                break;
            case 'reset':
                this.camera.position.set(12, 12, 12);
                this.camera.lookAt(0, 0, 0);
                break;
        }
        
        // Update orbit controls target and position
        if (this.controls) {
            this.controls.target.set(0, 2, 0);
            this.controls.update();
        }
    }

    // Settings
    updateSettings(newSettings) {
        Object.assign(this.settings, newSettings);
        
        if ('boardOpacity' in newSettings) {
            this.materials.lightSquare.opacity = newSettings.boardOpacity;
            this.materials.darkSquare.opacity = newSettings.boardOpacity;
        }
        
        if ('enableShadows' in newSettings) {
            this.renderer.shadowMap.enabled = newSettings.enableShadows;
        }
    }

    // Render loop
    startRenderLoop() {
        const animate = () => {
            requestAnimationFrame(animate);
            
            const delta = this.clock.getDelta();
            
            // Update orbit controls if available
            if (this.controls) {
                this.controls.update();
            }
            
            if (this.animationMixer) {
                this.animationMixer.update(delta);
            }
            
            this.renderer.render(this.scene, this.camera);
        };
        
        animate();
    }

    // Utility methods
    clearPieces() {
        // Remove all pieces from the scene and clear the pieces map
        this.pieceMeshes.forEach(piece => {
            this.scene.remove(piece);
        });
        this.pieceMeshes.clear();
    }

    getScreenPosition(x, y, z) {
        const squareSize = 1;
        const boardSize = 8;
        
        const worldPosition = new THREE.Vector3(
            (x - boardSize/2 + 0.5) * squareSize,
            (y - boardSize/2 + 0.5) * squareSize + 0.5,
            (z - boardSize/2 + 0.5) * squareSize
        );
        
        worldPosition.project(this.camera);
        
        return {
            x: (worldPosition.x + 1) * this.canvas.clientWidth / 2,
            y: (-worldPosition.y + 1) * this.canvas.clientHeight / 2
        };
    }

    dispose() {
        // Clean up resources
        this.scene.traverse((object) => {
            if (object.geometry) object.geometry.dispose();
            if (object.material) {
                if (Array.isArray(object.material)) {
                    object.material.forEach(material => material.dispose());
                } else {
                    object.material.dispose();
                }
            }
        });
        
        this.renderer.dispose();
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = Chess3DEngine;
}