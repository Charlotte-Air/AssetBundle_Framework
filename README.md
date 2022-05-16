# :smiley_cat: AssetBundle Framework
⭐️`Bundle Tool`</br>

Documentation in progress 。。。。。

⭐️`Bundle Assets`</br>
`Audio` - `Music` - `Sound` </br>
`Effect` </br>
`LuaScripts` </br>
`Model`-`prefabs` </br>
`Scenes` </br>
`Sprite` </br>
`UI` - `prefabs` - `Resources` </br>

⭐️`Assets FileInfo~`</br>
文件路径名|Bundle名|依赖文件列表 </br>

⭐️`ResoureceManager ` </br>
处理解析版本文件  </br>
处理Bundle与AssetBundle资源分配与加载  </br>
处理Bundle依赖引用计数与释放AssetBundle资源  </br>
加载资源的接口等...  </br>

⭐️`加载模式`  </br>
EditorMode （在编译器模式下可以直接调用资源） </br>
PackgeBundle </br>
UpdateMode </br>
![](http://cdn.processon.com/60dc74c9e401fd7e34265a10?e=1625064154&token=trhI0BY8QfVrIGn9nENop6JAc6l5nZuxhjQ62UfM:7_a8IxnSxliD5AbhvMyLqYqPU5c=) </br>

:smiley_cat:Hot update        
-
⭐️ Update Process~</br>
`HotUpdate脚本` UpdateMode模式下处理 `获取文件信息` -`检测更新` -`资源释放` -`进入游戏` 等... </br>
`初次安装阶段`（本项目策略是filelist文件解析信息是否一致与文件是否存在）</br>
`整包模式`:只读目录热更资源拷贝到可读写目录，框架直接向可读写目录->(检查版本号与解析filelist文件信息或判断文件是否存在) ->如果没有从资源服务器下载 </br>
`分包模式`:只读目录无热更资源，框架直接向可读写目录-> (检查版本号与解析filelist文件信息或判断文件是否存在) ->如果没有从资源服务器下载</br>
`检查更新阶段`：下载服务器资源的filelist文件，对比文件信息与本地一至文件路径 </br>
`其他路径`：`PathUtil` `AppConst`文件里定义路径目录和接口的获取
`只读路径`: Application.streamingAssetsPath/XX/xxxx.ab </br>
`可读写路径`: Application.persistentDataPath/XX/xxxx.ab </br>
`资源服务器`: http://XXX.X.X.X/AssetBundles/XX/xxxx.ab </br>
![](http://cdn.processon.com/60d446110e3e742d29ce8bee?e=1624527906&token=trhI0BY8QfVrIGn9nENop6JAc6l5nZuxhjQ62UfM:QdixZvR63xm_6l4FPRAVLsNz3oM=) </br>

![](http://cdn.processon.com/60dc74d91efad40c1bed6d22?e=1625064170&token=trhI0BY8QfVrIGn9nENop6JAc6l5nZuxhjQ62UfM:ALaLz4Ax3LpI4wWg-TECQkMwD1c=) </br>

⭐️`C# And Lua binding` </br>
Lua通过（Mian）脚本进行管理 </br>
C#通过LuaManager进行管理 与Lua调用进行接口设计 </br>
只是简单的测试一些UI/Scene/Entity的交互~ 还在学习多多见谅~  </br>
Lua与C# `Button`与`Silder`的监听

![](http://cdn.processon.com/60dc7b0ae0b34d238be07329?e=1625065754&token=trhI0BY8QfVrIGn9nENop6JAc6l5nZuxhjQ62UfM:XPmifvIIC42EydEyPpVE6fzcwKY=) </br>

#### 😺`Manager` 
⭐️`Object Pool` </br>
 (多类型对象池)：池内不创建对象，对象再从外部创建，使用完放入池子，再次使用从对象池取出使用 </br>
对象是多种类型、短时间内可以重复使用，过期自动销毁 (可以手动设置每个池子销毁时间) </br>

⭐️`Event Manager` </br>
使用观察者模式：事件订阅与监听等... </br>

⭐️`Sound Manager` </br>
管理UI界面音乐与音效的使用等.... </br>

⭐️`Net Manager` </br>
管理客户端与服务器Lua交互消息的处理.... </br>

⭐️`UI Manager` </br>
管理分组UI对象和实例化等... </br>

⭐️`Entity Manager` </br>
管理分组中Entity对象和实例化等... </br>

⭐️`Scene Manager` </br>
管理场景切换/加载/卸载等... </br>

~~~~~~~~~~~~~~~~~~~~~~~~~~~ </br>
