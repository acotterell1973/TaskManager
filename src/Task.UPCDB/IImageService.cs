using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task.UPCDB
{
    public interface IImageService
    {
        Task<UploadedImage> CreateUploadedImage(string imageUrl, string imageName);
        System.Threading.Tasks.Task AddImageToBlobStorageAsync(UploadedImage image);
    }
}
