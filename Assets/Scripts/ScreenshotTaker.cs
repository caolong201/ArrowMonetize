using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotTaker : SingletonMonoAwake<ScreenshotTaker>
{
    public string fileName = "ScreenshotBG"; // Base name for the screenshot file
    public int superSize = 4;             // Multiplier for resolution (4 = 4K for standard HD)

    void Update()
    {
        // Take a screenshot when the "P" key is pressed
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeScreenshot();
        }
    }

    public void TakeScreenshot()
    {
        // Generate a unique file name with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fullFileName = $"{fileName}_{timestamp}.png";

        // Capture the screenshot
        ScreenCapture.CaptureScreenshot(fullFileName, superSize);

        Debug.Log($"Screenshot saved as {fullFileName}");
    }
}
