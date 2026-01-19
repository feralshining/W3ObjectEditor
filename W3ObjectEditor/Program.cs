using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W3ObjectEditor
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                EnsureConsole();
                return RunConsole(args);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            return 0;
        }

        private static int RunConsole(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return 1;
            }

            string input = args[0];
            string output = args[1];

            try
            {
                ConvertFiles(input, output);
                Console.WriteLine($"Saved: {output}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void ConvertFiles(string input, string output)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Input and output paths are required.");

            if (!File.Exists(input))
                throw new FileNotFoundException("Input file not found.", input);

            bool inputIsCsv = IsCsv(input);
            bool outputIsCsv = IsCsv(output);
            bool inputIsW3 = IsW3ObjectFile(input);
            bool outputIsW3 = IsW3ObjectFile(output);

            if (inputIsCsv && outputIsW3)
            {
                var dt = W3ObjectCsvHandler.Load(input);
                W3ObjectFileHandler.Save(output, dt);
                return;
            }

            if (inputIsW3 && outputIsCsv)
            {
                var dt = W3ObjectFileHandler.Load(input);
                W3ObjectCsvHandler.Save(output, dt);
                return;
            }

            throw new ArgumentException("Unsupported conversion. Use .w3u/.w3a/.w3h/.w3t <-> .csv.");
        }

        private static bool IsCsv(string path)
        {
            return string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsW3ObjectFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".w3u" || ext == ".w3a" || ext == ".w3h" || ext == ".w3t";
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: W3ObjectEditor.exe <input> <output>");
            Console.WriteLine("Example: W3ObjectEditor.exe originalWar3map.w3t originalw3t.csv");
            Console.WriteLine("Example: W3ObjectEditor.exe w3t.csv War3map.w3t");
        }

        private static void EnsureConsole()
        {
            const int AttachParentProcess = -1;
            if (!AttachConsole(AttachParentProcess))
            {
                AllocConsole();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
    }
}
