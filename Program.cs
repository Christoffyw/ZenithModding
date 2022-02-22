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
    public class CoreMod
    {
        public string name;
        public string version;
    }

    public class CoreModRepo
    {
        public string name;
        public string owner;
        public string repo;
    }

    internal class Program
    {
        static List<CoreMod> coreMods = new List<CoreMod>();

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
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config"));
            }

            //Load config file
            LoadJson();
            System.Threading.Thread.Sleep(100);

            //Check core mod
            Task t = Task.Run(() => { CheckCoreMods(); return Task.CompletedTask; });

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
                    IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                    //ShowWindow(handle, 6); <---- Minimize window
                    myProcess.WaitForExit();
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
                coreMods = JsonConvert.DeserializeObject<List<CoreMod>>(json);
            }
        }

        static public void WriteJson()
        {
            string json = JsonConvert.SerializeObject(coreMods.ToArray());
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/config/core.json"), json);
        }

        static async public void CheckCoreMods()
        {
            Console.WriteLine("Checking current core mods...");

            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString("https://raw.githubusercontent.com/Christoffyw/ZenithCore/main/coremods.json");
                List<CoreModRepo> coreModRepos = JsonConvert.DeserializeObject<List<CoreModRepo>>(json);

                List<CoreMod> tempCoremods = new List<CoreMod>();

                for(int i = 0; i < coreModRepos.Count; i++)
                {
                    CoreModRepo coreModRepo = coreModRepos[i];

                    //Check Github releases
                    GitHubClient client = new GitHubClient(new ProductHeaderValue("ZenithModding"));
                    IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(coreModRepo.owner, coreModRepo.repo);

                    Version latestGitHubVersion = new Version(releases[0].TagName);
                    Version localVersion = new Version("0.0.0");

                    if (coreMods != null)
                    {
                        if (coreMods.Count() > 0)
                        {
                            foreach(CoreMod coremod in coreMods)
                            {
                                if(coremod.name == coreModRepo.name)
                                {
                                    localVersion = new Version(coremod.version);
                                }
                            }
                        }
                    }


                    int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                    if (versionComparison < 0)
                    {
                        Console.WriteLine($"Found latest release version of {coreModRepo.name}: {latestGitHubVersion}");
                        Console.WriteLine($"{coreModRepo.name} outdated. Updating...");
                        using (var web = new WebClient())
                        {
                            web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, $"{coreModRepo.name}.dll");
                            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll")))
                            {
                                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                            }
                            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins")))
                            {
                                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins"));
                            }
                            File.Move(Path.Combine(Directory.GetCurrentDirectory(), $"{coreModRepo.name}.dll"), Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                        }
                    }
                    else if (versionComparison > 0)
                    {
                        Console.WriteLine($"Invalid {coreModRepo.name}. Redownloading...");
                        using (var web = new WebClient())
                        {
                            web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, $"{coreModRepo.name}.dll");
                            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll")))
                            {
                                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                            }
                            File.Move(Path.Combine(Directory.GetCurrentDirectory(), $"{coreModRepo.name}.dll"), Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                        }
                    }
                    if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx\\plugins\\{coreModRepo.name}.dll")))
                    {
                        Console.WriteLine($"Found latest release version of {coreModRepo.name}: {latestGitHubVersion}");
                        Console.WriteLine($"Downloading {coreModRepo.name}...");
                        using (var web = new WebClient())
                        {
                            web.DownloadFile(releases[0].Assets[0].BrowserDownloadUrl, $"{coreModRepo.name}.dll");
                            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll")))
                            {
                                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                            }
                            File.Move(Path.Combine(Directory.GetCurrentDirectory(), $"{coreModRepo.name}.dll"), Path.Combine(Directory.GetCurrentDirectory(), $"BepInEx/plugins/{coreModRepo.name}.dll"));
                        }
                    }
                    CoreMod coreMod = new CoreMod();
                    coreMod.version = latestGitHubVersion.ToString();
                    coreMod.name = coreModRepo.name;
                    tempCoremods.Add(coreMod);
                }
                coreMods = tempCoremods;
                WriteJson();
            }
        }
    }
}
