using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Task.UPCDB
{
    public class ImageService : IImageService
    {
        private readonly string _imageRootPath;
        private readonly string _containerName;
        private readonly string _blobStorageConnectionString;
        public ImageService()
        {
            _imageRootPath = "https://winehunter.blob.core.windows.net/images-container";
            _containerName = "images-container";
            _blobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;";
        }

        public async static Task<byte[]> LoadImage(Uri uri)
        {
            byte[] bytes;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync(uri))
                    {
                        response.EnsureSuccessStatusCode();

                        using (Stream imageStream = await response.Content.ReadAsStreamAsync())
                        {
                            bytes = new byte[imageStream.Length];
                            imageStream.Read(bytes, 0, (int)imageStream.Length);
                        }
                    }
                }
                return bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load the image: {0}", ex.Message);
            }

            return null;
        }

        public async Task<UploadedImage> CreateUploadedImage(string imageUrl, string imageName)
        {
            if (!string.IsNullOrEmpty(imageName))
            {
                byte[] fileBytes = await LoadImage(new Uri(imageUrl));
                return new UploadedImage
                {
                    ContentType = "image/jpg",
                    Data = fileBytes,
                    Name = imageName,
                    Url = $"{_imageRootPath}/{imageName}"
                };
            }
            return null;
        }

        public async System.Threading.Tasks.Task AddImageToBlobStorageAsync(UploadedImage image)
        {
            //  get the container reference
            var container = GetImagesBlobContainer();
            // using the container reference, get a block blob reference and set its type
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(image.Name);
            blockBlob.Properties.ContentType = image.ContentType;
            // finally, upload the image into blob storage using the block blob reference
            var fileBytes = image.Data;
            await blockBlob.UploadFromByteArrayAsync(fileBytes, 0, fileBytes.Length);
        }

        private CloudBlobContainer GetImagesBlobContainer()
        {
            // use the connection string to get the storage account
            var storageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
            // using the storage account, create the blob client
            var blobClient = storageAccount.CreateCloudBlobClient();
            // finally, using the blob client, get a reference to our container
            var container = blobClient.GetContainerReference(_containerName);
            // if we had not created the container in the portal, this would automatically create it for us at run time
            container.CreateIfNotExists();
            // by default, blobs are private and would require your access key to download.
            //   You can allow public access to the blobs by making the container public.   
            container.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            return container;
        }
    }
}
