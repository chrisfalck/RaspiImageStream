using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace CSLiveStreamServer
{
    public class Camera : IDisposable
    {
        private readonly Process _camerProc;
        public const string PathToRamfs = "/home/pi/ramfs";

        public Camera()
        {
            // Files of the format img_XXXX.jpg .
            var outfileFormat = "img_%04d.jpg";

            // Args for the raspistill command.
            var cameraArgs = new List<string>
            {
                "-w",  "1920", // Width.
                "-h",  "1080", // Height.
                "-q",  "10", // 90% compression.
                "-e",  "jpg", // JPEG encoding.
                "-t",  "999999999999999999", // Run as long as possible.
                "-tl", "0", // Take pictures as fast as possible (typically one every 30-40 ms).
                "-o",  Path.Join(PathToRamfs, outfileFormat) // Write images to the ramfs directory.
            };

            // Configure the process that will run raspistill to capture image data.
            var startInfo = new ProcessStartInfo()
            {
                FileName = "/usr/bin/raspistill",
                Arguments = string.Join(' ', cameraArgs)            
            };

            _camerProc = Process.Start(startInfo);

            // Allow the camera to warm up.
            Thread.Sleep(2000);
        }

        /// <summary>
        /// If we don't send the process a kill signal when Camera goes out
        /// of scope, raspistill will enter a bad state and the raspberry pi
        /// must be restarted before it can be used again.
        /// </summary>
        public void Dispose()
        {
            _camerProc.Kill();
            _camerProc.WaitForExit();
            _camerProc.Dispose();
        }
    }
}