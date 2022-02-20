using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZenithModding
{
    internal class Program
    {

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);

        static void Main(string[] args)
        {
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
                    Console.WriteLine("Do not close this window!");
                    IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                    ShowWindow(handle, 6);
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
    }
}
