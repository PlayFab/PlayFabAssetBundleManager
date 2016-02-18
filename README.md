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



