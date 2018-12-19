using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace BlobStorageWebApp
{
	public static class BlobHelper
	{
		/// <summary>
		/// Downloads a blob from storage and stores it locally
		/// </summary>
		/// <param name="localfilepath"></param>
		public static void DownloadAndStoreBlobLocally(string blobName, string localfile)
		{
			//ex: https://webapplication220181218092344.azurewebsites.net/Content/Uploads/Images/Bart/5k.jpg

			//ensure local directory 
			EnsureLocalDirectoryForWebRequest(Path.GetDirectoryName(localfile));

			try
			{
				//grab the blob reference
				var blob = LoadBlobReference(blobName);

				//download it to the appropriate location
				Trace.WriteLine($"Attempting to grab blob at primary: {blob.StorageUri.PrimaryUri}");
				blob.DownloadToFile(localfile, FileMode.Create);

			}
			catch (StorageException ex)
			{
				Trace.WriteLine($"Bad things happened: {ex.Message}");
			}
			finally
			{
				Trace.WriteLine($"File downloaded successfully from {blobName} to {localfile}");
			}

		}

		/// <summary>
		/// Loads a blob reference from Azure blob storage. 
		/// Need to call things on teh Blob in order to see properties and such (ex: blob.FetchAttributes())
		/// </summary>
		/// <param name="blobName"></param>
		/// <returns></returns>
		public static CloudBlockBlob LoadBlobReference(string blobName)
		{
			//todo: externalize this in config
			string containerName = "test";
			return LoadBlobReference(GetBlobClient(), containerName, blobName);
		}

		/// <summary>
		/// Loads a blob reference from Azure blob storage. 
		/// Need to call things on teh Blob in order to see properties and such (ex: blob.FetchAttributes())
		/// </summary>
		/// <param name="containerName"></param>
		/// <param name="blobName"></param>
		/// <returns></returns>
		public static CloudBlockBlob LoadBlobReference(string containerName, string blobName)
		{
			return LoadBlobReference(GetBlobClient(), containerName, blobName);
		}

		/// <summary>
		/// Loads a blob reference from Azure blob storage. 
		/// Need to call things on teh Blob in order to see properties and such (ex: blob.FetchAttributes())
		/// </summary>
		/// <param name="client"></param>
		/// <param name="containerName"></param>
		/// <param name="blobName"></param>
		/// <returns></returns>
		public static CloudBlockBlob LoadBlobReference(CloudBlobClient client, string containerName, string blobName)
		{
			
			CloudBlockBlob blob = null;
			try
			{
				//grab the container
				Trace.WriteLine($"Attempting to load container reference: {containerName}");
				var container = client.GetContainerReference(containerName);

				//grab the blob reference
				Trace.WriteLine($"Attempting to load blob reference: {blobName}");
				blob = container.GetBlockBlobReference(blobName);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Bad things happened: {ex.Message}");
			}
			finally
			{
				Trace.WriteLine($"Blob reference created successfully for : {blobName}");
			}
			return blob;
		}

		/// <summary>
		/// Given a context, grabs a corresponding blob from the exact path in the Request and returns it into the Response stream
		/// </summary>
		/// <param name="context"></param>
		public static void DownloadAndStreamBlobToResponse(HttpContext context, string blobName)
		{
			string blobpath = string.Empty;
			try
			{
				//grab the blob reference
				var blob = LoadBlobReference(blobName);

				//todo: cache?

				//specify the mimetype
				string mimetype = MimeTypes.GetMimeType(Path.GetFileName(blob.StorageUri.PrimaryUri.ToString())); ;
				Trace.WriteLine($"Setting response MimeType to : {mimetype}");
				context.Response.ContentType = mimetype;

				//stream it straight into the Response
				Trace.WriteLine($"Attempting to grab blob at primary: {blob.StorageUri.PrimaryUri}");
				blob.DownloadToStream(context.Response.OutputStream);
			}
			catch (StorageException ex)
			{
				Trace.WriteLine($"Bad things happened: {ex.Message}");
			}
			finally
			{
				Trace.WriteLine($"File downloaded successfully from {blobpath}");
			}
		}

		/// <summary>
		/// Checks for a directory and creates it if not exists
		/// </summary>
		/// <param name="localpath"></param>
		public static string EnsureLocalDirectoryForWebRequest(string localdir)
		{
			//Create the local directory if needed
			Trace.WriteLine($"Checking for directory at '{localdir}'");
			if (!Directory.Exists(localdir))
			{
				Trace.WriteLine($"Not found.");
				Directory.CreateDirectory(localdir);
				Trace.WriteLine($"Created directory at '{localdir}'");
			}
			else
			{
				Trace.WriteLine($"Directory already exists at '{localdir}'");
			}
			return localdir;
		}


		/// <summary>
		/// Do checks to see if we need to dowload the file from blob storage
		/// </summary>
		/// <param name="localfilepath"></param>
		/// <returns></returns>
		public static bool IsCachedLocallyOnDisk(string localfilepath)
		{
			bool tmp = false;

			Trace.WriteLine($"Checking if file exists at {localfilepath}");
			if (File.Exists(localfilepath))
			{
				Trace.WriteLine($"Found existing file at {localfilepath}");

				//todo: do we need to check more than the name? what about versions, timestamp, etc.
				if (true)
				{
					Trace.WriteLine($"Additional cache check successful");
					tmp = true;
				}
				else
				{
					//freshness check failed
					Trace.WriteLine($"Stale file exists, need to refresh from blob.");
					tmp = false;
				}
			}
			else
			{
				//file doesn't exist
				Trace.WriteLine($"No file found at {localfilepath}");
				tmp = false;
			}

			return tmp;
		}


		/// <summary>
		/// Returns a valid storage account object to use for blob interaction
		/// </summary>
		/// <returns></returns>
		public static CloudBlobClient GetBlobClient()
		{
			//todo: put this in config
			string storageConnectionString = "";
			CloudStorageAccount tmp = null;

			//todo: determine if we want to show the actual connection string or not
			Trace.WriteLine($"Attempting to connect to blobs with connection string: {storageConnectionString}");

			// Check whether the connection string can be parsed.
			if (CloudStorageAccount.TryParse(storageConnectionString, out tmp))
			{
				Trace.WriteLine($"Successfully connected to blob storage");
			}
			else
			{
				//todo: determine if we want to show the actual connection string or not
				Console.WriteLine($"Your connection string sucks: {storageConnectionString}");
			}

			return tmp.CreateCloudBlobClient();

		}
	}
}