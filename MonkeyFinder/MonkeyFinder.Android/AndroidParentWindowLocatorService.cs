using System;
using Plugin.CurrentActivity;

namespace MonkeyFinder.Droid
{
    public class AndroidParentWindowLocatorService : IParentWindowLocatorService
    {        
        public object GetCurrentParentWindow()
        {
            return CrossCurrentActivity.Current.Activity;
        }
    }
}
