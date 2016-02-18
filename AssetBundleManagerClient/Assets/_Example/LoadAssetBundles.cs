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
                //Here we are kicking off a coroutine to download the inital manifest bundle.
                StartCoroutine(LoadBundles(contentDownloadResult.URL, platformKey, manifestFileName));

            },PlayFabErrorHandler.HandlePlayFabError);
            
        }, PlayFabErrorHandler.HandlePlayFabError);

        

	}

    IEnumerator LoadBundles(string contentUrl, string platformKey, string manifestFileName)
    {

        Debug.Log(contentUrl);

        //Get the url params ( in this case it is the AWS Security on the file ).
        var urlParams = contentUrl.Split('?');
        
        //Get the base URL for the bundle.
        var baseUrl = urlParams[0].Replace(platformKey + manifestFileName.ToLower(), "");
        Debug.Log(baseUrl);

        //Make sure you turn this off, or it will go into simulation mode and not use playfab CDN
        AssetBundleManager.SimulateAssetBundleInEditor = false;
        //Set the source download URL for the bundle.
        AssetBundleManager.SetSourceAssetBundleURL(baseUrl);

        //Initialize the AssetBundleManager with the Manifest bundle
        var request = AssetBundleManager.Initialize(manifestFileName.ToLower(), urlParams[1]);
        if (request != null)
        {
           yield return StartCoroutine(request);
        }

        var bundleKey = string.Format("{0}/{1}", BundleRoot, platformKey);
        yield return StartCoroutine(InstantiateGameObjectAsync(bundleKey + "weapons", "weapons" , "dagger"));

        //If you had more bundles to load, then you can do it here with more yield return coroutines.

    }

    protected IEnumerator InstantiateGameObjectAsync(string assetBundleName, string bundleName, string assetName)
    {
        // This is simply to get the elapsed time for this phase of AssetLoading.
        float startTime = Time.realtimeSinceStartup;

        var isDone = false;
        var urlParams = string.Empty;
        
        //We have to make a call to playfab to get the URL for each asset bundle that we want to download.
        PlayFabClientAPI.GetContentDownloadUrl(new GetContentDownloadUrlRequest()
        {
            Key = assetBundleName,
            HttpMethod = "GET"
        }, (getContentResult) =>
        {
            //Get the URL Params (AWS Security)
            urlParams = getContentResult.URL.Split('?')[1];
            //Flag that we are done calling playfab
            isDone = true;
        } ,PlayFabErrorHandler.HandlePlayFabError);


        //This is a little hoakey, because PlayFab API's use a callback system, which doesn't quite Jive with 
        //Coroutines,  so we are waiting .5f seconds, before we proceed which gives PlayFab Ample time to respond.
        //You can probably reduce this delay to .2f and it would still work.
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
