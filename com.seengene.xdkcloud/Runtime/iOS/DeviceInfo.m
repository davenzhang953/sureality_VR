//
//  ForWechat.m
//  Unity-iPhone
//
//  Created by vyyv on 2020/4/20.
//

#import "DeviceInfo.h"
#import "LocalManager.h"
#import <sys/utsname.h>
#import <AVFoundation/AVFoundation.h>

//#import "ZZUUIDManager.h"

@implementation DeviceInfo

+ (NSString *)getUUID{
    
    //NSString *getUDIDInKeychain = (NSString *)[BGKeychainTool load:KEY_UDID_INSTEAD];
    
    NSUserDefaults * defaults = [NSUserDefaults standardUserDefaults];
    NSString * uuid = [defaults objectForKey:@"uuid"];
    
    NSLog(@"从keychain中获取到的 UDID_INSTEAD %@",uuid);
    if (uuid == nil) {
        CFUUIDRef puuid = CFUUIDCreate( nil );
        CFStringRef uuidString = CFUUIDCreateString( nil, puuid );
        uuid = (NSString *)CFBridgingRelease(CFStringCreateCopy( NULL, uuidString));
        CFRelease(puuid);
        CFRelease(uuidString);
        NSLog(@"\n \n \n _____重新存储 UUID _____\n \n \n  %@",uuid);
        
        [defaults setObject:uuid forKey:@"uuid"];
    }
    NSLog(@"最终 ———— UDID_INSTEAD %@",uuid);
    return uuid;
}


+ (NSString *)getCurrentDeviceModel{
   struct utsname systemInfo;
   uname(&systemInfo);
   
   NSString *deviceModel = [NSString stringWithCString:systemInfo.machine encoding:NSASCIIStringEncoding];
   
if ([deviceModel isEqualToString:@"iPhone3,1"])    return @"iPhone 4";
if ([deviceModel isEqualToString:@"iPhone3,2"])    return @"iPhone 4";
if ([deviceModel isEqualToString:@"iPhone3,3"])    return @"iPhone 4";
if ([deviceModel isEqualToString:@"iPhone4,1"])    return @"iPhone 4S";
if ([deviceModel isEqualToString:@"iPhone5,1"])    return @"iPhone 5";
if ([deviceModel isEqualToString:@"iPhone5,2"])    return @"iPhone 5 (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPhone5,3"])    return @"iPhone 5c (GSM)";
if ([deviceModel isEqualToString:@"iPhone5,4"])    return @"iPhone 5c (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPhone6,1"])    return @"iPhone 5s (GSM)";
if ([deviceModel isEqualToString:@"iPhone6,2"])    return @"iPhone 5s (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPhone7,1"])    return @"iPhone 6 Plus";
if ([deviceModel isEqualToString:@"iPhone7,2"])    return @"iPhone 6";
if ([deviceModel isEqualToString:@"iPhone8,1"])    return @"iPhone 6s";
if ([deviceModel isEqualToString:@"iPhone8,2"])    return @"iPhone 6s Plus";
if ([deviceModel isEqualToString:@"iPhone8,4"])    return @"iPhone SE";
// 日行两款手机型号均为日本独占，可能使用索尼FeliCa支付方案而不是苹果支付
if ([deviceModel isEqualToString:@"iPhone9,1"])    return @"iPhone 7";
if ([deviceModel isEqualToString:@"iPhone9,2"])    return @"iPhone 7 Plus";
if ([deviceModel isEqualToString:@"iPhone9,3"])    return @"iPhone 7";
if ([deviceModel isEqualToString:@"iPhone9,4"])    return @"iPhone 7 Plus";
if ([deviceModel isEqualToString:@"iPhone10,1"])   return @"iPhone_8";
if ([deviceModel isEqualToString:@"iPhone10,4"])   return @"iPhone_8";
if ([deviceModel isEqualToString:@"iPhone10,2"])   return @"iPhone_8_Plus";
if ([deviceModel isEqualToString:@"iPhone10,5"])   return @"iPhone_8_Plus";
if ([deviceModel isEqualToString:@"iPhone10,3"])   return @"iPhone X";
if ([deviceModel isEqualToString:@"iPhone10,6"])   return @"iPhone X";
if ([deviceModel isEqualToString:@"iPhone11,8"])   return @"iPhone XR";
if ([deviceModel isEqualToString:@"iPhone11,2"])   return @"iPhone XS";
if ([deviceModel isEqualToString:@"iPhone11,6"])   return @"iPhone XS Max";
if ([deviceModel isEqualToString:@"iPhone11,4"])   return @"iPhone XS Max";
if ([deviceModel isEqualToString:@"iPhone12,1"])   return @"iPhone 11";
if ([deviceModel isEqualToString:@"iPhone12,3"])   return @"iPhone 11 Pro";
if ([deviceModel isEqualToString:@"iPhone12,5"])   return @"iPhone 11 Pro Max";
if ([deviceModel isEqualToString:@"iPhone12,8"])   return @"iPhone SE2";
if ([deviceModel isEqualToString:@"iPhone13,1"])   return @"iPhone 12 mini";
if ([deviceModel isEqualToString:@"iPhone13,2"])   return @"iPhone 12";
if ([deviceModel isEqualToString:@"iPhone13,3"])   return @"iPhone 12 Pro";
if ([deviceModel isEqualToString:@"iPhone13,4"])   return @"iPhone 12 Pro Max";
if ([deviceModel isEqualToString:@"iPhone14,4"])   return @"iPhone 13 mini";
if ([deviceModel isEqualToString:@"iPhone14,5"])   return @"iPhone 13";
if ([deviceModel isEqualToString:@"iPhone14,2"])   return @"iPhone 13 Pro";
if ([deviceModel isEqualToString:@"iPhone14,3"])   return @"iPhone 13 Pro Max";

if ([deviceModel isEqualToString:@"iPod1,1"])      return @"iPod Touch 1G";
if ([deviceModel isEqualToString:@"iPod2,1"])      return @"iPod Touch 2G";
if ([deviceModel isEqualToString:@"iPod3,1"])      return @"iPod Touch 3G";
if ([deviceModel isEqualToString:@"iPod4,1"])      return @"iPod Touch 4G";
if ([deviceModel isEqualToString:@"iPod5,1"])      return @"iPod Touch (5 Gen)";
if ([deviceModel isEqualToString:@"iPad1,1"])      return @"iPad";
if ([deviceModel isEqualToString:@"iPad1,2"])      return @"iPad 3G";
if ([deviceModel isEqualToString:@"iPad2,1"])      return @"iPad 2 (WiFi)";
if ([deviceModel isEqualToString:@"iPad2,2"])      return @"iPad 2";
if ([deviceModel isEqualToString:@"iPad2,3"])      return @"iPad 2 (CDMA)";
if ([deviceModel isEqualToString:@"iPad2,4"])      return @"iPad 2";
if ([deviceModel isEqualToString:@"iPad2,5"])      return @"iPad Mini (WiFi)";
if ([deviceModel isEqualToString:@"iPad2,6"])      return @"iPad Mini";
if ([deviceModel isEqualToString:@"iPad2,7"])      return @"iPad Mini (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPad3,1"])      return @"iPad 3 (WiFi)";
if ([deviceModel isEqualToString:@"iPad3,2"])      return @"iPad 3 (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPad3,3"])      return @"iPad 3";
if ([deviceModel isEqualToString:@"iPad3,4"])      return @"iPad 4 (WiFi)";
if ([deviceModel isEqualToString:@"iPad3,5"])      return @"iPad 4";
if ([deviceModel isEqualToString:@"iPad3,6"])      return @"iPad 4 (GSM+CDMA)";
if ([deviceModel isEqualToString:@"iPad4,1"])      return @"iPad Air (WiFi)";
if ([deviceModel isEqualToString:@"iPad4,2"])      return @"iPad Air (Cellular)";
if ([deviceModel isEqualToString:@"iPad4,4"])      return @"iPad Mini 2 (WiFi)";
if ([deviceModel isEqualToString:@"iPad4,5"])      return @"iPad Mini 2 (Cellular)";
if ([deviceModel isEqualToString:@"iPad4,6"])      return @"iPad Mini 2";
if ([deviceModel isEqualToString:@"iPad4,7"])      return @"iPad Mini 3";
if ([deviceModel isEqualToString:@"iPad4,8"])      return @"iPad Mini 3";
if ([deviceModel isEqualToString:@"iPad4,9"])      return @"iPad Mini 3";
if ([deviceModel isEqualToString:@"iPad5,1"])      return @"iPad Mini 4 (WiFi)";
if ([deviceModel isEqualToString:@"iPad5,2"])      return @"iPad Mini 4 (LTE)";
if ([deviceModel isEqualToString:@"iPad5,3"])      return @"iPad Air 2";
if ([deviceModel isEqualToString:@"iPad5,4"])      return @"iPad Air 2";
if ([deviceModel isEqualToString:@"iPad6,3"])      return @"iPad Pro 9.7";
if ([deviceModel isEqualToString:@"iPad6,4"])      return @"iPad Pro 9.7";
if ([deviceModel isEqualToString:@"iPad6,7"])      return @"iPad Pro 12.9";
if ([deviceModel isEqualToString:@"iPad6,8"])      return @"iPad Pro 12.9";
if ([deviceModel isEqualToString:@"iPad11,1"])      return @"iPad Mini 5 (5 Gen)";
if ([deviceModel isEqualToString:@"iPad11,2"])      return @"iPad Mini 5 (5 Gen)";
if ([deviceModel isEqualToString:@"iPad14,1"])      return @"iPad Mini 6 (6 Gen)";
if ([deviceModel isEqualToString:@"iPad14,2"])      return @"iPad Mini 6 (6 Gen)";
if ([deviceModel isEqualToString:@"iPad7,1"])      return @"iPad Pro 12.9 (2 Gen)";
if ([deviceModel isEqualToString:@"iPad7,2"])      return @"iPad Pro 12.9 (2 Gen)";
if ([deviceModel isEqualToString:@"iPad7,3"])      return @"iPad Pro 10.5";
if ([deviceModel isEqualToString:@"iPad7,4"])      return @"iPad Pro 10.5";
if ([deviceModel isEqualToString:@"iPad8,1"])      return @"iPad Pro 11.0";
if ([deviceModel isEqualToString:@"iPad8,2"])      return @"iPad Pro 11.0";
if ([deviceModel isEqualToString:@"iPad8,3"])      return @"iPad Pro 11.0";
if ([deviceModel isEqualToString:@"iPad8,4"])      return @"iPad Pro 11.0";
if ([deviceModel isEqualToString:@"iPad8,5"])      return @"iPad Pro 12.9 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad8,6"])      return @"iPad Pro 12.9 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad8,7"])      return @"iPad Pro 12.9 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad8,8"])      return @"iPad Pro 12.9 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad8,9"])      return @"iPad Pro 11.0 (2 Gen)";
if ([deviceModel isEqualToString:@"iPad8,10"])      return @"iPad Pro 11.0 (2 Gen)";
if ([deviceModel isEqualToString:@"iPad8,11"])      return @"iPad Pro 12.9 (4 Gen)";
if ([deviceModel isEqualToString:@"iPad8,12"])      return @"iPad Pro 12.9 (4 Gen)";
        
if ([deviceModel isEqualToString:@"iPad13,4"])      return @"iPad Pro 11.0 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad13,5"])      return @"iPad Pro 11.0 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad13,6"])      return @"iPad Pro 11.0 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad13,7"])      return @"iPad Pro 11.0 (3 Gen)";
if ([deviceModel isEqualToString:@"iPad13,8"])      return @"iPad Pro 12.9 (5 Gen)";
if ([deviceModel isEqualToString:@"iPad13,9"])      return @"iPad Pro 12.9 (5 Gen)";
if ([deviceModel isEqualToString:@"iPad13,10"])      return @"iPad Pro 12.9 (5 Gen)";
if ([deviceModel isEqualToString:@"iPad13,11"])      return @"iPad Pro 12.9 (5 Gen)";

if ([deviceModel isEqualToString:@"AppleTV2,1"])      return @"Apple TV 2";
if ([deviceModel isEqualToString:@"AppleTV3,1"])      return @"Apple TV 3";
if ([deviceModel isEqualToString:@"AppleTV3,2"])      return @"Apple TV 3";
if ([deviceModel isEqualToString:@"AppleTV5,3"])      return @"Apple TV 4";

if ([deviceModel isEqualToString:@"i386"])         return @"Simulator";
if ([deviceModel isEqualToString:@"x86_64"])       return @"Simulator";
    return deviceModel;
}@end

#if defined(__cplusplus)
extern "C"{
#endif
    
    char* cStringCopy(const char* string)
    {
        if (string == NULL)
            return NULL;

        char* res = (char*)malloc(strlen(string) + 1);
        strcpy(res, string);

        return res;
    }
        
    char* RequestGetDeviceInfo(){
        //NSLog(@"RequestGetDeviceInfo 00");
        //int notchHeight = getIPhoneNotchScreen();
        //int screenRound = getIPhoneScreenRound();
        NSString *model = [DeviceInfo getCurrentDeviceModel];
        NSString *iponeM = [[UIDevice currentDevice] systemVersion];
        NSString *UUID = [DeviceInfo getUUID];
        NSString *str = [NSString stringWithFormat: @"{\"dev_type\":\"Apple\", \"dev_model\":\"%@\",\"version\":\"%@\",\"dev_id\":\"%@\"}", model, iponeM, UUID];
        
        NSLog(@"RequestGetDeviceInfo 11 %@", str);
        
        const char* myChars =[str UTF8String];
        return cStringCopy(myChars);
    }

    char* getAppVersion(){
        // app版本
        NSString *ver = [[[NSBundle mainBundle] infoDictionary] objectForKey:@"CFBundleShortVersionString"];
       // NSString *build = [[[NSBundle mainBundle] infoDictionary] objectForKey:@"CFBundleVersion"];
        
        //NSString* version=[NSString stringWithFormat:@"%@.%@",ver,build];
        const char* myChars =[ver UTF8String];
        return cStringCopy(myChars);
    }

    
    const char * RequestCurrentLocation(){
        NSString *location = [[LocalManager sharedInstance]getLocation];
        NSLog(@"location: %@",location);
       // jsonstr = [location UTF8String];
        const char* myChars =[location UTF8String];
        return cStringCopy(myChars);
    }
    
    void InitNativeRequest(){
        LocalManager *manager = [LocalManager sharedInstance];
    }
    
    void StartUpdatingLocation(){
        LocalManager *manager = [LocalManager sharedInstance];
        [manager start];
    }
    
    void StopUpdatingLocation(){
        LocalManager *manager = [LocalManager sharedInstance];
        [manager stop];
    }
    
    
#if defined(__cplusplus)
}
#endif
