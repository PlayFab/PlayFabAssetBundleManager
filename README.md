# PlayFabAssetBundleManager 
Modification To AssetBundleManager for use with PlayFab

Unity released an AssetBundleManager in Version 5.x which allowed you to download and create bundles using a tool. This is a slightly modified version of that bundle tool.   The origional bundle tool can be downloaded from the [Asset Store](https://www.assetstore.unity3d.com/en/#!/content/45836).

### The problem
This utility solves a very unique problem that the current Bundle Asset Manager cannot do.  The current Asset Bundle Manager has no way to support UrlParams on the download Url.  This is required when using services like CloudFront, which PlayFab uses, and your files have any sort of Security on them.  

The second problem that this tool version of Bundle Manager solves, is that it will internally call the PlayFab GetContentURL for dependancy files.  So that if your manifests require additional files to be downloaded it will download those files (aka bundles) that are specified in the manifest dependancies.

### Prerequisites
You will need the most recent PlayFabSDK installed in order for this bundle manager to work properly.  If you do not have it, you can download it from our [https://api.playfab.com/sdks/unity/](https://api.playfab.com/sdks/unity/)

###  Getting Started
Once you have the playfab sdk installed in your unity project,  you will need to download the unity package in this repository. [Click Here to download](https://github.com/PlayFab/PlayFabAssetBundleManager/raw/master/AssetBundleManagerClient/PlayFabAssetBundleManager.unitypackage)

This contains the following 
- an example folder _Example
  - LoadAssetBundles.cs -  This is an example of how to use the Asset Bundle
  - TestScene - This is the example scene
- AssetBundleManager - This is the modified version of AssetBundleManager
- PlayFabErrorHandler - This is a generic error handler that I created to output errors from playfab calls to the console.

### The code
You will see in the *LoadAssetBundles.cs* file that the first thing we do is Login,  you can pretty much ignore this logic, because most likely you have already logged in your player.  However, if you have not you will need to use one of our [Login calls](https://api.playfab.com/Documentation/Client#Authentication) in order to use the GetContentDownloadUrl feature.

Here is an example of loging in with a custom Id.

```cs
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        }, (result) =>
        {
           //Do Something here..
        },PlayFabErrorHandler.HandlePlayFabError);

```

Once you have logged in
We need to make an inital call to get the download of the manifest bundle.  You can see below,  I am using the Utility to get the bundle for the platform it was built on.  When you created your bundle, it will have done some similar logic to export the bundle.

```cs
            var platformKey = Utility.GetPlatformName() + "/"; 
            var bundleKey = string.Format("{0}/{1}", BundleRoot, platformKey);
            var manifestFileName = string.Format("{0}", Utility.GetPlatformName());
            var baseManifest = bundleKey + manifestFileName.ToLower();
```

What this is doing, is getting the platform key (eg. "Windows/"), and it is also building a bundle key from it.  In this case, my bundle root is in "bundles".  there for my bundle key is going to be  "bundles/Windows/"

Next,  we need to specify the Bundle Manifest file name,  which is also Windows, however, when compiling the baseManifest, you'll notice that we need to make just the file name lower case.  This is because the Bundle manager when exporting ALWAYS makes your bundle names lower case.

Last we call GetContentDownloadUrl
```cs
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
```

### Loading the Bundles
Now that we have a download URL from PlayFab,  we can start to load the bundles.  In the example above, you will see we are kicking off a coroutine called LoadBundles and passing it our URL, platformKey & Manifest name.

This kicks off loading of the Manifest Bundle.  

```cs
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
```
When you use  AssetBundleManager.Initialize()  it will create an Operation request to actually download the bundle file into the Bundle Manager.   Don't be fooled,  this only downloads the bundle manifest bundle. (i know kind of confusing)

You then still need to call something to download your actual bundle.  You'll see that I have a weapons bundle and I want to load and instantiate the dagger.  So in order for me to do that we are going to call another method that I've created to handle that part for us.

```cs

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

```

Take note, that here we need to make another call to GetContentDownloadUrl to get the urlparams (the AWS Security key) for the bundle file.  and we need to wait for the playfab call to complete before we kick off the next operation.  Now the way I did it here is a bit jankey, and there is probably a better and more reliable way to do this,  but just for this example I put in a little wait before I kicked off the next request operation.

The next request operation actually downloads the asset bundle from the url provided,  and now you are free to get the asset out of the bundle.  Realistically you would probably want to store the bundle request somewhere so you can reference to it at any time.

Hope this brief walk through helps understand the code and what it is doing.




