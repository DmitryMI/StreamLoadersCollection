using CommandLine;
using DashDownloader.DashFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader
{
    class Program
    {

        private class CmdOptions
        {
            [Option('u', HelpText = "URL to DASH file", Required = true)]
            public string PlaylistUri { get; set; }

            [Option('o', "out", HelpText = "Output directory", Required = false)]
            public string OutputDirectory { get; set; }

            [Option("proxy_address")]
            public string ProxyAddress { get; set; }

            [Option("proxy_login")]
            public string ProxyLogin { get; set; }

            [Option("proxy_pwd")]
            public string ProxyPassword { get; set; }
        }

        static int ReadInt(string message)
        {
            while(true)
            {
                Console.Write(message);
                Console.Write(": ");
                string input = Console.ReadLine();

                bool ok = int.TryParse(input, out int result);
                if(ok)
                {
                    return result;
                }

                Console.WriteLine("Format error, try again.");
            }
        }

        static AdaptationSet SelectAudioAdaptationSet(List<AdaptationSet> audioSets)
        {
            Console.WriteLine("Please, select audio file: ");
            for (int i = 0; i < audioSets.Count; i++)
            {
                AdaptationSet set = audioSets[i];
                Console.WriteLine($"{i}: {set.Lang}");
            }

            int index = ReadInt("Index: ");

            return audioSets[index];
        }

        static AdaptationSet SelectVideoAdaptationSet(List<AdaptationSet> videoSets)
        {
            return videoSets[0];
        }

        static Representation SelectAudioRepresentation(Representation[] audioRepresentations)
        {
            return audioRepresentations[0];
        }

        static Representation SelectVideoRepresentation(Representation[] videoRepresentations)
        {
            Console.WriteLine("Please, select video file: ");
            for (int i = 0; i < videoRepresentations.Length; i++)
            {
                Representation representation = videoRepresentations[i];
                Console.WriteLine($"{i}: {representation.Width} x {representation.Height}");
            }

            int index = ReadInt("Index: ");

            return videoRepresentations[index];
        }

        static string AppendExtension(string path, string extenstion)
        {
            if(path.EndsWith("/"))
            {
                path = path.Remove(path.Length - 1);
            }
            if(!extenstion.StartsWith("."))
            {
                extenstion = '.' + extenstion;
            }

            return path + extenstion;
        }

        private static HttpStatusCode DownloadSegment(WebClient client, Uri from, string to)
        {
            FileInfo fileInfo = new FileInfo(to);
            if(fileInfo.Exists && fileInfo.Length > 0)
            {
                return HttpStatusCode.OK;
            }
            try
            {
                client.DownloadFile(from, to);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpStatusCode statusCode = ((HttpWebResponse)webException.Response).StatusCode;
                    return statusCode;
                }
            }

            return HttpStatusCode.OK;
        }

        static string TrimSlashes(string str)
        {
            return str.Replace("/", "").Replace("\\", "");
        }

        static void DownloadRepresentation(WebClient webClient, Representation representation, string qualifierSuffix, string urlBase, string outputPath)
        {
            Uri uriBase = new Uri(urlBase);
            SegmentTemplate segmentTemplate = representation.SegmentTemplate;
            Uri initUri = new Uri(uriBase, segmentTemplate.Initialization);

            List<string> loadedFileList = new List<string>();

            string rootQualifier = TrimSlashes(uriBase.Segments.Last()) + "-" + qualifierSuffix + "-";

            string initFileName = rootQualifier + TrimSlashes(initUri.Segments.Last());
            string initFilePath = Path.Combine(outputPath, initFileName);
            Console.Write($"Downloading init to {initFileName}... ");
            webClient.DownloadFile(initUri, initFilePath);
            Console.WriteLine("Done.");
            loadedFileList.Add(initFilePath);

            FormattedString mediaString = new FormattedString(segmentTemplate.Media);

            int number = segmentTemplate.StartNumber;
            while(true)
            {                
                mediaString["Number"] = number.ToString();
                string segmentPath = mediaString.ToString();
                Uri segmentUri = new Uri(uriBase, segmentPath);
                string segmentFileName = rootQualifier + TrimSlashes(segmentUri.Segments.Last());
                string segmentFilePath = Path.Combine(outputPath, segmentFileName);
                Console.Write($"Downloading {segmentPath} to {segmentFileName}... ");

                HttpStatusCode statusCode = DownloadSegment(webClient, segmentUri, segmentFilePath);
                if (statusCode == HttpStatusCode.OK)
                {
                    loadedFileList.Add(segmentFilePath);
                    Console.WriteLine("Done.");
                }
                else if (statusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Failed.");
                    Console.WriteLine($"Server returned 404 Not Found. Considering this as the End of File");
                    break;
                }
                else
                {
                    Console.WriteLine("Failed.");
                    Console.WriteLine($"Loading segment {segmentUri} failed due to error. Status Code: {statusCode}");
                    break;
                }

                number++;
            }

            FileInfo initFileInfo = new FileInfo(initFilePath);
            string initExtension = initFileInfo.Extension;
            string resultFileName = AppendExtension(rootQualifier + "final", initExtension);
            string resultFilePath = Path.Combine(outputPath, resultFileName);

            if(File.Exists(resultFilePath))
            {
                Console.WriteLine("Deleting existing file...");
                File.Delete(resultFilePath);
            }

            using (FileStream fileStream = new FileStream(Path.Combine(resultFilePath), FileMode.CreateNew, FileAccess.ReadWrite))
            {

                foreach (string segmentFile in loadedFileList)
                {
                    FileStream segmentStream = new FileStream(segmentFile, FileMode.Open, FileAccess.Read);
                    byte[] segmentData = new byte[segmentStream.Length];
                    segmentStream.Read(segmentData, 0, segmentData.Length);
                    fileStream.Write(segmentData, 0, segmentData.Length);

                    segmentStream.Close();
                }                

                fileStream.Flush();
                fileStream.Close();
            }

            foreach (string segmentFile in loadedFileList)
            {
                File.Delete(segmentFile);
            }
        }

        static string RemoveLastSegment(string uriString)
        {
            Uri uri = new Uri(uriString);

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(uri.Scheme).Append("://").Append(uri.Host);

            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                string segment = uri.Segments[i];
                stringBuilder.Append(segment);
            }

            return stringBuilder.ToString();
        }

        static void DownloadStream(WebClient webClient, string source, string outputPath)
        {
            var callbackBackup = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            Mpd mpd = DownloadMpd(webClient, source);

            List<AdaptationSet> videoAdaptationSets = new List<AdaptationSet>();
            List<AdaptationSet> audioAdaptationSets = new List<AdaptationSet>();
            
            foreach(var period in mpd.Periods)
            {
                foreach(var adaptationSet in period.AdaptationSets)
                {
                    if(adaptationSet.ContentType == "audio")
                    {
                        audioAdaptationSets.Add(adaptationSet);
                    }
                    else if(adaptationSet.ContentType == "video")
                    {
                        videoAdaptationSets.Add(adaptationSet);
                    }
                }
            }

            AdaptationSet audioSet = SelectAudioAdaptationSet(audioAdaptationSets);
            AdaptationSet videoSet = SelectVideoAdaptationSet(videoAdaptationSets);

            Representation audioRepresentation = SelectAudioRepresentation(audioSet.Representations);
            Representation videoRepresentation = SelectVideoRepresentation(videoSet.Representations);

            string urlBase = RemoveLastSegment(source);

            Console.WriteLine("Downloading audio...");
            DownloadRepresentation(webClient, audioRepresentation, audioSet.Lang, urlBase, outputPath);
            Console.WriteLine("Downloading audio finished");
            Console.WriteLine("Downloading video...");
            DownloadRepresentation(webClient, videoRepresentation, videoSet.MaxWidth.ToString(), urlBase, outputPath);
            Console.WriteLine("Downloading video finished");
        }

        static Mpd DownloadMpd(WebClient webClient, string source)
        {            
            string dashContent = webClient.DownloadString(source);           

            //string dashContent = File.ReadAllText("dash_example.xml");
            Mpd mpd = Dash.Parse(dashContent);
            return mpd;
        }

        static void Main(string[] args)
        {
            string urlString = null;
            string outPath = null;
            string proxyAddress = null;
            string proxyLogin = null;
            string proxyPassword = null;

            var parseResults = Parser.Default.ParseArguments<CmdOptions>(args);
            parseResults.WithParsed(o =>
            {
                urlString = o.PlaylistUri;
                outPath = o.OutputDirectory;
                proxyAddress = o.ProxyAddress;
                proxyLogin = o.ProxyLogin;
                proxyPassword = o.ProxyPassword;
            });

            parseResults.WithNotParsed((IEnumerable<Error> errorList) =>
            {
                foreach (var error in errorList)
                {
                    Console.WriteLine("Error in parsing argument: " + error.ToString());
                }
            });


            if (String.IsNullOrWhiteSpace(urlString))
            {
                Console.WriteLine("URI to DASH not specified");
                Console.ReadKey(true);
                return;
            }

            if (String.IsNullOrWhiteSpace(outPath))
            {
                Console.WriteLine("Output path not specified, using current directory: " + Environment.CurrentDirectory);
                outPath = Environment.CurrentDirectory;
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(outPath);
                if (!Directory.Exists(directoryInfo.FullName))
                {
                    Console.WriteLine("Directory created");
                    Directory.CreateDirectory(directoryInfo.FullName);
                }
                outPath = directoryInfo.FullName;
            }

            WebClient webClient = new WebClient();

            if (proxyLogin != null)
            {
                NetworkCredential networkCredential = null;
                networkCredential = new NetworkCredential(proxyLogin, proxyPassword);
                webClient.Proxy = new WebProxy(proxyAddress, false, null, networkCredential);
            }

            DownloadStream(webClient, urlString, outPath);

            Console.WriteLine("Finished");

            Console.ReadKey();
        }
    }
}
