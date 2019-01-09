using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureBlobLearning.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AzureBlobLearning.Controllers
{
	public class HomeController_Old : Controller
	{
		static CloudBlobClient _blobClient;
		const string _blobContainerName = "imagecontainer";
		private readonly IConfiguration _configuration;
		static CloudBlobContainer _blobContainer;

		public HomeController_Old(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task<ActionResult> Index()
		{
			try
			{
				var storageConnectionString = _configuration.GetValue<string>("StorageConnectionString");
				var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

				_blobClient = storageAccount.CreateCloudBlobClient();
				_blobContainer = _blobClient.GetContainerReference(_blobContainerName);
				await _blobContainer.CreateIfNotExistsAsync();

				await _blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

				List<Uri> allBlobs = new List<Uri>();
				BlobContinuationToken blobContinuationToken = null;
				do
				{
					var response = await _blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
					foreach (IListBlobItem blob in response.Results)
					{
						if (blob.GetType() == typeof(CloudBlockBlob))
							allBlobs.Add(blob.Uri);
					}
					blobContinuationToken = response.ContinuationToken;
				} while (blobContinuationToken != null);

				return View(allBlobs);
			}
			catch (Exception ex)
			{
				ViewData["message"] = ex.Message;
				ViewData["trace"] = ex.StackTrace;
				return View("Error");
			}
		}

		[HttpPost]
		public async Task<ActionResult> UploadAsync()
		{
			try
			{
				var request = await HttpContext.Request.ReadFormAsync();
				if (request.Files == null)
				{
					return BadRequest("Could not upload files");
				}
				var files = request.Files;
				if(files.Count == 0)
				{
					return BadRequest("Could not upload empty files");
				}

				for (int i = 0; i < files.Count; i++)
				{
					var blob = _blobContainer.GetBlockBlobReference(GetRandomBlobName(files[i].FileName));
					using (var stream = files[i].OpenReadStream())
					{
						await blob.UploadFromStreamAsync(stream);

					}
				}
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ViewData["message"] = ex.Message;
				ViewData["trace"] = ex.StackTrace;
				return View("Error");
			}
		}

		[HttpPost]
		public async Task<ActionResult> DeleteImage(string name)
		{
			try
			{
				Uri uri = new Uri(name);
				string filename = Path.GetFileName(uri.LocalPath);

				var blob = _blobContainer.GetBlockBlobReference(filename);
				await blob.DeleteIfExistsAsync();

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ViewData["message"] = ex.Message;
				ViewData["trace"] = ex.StackTrace;
				return View("Error");
			}
		}

		[HttpPost]
		public async Task<ActionResult> DeleteAll()
		{
			try
			{
				BlobContinuationToken blobContinuationToken = null;
				do
				{
					var response = await _blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
					foreach (IListBlobItem blob in response.Results)
					{
						if (blob.GetType() == typeof(CloudBlockBlob))
							await ((CloudBlockBlob)blob).DeleteIfExistsAsync();
					}
					blobContinuationToken = response.ContinuationToken;
				} while (blobContinuationToken != null);

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ViewData["message"] = ex.Message;
				ViewData["trace"] = ex.StackTrace;
				return View("Error");
			}
		}

		/// <summary> 
		/// string GetRandomBlobName(string filename): Generates a unique random file name to be uploaded  
		/// </summary> 
		private string GetRandomBlobName(string filename)
		{
			string ext = Path.GetExtension(filename);
			return string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
