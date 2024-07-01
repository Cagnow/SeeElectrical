using Memory;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Channels;
using System.Text;
class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll")]
    public static extern int SuspendThread(IntPtr hThread);
    [DllImport("kernel32.dll")]
    public static extern int ResumeThread(IntPtr hThread);
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);


    public const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    public const int PROCESS_QUERY_INFORMATION = 0x0400;
    public const int MEM_COMMIT = 0x00001000;
    public const int PAGE_READWRITE = 0x04;
    public const int PROCESS_WM_READ = 0x0010;
    public const int THREAD_SUSPEND_RESUME = 0x0002;
    public Mem MemLib = new Mem();
  

    static async Task Main(string[] args)
    {
        string executablePath = Assembly.GetExecutingAssembly().Location;
        string executableDirectory = Path.GetDirectoryName(executablePath);
        string configFilePath = $"{executableDirectory}\\config.ini";
        Dictionary<string, string> configParameters = ReadConfigFile(configFilePath);


        string processPath = configParameters.ContainsKey("processPath") ? configParameters["processPath"] : null;
        int waitingTime = configParameters.ContainsKey("waitingTime") ? int.Parse(configParameters["waitingTime"]) : 0;
        string key = configParameters.ContainsKey("key") ? configParameters["key"] : null;

        byte[] keyHex = Encoding.UTF8.GetBytes(key);

        // Convertir le tableau de bytes en chaîne hexadécimale
        string hex = BitConverter.ToString(keyHex).Replace("-", "").ToUpper();
        string Serial = "";
        for (int i = 0; i < hex.Length; i += 2)
        {
            // Extraire une paire de caractères (ou moins si à la fin)
            string paire = hex.Substring(i, Math.Min(2, hex.Length - i));
            Serial += paire + " ";
        }
        Serial = Serial.Trim();
        Mem memLib = new Mem();

        // Démarrer le processus
       ProcessStartInfo startInfo = new ProcessStartInfo(processPath);
       Process targetProcess = Process.Start(startInfo);
        Thread.Sleep(waitingTime);
        int pid = memLib.GetProcIdFromName("See");
        // Attendre que le processus soit prêt

        foreach (ProcessThread thread in targetProcess.Threads)
        {
            IntPtr threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
            if (threadHandle != IntPtr.Zero)
            {
                SuspendThread(threadHandle);
                CloseHandle(threadHandle);
                Console.WriteLine($"{threadHandle}suspendu");
            }
   
        }
       
        memLib.OpenProcess("See");

        // AoB scan and store it in AoBScanResults. We specify our start and end address regions to decrease scan time.
        //IEnumerable<long> AoBScanResults = await memLib.AoBScan("4D 33 4B 4D 46 34 39 33 34 57 32 33 35 39 31 31 41 37 4E 42", true, true);
        IEnumerable<long> AoBScanResults = await memLib.AoBScan(Serial, true, true);

        // get the first found address, store it in the variable SingleAoBScanResult
        long SingleAoBScanResult = AoBScanResults.FirstOrDefault();

            // pop up message box that shows our first result

            // Ex: iterate through each found address. This prints each address in the debug console in Visual Studio.
            foreach (long res in AoBScanResults)
            {
            Console.WriteLine($"{res}");
            byte[] bytes = BitConverter.GetBytes(res);
            string addressmod = res.ToString("X");
            memLib.WriteMemory($"0x{addressmod}", "bytes", "0x35 0x37 0x37 0x39 0x36 0x34 0x35 0x53 0x32 0x53 0x56 0x35 0x35 0x39 0x51 0x51 0x41 0x37 0x4E 0x42");
            }
        foreach (ProcessThread thread in targetProcess.Threads)
        {
            IntPtr threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
            if (threadHandle != IntPtr.Zero)
            {
                ResumeThread(threadHandle);
                CloseHandle(threadHandle);
                Console.WriteLine($"{threadHandle}repris");
            }

        }


    }
    static Dictionary<string, string> ReadConfigFile(string filePath)
    {
        var configParameters = new Dictionary<string, string>();

        try
        {
            // Lire chaque ligne du fichier
            foreach (var line in File.ReadLines(filePath))
            {
                // Ignorer les lignes vides ou les commentaires (commençant par #)
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                // Séparer la ligne en clé et valeur
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Ajouter le paramètre au dictionnaire
                    configParameters[key] = value;
                }
                else
                {
                    Console.WriteLine($"Ligne invalide dans le fichier de configuration: {line}");
                }
            }
        }
        catch (Exception ex)
        {
            // Gérer les erreurs de lecture de fichier
            Console.WriteLine($"Une erreur s'est produite lors de la lecture du fichier de configuration : {ex.Message}");
        }

        return configParameters;
    }
}
