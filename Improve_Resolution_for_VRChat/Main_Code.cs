using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Improve_Resolution_for_VRChat
{
    public class Main_Code
    {
        private class Queue
        {
            public string From_File = "";
            public string To_File = "";
            public string Format = "";
            public byte Mode = 2;
            public bool IsCopyOnly = false;
            public Queue(string from_File, string to_File, string format, byte mode, bool IsCopyOnly)
            {
                From_File = from_File;
                To_File = to_File;
                Format = format;
                Mode = mode;
            }
        }
        public static string? Error_Message { get; private set; }
        private static readonly List<Queue> Queues = new();
        private static string? Special_Path = "";
        private static async void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("直接起動しないで～！壊れちゃうのぉ～   キーを押すと終了します。");
                Console.ReadKey();
                return;
            }
            Console.WriteLine();
            Console.WriteLine("---VRChat解像度向上 V1.0---");
            Console.WriteLine();
            Special_Path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            byte Mode = byte.Parse(args[0]);
            string[] From_Files = args[1].Split('|');
            for (int i = 2; i < 5; i++)
            {
                if (!File.Exists(Special_Path + "\\models\\realesr-animevideov3-x" + i + ".bin") || !File.Exists(Special_Path + "\\models\\realesr-animevideov3-x" + i + ".param"))
                {
                    Console.WriteLine("realesr-animevideov3-x" + i + "が存在しませんでした。");
                    Console.WriteLine("キーを押すと終了します。");
                    Console.CursorVisible = true;
                    Console.ReadKey();
                    return;
                }
            }
            if (!File.Exists(Special_Path + "\\realesrgan-ncnn-vulkan.exe"))
            {
                Console.WriteLine("realesrgan-ncnn-vulkan.exeが存在しませんでした。");
                Console.WriteLine("キーを押すと終了します。");
                Console.CursorVisible = true;
                Console.ReadKey();
                return;
            }
            for (int i = 0; i < From_Files.Length; i++)
            {
                string? aaa = Path.GetDirectoryName(From_Files[i]);
                string Dir = Directory.GetCurrentDirectory() + (aaa == null ? "\\" : "\\" + aaa + "\\");
                string To_File = Dir + Path.GetFileNameWithoutExtension(From_Files[i]) + "_x" + Mode + Path.GetExtension(From_Files[i]);
                Image image = Image.Load(From_Files[i]);
                byte Now_Mode = Mode;
                bool IsCopyOnly = false;
                while (true)
                {
                    if (image.Width * Now_Mode <= 7680 || image.Height * Now_Mode <= 4320)
                        break;
                    if (image.Width * (Now_Mode - 1) < 7680 || image.Height * (Now_Mode - 1) < 4320)
                        break;
                    if (Now_Mode == 2)
                    {
                        IsCopyOnly = true;
                        break;
                    }
                    Now_Mode--;
                }
                Queues.Add(new Queue(From_Files[i], To_File, Path.GetExtension(From_Files[i]).Replace(".", ""), Now_Mode));
            }
            bool HasError = false;
            bool IsComplete = false;
            Task task = Task.Run(() =>
            {
                foreach (Queue q in Queues)
                {
                    if (!File.Exists(q.From_File))
                    {
                        Error_Message = "画像ファイルが存在しませんでした。";
                        HasError = true;
                        break;
                    }
                    Console.WriteLine("'" + Path.GetFileName(q.From_File) + "'の高解像度化を開始します...");
                    Console.Write("処理しています...");
                    Process process = new();
                    ProcessStartInfo processInfo = new()
                    {
                        FileName = Special_Path + "\\realesrgan-ncnn-vulkan.exe",
                        Arguments = "-s " + Mode + " -i \"" + q.From_File + "\" -o \"" + q.To_File + "\" -f " + q.Format,
                        WorkingDirectory = Directory.GetCurrentDirectory(),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    process.StartInfo = processInfo;
                    if (process == null)
                    {
                        Error_Message = "正常にrealesrgan-ncnn-vulkan.exeを実行できませんでした。";
                        HasError = true;
                        break;
                    }
                    process.Start();
                    process.WaitForExit();
                    process.Close();
                    if (!File.Exists(q.To_File))
                    {
                        Error_Message = "正常に画像が生成されませんでした。";
                        HasError = true;
                        break;
                    }
                    Delete_Text("処理しています...");
                    Console.WriteLine("完了しました。");
                    Console.WriteLine();
                }
                IsComplete = true;
            });
            while (!IsComplete)
                Thread.Sleep(1000 / 60);
            if (HasError)
            {
                Console.WriteLine("エラー:" + Error_Message);
                Console.WriteLine("キーを押すと終了します。");
                Console.CursorVisible = true;
                Console.ReadKey();
                Queues.Clear();
                return;
            }
            Queues.Clear();
            Console.WriteLine("全ての処理が完了しました。ソフトは自動で閉じられます。");
            Thread.Sleep(1500);
        }
        private static void Delete_Text(string Text)
        {
            byte[] Text_Bytes = Encoding.UTF8.GetBytes(Text);
            for (int Number = 0; Number < Text_Bytes.Length; Number++)
                Console.Write("\b \b");
        }
    }
}