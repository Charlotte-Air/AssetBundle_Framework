using UnityEditor;

namespace Builder
{

    public class ResolverShader : ResolverName
    {

        public ResolverShader(BuildData data) : base(data)
        {
            
        }

        public override void Build(BuildConfig config)
        {
            var objs = ParseBuildFiles();
            foreach (var path in objs.Keys)
            {
                AssetImporter ai = AssetImporter.GetAtPath(path);
                ai.assetBundleName = "shadervariants.assetbundles";
            }
        }

    }

}
