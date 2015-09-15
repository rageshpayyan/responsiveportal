using System;
using System.IO;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ResponsivePortal.Startup), "Configuration")]

namespace ResponsivePortal
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            ConfigureAuth(app);
        }
          
    }
}