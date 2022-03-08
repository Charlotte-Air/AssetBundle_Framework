using UnityEngine;
using UnityEditor;
using System.IO;

namespace Builder
{
    public class ABBuilder
    {
        public static void BuildScene(BuildConfig config)
        {
            ResolverName r = new ResolverName(config.SceneData);
            r.Build(config);
            //EditorUtility.DisplayDialog("AssetBunlde", "Build Scene Finish", "Close");
        }

        public static void BuildPrefab(BuildConfig config)
        {
            ResolverName r = new ResolverName(config.PrefabData);
            r.Build(config);
            //EditorUtility.DisplayDialog("AssetBunlde", "Build Prefab Finish", "Close");
        }

        public static void BuildShaderVariants(BuildConfig config)
        {
            ResolverShader r = new ResolverShader(config.ShaderData);
            r.Build(config);
            //EditorUtility.DisplayDialog("AssetBunlde", "Build ShaderVariant Finish", "Close");
        }

        public static void GenCustom(BuildConfig config, BuildData data)
        {
            ResolverFolder r = new ResolverFolder(data);
            r.Gen(config);
            //EditorUtility.DisplayDialog("AssetBunlde", string.Format("Build {0} Finish", data.Name), "Close");
        }

        public static void CleanBuildName()
        {
            string[] names = AssetDatabase.GetAllAssetBundleNames();
            foreach (var item in names)
            {
                string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle(item);
                foreach (var asset in assets)
                {
                    AssetImporter obj = AssetImporter.GetAtPath(asset);
                    obj.assetBundleName = null;
                }
            }
            //EditorUtility.DisplayDialog("AssetBunlde", "Clean Finish", "Close");
        }

        public static void GenAllBuildName(BuildConfig config)
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
            string output_dir = string.Format("{0}/{1}/{2}", config.OutputPath, config.GetBuildTarget().ToString(), config.ModName).ToLower();
            if (!Directory.Exists(output_dir))
                Directory.CreateDirectory(output_dir);
            BuildPipeline.BuildAssetBundles(output_dir, BuildAssetBundleOptions.None, config.GetBuildTarget());

            //把shader挪到外层目录，作为所有mod的公共资源
            string[] shadervariants = { "shadervariants.assetbundles", "shadervariants.assetbundles.manifest" };
            foreach (var name in shadervariants)
            {
                if (File.Exists(output_dir + '/' + name))
                {
                    File.Copy(output_dir + '/' + name, output_dir + "/../" + name, true);
                    File.Delete(output_dir + '/' + name);
                }
            }
            //EditorUtility.DisplayDialog("AssetBunlde", "Gen AssetBundle Finish", "Close");
        }

        /// <summary>
        /// 清理并编译一键打包所有资源
        /// </summary>
        /// <param name="config"></param>
        public static void CleanAndGenAllBuildName(BuildConfig config)
        {
            CleanBuildName();
            BuildScene(config);
            BuildPrefab(config);
            BuildShaderVariants(config);
            GenAllBuildName(config);
            //EditorUtility.DisplayDialog("AssetBunlde", "CleanAndGenAllBuild AssetBundle Finish", "Close");
        }

        /// <summary>
        /// 清理并编译一键打包所有编译资源和文件夹资源
        /// </summary>
        /// <param name="config"></param>
        public static void CleanAndGenConfig(BuildConfig config)
        {
            CleanAndGenAllBuildName(config);
            for (int i = 0; i < config.CustomBuild.Count; ++i)
            {
                ResolverFolder r = new ResolverFolder(config.CustomBuild[i]);
                r.Gen(config);
            }
            //EditorUtility.DisplayDialog("AssetBunlde", "CleanAndGenConfig Finish", "Close");
        }

        /// <summary>
        /// 这种方式不会计算依赖
        /// </summary>
        public static void BuildSingleScene(string path, BuildTarget bt)
        {
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            string[] s = new string[1];
            s[0] = path;
            buildMap[0].assetNames = s;
            int start = path.LastIndexOf('/');
            int len = path.Length - start - 1 - (".unity").Length;
            string name = path.Substring(start + 1, len) + ".assetbundles";
            buildMap[0].assetBundleName = name;

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            BuildPipeline.BuildAssetBundles("AssetBundle", buildMap, BuildAssetBundleOptions.None, bt);
        }

        /// <summary>
        /// 这种方式不会计算依赖
        /// </summary>
        public static void BuildSinglePrefab(Object[] objs, BuildTarget bt)
        {
            BuildAssetBundleOptions options =
            //BuildAssetBundleOptions.ChunkBasedCompression |
            //BuildAssetBundleOptions.UncompressedAssetBundle |
            BuildAssetBundleOptions.CollectDependencies |
            BuildAssetBundleOptions.CompleteAssets |
            BuildAssetBundleOptions.DeterministicAssetBundle;

            foreach (var obj in objs)
            {
                BuildPipeline.PushAssetDependencies();
                BuildPipeline.BuildAssetBundle(null, new Object[] { obj }, "AssetBundle/" + obj.name.ToLower() + ".assetbundles".ToLower(), options, bt);
                BuildPipeline.PopAssetDependencies();
            }
        }

    }

}
