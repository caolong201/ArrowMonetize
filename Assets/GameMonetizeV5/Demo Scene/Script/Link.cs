using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class Link : MonoBehaviour
{
#if  UNITY_WEBGL || UNITY_EDITOR


    public void ______OpenLinkToYourWebsite()
    {
        // here you can add your website or else if not have website disable button more games :)
        openWindow("https://www.playbestgames.online/");
    }

    [DllImport("__Internal")]
    private static extern void openWindow(string url);
#endif
}