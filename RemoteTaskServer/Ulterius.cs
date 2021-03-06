﻿#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using UlteriusServer.Api;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Settings;
using UlteriusServer.WebCams;
using UlteriusServer.WebServer;

#endregion

namespace UlteriusServer
{
    public class Ulterius
    {
        private SystemService systemService;
        private bool isService;

        public void Start(bool serviceMode = false)
        {
            isService = serviceMode;

            if (Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1)
            {
                Process.GetCurrentProcess().Kill();
            }
            if (!Directory.Exists(AppEnvironment.DataPath))
            {
                Directory.CreateDirectory(AppEnvironment.DataPath);
            }
           
            //Fix screensize issues for Screen Share
            if (Tools.RunningPlatform() == Tools.Platform.Windows && Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            Setup();
        }

        /// <summary>
        ///     Starts various parts of the server than loop to keep everything alive.
        /// </summary>
        private void Setup()
        {
            if (Tools.RunningPlatform() == Tools.Platform.Windows)
            {
                try
                {
                    HideWindow();
                }
                catch
                {
                    //Failed to hide window, probably in service mode.
                }
            }


             Console.WriteLine("Creating settings");
           
            var settings = Config.Load();

        
           
            Console.WriteLine("Configuring up server");
            Tools.ConfigureServer();
          
          
        
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
            var useTerminal = settings.Terminal.AllowTerminal;
            var useWebServer = settings.WebServer.ToggleWebServer;
            var useWebCams = settings.Webcams.UseWebcams;
            if (useWebCams)
            {
                Console.WriteLine("Loading Webcams");
                WebCamManager.LoadWebcams();
            }
            if (useWebServer)
            {
                Console.WriteLine("Setting up HTTP Server");
                HttpServer.Setup();
            }
            systemService = new SystemService();
            Console.WriteLine("Creating system service");
            systemService.Start();
            UlteriusApiServer.RunningAsService = Tools.RunningAsService();
            UlteriusApiServer.Start();
           
            if (useTerminal)
            {
                Console.WriteLine("Starting Terminal API");
                TerminalManagerServer.Start();
            }
            try
            {
                var useUpnp = settings.Network.UpnpEnabled;
                if (useUpnp)
                {
                    Console.WriteLine("Trying to forward ports");
                    Tools.ForwardPorts();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to forward ports");
            }
        }


        /// <summary>
        ///     Hide the console window from the user
        /// </summary>
        private void HideWindow()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, Hide);
        }

        #region win32

        private const int Hide = 0;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();


        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        #endregion
    }
}