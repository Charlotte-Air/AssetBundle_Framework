public static class AppConst
{
    public enum GameMode
    {
        EditorMode,   //编辑器模式
        PackgeBundle, //Bundle模式
        UpdateMode,   //热更新模式
    }
    
    public static bool OpenLog = true;
    public const string BundleExtension = ".ab";
    public const string FileListName = "filelist.txt";
    public static GameMode gameMode = GameMode.UpdateMode;
    public const string ResouresUrl =  "http://139.224.75.221/AssetBundles";
}