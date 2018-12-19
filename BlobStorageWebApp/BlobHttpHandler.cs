using System.Diagnostics;
using System.IO;
using System.Web;

namespace BlobStorageWebApp
{
	public class BlobHttpHandler:IHttpHandler
	{

		//this instance is not reusable (each request requires a new instance because we're referencing HttpContext.Current)
		public bool IsReusable { get { return false; } }

		/// <summary>
		/// Uses the context.Request to :
		///		Determine if a file already exists with the same relative path
		///		Or
		///		Download a file from blob with the same relative path
		///	Then
		///		Update MimeType to match the filetype
		///		Insert the file data into the context.Response
		/// </summary>
		/// <param name="context"></param>
		public void ProcessRequest(HttpContext context)
		{
			//ex: https://webapplication220181218092344.azurewebsites.net/Content/Uploads/Images/Bart/5k.jpg

			string localpath = context.Server.MapPath(context.Request.Path);
			string localdir = Path.GetDirectoryName(localpath);

			//localfile will look something like this: 
			//	"G:\source\WebApplication2\WebApplication2\Content\Uploads\Images\Bart\5k.jpg"	(local machine)
			//	"D:\home\site\wwwroot\Content\Content\Uploads\Images\Bart\5k.jpg"				(app service)
			string localfile = Path.Combine(localdir, Path.GetFileName(context.Request.Path));

			//check if file already exists, if so, return the contents
			if (BlobHelper.IsCachedLocallyOnDisk(localfile))
			{
				Trace.WriteLine($"BlobImageHandler: Early exit because file is already cached at {localfile}.");

				//specify the mimetype
				string mimetype = MimeTypes.GetMimeType(localfile); ;
				Trace.WriteLine($"Setting response MimeType to : {mimetype}");
				context.Response.ContentType = mimetype;

				//stream it straight into the Response
				Trace.WriteLine($"Returning file to Response from : {localfile}");
				byte[] data = File.ReadAllBytes(localfile);
				context.Response.OutputStream.Write(data, 0, data.Length);
			}
			else
			{
				//grab and stream
				string blobname = HttpContext.Current.Request.Path.TrimStart('/');
				BlobHelper.DownloadAndStreamBlobToResponse(context, blobname);
			}
		}

	}
}