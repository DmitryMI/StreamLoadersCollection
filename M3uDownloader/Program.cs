using CommandLine;
using m3uParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace M3uDownloader
{
    class Program
    {
        private class CmdOptions
        {
            [Option('u', HelpText = "URL to m3u playlist", Required = true)]
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

        static void SetHeaders(WebClient client)
        {
            
        }

        static string RemoveQuery(string uri)
        {
            int queryStart = uri.IndexOf('?');
            string result = uri.Remove(queryStart);
            return result;
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

        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        static void DownloadStream(WebClient webClient, string source, string outputPath)
        {
            SetHeaders(webClient);

            var callbackBackup = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            string playlist = webClient.DownloadString(source);

            var playlistContent = M3U.Parse(playlist);

            string rootPath = RemoveLastSegment(source);

            Uri rootPathUri = new Uri(rootPath);

            List<string> loadedList = new List<string>();

            foreach (var media in playlistContent.Medias)
            {
                Uri absoluteUri = new Uri(rootPathUri, media.MediaFile);
                Console.Write(absoluteUri.Segments.Last() + "... ");
                string targetName = ReplaceInvalidChars(absoluteUri.Segments.Last());
                string targetPath = Path.Combine(outputPath, targetName);

                FileInfo targetFileInfo = new FileInfo(targetPath);
                if(!targetFileInfo.Exists || targetFileInfo.Length == 0)
                {
                    webClient.DownloadFile(absoluteUri, targetPath);                    
                    Console.WriteLine("Done.");
                }
                else
                {
                    Console.WriteLine("Exists.");
                }

                loadedList.Add(targetPath);

            }

            ServicePointManager.ServerCertificateValidationCallback = callbackBackup;

            Console.WriteLine("Combining segments...");

            string fileName = rootPathUri.Segments.Last();
            fileName = ReplaceInvalidChars(fileName);
            string filePath = Path.Combine(outputPath, fileName);
            if (filePath.EndsWith("/"))
            {
                filePath = filePath.Remove(filePath.Length - 1);
            }
            filePath += ".ts";
            if(File.Exists(filePath))
            {
                Console.Write($"Deleting existing file {filePath}... ");
                File.Delete(filePath);
                Console.WriteLine("Done.");
            }
            using (FileStream fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                foreach (var file in loadedList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    Console.WriteLine($"Writing {fileInfo.Name} to {fileName}...");
                    using(FileStream segmentFs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        byte[] segment = new byte[segmentFs.Length];
                        segmentFs.Read(segment, 0, segment.Length);
                        fs.Write(segment, 0, segment.Length);
                        segmentFs.Close();
                    }

                    File.Delete(file);
                }

                fs.Flush();
                fs.Close();                
            }
            
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
                foreach(var error in errorList)
                {
                    Console.WriteLine("Error in parsing argument: " + error.ToString());
                }
            });
            

            if(String.IsNullOrWhiteSpace(urlString))
            {
                Console.WriteLine("URI to playlist not specified");
                Console.ReadKey(true);
                return;
            }

            if(String.IsNullOrWhiteSpace(outPath))
            {
                Console.WriteLine("Output path not specified, using current directory: " + Environment.CurrentDirectory);
                outPath = Environment.CurrentDirectory;
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(outPath);
                if(!Directory.Exists(directoryInfo.FullName))
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
