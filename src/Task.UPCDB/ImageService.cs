using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
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
            _imageRootPath = "https://winehunter.blob.core.windows.net/wine-bottles";
            _containerName = "wine-bottles";
            _blobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;";
        }

        public static async Task<byte[]> LoadImage(Uri uri)
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

        public async Task<UploadedImage> CreateUploadedImage(string imageUrl, string imageName, string imagePath="")
        {
            if (!string.IsNullOrEmpty(imageName))
            {
                byte[] fileBytes = await LoadImage(new Uri(imageUrl));
                if (fileBytes == null) return null;

                ISupportedImageFormat format = new PngFormat(); {   };
                //   Size size = new Size(150, 0)
                using (MemoryStream inStream = new MemoryStream(fileBytes))
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                        {
                            // Load, resize, set the format and quality and save an image.
                            imageFactory.Load(inStream)
                                        // .Resize(size)
                                         .Format(format)
                                        .Save(outStream);
                        }
                        // Do something with the stream.
                        var imageFileName = imagePath + imageName + ".png";
                        var fi = new FileInfo(imageFileName);
                        if (!fi.Exists)
                        {
                            using (
                                var fileStream = new FileStream(imageFileName, FileMode.CreateNew,
                                    FileAccess.ReadWrite))
                            {
                                outStream.Position = 0;
                                outStream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                
                return new UploadedImage
                {
                    ContentType = "image/png",
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
