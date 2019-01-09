using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureBlobLearning.Services
{
	public interface IAzureBlobService
	{
		Task<IEnumerable<Uri>> ListAsync();
		Task UploadAsync(IFormFileCollection files);
		Task DeleteAsync(string fileUri);
		Task DeleteAllAsync();
	}

	public class AzureBlobService : IAzureBlobService
	{
		private readonly IAzureBlobConnectionFactory _azureBlobConnectionFactory;

		public AzureBlobService(IAzureBlobConnectionFactory azureBlobConnectionFactory)
		{
			_azureBlobConnectionFactory = azureBlobConnectionFactory;
		}

		public async Task DeleteAllAsync()
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			BlobContinuationToken blobContinuationToken = null;
			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
				foreach (IListBlobItem blob in response.Results)
				{
					if (blob.GetType() == typeof(CloudBlockBlob))
						await((CloudBlockBlob)blob).DeleteIfExistsAsync();
				}
				blobContinuationToken = response.ContinuationToken;
			} while (blobContinuationToken != null);
		}

		public async Task DeleteAsync(string fileUri)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			Uri uri = new Uri(fileUri);
			string filename = Path.GetFileName(uri.LocalPath);

			var blob = blobContainer.GetBlockBlobReference(filename);
			await blob.DeleteIfExistsAsync();
		}

		public async Task<IEnumerable<Uri>> ListAsync()
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
			var allBlobs = new List<Uri>();
			BlobContinuationToken blobContinuationToken = null;
			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
				foreach (IListBlobItem blob in response.Results)
				{
					if (blob.GetType() == typeof(CloudBlockBlob))
						allBlobs.Add(blob.Uri);
				}
				blobContinuationToken = response.ContinuationToken;
			} while (blobContinuationToken != null);
			return allBlobs;
		}

		public async Task UploadAsync(IFormFileCollection files)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			for (int i = 0; i < files.Count; i++)
			{
				var blob = blobContainer.GetBlockBlobReference(GetRandomBlobName(files[i].FileName));
				using (var stream = files[i].OpenReadStream())
				{
					await blob.UploadFromStreamAsync(stream);

				}
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
	}
}
