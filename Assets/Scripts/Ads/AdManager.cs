using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdManager : SingletonMonoStart<AdManager>
{
    
    public override void OnStart()
    {
        base.OnStart();
        //Applovin
        AdUtil.Init();
        
        //Adjust
        TrackingUtil.Init();
    }
}
