using System;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CSLiveStreamServer
{
    class Program
    {
        private static Camera _camera;

        // Start a cleanup thread for ramfs and then start a thread to handle requests.
        public static void Main(string[] args)
        {
            _camera = new Camera();

            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://*:80/");
            httpListener.Start();

            AsyncMain(httpListener).Wait();
        }

        private static async Task AsyncMain(HttpListener httpListener)
        {
            var cleanupTask = ManageRamfsStorage();
            var serverTask = AsyncListener(httpListener);

            await Task.WhenAll(new[] {cleanupTask, serverTask});
        }

        // Make sure a limited number of files exists in the ramfs directory at any given time.
        private static async Task ManageRamfsStorage(int fileLimit = 20, int msBetweenCleanups = 5000)
        {
            while (true)
            {
                try
                {
                    var fileInfos = GetRamfsFileInfosSorted();

                    if (fileInfos.Count() <= fileLimit)
                    {
                        await Task.Delay(msBetweenCleanups);
                        continue;
                    }

                    // To get here, the number of fileInfos must be (fileLimit + 1), so i = fileLimit will point to the 
                    // last valid index in the fileInfos array.
                    var fileDeleteCount = 0;
                    for (var i = fileLimit; i < fileInfos.Count(); ++i)
                    {
                        ++fileDeleteCount;
                        File.Delete(fileInfos.ElementAt(i).FullName);
                    }

                    await Task.Delay(msBetweenCleanups);
                }
                catch (Exception)
                { /* Continue trying to clean up. */}
            }
        }

        // Listen for HTTP connections.
        private static async Task AsyncListener(HttpListener httpListener)
        {
            while (true)
            {
                try
                {
                    var ctx = await httpListener.GetContextAsync();
                    await HandleRequest(ctx);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Encountered exception {e}, continuing.");
                }
            }
        }

        // Call the appropraite handler function based on the URI path.
        static async Task HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var route = ctx.Request.Url.AbsolutePath;
                if (string.Equals(route, "/"))
                {
                    await SendIndexPage(ctx);
                    ctx.Response.Close();
                }
                else if (string.Equals(route, "/image"))
                {
                    await SendImage(ctx);
                    ctx.Response.Close();
                }
                else
                {
                    ctx.Response.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Encountered exception {e}, continuing.");
            }
        }

        // Get the second to latest image in ramfs and send it.
        // We pick the second to make sure we don't try to send an 
        // image that's still being written by the camera process.
        private static async Task SendImage(HttpListenerContext ctx)
        {
            var latestImage = GetRamfsFileInfosSorted().ElementAt(1);
            ctx.Response.OutputStream.Write(
                await File.ReadAllBytesAsync(latestImage.FullName)
            );
        }

        // Returns an enumerable of files in ramfs sorted with the most recently written file at index 0.
        private static IEnumerable<FileInfo> GetRamfsFileInfosSorted()
        {
            var directoryInfo = new DirectoryInfo(Camera.PathToRamfs);
            var fileInfos = directoryInfo.GetFiles();
            return fileInfos.OrderByDescending(f => f.LastWriteTime);
        }

        // Send the HTML for the index page.
        private static async Task SendIndexPage(HttpListenerContext ctx)
        {
            var indexPath = Path.Join("./", "HTML", "Index.html");
            var indexHtml = await File.ReadAllBytesAsync(indexPath);
            ctx.Response.ContentType = "text/html";
            await ctx.Response.OutputStream.WriteAsync(indexHtml);
        }
    }
}
