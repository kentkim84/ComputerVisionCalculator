using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;
using Emgu.CV;
using Emgu.CV.Structure;

namespace FunctionObjectDetector
{
    public static class Function1
    {
        [FunctionName("DetectObjects")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            [Blob("classifier-container/haarcascade_frontalface_alt2.xml", FileAccess.Read)] Stream blobStream,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            log.Info($"Blob Nmme: {blobStream} Size: {blobStream.Length} bytes");

            // parse query parameter
            string base64String = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "base64String", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            base64String = base64String ?? data?.base64String;

            // Setup Cascade Classifier                        
            HaarCascade cascade;
            HaarObjectDetector detector;
            Rectangle[] objects;
            CascadeClassifier classifierFace;
            classifierFace = new CascadeClassifier(@"D:\home\site\wwwroot\DetectObjects\haarcascade_frontalface_default.xml");

            using (var ms = new MemoryStream())
            {
                // Copy blob-stream to memory stream
                blobStream.CopyTo(ms);
                //var buffer = ms.ToArray();
                                
                //cascade = HaarCascade.FromXml(blobStream); // Loads a HaarCascade from a OpenCV-compatible XML file text reader
                cascade = new FaceHaarCascade(); // Loads premade Face HaarCascade
                detector = new HaarObjectDetector(cascade, 30); // Create a new Haar object detector with the cascade

                log.Info("\tCascade states: " + cascade.Stages);
            }

            // Base64 string -> byte array
            var bytes = Convert.FromBase64String(base64String);

            // Create image from byte array
            // Convert into gray-scale image
            using (var ms = new MemoryStream(bytes))
            {
                Bitmap bitmap = new Bitmap(ms);
                Image<Bgr, byte> image = new Image<Bgr, byte>(bitmap);
                Image<Gray, byte> grayImage = image.Convert<Gray, byte>();
                var faces = classifierFace.DetectMultiScale(grayImage, 1.1, 4);

                objects = detector.ProcessFrame(bitmap);  
            }

            return objects == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Face found: " + objects + " size: " + objects.Length);

            //return base64String == null
            //    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            //    : req.CreateResponse(HttpStatusCode.OK, "Response: " + base64String);
        }

        //private static Task<byte> FromBase64(string base64String)
        //{
        //    // Read stream


        //    return output;
        //}
    }
}