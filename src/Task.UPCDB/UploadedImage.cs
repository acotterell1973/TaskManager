using System.Collections.Generic;

namespace Task.UPCDB
{
    public class UploadedImage
    {
        public UploadedImage()
        {
            // hard-coded to a single thumbnail at 200 x 300 for now
            Thumbnails = new List<Thumbnail> { new Thumbnail { Width = 200, Height = 300 } };
        }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public string Url { get; set; }
        public List<Thumbnail> Thumbnails { get; set; }
    }
    public class Thumbnail
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Url { get; set; }
    }
}