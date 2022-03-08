using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Builder
{

    public class ABBuilderUI : EditorWindow
    {

        public BuildConfig Config { get; set; }

        private Vector2 mScrollPos;
        private GUIStyle mFontStyle = new GUIStyle();
        private static List<ABBuilderUI> Windows = new List<ABBuilderUI>();

        private readonly string[] Tips =
        {
            "场景资源编译路径配置，编译所有此路径下的.unity文件",
            "预制件资源编译路径配置，编译所有此路径下的.prefab文件",
            "Shader资源编译路径配置，编译此路径下的.shadervariants和所有.shader文件",
            "文件夹打包路径配置，此路径资源按照文件夹名字打成AB包"
        };

        [MenuItem("BuilderTool/Builder Config", false, 0)]
        public static void ShowWindow()
        {
            string path = string.Format("{0}/{1}", Environment.CurrentDirectory, BuildConfig.ConfigPath);
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                if (files.Length > 0)
                {
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(files[i]);
                        AddConfig(fileName, i == 0);
                    }
                    return;
                }
            }

            AddConfig("mod_default", true);
        }

        private static void AddConfig(string name, bool first = false)
        {
            if (!CheckConfigName(name, 0))
                return;

            ABBuilderUI window = EditorWindow.CreateWindow<ABBuilderUI>(name, typeof(ABBuilderUI));
            if (first)
            {
                window.minSize = new Vector2(720, 560);
                window.CenterOnMainWin(new Vector2(0, -100));
            }
            window.Init(name);
            window.Show();
            Windows.Add(window);
        }

        private void DeleteConfig(string name)
        {
            string path = string.Format("{0}/{1}{2}.txt", Environment.CurrentDirectory, BuildConfig.ConfigPath, name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Windows.Remove(this);

            if (Windows.Count == 0)
            {
                AddConfig("mod_default");
            }
            this.Close();
        }

        private void Init(string name)
        {
            mFontStyle.normal.textColor = Color.gray;
            mFontStyle.fontSize = 16;
            mFontStyle.alignment = TextAnchor.LowerCenter;

            Config = BuildConfig.GetBuildConfig(name);
        }

        private void AddCustomConfig()
        {
            ShowPopupWindow("配置名称", (name) =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = "Custom";
                }
                Config.AddCustomBuild(name);
            });
        }

        private void RemoveCustomConfig(BuildData data)
        {
            Config.RemoveCustomBuild(data);
        }

        void OnFocus()
        {

        }

        void OnGUI()
        {
            mScrollPos = GUILayout.BeginScrollView(mScrollPos);
            GUILayout.Space(6f);

            //set mod name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模组名称（资源输出的根目录,1个模组对应1个Manifest）", GUILayout.Width(302));
            Config.ModName = EditorGUILayout.TextField(new GUIContent("", ""), string.IsNullOrEmpty(Config.ModName) ? "mod_default" : Config.ModName);
            if (!Config.ModName.StartsWith("mod_"))
            {
                Config.ModName = "mod_" + Config.ModName;
            }
            if (titleContent.text != Config.ModName)
            {
                titleContent.text = Config.ModName;
            }
            EditorGUILayout.EndHorizontal();

            //set output path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出路径", GUILayout.Width(50));
            Config.OutputPath = EditorGUILayout.TextField(new GUIContent("", ""), string.IsNullOrEmpty(Config.OutputPath) ? Application.streamingAssetsPath : Config.OutputPath);
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                EditorApplication.delayCall = () =>
                {
                    string ret = WindowsExplorer.FolderSelector();
                    if (!string.IsNullOrEmpty(ret))
                    {
                        Config.OutputPath = ret.Replace("\\", "/");
                    }
                    Debug.Log(ret);
                };
            }
            EditorGUILayout.EndHorizontal();

            //platform
            Config.Platform = (BuildPlatform)EditorGUILayout.EnumPopup("选择打包平台", Config.Platform);
            //EditorGUILayout.LabelField(Config.OutputPath + '/' + Config.Platform + '/' + Config.ModName);

            GUILayout.Space(10f);

            //build scene
            CustomBuildConfig(Config.SceneData);

            //build prefab
            CustomBuildConfig(Config.PrefabData);

            //build shader
            CustomBuildConfig(Config.ShaderData);

            //gen
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清除标签"))
            {
                ABBuilder.CleanBuildName();
                EditorUtility.DisplayDialog("AssetBunlde", "Clean Finish", "Close");
            }

            if (GUILayout.Button("生成资源"))
            {
                ABBuilder.GenAllBuildName(Config);
                EditorUtility.DisplayDialog("AssetBunlde", "Gen AssetBundle Finish", "Close");
            }

            if (GUILayout.Button("一键编译生成"))
            {
                ABBuilder.CleanAndGenAllBuildName(Config);
                EditorUtility.DisplayDialog("AssetBunlde", "CleanAndGenAllBuild AssetBundle Finish", "Close");
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);


            //custom cfg
            EditorGUILayout.LabelField("----- 以下是按文件夹打包，点击+添加配置，每个配置单独打包，不会编译标签 -----", mFontStyle);
            for (int i = 0; i < Config.CustomBuild.Count; ++i)
            {
                CustomBuildConfig(Config.CustomBuild[i]);
            }

            if (GUILayout.Button("+"))
            {
                AddCustomConfig();
            }
            GUILayout.Space(10f);

            if (GUILayout.Button("一键清标签、编译、打包以上所有配置"))
            {
                ABBuilder.CleanAndGenConfig(Config);
                EditorUtility.DisplayDialog("AssetBunlde", "CleanAndGenConfig Finish", "Close");
            }
            GUILayout.Space(10f);

            //save
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("保存配置"))
            {
                if (CheckConfigName(Config.ModName, 1))
                {
                    Config.Save();
                }
            }

            if (GUILayout.Button("新建配置"))
            {
                ShowPopupWindow("模组名称", (name) =>
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!name.StartsWith("mod_"))
                        {
                            name = "mod_" + name;
                        }
                        AddConfig(name);
                    }
                });
            }

            if (GUILayout.Button("删除配置"))
            {
                if(EditorUtility.DisplayDialog("AssetBunlde", "删除配置后将无法恢复，确定删除？", "确定删除", "取消"))
                    DeleteConfig(Config.ModName);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.EndScrollView();
        }

        private void CustomBuildConfig(BuildData data)
        {
            //tips
            EditorGUILayout.LabelField(Tips[(int)data.Type]);

            //path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("指定路径", GUILayout.Width(50));
            var inputPath = EditorGUILayout.TextField(new GUIContent("", ""), string.IsNullOrEmpty(data.InputPath) ? BuildConfig.InputPathTips : data.InputPath);
            data.InputPath = inputPath == BuildConfig.InputPathTips ? "" : inputPath;
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                EditorApplication.delayCall = () =>
                {
                    string path = WindowsExplorer.FolderSelector();
                    if (CheckPath(path, true, out var ret))
                    {
                        if (data.InputPath == BuildConfig.InputPathTips)
                        {
                            data.InputPath = "";
                        }
                        if (!string.IsNullOrEmpty(data.InputPath))
                            data.InputPath += '|';
                        data.InputPath += ret;
                    }
                };
            }
            EditorGUILayout.EndHorizontal();

            //files
            if (data.Type == BuildType.Scene)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("指定文件", GUILayout.Width(50));
                var inputFiles = EditorGUILayout.TextField(new GUIContent("", ""), string.IsNullOrEmpty(data.InputFiles) ? BuildConfig.InputFilesTips : data.InputFiles);
                data.InputFiles = inputFiles == BuildConfig.InputFilesTips ? "" : inputFiles;
                if (GUILayout.Button("...", GUILayout.Width(20)))
                {
                    EditorApplication.delayCall = () =>
                    {
                        var pathList = WindowsExplorer.FileSelector(Application.dataPath, "unity文件(*.unity)\0*.unity");
                        if (pathList != null)
                        {
                            if (data.InputFiles == BuildConfig.InputFilesTips)
                            {
                                data.InputFiles = "";
                            }
                            for (int i = 0; i < pathList.Count; ++i)
                            {
                                if (CheckPath(pathList[i], false, out var ret))
                                {
                                    if (!string.IsNullOrEmpty(data.InputFiles))
                                        data.InputFiles += '|';
                                    data.InputFiles += ret;
                                }
                            }
                        }
                    };
                }
                EditorGUILayout.EndHorizontal();
            }

            if (data.Type != BuildType.Shader)
            {
                EditorGUILayout.BeginHorizontal();
                //suffix
                EditorGUILayout.LabelField("指定扩展名", GUILayout.Width(62));
                var suffix = EditorGUILayout.TextField(new GUIContent("", ""), string.IsNullOrEmpty(data.Suffix) ? BuildConfig.SuffixTips : data.Suffix);
                data.Suffix = suffix == BuildConfig.SuffixTips ? "" : suffix;
                //src dir
                EditorGUILayout.LabelField("基准路径", GUILayout.Width(50));
                data.InputRoot = EditorGUILayout.TextField(new GUIContent("", ""), data.InputRoot, GUILayout.Width(160));
                //out dir
                EditorGUILayout.LabelField("输出文件夹", GUILayout.Width(62));
                data.GenPath = EditorGUILayout.TextField(new GUIContent("", ""), data.GenPath, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            //show path
            string genPath = string.IsNullOrEmpty(data.GenPath) ? "" : '/' + data.GenPath;
            //EditorGUILayout.LabelField(string.Format("{0}/{1}/{2}{3}", Config.OutputPath, Config.Platform, Config.ModName, genPath));
            var dirs = data.InputPath.Split('|');
            List<string> dirList = new List<string>();
            for (int i = 0; i < dirs.Length; ++i)
            {
                var dir = dirs[i];
                if (!dir.StartsWith("~"))
                {
                    if (Directory.Exists(dir))
                        dirList.Add(dir);
                }
            }
            for (int i = 0; i < dirList.Count; ++i)
            {
                string relativePath = "";
                if (data.Type == BuildType.Custom)
                {
                    int index = dirList[i].LastIndexOf('/');
                    if (index == -1)
                        relativePath = dirList[i] + '/';
                    else
                        relativePath = dirList[i].Substring(index + 1) + '/';

                    if (!string.IsNullOrEmpty(data.InputRoot))
                    {
                        string inputRoot = data.InputRoot.EndsWith("/") ? data.InputRoot.Substring(0, data.InputRoot.Length - 1) : data.InputRoot;
                        if (dirList[i].StartsWith(inputRoot) && Directory.Exists(inputRoot))
                        {
                            if (dirList[i] == inputRoot)
                                relativePath = "";
                            else
                                relativePath = dirList[i].Substring(inputRoot.Length + 1) + '/';
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(data.InputRoot))
                    {
                        string inputRoot = data.InputRoot.EndsWith("/") ? data.InputRoot.Substring(0, data.InputRoot.Length - 1) : data.InputRoot;
                        if (dirList[i].StartsWith(inputRoot) && Directory.Exists(inputRoot))
                        {
                            if (dirList[i] == inputRoot)
                                relativePath = "";
                            else
                                relativePath = dirList[i].Substring(inputRoot.Length + 1) + '/';
                        }
                    }
                }
                EditorGUILayout.LabelField(string.Format("{0}/{1}/{2}{3}/{4}xxx.assetbundles", Config.OutputPath, Config.Platform, Config.ModName, genPath, relativePath));
            }

            //build and gen button
            EditorGUILayout.BeginHorizontal();
            if (data.Type == BuildType.Custom)
            {
                EditorGUILayout.LabelField("Manifest", GUILayout.Width(52));
                bool oldState = data.NeedDeps;
                data.NeedDeps = EditorGUILayout.Toggle("", data.NeedDeps, GUILayout.Width(16));
                if (data.NeedDeps && oldState != data.NeedDeps && string.IsNullOrEmpty(data.GenPath))
                {
                    EditorUtility.DisplayDialog("AssetBunlde", "请设置输出文件夹，否则Manifest会相互覆盖！", "Close");
                }
            }
            string btnPre = data.Type == BuildType.Custom ? "Gen " : "Build ";
            if (GUILayout.Button(btnPre + data.Name))
            {
                switch (data.Type)
                {
                    case BuildType.Scene:
                        ABBuilder.BuildScene(Config);
                        EditorUtility.DisplayDialog("AssetBunlde", "Build Scene Finish", "Close");
                        break;
                    case BuildType.Prefab:
                        ABBuilder.BuildPrefab(Config);
                        EditorUtility.DisplayDialog("AssetBunlde", "Build Prefab Finish", "Close");
                        break;
                    case BuildType.Shader:
                        ABBuilder.BuildShaderVariants(Config);
                        EditorUtility.DisplayDialog("AssetBunlde", "Build ShaderVariant Finish", "Close");
                        break;
                    case BuildType.Custom:
                        ABBuilder.GenCustom(Config, data);
                        EditorUtility.DisplayDialog("AssetBunlde", string.Format("Build {0} Finish", data.Name), "Close");
                        break;
                }
            }
            if (data.Type == BuildType.Custom)
            {
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    RemoveCustomConfig(data);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10f);
        }

        private void ShowPopupWindow(string tips, Action<string> callback)
        {
            var pop = new PopupInput(tips);
            pop.Callback = callback;
            PopupWindow.Show(new Rect((this.position.width - 200) * 0.5f, 0, 200, 200), pop);
        }

        private bool CheckPath(string path, bool isDir, out string ret)
        {
            ret = null;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            path = path.Replace("\\", "/");
            Debug.Log(path);
            if (isDir)
            {
                if (!Directory.Exists(path))
                {
                    EditorUtility.DisplayDialog("ABBuilder Config", "无效的路径！", "Close");
                    return false;
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    EditorUtility.DisplayDialog("ABBuilder Config", "无效的路径！", "Close");
                    return false;
                }
            }
            if (!path.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("ABBuilder Config", "必须选择在项目Assets下的路径！", "Close");
                return false;
            }
            ret = path.Substring(Application.dataPath.Length - "Assets".Length);
            return true;
        }

        private static bool CheckConfigName(string name, int allowCount)
        {
            int count = 0;
            foreach (var window in Windows)
            {
                if (name == window.Config.ModName)
                {
                    count++;
                }
            }
            if (count > allowCount)
            {
                EditorUtility.DisplayDialog("ABBuilder Config", "不能跟其他配置同名！", "Close");
                return false;
            }
            return true;
        }

        void OnDestroy()
        {
            Windows.Clear();
        }

        #region Gen Single

        [MenuItem("BuilderTool/Gen Single Scene/StandaloneWindows", false, 100)]
        static void BuildSceneWindows()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            ABBuilder.BuildSingleScene(scene.path, BuildTarget.StandaloneWindows);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Scene Finish", "Close");
        }
        [MenuItem("BuilderTool/Gen Single Scene/Android", false, 100)]
        static void BuildSceneAndroid()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            ABBuilder.BuildSingleScene(scene.path, BuildTarget.Android);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Scene Finish", "Close");
        }
        [MenuItem("BuilderTool/Gen Single Scene/iOS", false, 100)]
        static void BuildSceneIOS()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            ABBuilder.BuildSingleScene(scene.path, BuildTarget.iOS);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Scene Finish", "Close");
        }


        [MenuItem("BuilderTool/Gen Single Prefab/StandaloneWindows", false, 101)]
        static void BuildPrefabWindows()
        {
            UnityEngine.Object[] root = Selection.objects;
            ABBuilder.BuildSinglePrefab(root, BuildTarget.StandaloneWindows);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Prefab Finish", "Close");
        }
        [MenuItem("BuilderTool/Gen Single Prefab/Android", false, 101)]
        static void BuildPrefabAndroid()
        {
            UnityEngine.Object[] root = Selection.objects;
            ABBuilder.BuildSinglePrefab(root, BuildTarget.Android);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Prefab Finish", "Close");
        }
        [MenuItem("BuilderTool/Gen Single Prefab/iOS", false, 101)]
        static void BuildPrefabIOS()
        {
            UnityEngine.Object[] root = Selection.objects;
            ABBuilder.BuildSinglePrefab(root, BuildTarget.iOS);
            EditorUtility.DisplayDialog("AssetBunlde", "Gen Single Prefab Finish", "Close");
        }

        #endregion

    }

    public class PopupInput : PopupWindowContent
    {

        public string Content { get; private set; }
        public Action<string> Callback { get; set; }

        private string mTips;

        public PopupInput(string tips)
        {
            this.mTips = tips;
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(mTips, GUILayout.Width(50));
            Content = EditorGUILayout.TextField(new GUIContent("", ""), Content, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5f);
            if (GUILayout.Button("创建"))
            {
                Callback?.Invoke(Content);
                this.editorWindow.Close();
            }
        }

    }

}

