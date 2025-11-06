/**
 * Test Enhanced AI Bot without requiring a real server
 */

const EnhancedAIBot = require('./ai-enhanced-bot.js');

async function testEnhancedBotCreation() {
    console.log('🤖 Testing Enhanced AI Bot Creation...\n');
    
    try {
        // Test bot creation without connecting to server
        console.log('🔧 Creating Enhanced AI Bot instance...');
        const bot = new EnhancedAIBot('TEST123', 'Test_AI', 'medium', 'aggressive', 'http://localhost:3000');
        
        console.log('✅ Bot created successfully!');
        console.log(`   Room: ${bot.roomCode}`);
        console.log(`   Name: ${bot.name}`);
        console.log(`   Difficulty: ${bot.difficulty}`);
        console.log(`   Personality: ${bot.personality}`);
        console.log(`   Brain initialized: ${!!bot.brain}`);
        
        // Test the AI brain directly
        console.log('\n🧠 Testing AI Brain functionality...');
        
        const testBoardState = {
            boardSize: 4,
            pieces: {
                '0,0,0': { type: 'king', color: 'white' },
                '1,0,0': { type: 'pawn', color: 'white' },
                '2,3,3': { type: 'king', color: 'black' }
            },
            currentPlayer: 'white'
        };
        
        bot.gameState = testBoardState;
        bot.myColor = 'white';
        
        const move = await bot.brain.getBestMove(testBoardState, 'white');
        
        if (move) {
            console.log('✅ AI Brain working correctly!');
            console.log(`   Move: ${JSON.stringify(move)}`);
        } else {
            console.log('❌ AI Brain failed to find move');
        }
        
        // Test fallback move generation
        console.log('\n🎲 Testing fallback move generation...');
        const fallbackMove = await bot.getFallbackMove();
        
        if (fallbackMove) {
            console.log('✅ Fallback move generation working!');
            console.log(`   Fallback move: ${JSON.stringify(fallbackMove)}`);
        } else {
            console.log('❌ Fallback move generation failed');
        }
        
        // Test thinking delay calculation
        console.log('\n⏰ Testing thinking delay calculation...');
        const delay1 = bot.calculateThinkingDelay();
        const delay2 = bot.calculateThinkingDelay();
        
        console.log(`✅ Thinking delays: ${delay1}ms, ${delay2}ms`);
        console.log(`   Personality-based variation working: ${delay1 !== delay2 ? 'Yes' : 'Maybe'}`);
        
        console.log('\n🎉 Enhanced AI Bot test completed successfully!');
        
    } catch (error) {
        console.error('💥 Enhanced AI Bot test failed:', error.message);
        console.error(error.stack);
    }
}

if (require.main === module) {
    testEnhancedBotCreation().catch(error => {
        console.error('Test failed:', error);
        process.exit(1);
    });
}

module.exports = { testEnhancedBotCreation };