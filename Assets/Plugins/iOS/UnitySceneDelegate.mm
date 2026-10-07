// Adopts the UIScene app lifecycle, which iOS 27 requires (apps without it are stopped
// at launch with "needs to be updated"). Unity 6.1's app controller still uses the
// older app-delegate lifecycle, so this scene delegate attaches Unity's window to the
// scene and forwards scene lifecycle events to UnityAppController.
// The Info.plist entry that points iOS at this class is added by iOSScenePostProcess.cs.

#import <UIKit/UIKit.h>
#import "UnityAppController.h"

@interface UnitySceneDelegate : UIResponder <UIWindowSceneDelegate>
@property (strong, nonatomic) UIWindow* window;
@end

@implementation UnitySceneDelegate

static UnityAppController* AppController()
{
    return (UnityAppController*)[UIApplication sharedApplication].delegate;
}

- (void)attachUnityWindowTo:(UIWindowScene*)scene
{
    UIWindow* unityWindow = AppController().window;
    if (unityWindow == nil)
        return; // Unity not started yet; it creates its window with this scene when it becomes active
    if (unityWindow.windowScene != scene)
        unityWindow.windowScene = scene;
    self.window = unityWindow;
    [unityWindow makeKeyAndVisible];
}

- (void)scene:(UIScene*)scene willConnectToSession:(UISceneSession*)session options:(UISceneConnectionOptions*)connectionOptions
{
    if ([scene isKindOfClass: [UIWindowScene class]])
        [self attachUnityWindowTo: (UIWindowScene*)scene];
}

- (void)sceneWillEnterForeground:(UIScene*)scene
{
    [AppController() applicationWillEnterForeground: [UIApplication sharedApplication]];
}

- (void)sceneDidBecomeActive:(UIScene*)scene
{
    // Starts Unity on first activation (the scene is connected by now)
    [AppController() applicationDidBecomeActive: [UIApplication sharedApplication]];
    if ([scene isKindOfClass: [UIWindowScene class]])
        [self attachUnityWindowTo: (UIWindowScene*)scene];
}

- (void)sceneWillResignActive:(UIScene*)scene
{
    [AppController() applicationWillResignActive: [UIApplication sharedApplication]];
}

- (void)sceneDidEnterBackground:(UIScene*)scene
{
    [AppController() applicationDidEnterBackground: [UIApplication sharedApplication]];
}

- (void)scene:(UIScene*)scene openURLContexts:(NSSet<UIOpenURLContext*>*)URLContexts
{
    for (UIOpenURLContext* context in URLContexts)
        [AppController() application: [UIApplication sharedApplication] openURL: context.URL options: @{}];
}

@end
