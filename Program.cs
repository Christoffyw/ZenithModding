using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;

namespace ZenithModding
{
    public class ZenithCore
    {
        public string name;
        public string version;
    }

    internal class Program
    {
        static List<ZenithCore> items = new List<ZenithCore>();

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);

        static void Main(string[] args)
        {
            //Dependency checks
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UnityClient@Windows.exe")) ||
                !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "GameAssembly.dll")))
            {
                Console.WriteLine("Game file(s) not found.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Process.GetCurrentProcess().Kill();
            }
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx")))
            {
                Console.WriteLine("BepInEx not found.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Process.GetCurrentProcess().Kill();
            }

            //Load config file
            LoadJson();
            System.Threading.Thread.Sleep(100);
            
            //Check core mod
            Task t = Task.Run(async () =>
            {
                Console.WriteLine("Checking current core mods...");

                //Check Github releases
                GitHubClient client = new GitHubClient(new ProductHeaderValue("Christoffyw"));
                IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("Christoffyw", "ZenithCore");

                Version latestGitHubVersion = new Version(releases[0].TagName);
                Version localVersion = new Version("0.0.0");

                if(items != null)
                {
                    if (items.Count() > 0)
                        localVersion = new Version(items.First().version);
                }


                int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                if (versionComparison < 0)
                {
                    Console.WriteLine($"Found latest release version {latestGitHubVersion}");
                    Console.WriteLine("Coremods outdated. Updating...");
                    using (var web = new WebClient())
                    {
                        web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, "ZenithCore.dll");
                        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll")))
                        {
                            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                        }
                        File.Move(Path.Combine(Directory.GetCurrentDirectory(), "ZenithCore.dll"), Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                    }
                }
                else if (versionComparison > 0)
                {
                    Console.WriteLine("Invalid coremods. Redownloading...");
                    using (var web = new WebClient())
                    {
                        web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, "ZenithCore.dll");
                        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll")))
                        {
                            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                        }
                        File.Move(Path.Combine(Directory.GetCurrentDirectory(), "ZenithCore.dll"), Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                    }
                }
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx\\plugins\\ZenithCore.dll")))
                {
                    Console.WriteLine($"Found latest release version {latestGitHubVersion}");
                    Console.WriteLine("Downloading core mods...");
                    using (var web = new WebClient())
                    {
                        web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, "ZenithCore.dll");
                        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll")))
                        {
                            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                        }
                        File.Move(Path.Combine(Directory.GetCurrentDirectory(), "ZenithCore.dll"), Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/ZenithCore.dll"));
                    }
                }

                items = new List<ZenithCore>();
                ZenithCore zenCore = new ZenithCore();
                zenCore.version = latestGitHubVersion.ToString();
                zenCore.name = "ZenithCore";
                items.Add(zenCore);
                WriteJson();
            });

            t.Wait();

            //Deal with doorstop
            Console.WriteLine("Checking doorstop...");
            if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "winhttp_alt.dll")))
            {
                Console.WriteLine("Reverting doorstop...");
                File.Move(Path.Combine(Directory.GetCurrentDirectory(), "winhttp_alt.dll"), Path.Combine(Directory.GetCurrentDirectory(), "winhttp.dll"));
            }

            //Launch zenith
            Console.WriteLine("Attempting to launch Zenith...");
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "UnityClient@Windows.exe");
                    myProcess.Start();
                    System.Threading.Thread.Sleep(100);
                    Console.WriteLine("Updating doorstop...");
                    File.Move(Path.Combine(Directory.GetCurrentDirectory(), "winhttp.dll"), Path.Combine(Directory.GetCurrentDirectory(), "winhttp_alt.dll"));
                    System.Threading.Thread.Sleep(100);
                    Console.WriteLine("DO NOT CLOSE THIS WINDOW!");
                    IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                    //ShowWindow(handle, 6); <---- Minimize window
                    myProcess.WaitForExit();
                    Console.WriteLine("Updating doorstop...");
                    File.Move(Path.Combine(Directory.GetCurrentDirectory(), "winhttp_alt.dll"), Path.Combine(Directory.GetCurrentDirectory(), "winhttp.dll"));
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static public void LoadJson()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config/core.json")))
            {
                WriteJson();
            }
            using (StreamReader r = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config/core.json")))
            {
                Console.WriteLine("Reading config...");
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<ZenithCore>>(json);
            }
        }

        static public void WriteJson()
        {
            string json = JsonConvert.SerializeObject(items.ToArray());
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config/core.json"), json);
        }
    }
}
