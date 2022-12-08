using System;
using System.Drawing;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
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
                    var ser = new DataContractJsonSerializer(typeof(Result));
                    var res = (Result)ser.ReadObject(jsonStream);
                    using (var imgStream = await client.GetStreamAsync(new Uri(baseUri + res.images[0].URL)))
                    {
                        return new BingImage(Image.FromStream(imgStream), res.images[0].Title, (baseUri + res.images[0].Quiz), res.images[0].Copyright, res.images[0].CopyrightLink);
                    }
                }
            }
        }

        [DataContract]
        private class Result
        {
            [DataMember(Name = "images")]
            public ResultImage[] images { get; set; }
        }

        [DataContract]
        private class ResultImage
        {
            [DataMember(Name = "enddate")]
            public string EndDate { get; set; }
            [DataMember(Name = "url")]
            public string URL { get; set; }
            [DataMember(Name = "urlbase")]
            public string URLBase { get; set; }
            [DataMember(Name = "title")]
            public string Title { get; set; }
            [DataMember(Name = "quiz")]
            public string Quiz { get; set; }
            [DataMember(Name = "copyright")]
            public string Copyright { get; set; }
            [DataMember(Name = "copyrightlink")]
            public string CopyrightLink { get; set; }
        }
    }

    public class BingImage
    {
        public BingImage(Image img, string title, string quiz, string copyright, string copyrightLink)
        {
            Img = img;
            Title = title;
            Quiz = quiz;
            Copyright = copyright;
            CopyrightLink = copyrightLink;
        }
        public Image Img { get; set; }
        public string Title { get; set; }
        public string Quiz { get; set; }
        public string Copyright { get; set; }
        public string CopyrightLink { get; set; }
    }
}
