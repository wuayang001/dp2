﻿using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("dp2SSL")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("dp2SSL")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

//若要开始生成可本地化的应用程序，请设置
//.csproj 文件中的 <UICulture>CultureYouAreCodingWith</UICulture>
//例如，如果您在源文件中使用的是美国英语，
//使用的是美国英语，请将 <UICulture> 设置为 en-US。  然后取消
//对以下 NeutralResourceLanguage 特性的注释。  更新
//以下行中的“en-US”以匹配项目文件中的 UICulture 设置。

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //主题特定资源词典所处位置
                                     //(未在页面中找到资源时使用，
                                     //或应用程序资源字典中找到时使用)
    ResourceDictionaryLocation.SourceAssembly //常规资源词典所处位置
                                              //(未在页面中找到资源时使用，
                                              //、应用程序或任何主题专用资源字典中找到时使用)
)]


// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
// 可以指定所有值，也可以使用以下所示的 "*" 预置版本号和修订号
// 方法是按如下所示使用“*”: :
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.8.34")]    // 1.5.*
[assembly: AssemblyFileVersion("1.8.34.0")]  // 1.5.0.0

// 1.0 2019/2/21 第一个版本
// 1.1 2019/2/26 可以显示版本号了
// 1.2 2019/6/14 代码重构以后的版本，并具备人脸识别功能
// 1.3 2019/11/27 这个版本的 dp2ssl 要和 RfidCenter 1.4 版配套使用才行。基本实现了智能书柜功能
// 1.4 2020/4/2 重构算法。书柜 SaveActions() 阶段即刷新门控件上的数字，和与 dp2library 同步无关。具有本地数据库保存操作
// 1.5 2020/9/8 新增加 greensetup.exe 绿色安装，并测试完成
//      1.5.18 (2020/10/14) 消除“清掉前一个同天线号的门的图书数字 bug”
//      1.5.19 (2020/11/5) 增加 “盘点”功能
//      1.5.20 (2020/11/24) 智能书柜：修正密集开关门时的 bug；解决开门后立刻关门情形的状态可靠问题。自助借还：增加自动返回主菜单的配置参数
//      1.5.21 (2020/11/25) 智能书柜的 NewTagList 算法重构，彻底分离图书和读者证读卡器的线程和标签内存列表，以获得任何时候刷读者卡的敏捷性。增加了处理过程中防范返回主菜单的机制
//      1.5.22 (2020/11/30) 预备 1.6 版本
//      1.5.23 (2020/12/1) 简化 PageShelf 中的 PatronTags 算法，直接用 ShelfData.PatronTagList.Tags
//      1.5.24 (2020/12/4) 增加物理开关灯错误日志记载
//      1.5.25 (2020/12/7) 预备 1.6 版本，回归测试自助借还功能
//      1.5.26 (2020/12/8) RestoreRealTags() 不显示错误信息，而是直接写入错误日志文件
// 1.6 2020/12/8 增加了盘点功能，重构了开关门相关算法
//      1.6.0 (2020/12/8) 新版本
//      1.6.1 (2020/12/9) (设置为身份读卡器竖放情况下)自助借还界面返回主菜单时会残留读者信息区的红色报错信息谓语清除。这个问题已经解决
//      1.6.2 (2021/1/3) 远程控制命令增加 check tag 命令
// 1.7 2021/1/16 盘点功能增加对 SIP2 协议的支持
//      1.7.0 (2021/1/16) 增加 inventory.xml 配置文件。开始盘点对话框暂不支持“校验 EAS”功能
//      1.7.1 (2021/1/17) 增加导入 UID 对照表和清除本地 UID 缓存的功能
//      1.7.2 (2021/1/21) 书柜增加 set sterilamp time 6:00,22:40 命令
//      1.7.3 (2021/1/22)
//      1.7.4 (2021/1/25)
//      1.7.5 (2021/1/27)
//      1.7.6 (2021/1/31) 盘点功能严格检查标签 OI
// 1.8 2021/2/4 盘点功能累积改进阶段测试完成后后发布的正式版
//      1.8.0 (2021/2/4) 发布正式版
//      1.8.1 (2021/2/4) 智能书柜界面可以设置为在休眠一定时间后自动返回主菜单
//      1.8.2 (2021/2/5) 智能书柜界面返回主菜单以前会自动检查当前是否有正在处理中的后台任务(门状态变化引起的处理)，当后台任务全部完成后才允许返回主菜单
//      1.8.3 (2021/2/5) 智能书柜界面可以接受形如 ~supervisor 的工作人员身份条码扫入，设置画面增加了针对工作人员扫入条码的配置参数
//      1.8.4 (2021/2/8) 智能书柜界面如果固定读者信息的时间超过十分钟，软件会弹出对话框确认读者是否还在机器前面，如果读者没有点一次按钮则会自动清除读者信息
//      1.8.5 (2021/2/9) 增加了智能书柜界面固定工作人员信息时间太长，然后自动清除功能；还对自助借还界面也增加了读者信息固定时间太长然后自动清除的功能，只对设置为读者身份读卡器竖放的状态起作用
//      1.8.6 (2021/2/18)
//      1.8.7 (2021/2/18) shelf.xml 中增加 settings/key/@name="菜单页面显示图书馆名" 配置参数，定义在菜单页面是否显示图书馆名字
//      1.8.8 (2021/3/8) 增加下载全部和自动同步册记录和书目摘要到本地缓存功能。断网情况下连接 dp2MServer 的错误日志条目改为紧凑日志形态
//      1.8.9 (2021/3/9) 下载全部册记录功能和同步册记录功能做了改进，只下载和同步当前 dp2library 账户所管辖的馆代码内的馆藏地对应的册记录。shelf.xml 中增加配置参数 <key name="断网模式下开门前检查读者是否超期" value="true"/>
//      1.8.10 (2021/3/10) 盘点功能增加上传外部接口。在 inventory.xml 中增配参数 <uploadInterface protocol='' baseUrl='http://localhost:62022/'/>
//      1.8.11 (2021/4/1) 盘点功能增加导入 UID-->UII 对照表到 dp2library 实体库的功能。原先只能导入到 sip 方式下的本地对照库
//                          自助借还功能的 SIP 模式根据最新版 dp2capo 做了修改，能按照 dp2capo 要求在请求中包含正确的 AO 字段
//      1.8.12 (2021/4/2) 自助借还功能中，ISO15693 读者证严格要求具备 OI 字段，图书标签也严格要求具备 OI 字段
//      1.8.13 (2021/4/8) 自助借还 SIP Login() 消除了一处 bug (返回 -1 和 0 都不是登录成功，1 才是登录成功)
//      1.8.14 (2021/4/14) 发布正式版
//      1.8.15 (2021/4/15) 增加 charging.xml，提供配置事项 <key name="图书标签严格要求机构代码" value="true"/>，用于定义自助借还功能是否严格要求图书标签具备机构代码
//      1.8.16 (2021/4/15) charging.xml 中增加了 verify 属性
//      1.8.17 (2021/4/15) inventory.xml 中增加 <key name="RPAN图书标签和层架标状态切换" value="true"/> 参数，启用了新版 R-PAN 手柄切换两类标签状态功能
//      1.8.18 (2021/5/11) 设置页面增加了导入脱机册信息功能
//      1.8.19 (2021/5/11) 书柜在断网模式下，点门控件上的数字查看图书信息，新导入的册信息有时候会没有当前位置显示。这一 bug 已经修正
//                          书柜在断网模式下借出以后，读者信息里面的在借册列表信息中期限显示为空。这一 bug 已经修正
//      1.8.20 (2021/5/12) 书柜启动的时候，对书柜中现存的图书对应的、上次暂时没有来得及同步的 action 记录，会修改 State 为 "dontsync"，然后本次启动又会新增必要的 action 记录。最新版在修改 State 的同时，也会在 SyncErrorCode 字段中添加 removeRetry 值，表明原因
//      1.8.21 (2021/5/13) shelf.xml 中增加 <key name="休眠关闭提交对话框秒数" value="0"/> 参数，可配置书柜借书还书时“提交对话框”休眠多少秒以后自动关闭。另外刷读者卡会自动关闭开着的“提交对话框”
//      1.8.22 (2021/5/13) 取消 ShelfData.RemoveRetryActionsFromDatabaseAsync()。这样以前积累的尚未同步的动作无论如何后面都会尝试同步
//      1.8.23 (2021/5/14) 当书柜启动的时候，原先版本在网络良好的情况下会直接向 dp2library 服务器提交 return 和 inventory 动作，如果先前还有断网或者网络不良阶段积累的未同步动作，后面就会造成同步顺序颠倒。新版本在这里，改为无论网络是否良好，都先把请求放入队列，等后台同步线程去处理同步
//      1.8.24 (2021/5/17) 当书柜启动的时候，原先版本在网络良好的情况下会直接向 dp2library 服务器提交 return 和 inventory 动作，并直接修改本地 action 数据库记录中的 State 和 SyncOperTime 等字段。新版本去掉了这些修改动作，改为无修改地写入本地 action 库
//      1.8.25 (2021/5/17) 断网模式下，新放入书柜的图书，详细信息不会再出现“通讯失败”的报错，而是会出现“本机没有此册信息”的报错
//      1.8.26 (2021/5/18) 当书柜功能跟踪 dp2library 操作日志的时候，如果发现 new 一个册记录的日志记录，会在本地动作库中搜寻这个创建册记录操作时间以后的所有动作，把 State 修改为空，这样可以促使这些动作重新开始同步
//      1.8.27 (2021/5/19) 书柜界面刷指纹、一维码、二维码成功显示读者信息时，会自动关闭以前残留的提交对话框
//      1.8.28 (2021/5/24) GetChannel() 增加了检查并清理通道、防止通道数量过多的代码
//      1.8.29 (2021/5/25) 消除自助借还和书柜界面以读者二维码登入然后登记人脸成功后自动刷新右侧读者信息报错的 bug
//      1.8.30 (2021/6/9) 消除自助借还界面以二维码借书以后自动刷新右侧读者信息报错的 bug
//      1.8.31 (2021/6/17) 优化自助借还功能中绑定副卡时候的文字提示
//      1.8.32 (2021/6/30) shelf.xml 中可以用参数 <key name="语音提醒关门延迟秒数" value="15,10"/> 定义提醒关门的延迟时间
//      1.8.33 (2021/7/2) 智能书柜的动作同步到 dp2library 服务器，这些动作中的时间信息采用了本地软时钟。dp2ssl 启动的时候，和每隔一个小时，会自动请求 dp2library GetClock() 获得服务器时间，形成一种本地软时钟，可以逼近服务器时间
//      1.8.34 (2021/7/2) 设置页面增加临时菜单命令“* 清除动作记录的 dontsync 状态”