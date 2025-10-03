using System;
using System.IO;
namespace ScoopHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("潇然软件商店Scoop助手");
            Console.WriteLine(new string('=', 22));

            Console.WriteLine("检查系统环境...");

            // 判断系统版本，仅支持 Windows 10 及以上
            if (System.Environment.OSVersion.Version.Major < 10)
            {
                Console.WriteLine($"错误: 仅支持 Windows 10 及以上版本，当前为：{System.Environment.OSVersion.Version}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 判断 Scoop 是否安装
            if (ScoopManager.IsScoopInstalled())
            {
                Console.WriteLine("Scoop 已安装！");
                Console.WriteLine("Scoop 安装目录: " + ScoopManager.GetScoopInstallDirectory());
            }
            else
            {
                Console.WriteLine("Scoop 未安装！");

                // 询问用户是否安装
                Console.Write("是否安装 Scoop? (Y/N，默认 5 秒后自动安装): ");

                bool shouldInstall = false;
                bool userResponded = false;

                // 倒计时 5s，期间可以输入
                int seconds = 5;
                for (int i = seconds; i > 0; i--)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        userResponded = true;
                        if (key.Key == ConsoleKey.Y)
                        {
                            shouldInstall = true;
                            Console.WriteLine("Y");
                            break;
                        }
                        else if (key.Key == ConsoleKey.N)
                        {
                            shouldInstall = false;
                            Console.WriteLine("N");
                            Console.WriteLine("用户取消安装");
                            Console.WriteLine("按任意键退出...");
                            Console.ReadKey();
                            return;
                        }
                    }

                    if (!userResponded)
                    {
                        Console.Write($"\r是否安装 Scoop? (Y/N，默认 {i} 秒后自动安装): ");
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                if (!userResponded)
                {
                    shouldInstall = true;
                    Console.WriteLine("\r" + new string(' ', 60));
                    Console.WriteLine("自动确认安装 Scoop");
                }

                if (shouldInstall)
                {
                    Console.WriteLine("开始安装 Scoop...");

                    Utils.RunPsCommand("(New-Object System.Net.WebClient).DownloadString('http://c.xrgzs.top/c/scoop') | iex");

                    // 刷新环境变量
                    Utils.RefreshEnvironmentVariables();

                    if (ScoopManager.IsScoopInstalled())
                    {
                        Console.WriteLine("Scoop 安装成功！");
                    }
                    else
                    {
                        Console.WriteLine("Scoop 安装失败，请手动检查！");
                        Console.WriteLine("按任意键退出...");
                        Console.ReadKey();
                        return;
                    }
                }
            }


            // 尝试将可执行文件名作为 Base64 解码后的命令
            try
            {
                string currentName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
                if (currentName != null && currentName.StartsWith("ScoopHelper-"))
                {
                    // 只解码 "ScoopHelper-" 后面的部分
                    string base64Part = currentName.Substring("ScoopHelper-".Length);
                    byte[] data = Convert.FromBase64String(base64Part);
                    string decodedCommand = System.Text.Encoding.UTF8.GetString(data);
                    if (!string.IsNullOrWhiteSpace(decodedCommand))
                    {
                        Console.WriteLine("从文件名中检测到命令：" + decodedCommand);
                        args = decodedCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析文件名命令时出错: {ex.Message}");
            }

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "install":
                        // 安装指定的软件
                        if (args.Length < 2)
                        {
                            Console.WriteLine("请指定要安装的软件名称！");
                            break;
                        }

                        // 支持一次安装多个软件
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (!args[i].StartsWith("-"))
                            {
                                ScoopManager.InstallApp(args[i]);
                            }
                        }
                        break;

                    default:
                        // 直接转发给 scoop
                        string command = "scoop " + string.Join(" ", args);
                        Utils.RunPsCommand(command);
                        break;
                }
            }
            else
            {
                Console.WriteLine("错误：没有提供任何命令！");
                Console.WriteLine("用法: ScoopHelper.exe install <app1> <app2> ...");
                Console.WriteLine("      ScoopHelper.exe <scoop-commands>");
                Console.WriteLine("亦或者将 Scoop 命令 Base64 编码（UTF-8）到文件名，如：ScoopHelper-<Base64内容>.exe");
            }

            // 等待用户按键退出
            Console.WriteLine("按任意键或关闭命令窗口退出...");
            Console.ReadKey();
        }

       
    }
}