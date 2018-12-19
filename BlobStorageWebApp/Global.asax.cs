using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace BlobStorageWebApp
{
	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}


		protected void Application_BeginRequest(Object sender, EventArgs e)
		{
			//check to see if the request is asking for an Uploaded artifact (by checking the path)
			if (HttpContext.Current.Request.RawUrl.Contains("/Content/Uploads/"))
			{
				//ex: https://webapplication220181218092344.azurewebsites.net/Content/Uploads/Images/Bart/5k.jpg

				//localpath will look like this: 
				//D:\home\site\wwwroot\Content\Uploads\Images\Bart\5k.jpg
				string localpath = HttpContext.Current.Server.MapPath(HttpContext.Current.Request.Path);
				string localdir = Path.GetDirectoryName(localpath);

				//localfile will look something like this: 
				//	"G:\source\WebApplication2\WebApplication2\Content\Uploads\Images\Bart\5k.jpg"	(local machine)
				//	"D:\home\site\wwwroot\Content\Content\Uploads\Images\Bart\5k.jpg"				(app service)
				string localfile = Path.Combine(localdir, Path.GetFileName(HttpContext.Current.Request.Path));

				//check if file already exists, if so, exit
				if (BlobHelper.IsCachedLocallyOnDisk(localfile))
				{
					Trace.WriteLine($"Application_BeginRequest: No need to process anything because file is already cached at {localfile}.");
				}
				else
				{
					//fetch and store
					string blobname = HttpContext.Current.Request.Path.TrimStart('/');
					BlobHelper.DownloadAndStoreBlobLocally(blobname, localfile);
				}
			}
		}
	}
}
