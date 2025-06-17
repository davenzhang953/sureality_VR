//
//  LocalManager.h
//  SeenGeneReclocation
//
//  Created by vyyv on 2020/5/25.
//  Copyright © 2020 vyyv. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>

NS_ASSUME_NONNULL_BEGIN

@interface LocalManager : NSObject<CLLocationManagerDelegate>{
    
    CLLocationManager* _locationManager;
    CLLocationCoordinate2D _newCoor;
    float _radius;
    NSString* _locationStr;
    float angle;

}

+ (id)sharedInstance;
-(NSString *)getLocation;
- (void) initLocationManager;

- (void) stop;
- (void) start;
@end

NS_ASSUME_NONNULL_END
