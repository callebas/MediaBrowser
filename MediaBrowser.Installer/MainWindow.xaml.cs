﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Linq;
using Ionic.Zip;
using MediaBrowser.Installer.Code;
using ServiceStack.Text;
using IWshRuntimeLibrary;

namespace MediaBrowser.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected PackageVersionClass PackageClass = PackageVersionClass.Release;
        protected Version PackageVersion = new Version(10,0,0,0);
        protected string PackageName = "MBServer";
        protected string RootSuffix = "-Server";
        protected string TargetExe = "MediaBrowser.ServerApplication.exe";
        protected string FriendlyName = "Media Browser Server";
        protected string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MediaBrowser-Server");

        protected bool SystemClosing = false;

        protected string TempLocation = Path.Combine(Path.GetTempPath(), "MediaBrowser");

        public MainWindow()
        {
            GetArgs();
            InitializeComponent();
            DoInstall();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!SystemClosing && MessageBox.Show("Cancel Installation - Are you sure?", "Cancel", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            ClearTempLocation(TempLocation);
            base.OnClosing(e);
        }

        protected void SystemClose(string message = null)
        {
            if (message != null)
            {
                MessageBox.Show(message, "Error");
            }
            SystemClosing = true;
            this.Close();
        }

        protected void GetArgs()
        {
            var args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;

            if (args == null || args.ActivationData == null || args.ActivationData.Length <= 0) return;
            var url = new Uri(args.ActivationData[0], UriKind.Absolute);

            var parameters = HttpUtility.ParseQueryString(url.Query);

            // fill in our arguments if there
            PackageName = parameters["package"] ?? "MBServer";
            PackageClass = (PackageVersionClass)Enum.Parse(typeof(PackageVersionClass), parameters["class"] ?? "Release");
            PackageVersion = new Version(parameters["version"].ValueOrDefault("10.0.0.0"));
            RootSuffix = parameters["suffix"] ?? "-Server";
            TargetExe = parameters["target"] ?? "MediaBrowser.ServerApplication.exe";
            FriendlyName = parameters["name"] ?? PackageName;
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MediaBrowser" + RootSuffix);

        }

        /// <summary>
        /// Execute the install process
        /// </summary>
        /// <returns></returns>
        protected async Task DoInstall()
        {
            lblStatus.Content = "Downloading "+FriendlyName+"...";
            dlAnimation.StartAnimation();
            prgProgress.Value = 0;
            prgProgress.Visibility = Visibility.Visible;

            // Download
            var archive = await DownloadPackage();
            dlAnimation.StopAnimation();
            prgProgress.Visibility = btnCancel.Visibility = Visibility.Hidden;

            // Extract
            lblStatus.Content = "Extracting Package...";
            try 
            {
                ExtractPackage(archive);
            }
            catch (Exception e)
            {
                SystemClose("Error Extracting - " + e.GetType().FullName + "\n\n" + e.Message);
            }

            // Create shortcut
            var fullPath = Path.Combine(RootPath, "System", TargetExe);
            try
            {
                CreateShortcut(fullPath);
            }
            catch (Exception e)
            {
                SystemClose("Error Creating Shortcut - "+e.GetType().FullName+"\n\n"+e.Message);
            }

            // And run
            try
            {
                Process.Start(fullPath);
            }
            catch (Exception e)
            {
                SystemClose("Error Executing - "+fullPath+ " "+e.GetType().FullName+"\n\n"+e.Message);
            }

            SystemClose();

        }

        /// <summary>
        /// Download our specified package to an archive in a temp location
        /// </summary>
        /// <returns>The fully qualified name of the downloaded package</returns>
        protected async Task<string> DownloadPackage()
        {
            using (var client = new WebClient())
            {
                try
                {
                    // get the package information for the server
                    var json = await client.DownloadStringTaskAsync("http://www.mb3admin.com/admin/service/package/retrieveAll?name="+PackageName);
                    var packages = JsonSerializer.DeserializeFromString<List<PackageInfo>>(json);

                    var version = packages[0].versions.Where(v => v.classification == PackageClass).OrderByDescending(v => v.version).FirstOrDefault(v => v.version <= PackageVersion);
                    if (version == null)
                    {
                        SystemClose("Could not locate download package.  Aborting.");
                        return null;
                    }
                    var archiveFile = Path.Combine(PrepareTempLocation(), version.targetFilename);

                    // setup download progress and download the package
                    client.DownloadProgressChanged += DownloadProgressChanged;
                    await client.DownloadFileTaskAsync(version.sourceUrl, archiveFile);
                    return archiveFile;
                }
                catch (Exception e)
                {
                    SystemClose(e.GetType().FullName + "\n\n" + e.Message);
                }
            }
        return "";

        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            prgProgress.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// Extract the provided archive to our program root
        /// It is assumed the archive is a zip file relative to that root (with all necessary sub-folders)
        /// </summary>
        /// <param name="archive"></param>
        protected void ExtractPackage(string archive)
        {
            using (var fileStream = System.IO.File.OpenRead(archive))
            {
                using (var zipFile = ZipFile.Read(fileStream))
                {
                    zipFile.ExtractAll(RootPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }

        }

        /// <summary>
        /// Create a shortcut in the current user's start menu
        ///  Only do current user to avoid need for admin elevation
        /// </summary>
        /// <param name="targetExe"></param>
        protected void CreateShortcut(string targetExe)
        {
            // get path to all users start menu
            var shell = new WshShell();
            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Media Browser");
            if (!Directory.Exists(startMenu)) Directory.CreateDirectory(startMenu);
            var myShortcut = (IWshShortcut)shell.CreateShortcut(Path.Combine(startMenu, "Media Browser Server.lnk"));
            myShortcut.TargetPath = targetExe;
            myShortcut.Description = "Run " + FriendlyName;
            myShortcut.Save(); 
            
        }

        /// <summary>
        /// Prepare a temporary location to download to
        /// </summary>
        /// <returns>The path to the temporary location</returns>
        protected string PrepareTempLocation()
        {
            ClearTempLocation(TempLocation);
            Directory.CreateDirectory(TempLocation);
            return TempLocation;
        }

        /// <summary>
        /// Clear out (delete recursively) the supplied temp location
        /// </summary>
        /// <param name="location"></param>
        protected void ClearTempLocation(string location)
        {
            if (Directory.Exists(location))
            {
                Directory.Delete(location, true);
            }
        }
    }
}