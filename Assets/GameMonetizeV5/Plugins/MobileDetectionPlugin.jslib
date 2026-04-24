 var MobileDetectionPlugin = {
     IsMobile: function()
     {
         return Module.SystemInfo.mobile;
     }
 };
 
 mergeInto(LibraryManager.library, MobileDetectionPlugin);