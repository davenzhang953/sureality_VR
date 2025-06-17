//
//  LocalManager.m
//  SeenGeneReclocation
//
//  Created by vyyv on 2020/5/25.
//  Copyright © 2020 vyyv. All rights reserved.
//

#import "LocalManager.h"

@implementation LocalManager

- (id) init {
    if ( self=[super init] ){   // 必須呼叫父類的init
        [self initLocationManager];
    }
    return self;
}

+ (id)sharedInstance
{
    static dispatch_once_t pred = 0;
    __strong static id _sharedObject = nil;
    dispatch_once(&pred, ^{
        _sharedObject = [[self alloc] init]; // or some other init method
    });
    return _sharedObject;
}


#pragma mark Init Beacons
- (void) initLocationManager{
    if (!_locationManager) {
        _locationManager = [[CLLocationManager alloc] init];
        _locationManager.delegate = self;
        
        _locationManager.desiredAccuracy = kCLLocationAccuracyBestForNavigation;
        _locationManager.distanceFilter = kCLDistanceFilterNone;
        
        angle = 0.0;
        
        [_locationManager requestAlwaysAuthorization];
        [_locationManager startUpdatingLocation];
     //   if (_locationManager.headingAvailable){
            [_locationManager startUpdatingHeading];
     //   }
        
        if ([_locationManager respondsToSelector:@selector(requestWhenInUseAuthorization)]) {
            [_locationManager requestWhenInUseAuthorization];
        }
        
        
    }
}

- (void) stop{
    [_locationManager stopUpdatingLocation];
   // if (_locationManager.headingAvailable){
        [_locationManager stopUpdatingHeading];
   // }
}

-(void) start{
    [_locationManager startUpdatingLocation];
   // if (_locationManager.headingAvailable){
        [_locationManager startUpdatingHeading];
   // }
}


- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray<CLLocation *> *)locations {
    
    //NSLog(@"didUpdateLocations is working");
    
    CLLocation *location = locations[0];
    
    NSMutableString *result=[[NSMutableString alloc]init];
    _newCoor = location.coordinate;//原始坐标
    _radius = location.horizontalAccuracy;
    NSLog(@"coor %f %f",_newCoor.latitude,_newCoor.longitude);
    NSLog([self transform]);
    
    CLGeocoder *geocoder = [[CLGeocoder alloc]init];
    [geocoder reverseGeocodeLocation:location completionHandler:^(NSArray<CLPlacemark *> *_Nullable placemarks, NSError * _Nullable error) {
        for (CLPlacemark *place in placemarks) {
           NSMutableString *tempstr = [[NSMutableString alloc]init];
           if (place.locality != nil) {
              [tempstr appendString:place.locality];
           //   [tempstr appendString:@" "];
           }

           if (place.thoroughfare != nil) {
              [tempstr appendString:place.thoroughfare];
           }
           
           if (place.subThoroughfare != nil) {
              [tempstr appendString:place.subThoroughfare];
           //    [tempstr appendString:@" "];
           }
           
           _locationStr = tempstr;
            NSLog(@"name,%@",_locationStr);
        }
    }];

//    NSDictionary* testdic = BMKConvertBaiduCoorFrom(coor,BMK_COORDTYPE_COMMON);
//    testdic = BMKConvertBaiduCoorFrom(coor,BMK_COORDTYPE_GPS);
//    CLLocationCoordinate2D baiduCoor = BMKCoorDictionaryDecode(testdic);//转换后的百度坐标
//    
//    Coordinate *coordinate=[[Coordinate alloc]initWithLat:baiduCoor.latitude lon:baiduCoor.longitude alt:location.altitude];
//    _currentCoor=coordinate;
//    
//  //[activityManager refreshCoordinate:_currentCoor];
//    
//    NSLog(@"didUpdateLocations %@  %@",coordinate.lat,coordinate.lon);
//    
//    if(_isCreaked){
//        ScenicspotManager *scenicspotManager=[ScenicspotManager sharedInstance];
//        _creakincr=_creakincr+0.0002;
//        Coordinate *coor=[[Coordinate alloc]initWithLat:_creakedCoor.lat.floatValue+_creakincr lon:_creakedCoor.lon.floatValue+_creakincr alt:location.altitude];
//        [scenicspotManager updataCoor:coor];
//    }else{
//        ScenicspotManager *scenicspotManager=[ScenicspotManager sharedInstance];
//        [scenicspotManager updataCoor:_currentCoor];
//    }
    
}


-(void)locationManager:(CLLocationManager *)manager didUpdateHeading:(CLHeading *)newHeading{
    
    /**
     * magneticHeading 表示磁北极偏转的角度
     ＊trueHeading  磁北极的真实角度
     ＊0.0 - 359.9 度, 0 being true North
     */
    
   // NSLog(@"%f",newHeading.magneticHeading);
    
    //将我们的角度转换为弧度
    angle=newHeading.magneticHeading/180.0*M_PI;
}

-(NSString *)getLocation{

    if (_locationStr == nil) {
        return @"{\"success\":false}";
    }
    NSString* str = [NSString stringWithFormat:@"{\"success\":true, %@, \"radius\": %f,\"magneticHeading\": %f,\"thoroughfare\":\"%@\"}",[self transform], _radius, angle*180/3.1416, _locationStr];
    return str;
}


-(NSString *)transform {
    //var latlng = [];
    double pi = 3.14159265358979324;
    double a = 6378245.0;
    double ee = 0.00669342162296594323;

    double wgLon = _newCoor.longitude;
    double wgLat = _newCoor.latitude;

    double dLat = [self transformLat:wgLon - 105.0 lat:wgLat - 35.0];
    double dLon = [self transformLon:wgLon - 105.0 lat:wgLat - 35.0];
    double radLat = wgLat / 180.0 * pi;
    double magic = sin(radLat);
    magic = 1 - ee * magic * magic;
    double sqrtMagic = sqrt(magic);
    dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
    dLon = (dLon * 180.0) / (a / sqrtMagic * cos(radLat) * pi);
    
    NSString* latlng = [NSString stringWithFormat:@"\"longitude\":%f, \"latitude\":%f",wgLon + dLon,wgLat + dLat];
    return latlng;
};

-(double)transformLat:(double)x lat:(double)y {
    double pi = 3.14159265358979324;
    double ret = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * sqrt(fabs(x));
    ret += (20.0 * sin(6.0 * x * pi) + 20.0 * sin(2.0 * x * pi)) * 2.0 / 3.0;
    ret += (20.0 * sin(y * pi) + 40.0 * sin(y / 3.0 * pi)) * 2.0 / 3.0;
    ret += (160.0 * sin(y / 12.0 * pi) + 320 * sin(y * pi / 30.0)) * 2.0 / 3.0;
    return ret;
};

-(double)transformLon:(double)x lat:(double)y {
    double pi = 3.14159265358979324;
    double ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * sqrt(fabs(x));
    ret += (20.0 * sin(6.0 * x * pi) + 20.0 * sin(2.0 * x * pi)) * 2.0 / 3.0;
    ret += (20.0 * sin(x * pi) + 40.0 * sin(x / 3.0 * pi)) * 2.0 / 3.0;
    ret += (150.0 * sin(x / 12.0 * pi) + 300.0 * sin(x / 30.0 * pi)) * 2.0 / 3.0;
    return ret;
};

@end
