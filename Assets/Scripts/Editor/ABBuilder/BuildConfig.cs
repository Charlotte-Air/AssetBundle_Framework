using System;
using System.IO;
using System.Text;
using UnityEditor;
using System.Collections.Generic;

namespace Builder
{

    public enum BuildType
    {
        Scene,
        Prefab,
        Shader,
        Custom
    }

    public enum BuildPlatform
    {
        StandaloneWindows,
        Android,
        iOS
    }

    public class BuildData
    {
        public BuildType Type { get; set; }
        public string Name { get; set; }
        public string InputPath { get; set; }
        public string InputFiles { get; set; }
        public string Suffix { get; set; }
        public string InputRoot { get; set; }
        public string GenPath { get; set; }
        public bool NeedDeps { get; set; }

        public BuildData()
        {

        }

        public BuildData(BuildType type, string name)
        {
            this.Type = type;
            this.Name = name;
            this.InputPath = "";
            this.InputFiles = "";
            this.Suffix = "";
            this.InputRoot = "";
            this.GenPath = "";
            this.NeedDeps = false;
        }
    }

    public class BuildConfig
    {

        public string ModName { get; set; }
        public string OutputPath { get; set; }
        public string NoDepsPath { get; set; }
        public BuildPlatform Platform { get; set; }

        public BuildData SceneData { get; private set; }
        public BuildData PrefabData { get; private set; }
        public BuildData ShaderData { get; private set; }
        public List<BuildData> CustomBuild { get; private set; }

        public const string ConfigPath = "AssetBundle/_config/";
        public const string InputPathTips = "输入要打包的资源目录，支持多个目录，用|分隔";
        public const string InputFilesTips = "输入要打包的资源文件，支持多个文件，用|分隔";
        public const string SuffixTips = "输入要打包的资源扩展名，支持多个，用|分隔。不填表示打所有文件";

        public BuildConfig()
        {
            CustomBuild = new List<BuildData>();
        }

        public static BuildConfig GetBuildConfig(string name)
        {
            var ret = new BuildConfig();
            ret.Load(name);
            return ret;
        }

        public void AddCustomBuild(string name)
        {
            var data = new BuildData(BuildType.Custom, name);
            CustomBuild.Add(data);
        }

        public void RemoveCustomBuild(BuildData data)
        {
            CustomBuild.Remove(data);
        }

        public BuildTarget GetBuildTarget()
        {
            switch (Platform)
            {
                case BuildPlatform.StandaloneWindows:
                    return BuildTarget.StandaloneWindows;
                case BuildPlatform.Android:
                    return BuildTarget.Android;
                case BuildPlatform.iOS:
                    return BuildTarget.iOS;
            }
            return BuildTarget.NoTarget;
        }

        #region Save Load

        public void Save()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(string.Format("ModName={0}\r\n", ModName));
            sb.AppendFormat(string.Format("OutputPath={0}\r\n", OutputPath));
            sb.AppendFormat(string.Format("NoDepsPath={0}\r\n", NoDepsPath));
            sb.AppendFormat(string.Format("Platform={0}\r\n\r\n", Platform));
            SaveBuildData(SceneData, sb);
            SaveBuildData(PrefabData, sb);
            SaveBuildData(ShaderData, sb);
            sb.AppendFormat(string.Format("Custom={0}\r\n", CustomBuild.Count));
            for (int i = 0; i < CustomBuild.Count; ++i)
            {
                SaveBuildData(CustomBuild[i], sb);
            }
            string path = string.Format("{0}/{1}", Environment.CurrentDirectory, ConfigPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path + ModName + ".txt", sb.ToString());
        }

        private void SaveBuildData(BuildData data, StringBuilder sb)
        {
            sb.AppendFormat(string.Format("{0}.Type={1}\r\n", data.Name, data.Type));
            sb.AppendFormat(string.Format("{0}.Name={1}\r\n", data.Name, data.Name));
            sb.AppendFormat(string.Format("{0}.InputPath={1}\r\n", data.Name, data.InputPath == InputPathTips ? null : data.InputPath));
            sb.AppendFormat(string.Format("{0}.InputFiles={1}\r\n", data.Name, data.InputFiles == InputFilesTips ? null : data.InputFiles));
            sb.AppendFormat(string.Format("{0}.Suffix={1}\r\n", data.Name, data.Suffix == SuffixTips ? null : data.Suffix));
            sb.AppendFormat(string.Format("{0}.InputRoot={1}\r\n", data.Name, data.InputRoot));
            sb.AppendFormat(string.Format("{0}.GenPath={1}\r\n", data.Name, data.GenPath));
            sb.AppendFormat(string.Format("{0}.NeedDeps={1}\r\n\r\n", data.Name, data.NeedDeps));
        }

        public void Load(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "mod_default";
            }
            else
            {
                string path = string.Format("{0}/{1}{2}.txt", Environment.CurrentDirectory, ConfigPath, name);
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    int index = 0;
                    LoadGlobalData(lines, ref index);
                    SceneData = LoadBuildData(lines, ref index);
                    PrefabData = LoadBuildData(lines, ref index);
                    ShaderData = LoadBuildData(lines, ref index);
                    int customCount = Convert.ToInt32(LoadLine(lines[index++]));
                    if (customCount > 0)
                    {
                        for (int j = 0; j < customCount; ++j)
                        {
                            var cutmData = LoadBuildData(lines, ref index);
                            CustomBuild.Add(cutmData);
                        }
                    }
                    return;
                }
            }

            ModName = name;
            SceneData = new BuildData(BuildType.Scene, "Scene") { Suffix = ".unity" };
            PrefabData = new BuildData(BuildType.Prefab, "Prefab") { Suffix = ".prefab" };
            ShaderData = new BuildData(BuildType.Shader, "ShaderVariants") { Suffix = ".shader|.shadervariants" };
        }

        private void LoadGlobalData(string[] lines, ref int index)
        {
            var dataType = this.GetType();
            while (true)
            {
                if (index == lines.Length)
                    break;
                if (!LoadLine(lines[index++], out var key, out var value))
                    break;
                var prop = dataType.GetProperty(key);
                if (prop != null)
                {
                    object v = value;
                    if (prop.PropertyType == typeof(BuildPlatform))
                    {
                        v = Enum.Parse(prop.PropertyType, value);
                    }
                    prop.SetValue(this, v);
                }
            }
        }

        private BuildData LoadBuildData(string[] lines, ref int index)
        {
            BuildData data = new BuildData();
            var dataType = data.GetType();
            while (true)
            {
                if (index == lines.Length)
                    break;
                if (!LoadLine(lines[index++], out var key, out var value))
                    break;
                var prop = dataType.GetProperty(key);
                if (prop != null)
                {
                    object v = value;
                    if (prop.PropertyType == typeof(bool))
                    {
                        v = Convert.ToBoolean(value);
                    }
                    else if (prop.PropertyType == typeof(BuildType))
                    {
                        v = Enum.Parse(prop.PropertyType, value);
                    }
                    prop.SetValue(data, v);
                }
            }
            return data;
        }

        private string LoadLine(string src)
        {
            int index = src.IndexOf('=');
            if (index != -1)
            {
                string value = src.Substring(index + 1);
                return value == null ? "" : value;
            }
            return null;
        }

        private bool LoadLine(string src, out string key, out string value)
        {
            key = value = null;
            int index = src.IndexOf('=');
            if (index != -1)
            {
                int keyStart = src.IndexOf('.') + 1;
                key = src.Substring(keyStart, index - keyStart);
                value = src.Substring(index + 1);
                value = value == null ? "" : value;
                return true;
            }
            return false;
        }

        #endregion
    }

}
