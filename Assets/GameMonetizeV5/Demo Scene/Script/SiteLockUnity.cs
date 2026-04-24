using System.Runtime.InteropServices;
using UnityEngine;

public class SiteLockUnity : MonoBehaviour
{

    [DllImport("__Internal")]
    private static extern void SitelockCheck();
    
    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Call the JS function
        SitelockCheck();
#endif
    }
}
