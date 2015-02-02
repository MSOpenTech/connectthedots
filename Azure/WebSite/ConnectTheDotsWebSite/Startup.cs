using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ConnectTheDotsWebSite.Startup))]

namespace ConnectTheDotsWebSite
{
    public class Startup
    {
        #region public

        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }

        #endregion
    }
}