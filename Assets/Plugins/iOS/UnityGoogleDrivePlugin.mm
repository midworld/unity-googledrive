//
//  UnityGoogleDrivePlugin.m
//  Unity-iPhone
//
//  Created by Midworld Kim on 13. 3. 27..
//
//

#import "UnityGoogleDrivePlugin.h"
#import "GTMOAuth2ViewControllerTouch.h"

static NSString *const kKeychainItemName = @"Unity Google Drive Plugin";
static NSString *const kClientID = @"897584417662-rnkgkl5tlpnsau7c4oc0g2jp08cpluom.apps.googleusercontent.com";
static NSString *const kClientSecret = @"tGNLbYnrdRO2hdFmwJAo5Fbt";

@implementation UnityGoogleDrivePlugin

@synthesize driveService;

static UnityGoogleDrivePlugin* g_instance = nil;

+ (UnityGoogleDrivePlugin*)getInstance
{
    NSLog(@"----> 1");
    
    if (g_instance == nil)
    {
        g_instance = [[UnityGoogleDrivePlugin alloc] init];
    }
    
    return g_instance;
}

- (id)init
{
    NSLog(@"----> 2");
    
    self = [super init];
    
    if (g_instance != nil)
        NSLog(@"Unity Google Drive Plugin is duplicated!");
    
    g_instance = self;
    
    NSLog(@"----> 3");
    
    // Initialize the drive service & load existing credentials from the keychain if available
    self.driveService = [[GTLServiceDrive alloc] init];
    self.driveService.authorizer = [GTMOAuth2ViewControllerTouch
                                    authForGoogleFromKeychainForName:kKeychainItemName
                                    clientID:kClientID
                                    clientSecret:kClientSecret];
    
    NSLog(@"----> 4");
    
    return self;
}

- (void)auth
{
    NSLog(@"----> 5");
    
    if ([self isAuthorized])
    {
        // test ----
        {
            NSLog(@"GTLQueryDrive queryForFilesList");
            
            driveService.shouldFetchNextPages = YES;
            
            GTLQueryDrive* query = [GTLQueryDrive queryForFilesList];
            GTLServiceTicket* queryTicket =
            [driveService executeQuery:query
                     completionHandler:^(GTLServiceTicket* ticket, GTLDriveFileList* files, NSError* error) {
                         if (error == nil)
                         {
                             NSLog(@"%@", files);
                             //files.JSONString
                             
                             for (int i = 0; i < files.items.count; i++)
                             {
                                 NSLog(@"[%d] %@", i, [files.items objectAtIndex:i]);
                             }
                         } else {
                             NSLog(@"File List Error: %@", error);
                         }
                     }];
        }
        
        return;
    }
    
    GTMOAuth2ViewControllerTouch* authController;
    authController = [[GTMOAuth2ViewControllerTouch alloc]
                      initWithScope:kGTLAuthScopeDrive
                      clientID:kClientID
                      clientSecret:kClientSecret
                      keychainItemName:kKeychainItemName
                      delegate:self
                      finishedSelector:@selector(viewController:finishedWithAuth:error:)];
    
    NSLog(@"----> 6");
    
    UIViewController* vc = [[[UIApplication sharedApplication] keyWindow] rootViewController];
    
    NSLog(@"%@", vc);
    
    [vc presentModalViewController:authController animated:YES];
    
    NSLog(@"----> 7");
}

// Handle completion of the authorization process, and updates the Drive service
// with the new credentials.
- (void)viewController:(GTMOAuth2ViewControllerTouch *)viewController
      finishedWithAuth:(GTMOAuth2Authentication *)authResult
                 error:(NSError *)error
{
    NSLog(@"----> 8 %@", error);
    
    if (error != nil)
    {
        [self showAlert:@"Authentication Error" message:error.localizedDescription];
        self.driveService.authorizer = nil;
    }
    else
    {
        self.driveService.authorizer = authResult;
    }
}


// Helper to check if user is authorized
- (BOOL)isAuthorized
{
    return [((GTMOAuth2Authentication*)self.driveService.authorizer) canAuthorize];
}

extern "C" void Auth()
{
    NSLog(@"UnityGoogleDrivePlugin: Auth()");
    
    [[UnityGoogleDrivePlugin getInstance] auth];
}

@end
