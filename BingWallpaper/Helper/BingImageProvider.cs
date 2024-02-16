using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BingWallpaper
{
    public class BingImageProvider
    {
        public async Task<BingImage> GetImage(string mkt)
        {
            string baseUri = "https://www.bing.com";
            using (var client = new HttpClient())
            {
                using (var jsonStream = await client.GetStreamAsync("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1" + mkt))
                {
                    var res = new DataContractJsonSerializer(typeof(Result)).ReadObject(jsonStream) as Result;
                    using (var imgStream = await client.GetStreamAsync(new Uri(baseUri + res.Images[0].URLBase + "_UHD.jpg")))
                    {
                        var img = Image.FromStream(imgStream);

                        var copyrightPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                        copyrightPropertyItem.Id = 33432; //Copyright
                        copyrightPropertyItem.Type = 2; //ASCII
                        copyrightPropertyItem.Value = Encoding.ASCII.GetBytes(res.Images[0].Copyright);
                        copyrightPropertyItem.Len = copyrightPropertyItem.Value.Length;

                        img.SetPropertyItem(copyrightPropertyItem);

                        return new BingImage()
                        {
                            Img = img,
                            Copyright = res.Images[0].Copyright,
                            CopyrightLink = res.Images[0].CopyrightLink,
                            Title = res.Images[0].Title,
                            QuizLink = baseUri + res.Images[0].Quiz,
                            ShortFileName = res.Images[0].StartDate
                        };
                    }
                }
            }
        }

        [DataContract]
        private class Result
        {
            [DataMember(Name = "images")]
            public ResultImage[] Images { get; set; }
        }

        [DataContract]
        private class ResultImage
        {
            [DataMember(Name = "urlbase")]
            public string URLBase { get; set; }

            [DataMember(Name = "startdate")]
            public string StartDate { get; set; }

            [DataMember(Name = "copyright")]
            public string Copyright { get; set; }

            [DataMember(Name = "copyrightlink")]
            public string CopyrightLink { get; set; }

            [DataMember(Name = "title")]
            public string Title { get; set; }

            [DataMember(Name = "quiz")]
            public string Quiz { get; set; }
        }
    }

    public class BingImage
    {
        public Image Img { get; set; }
        public string Copyright { get; set; }
        public string CopyrightLink { get; set; }
        public string Title { get; set; }
        public string QuizLink { get; set; }
        public string LongFileName => string.Join("_", Copyright.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        public string ShortFileName { get; set; }
    }
}
