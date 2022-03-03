public enum GameMode
{
<<<<<<< HEAD
=======
    Default,
>>>>>>> 02a2b943112880eea05a74e017239d1b82d3cda6
    EditorMode,   //编辑器模式
    PackgeBundle, //打包模式
    UpdateMode,   //热更新模式
}

public enum GameEvent
{
    GameInit = 10000,
    StartLua,
}

public static class AppConst
{
    public static bool OpenLog = true;
    public const string BundleExtension = ".ab";
    public const string FileListName = "filelist.txt";
    public static GameMode gameMode = GameMode.EditorMode;
<<<<<<< HEAD
    public const string ResouresUrl =  "http://139.224.75.221/AssetBundles";
=======
    public const string ResouresUrl = "http://139.224.75.221/AssetBundles"; //热更新服务器地址
>>>>>>> 02a2b943112880eea05a74e017239d1b82d3cda6
}
