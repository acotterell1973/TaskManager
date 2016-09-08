using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Task.UPCDB.Tasks
{
    public class ImageLoader
    {
        //public async static Task<BitmapImage> LoadImage(Uri uri)
        //{
        //    BitmapImage bitmapImage = new BitmapImage();
        //    Image image = Image.FromStream(model.DisplayPicture.InputStream);
        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            using (var response = await client.GetAsync(uri))
        //            {
        //                response.EnsureSuccessStatusCode();

        //                using (Stream inputStream = await response.Content.ReadAsStreamAsync())
        //                {
        //                    //bitmapImage.SetSource(inputStream.Read());
        //                }
        //            }
        //        }
        //        return bitmapImage;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Failed to load the image: {0}", ex.Message);
        //    }

        //    return null;
        //}
    }
}
