# BlobStorageWebApp 
A sample web app that serves files from blob storage rather than local storage.

## A solution for reatining links to content that appear local, but are served by blobs. Solves for what? 
  1. Historical content.  Web apps that have allowed file uploads to the local machine will have links (either relative or absolute) that point to a URL that was historically served by a local file, mapped in IIS.  After migration to blob storage, the local content will no longer exist, and all links will be broken. 
  
  2. Content Offload.  Any web app might want content to appear local to the machine, but remain in blob storage. 
  
## Can I do blob stuff without this? ##
Sure - If. If you don't have any historical data, or if you can change all existing link/bookmarks to point to the blob Url, rather than local/relative, then you won't need this.  If you're OK with public blob access, then you won't need this.  If you're OK solving for security via blob SAS or policies, then you won't need this. *This solution allows for completely private blob containers, and requires no changes to existing links/bookmarks.*
  
## Implementation :

1. HttpHander (BlobHttpHandler.cs).  

    What it does: Intercepts Http requests at whatever path and verb are provided in the web.config.  Checks for a local version of the file, and returns that if it exists. Otherwise, it fetches the blob and sets the ```Response.ContentType``` to the correct MimeType, then simply chunks the btye array or stream into the ```Response.OutputStream```.

    Things to know: This fires *after* the Global.asax method
    
    Why It's Needed: *Global.asax.cs may not be enough sometimes*. In some versions of IIS, even if the content is on disk, it may not be returned. Why? Because it the entire directory structure was not included with the web app deployment, IIS thinks the content isn't there, *even if it really is*.  This handler takes care of this edge-case by simply grabbing the local file and returning it in the stream. In my anecdotal experience, this problems simply goes away after some time - I've not tried to determine why it doesn't actually work always, nor why is magically starts working.
    
    web.config setup
```
<system.webServer>
  <modules>
    ...
  </modules>
  <handlers>
    <remove name="MyHandler" />
    <add name="MyHandler" verb="GET" path="Content/Uploads" type="BlobStorageWebApp.BlobHttpHandler" />
  </handlers>
</system.webServer>
```

    relevant code
```
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
```

2. Global.asax.cs.Application_OnStart(). 

    What it does: Intercepts each call to the web app and performs a pattern match on the Url to determine if the code should try and fetch content from blob storage.
    
    Things to know: We're using Application_BeginRequest, so Session content isn't available yet in the pipeline
    
    Why It's Needed: This is the code that fetches and stores content locally. It will store the content at the exact file path that matches whatever Url was GET'd. Subsequent calls for the content *just work* and return the content from local disk, except in the case mentioned in "Why It's Needed" for the HttpHandler.
  
### To Run this solution you must create a file named private.config that contains the following content:
```
<connectionStrings>
	<add name="BlobStorageConnectionString" 
		 connectionString="DefaultEndpointsProtocol=https;AccountName={{name}};AccountKey={{key}};EndpointSuffix=core.windows.net" />
</connectionStrings>
```
