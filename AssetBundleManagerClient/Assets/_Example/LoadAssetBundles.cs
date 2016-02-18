using System;
using System.Collections;
using UnityEngine;
using AssetBundles;
using PlayFab;
using PlayFab.ClientModels;

public class LoadAssetBundles : MonoBehaviour
{
    public string BundleRoot;
    public string TitleId;
    void Awake()
    {
        PlayFabSettings.TitleId = TitleId;
    }

	// Use this for initialization
	void Start () {
	    
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        }, (result) =>
        {

            //Get Bundle Manifest url from playfab
            var platformKey = Utility.GetPlatformName() + "/"; 
            var bundleKey = string.Format("{0}/{1}", BundleRoot, platformKey);
            var manifestFileName = string.Format("{0}", Utility.GetPlatformName());
            var baseManifest = bundleKey + manifestFileName.ToLower();
            Debug.Log("Base Manifest: " + baseManifest);

            PlayFabClientAPI.GetContentDownloadUrl(new GetContentDownloadUrlRequest()
            {
                Key = baseManifest,
                HttpMethod = "GET",
                ThruCDN = true
            }, (contentDownloadResult) =>
            {
                /*
                Debug.Log(contentDownloadResult.URL);
                var urlParams = contentDownloadResult.URL.Split('?');
                var baseUrl = urlParams[0].Replace(platformKey + manifestFileName.ToLower(), "");
                Debug.Log(baseUrl);
                AssetBundleManager.SimulateAssetBundleInEditor = false;
                AssetBundleManager.SetSourceAssetBundleURL(baseUrl);
                var request = AssetBundleManager.Initialize(manifestFileName.ToLower(), urlParams[1]);
                if (request != null)
                {
                    StartCoroutine(request);
                }
                */

                StartCoroutine(LoadBundles(contentDownloadResult.URL, platformKey, manifestFileName));

            },PlayFabErrorHandler.HandlePlayFabError);
            
        }, PlayFabErrorHandler.HandlePlayFabError);

        

	}

    IEnumerator LoadBundles(string contentUrl, string platformKey, string manifestFileName)
    {

        Debug.Log(contentUrl);
        var urlParams = contentUrl.Split('?');
        var baseUrl = urlParams[0].Replace(platformKey + manifestFileName.ToLower(), "");
        Debug.Log(baseUrl);
        AssetBundleManager.SimulateAssetBundleInEditor = false;
        AssetBundleManager.SetSourceAssetBundleURL(baseUrl);
        var request = AssetBundleManager.Initialize(manifestFileName.ToLower(), urlParams[1]);
        if (request != null)
        {
           yield return StartCoroutine(request);
        }

        var bundleKey = string.Format("{0}/{1}", BundleRoot, platformKey);
        yield return StartCoroutine(InstantiateGameObjectAsync(bundleKey + "weapons", "weapons" , "dagger"));
    }

    protected IEnumerator InstantiateGameObjectAsync(string assetBundleName, string bundleName, string assetName)
    {
        // This is simply to get the elapsed time for this phase of AssetLoading.
        float startTime = Time.realtimeSinceStartup;


        var isDone = false;
        var urlParams = string.Empty;

        PlayFabClientAPI.GetContentDownloadUrl(new GetContentDownloadUrlRequest()
        {
            Key = assetBundleName,
            HttpMethod = "GET"
        }, (getContentResult) =>
        {
            urlParams = getContentResult.URL.Split('?')[1];
            isDone = true;
        } ,PlayFabErrorHandler.HandlePlayFabError);

        if (!isDone)
        {
            yield return new WaitForSeconds(.5f);
        }

        // Load asset from assetBundle.
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundleName, assetName, typeof(GameObject), urlParams);
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // Get the asset.
        GameObject prefab = request.GetAsset<GameObject>();

        if (prefab != null)
            GameObject.Instantiate(prefab);

        // Calculate and display the elapsed time.
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        Debug.Log(assetName + (prefab == null ? " was not" : " was") + " loaded successfully in " + elapsedTime + " seconds");
    }


}
