public enum GameMode
{
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
    public const string ResouresUrl =  "http://139.224.75.221/AssetBundles";
}
