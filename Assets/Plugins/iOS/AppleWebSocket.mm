// WebSocket for iOS built on Apple's NSURLSessionWebSocketTask. Unity's managed
// ClientWebSocket fails to connect to the game server over https on iOS, so the iPhone
// build uses this instead (see Assets/Scripts/Network/Online/GameSocket.cs).
// Events are delivered to the Unity GameObject named by the caller via UnitySendMessage.

#import <Foundation/Foundation.h>

extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface AppleSocket : NSObject <NSURLSessionWebSocketDelegate>
@property (nonatomic) int socketId;
@property (nonatomic, copy) NSString* receiver;
@property (nonatomic, strong) NSURLSession* session;
@property (nonatomic, strong) NSURLSessionWebSocketTask* task;
@property (nonatomic) BOOL closed;
@end

static NSMutableDictionary<NSNumber*, AppleSocket*>* sockets;

static void Send(AppleSocket* s, const char* method, NSString* payload)
{
    NSString* message = [NSString stringWithFormat:@"%d|%@", s.socketId, payload ?: @""];
    NSString* receiver = s.receiver;
    dispatch_async(dispatch_get_main_queue(), ^{
        UnitySendMessage(receiver.UTF8String, method, message.UTF8String);
    });
}

@implementation AppleSocket

- (void)receiveNext
{
    __weak AppleSocket* weakSelf = self;
    [self.task receiveMessageWithCompletionHandler:^(NSURLSessionWebSocketMessage* message, NSError* error) {
        AppleSocket* s = weakSelf;
        if (s == nil || s.closed) return;
        if (error != nil)
        {
            [s finishWithReason:error.localizedDescription isError:YES];
            return;
        }
        NSString* text = message.type == NSURLSessionWebSocketMessageTypeString
            ? message.string
            : [[NSString alloc] initWithData:message.data encoding:NSUTF8StringEncoding];
        Send(s, "OnAppleSocketMessage", text);
        [s receiveNext];
    }];
}

- (void)finishWithReason:(NSString*)reason isError:(BOOL)isError
{
    if (self.closed) return;
    self.closed = YES;
    if (isError) Send(self, "OnAppleSocketError", reason);
    Send(self, "OnAppleSocketClose", reason);
    [self.session invalidateAndCancel];
}

- (void)URLSession:(NSURLSession*)session webSocketTask:(NSURLSessionWebSocketTask*)webSocketTask didOpenWithProtocol:(NSString*)protocol
{
    Send(self, "OnAppleSocketOpen", @"");
    [self receiveNext];
}

- (void)URLSession:(NSURLSession*)session webSocketTask:(NSURLSessionWebSocketTask*)webSocketTask didCloseWithCode:(NSURLSessionWebSocketCloseCode)closeCode reason:(NSData*)reason
{
    [self finishWithReason:[NSString stringWithFormat:@"closed %ld", (long)closeCode] isError:NO];
}

- (void)URLSession:(NSURLSession*)session task:(NSURLSessionTask*)task didCompleteWithError:(NSError*)error
{
    if (error != nil) [self finishWithReason:error.localizedDescription isError:YES];
    else [self finishWithReason:@"closed" isError:NO];
}

@end

extern "C" {

void AppleSocket_Open(int socketId, const char* url, const char* receiver)
{
    if (sockets == nil) sockets = [NSMutableDictionary dictionary];
    AppleSocket* s = [AppleSocket new];
    s.socketId = socketId;
    s.receiver = [NSString stringWithUTF8String:receiver];
    s.session = [NSURLSession sessionWithConfiguration:[NSURLSessionConfiguration defaultSessionConfiguration]
                                              delegate:s
                                         delegateQueue:nil];
    s.task = [s.session webSocketTaskWithURL:[NSURL URLWithString:[NSString stringWithUTF8String:url]]];
    s.task.maximumMessageSize = 4 * 1024 * 1024; // full board states are a few KB
    sockets[@(socketId)] = s;
    [s.task resume];
}

void AppleSocket_Send(int socketId, const char* text)
{
    AppleSocket* s = sockets[@(socketId)];
    if (s == nil || s.closed) return;
    NSURLSessionWebSocketMessage* message = [[NSURLSessionWebSocketMessage alloc] initWithString:[NSString stringWithUTF8String:text]];
    __weak AppleSocket* weak = s;
    [s.task sendMessage:message completionHandler:^(NSError* error) {
        if (error != nil) [weak finishWithReason:error.localizedDescription isError:YES];
    }];
}

void AppleSocket_Close(int socketId)
{
    AppleSocket* s = sockets[@(socketId)];
    if (s == nil) return;
    [sockets removeObjectForKey:@(socketId)];
    if (!s.closed)
    {
        s.closed = YES;
        [s.task cancelWithCloseCode:NSURLSessionWebSocketCloseCodeNormalClosure reason:nil];
        [s.session invalidateAndCancel];
    }
}

}
