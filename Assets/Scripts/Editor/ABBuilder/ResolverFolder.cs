using System.IO;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Builder
{

    public class ResolverFolder
    {

        private BuildData mBuildData;

        //key:dir value:files in dir
        //private Dictionary<string, List<string>> mObjs = new Dictionary<string, List<string>>();

        public ResolverFolder(BuildData data)
        {
            mBuildData = data;
        }

        public void ScanFiles(string path, Dictionary<string, List<string>> objs, HashSet<string> blackDir, HashSet<string> blackFiles)
        {
            if (blackDir.Contains(path))
                return;

            objs.Add(path, new List<string>());

            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i].Replace("\\", "/");
                ScanFiles(dir, objs, blackDir, blackFiles);
            }
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].Replace("\\", "/");
                if (blackFiles.Contains(filename))
                    continue;
                AddBuildFile(path, filename, objs);
            }
        }

        private void AddBuildFile(string path, string filename, Dictionary<string, List<string>> objs)
        {
            if (string.IsNullOrEmpty(mBuildData.Suffix))
            {
                if (!filename.EndsWith(".meta"))
                {
                    filename = filename.Replace("\\", "/");
                    if (objs.TryGetValue(path, out var list))
                    {
                        list.Add(filename);
                    }
                }
            }
            else
            {
                var match = Regex.Match(filename, string.Format("({0})$", mBuildData.Suffix));
                if (!string.IsNullOrEmpty(match.Value))
                {
                    filename = filename.Replace("\\", "/");
                    if (objs.TryGetValue(path, out var list))
                    {
                        list.Add(filename);
                    }
                }
            }
        }

        private Dictionary<string, List<string>> ParseBuildFiles(out Dictionary<string, string> relativeDir)
        {
            Dictionary<string, List<string>> allObjs = new Dictionary<string, List<string>>();
            relativeDir = new Dictionary<string, string>();
            var dirs = mBuildData.InputPath.Split('|');
            var files = mBuildData.InputFiles.Split('|');
            if (dirs.Length > 0 || files.Length > 0)
            {
                //筛选要编译的文件夹和文件，生成黑名单和白名单
                List<string> buildDir = new List<string>();
                List<string> buildFiles = new List<string>();
                HashSet<string> blackDir = new HashSet<string>();
                HashSet<string> blackFiles = new HashSet<string>();
                for (int i = 0; i < dirs.Length; ++i)
                {
                    var dir = dirs[i];
                    if (dir.StartsWith("~"))
                    {
                        string path = dir.Substring(1);
                        if (Directory.Exists(path))
                            blackDir.Add(path);
                    }
                    else if (Directory.Exists(dir))
                        buildDir.Add(dir);
                }
                for (int i = 0; i < files.Length; ++i)
                {
                    var file = files[i];
                    if (file.StartsWith("~"))
                    {
                        string path = file.Substring(1);
                        if (File.Exists(path))
                            blackFiles.Add(path);
                    }
                    else if (File.Exists(file))
                        buildFiles.Add(file);
                }

                //扫描指定文件夹下的文件
                if (buildDir.Count > 0)
                {
                    var inputRoot = mBuildData.InputRoot;
                    for (int i = 0; i < buildDir.Count; ++i)
                    {
                        string dir = buildDir[i];
                        Dictionary<string, List<string>> objs = new Dictionary<string, List<string>>();
                        ScanFiles(dir, objs, blackDir, blackFiles);

                        //生成每个文件在源文件夹下的相对路径
                        var keys = objs.Keys.ToArray();
                        for (int j = 0; j < keys.Length; ++j)
                        {
                            string path = keys[j];
                            string relativePath = path;
                            if (!string.IsNullOrEmpty(inputRoot))
                            {
                                inputRoot = inputRoot.EndsWith("/") ? inputRoot.Substring(0, inputRoot.Length - 1) : inputRoot;
                                if (path.StartsWith(inputRoot) && Directory.Exists(inputRoot))
                                {
                                    if (path.Length > inputRoot.Length)
                                        relativePath = path.Substring(inputRoot.Length + 1);
                                }
                            }

                            if(relativePath == path)
                            {
                                int index = dir.LastIndexOf('/');
                                if (index == -1)
                                    relativePath = path;
                                else
                                    relativePath = path.Substring(index + 1);
                            }
                            relativeDir[path] = relativePath;
                            allObjs[path] = objs[path];
                        }
                    }
                }
                ////添加指定的单个文件
                //if (buildFiles.Count > 0)
                //{
                //    for (int i = 0; i < buildFiles.Count; ++i)
                //    {
                //        AddBuildFile(buildFiles[i], allObjs);
                //    }
                //}
            }
            return allObjs;
        }

        public void Gen(BuildConfig config)
        {
            var objs = ParseBuildFiles(out var relativeDir);

            AssetBundleBuild[] builds = new AssetBundleBuild[objs.Count];
            int index = 0;
            string bundleName = null;
            foreach (var p in objs)
            {
                string dir = p.Key;
                List<string> list = p.Value;
                if (list.Count == 0)
                    continue;
                string relativePath = relativeDir[dir];
                bundleName = relativePath.ToLower() + ".assetbundles";
                string[] assets = new string[list.Count];
                var build = new AssetBundleBuild()
                {
                    assetBundleName = bundleName,
                    assetNames = assets
                };
                builds[index++] = build;
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i] = list[i].Replace('\\', '/');
                    assets[i] = list[i];
                }
            }

            //打在指定目录下
            string output_dir = string.Format("{0}/{1}/{2}", config.OutputPath, config.GetBuildTarget().ToString(), config.ModName).ToLower();
            if (!string.IsNullOrEmpty(mBuildData.GenPath))
            {
                output_dir += '/' + mBuildData.GenPath;
                int preDirIdx = output_dir.IndexOf("/..");
                while (preDirIdx != -1)
                {
                    int startIndex = output_dir.LastIndexOf('/', preDirIdx - 1);
                    string preDir = output_dir.Substring(startIndex, preDirIdx - startIndex + 3);
                    output_dir = output_dir.Replace(preDir, "");
                    preDirIdx = output_dir.IndexOf("/..");
                }
            }
            if (!Directory.Exists(output_dir))
                Directory.CreateDirectory(output_dir);

            //如果不需要manifest，又和别的manifest冲突，尝试先把别人的重命名
            if (!mBuildData.NeedDeps)
            {
                string manifestName = output_dir.Substring(output_dir.LastIndexOf('/') + 1);
                string[] fileName = { manifestName, manifestName + ".manifest" };
                foreach (var name in fileName)
                {
                    if (File.Exists(output_dir + '/' + name))
                    {
                        File.Move(output_dir + '/' + name, output_dir + "/tmp_" + name);
                    }
                }
            }

            //开始打包
            BuildPipeline.BuildAssetBundles(output_dir, builds, BuildAssetBundleOptions.None, config.GetBuildTarget());

            if (!mBuildData.NeedDeps)
            {
                string manifestName = output_dir.Substring(output_dir.LastIndexOf('/') + 1);
                string[] fileName = { manifestName, manifestName + ".manifest" };
                foreach (var name in fileName)
                {
                    //删除自己的manifest
                    if (File.Exists(output_dir + '/' + name))
                    {
                        File.Delete(output_dir + '/' + name);
                    }
                    //还原别人的manifest
                    if (File.Exists(output_dir + "/tmp_" + name))
                    {
                        File.Move(output_dir + "/tmp_" + name, output_dir + '/' + name);
                    }
                }
            }
        }

    }

}
