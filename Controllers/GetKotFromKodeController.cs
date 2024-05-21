using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Pract_kotiki.Controllers
{
    [Route("api/[controller]/{url}")]
    [ApiController]
    public class GetKotFromKodeController : ControllerBase
    {
        static List<(string code,byte[] image)> cache = new List<(string code, byte[] image)>();
        object cache_lock_obj = new object();
        [HttpGet]
        public IActionResult Get(string url)//try like https://404.returnco.de/whatever for test
        {
            string status_code;
            #region GetStatusCodeFromURL
            url = url.Replace("%2F", "/");
            {
                WebResponse response = null;
                try
                {
                    WebRequest req = WebRequest.Create(url);
                    response = req.GetResponse();
                    HttpWebResponse httpWebResponse = (HttpWebResponse)response;
                    status_code = ((int)httpWebResponse.StatusCode).ToString();
                }
                catch (WebException we)//If the connection failed and thus the request could not be sent and no response could be received, there won't be any http status code
                {
                    status_code = ((int)((HttpWebResponse)we.Response).StatusCode).ToString();
                }

            }
            #endregion
            #region Search image in cache
            lock (cache_lock_obj)
            {
                foreach (var row in cache)
                {
                    if (row.code == status_code)
                    {
                        return File(row.image, "image/jpeg");
                    }
                }
            }
            #endregion
            #region DownloadImage
            {
                WebRequest req = WebRequest.Create($"https://http.cat/images/{status_code}.jpg");
                WebResponse response = req.GetResponse();
                Stream image_stream = response.GetResponseStream();
                byte[] image;
                using (var memoryStream = new MemoryStream())
                {
                    image_stream.CopyTo(memoryStream);
                    image = memoryStream.ToArray();
                }
                Thread thread = new Thread(() =>
                {
                    lock (cache_lock_obj)
                    {

                        cache.Add((status_code, image));
                    }
                });
                thread.Start();
                return File(image, "image/jpeg");
            }
            #endregion
        }
    }
}
