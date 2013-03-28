//
//  UnityGoogleDrivePlugin.h
//  Unity-iPhone
//
//  Created by Midworld Kim on 13. 3. 27..
//
//

#import <Foundation/Foundation.h>
#import "GTLDrive.h"

@interface UnityGoogleDrivePlugin : NSObject

@property (nonatomic, retain) GTLServiceDrive *driveService;

+ (UnityGoogleDrivePlugin*)getInstance;

@end
