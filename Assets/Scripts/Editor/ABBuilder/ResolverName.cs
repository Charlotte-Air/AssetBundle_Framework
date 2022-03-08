using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;

namespace Builder
{

    public class ResolverName
    {

        protected BuildData mBuildData;

        private Dictionary<string, string> mNoSharedFiles;

        //key:rawdata file  value:object file
        private Dictionary<string, SortedList<string, string>> mSharedDeps = new Dictionary<string, SortedList<string, string>>();

        //key:depsAB name  value:filename in depsAB
        private Dictionary<string, List<string>> mDepsAB = new Dictionary<string, List<string>>();

        //key:objAB name   value:mDepsAB name
        private Dictionary<string, List<string>> mObjectAB = new Dictionary<string, List<string>>();

        public ResolverName(BuildData data)
        {
            mBuildData = data;
        }

        protected void ScanFiles(string path, Dictionary<string, string> objs, HashSet<string> blackDir, HashSet<string> blackFiles)
        {
            if (blackDir.Contains(path))
                return;

            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i].Replace("\\", "/");
                ScanFiles(dir, objs, blackDir, blackFiles);
            }
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].Replace("\\", "/"); ;
                if (blackFiles.Contains(filename))
                    continue;
                AddBuildFile(filename, objs);
            }
        }

        private void AddBuildFile(string filename, Dictionary<string, string> objs)
        {
            if (string.IsNullOrEmpty(mBuildData.Suffix))
            {
                if (!filename.EndsWith(".meta"))
                {
                    filename = filename.Replace("\\", "/");
                    objs[filename] = filename;
                }
            }
            else
            {
                var match = Regex.Match(filename, string.Format("({0})$", mBuildData.Suffix));
                if (!string.IsNullOrEmpty(match.Value))
                {
                    filename = filename.Replace("\\", "/");
                    objs[filename] = filename;
                }
            }
        }

        protected Dictionary<string, string> ParseBuildFiles()
        {
            Dictionary<string, string> allObjs = new Dictionary<string, string>();
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
                        Dictionary<string, string> objs = new Dictionary<string, string>();
                        ScanFiles(dir, objs, blackDir, blackFiles);

                        //生成每个文件在源文件夹下的相对路径
                        var keys = objs.Keys.ToArray();
                        for (int j = 0; j < keys.Length; ++j)
                        {
                            string path = keys[j];
                            string root = dir;
                            if (!string.IsNullOrEmpty(inputRoot))
                            {
                                inputRoot = inputRoot.EndsWith("/") ? inputRoot.Substring(0, inputRoot.Length - 1) : inputRoot;
                                if (path.StartsWith(inputRoot) && Directory.Exists(inputRoot))
                                {
                                    root = inputRoot;
                                }
                            }
                            string relativePath = path.Substring(root.Length + 1);
                            objs[path] = relativePath;
                            allObjs[path] = relativePath;
                        }
                    }
                }
                //添加指定的单个文件
                if (buildFiles.Count > 0)
                {
                    for (int i = 0; i < buildFiles.Count; ++i)
                    {
                        string path = buildFiles[i];
                        AddBuildFile(path, allObjs);

                        //散打的资源都只保留文件名
                        string filename = path.Substring(path.LastIndexOf('/') + 1);
                        allObjs[path] = filename;
                    }
                }
            }
            return allObjs;
        }

        /// <summary>
        /// 检查这个文件所依赖的rawdata
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="relativePath">相对路径，最终打成AB包使用的路径</param>
        /// <returns></returns>
        protected HashSet<string> CheckDependencies(string path, string relativePath)
        {
            HashSet<string> deps = new HashSet<string>();

            if (mBuildData.Type == BuildType.Scene)
            {
                var sa = AssetDatabase.LoadMainAssetAtPath(path) as SceneAsset;
                GameObject[] sceneObjs = EditorSceneManager.OpenScene(path).GetRootGameObjects();
                if (sceneObjs == null)
                    throw new System.Exception("[Scene]" + path + " :load scene failed!");

                for (int i = 0; i < sceneObjs.Length; ++i)
                {
                    GameObject obj = sceneObjs[i];
                    CheckRawData(obj, ref deps);
                }
            }
            else if ((mBuildData.Type == BuildType.Prefab))
            {
                GameObject prefab = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                CheckRawData(prefab, ref deps);
            }

            deps.Add("");   //至少会有一个，保证自身被打进去

            foreach (var rawdata in deps)
            {
                if (!mSharedDeps.ContainsKey(rawdata))
                {
                    mSharedDeps[rawdata] = new SortedList<string, string>();
                }
                if (!mSharedDeps[rawdata].ContainsKey(path))
                {
                    mSharedDeps[rawdata].Add(path, relativePath);
                }
            }

            return deps;
        }

        private void CheckRawData(GameObject obj, ref HashSet<string> deps)
        {
            CheckTexture(ref obj, ref deps);
            CheckMesh(ref obj, ref deps);
            CheckAudioClip(ref obj, ref deps);
            CheckAnimation(ref obj, ref deps);
        }

        private void CheckTexture(ref GameObject o, ref HashSet<string> deps)
        {
            Object[] objs = EditorUtility.CollectDependencies(new UnityEngine.Object[] { o });
            foreach (Object obj in objs)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (obj is Cubemap || obj is Material || obj is Texture)
                {
                    string name = path;
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }
        }

        private void CheckMesh(ref GameObject o, ref HashSet<string> deps)
        {
            MeshFilter[] rs = o.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Mesh mesh = rs[i].sharedMesh;
                if (mesh != null)
                {
                    string name = AssetDatabase.GetAssetPath(mesh);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }

            SkinnedMeshRenderer[] smrs = o.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                Mesh mesh = smrs[i].sharedMesh;
                if (mesh != null)
                {
                    string name = AssetDatabase.GetAssetPath(mesh);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }

            MeshCollider[] mcs = o.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < mcs.Length; i++)
            {
                Mesh mesh = mcs[i].sharedMesh;
                if (mesh != null)
                {
                    string name = AssetDatabase.GetAssetPath(mesh);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }

            ParticleSystemRenderer[] pss = o.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < pss.Length; i++)
            {
                Mesh mesh = pss[i].mesh;
                if (mesh != null)
                {
                    string name = AssetDatabase.GetAssetPath(mesh);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }
        }

        private void CheckAnimation(ref GameObject o, ref HashSet<string> deps)
        {
            Animation[] rs = o.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null)
                {
                    foreach (AnimationState item in rs[i])
                    {
                        string name = AssetDatabase.GetAssetPath(item.clip);
                        if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                        {
                            if (!deps.Contains(name))
                                deps.Add(name);
                        }
                    }
                }
            }

            Animator[] rs2 = o.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < rs2.Length; i++)
            {
                if (rs2[i].runtimeAnimatorController == null) continue;
                string contorller_name = AssetDatabase.GetAssetPath(rs2[i].runtimeAnimatorController);
                if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(contorller_name))
                {
                    if (!deps.Contains(contorller_name))
                        deps.Add(contorller_name);
                }

                foreach (AnimationClip item in rs2[i].runtimeAnimatorController.animationClips)
                {
                    string name = AssetDatabase.GetAssetPath(item);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }
        }

        private void CheckAudioClip(ref GameObject o, ref HashSet<string> deps)
        {
            AudioSource[] rs = o.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                AudioClip ac = rs[i].clip;
                if (ac != null)
                {
                    string name = AssetDatabase.GetAssetPath(ac);
                    if (mNoSharedFiles == null || !mNoSharedFiles.ContainsKey(name))
                    {
                        if (!deps.Contains(name))
                            deps.Add(name);
                    }
                }
            }
        }

        private Dictionary<string, string> LoadNoShared(string path)
        {
            if (Directory.Exists(path))
            {
                mNoSharedFiles = new Dictionary<string, string>();
                LoadFile(path, ref mNoSharedFiles);
            }
            return mNoSharedFiles;
        }

        private void LoadFile(string path, ref Dictionary<string, string> dict)
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].ToLower();
                if (!filename.EndsWith(".meta"))
                {
                    string name = files[i].Replace("\\", "/");
                    //int index = name.IndexOf(mBuildData.InputPath);
                    //name = name.Substring(index);
                    dict[name] = files[i];
                }
            }
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++)
            {
                LoadFile(dirs[i], ref dict);
            }
        }

        private void RemoveStandaloneDeps()
        {
            foreach (var item in new List<string>(mSharedDeps.Keys))
            {
                if (item != "" && mSharedDeps[item].Count == 1)
                {
                    mSharedDeps.Remove(item);
                }
            }
        }

        private void CalculateDepsSet()
        {
            mDepsAB.Clear();
            mObjectAB.Clear();
            foreach (var p in mSharedDeps)
            {
                string depsKey = "";
                List<string> names = new List<string>(p.Value.Keys);
                names.Sort();
                foreach (var name in names)
                {
                    depsKey += name;
                }
                depsKey = CalculateMD5Hash(depsKey);
                if (!mDepsAB.ContainsKey(depsKey))
                {
                    mDepsAB[depsKey] = new List<string>();
                    foreach (var p1 in p.Value)
                    {
                        string path = p1.Key;
                        string relativePath = p1.Value;
                        if (!mObjectAB.ContainsKey(path))
                        {
                            mObjectAB[path] = new List<string>();
                        }
                        mObjectAB[path].Add(depsKey);
                        ResolveTarget(path, relativePath);
                    }
                }
                mDepsAB[depsKey].Add(p.Key);
                ResolveDeps(depsKey, p.Key);
            }
        }

        private string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString().Substring(0, 8);
        }

        private void ResolveDeps(string depsABName, string rawdata)
        {
            if (!string.IsNullOrEmpty(rawdata))
            {
                string prefix_dep = "_deps/";
                AssetImporter ai = AssetImporter.GetAtPath(rawdata);
                if (ai != null)
                {
                    ai.assetBundleName = string.Format("{0}/{1}.assetbundles", prefix_dep, depsABName);
                }
            }
        }

        private void ResolveTarget(string path, string relativePath)
        {
            string name = relativePath.Replace("\\", "/");
            name = name.Substring(0, name.LastIndexOf('.'));
            string genPath = string.IsNullOrEmpty(mBuildData.GenPath) ? "" : mBuildData.GenPath + '/';
            AssetImporter ai = AssetImporter.GetAtPath(path);
            ai.assetBundleName = string.Format("{0}{1}.assetbundles", genPath, name);
        }

        public virtual void Build(BuildConfig config)
        {
            var objs = ParseBuildFiles();
            if (objs.Count > 0)
            {
                //检查每个文件的依赖关系
                LoadNoShared(config.NoDepsPath);
                foreach (var p in objs)
                {
                    CheckDependencies(p.Key, p.Value);
                }

                //计算依赖
                RemoveStandaloneDeps();
                CalculateDepsSet();
            }
        }

    }

}
