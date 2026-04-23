
using UnityEngine;

public static class AdUtil
{
    private static readonly string kMaxSdkKey =
        "uoUeqafHkGFIAZiF5MuQ6TtTnKkEDFSF4cT9m2fwJAMddQmxlzaXDThNlT2i40sxDBoZ8Cuz3nu5UWSlG39d9u";

    private static bool _isInitialized = false;

    public static void Init()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        InitializeMax();
    }

    #region MAX SDK

    private static void InitializeMax()
    {
        Debug.Log("Initializing Max");
        MaxSdk.SetVerboseLogging(true);
        MaxSdk.SetSdkKey(kMaxSdkKey);
        MaxSdk.InitializeSdk();
    }

    #endregion
}