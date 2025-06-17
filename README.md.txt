# 说明
 
## Pico 4 Ultra Enterprise SDK

- PICO Integration SDK version == 3.0.4

（联系Pico相关团队获取该SDK）

## 修复package错误

升级至Unity6000.0.28f1

```
// PBXProjectConfig.cs
// 增加macro
// origin:
using UnityEditor.iOS;
using UnityEditor.iOS.Xcode;
// changed:
#if UNITY_IOS
using UnityEditor.iOS;
using UnityEditor.iOS.Xcode;
#endif
```

```
// PlasticSCMWindow.cs
// CloudEditionWelcomeWindow.cs
// 以上两个脚本中在声明TableView时明确指定对象即可
// origin：
mTabView = new TabView();
// changed：
mTabView = new Unity.PlasticSCM.Editor.UI.UIElements.TabView();
```
