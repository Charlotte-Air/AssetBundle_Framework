using System;

namespace AssetBundleTool
{

    public interface IAssetReferenceHandler
    {
        bool AddAssetRef(string name);
        bool RemoveAssetRef(string name);
    }

    public interface IAssetLocalFileHandler
    {
        bool Exists(string filename);
    }

    public interface IAssetDownloadHandler
    {
        UnityEngine.AssetBundle RequestDownload(string modName, string bundleName, string assetName);
        void RequestDownloadAsync(string modName, string bundleName, string assetName, Action<UnityEngine.AssetBundle> callback);
        void CancelDownload(string modName, string bundleName, string assetName);
    }

}
