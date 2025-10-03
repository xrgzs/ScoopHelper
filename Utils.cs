using System;
using System.IO;
using System.Text;

namespace ScoopHelper
{
    internal class Utils
    {
        // 刷新当前进程的环境变量
        public static void RefreshEnvironmentVariables()
        {
            try
            {
                // 从系统和用户级别重新读取 PATH
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                string combinedPath = machinePath + ";" + userPath;

                // 更新当前进程的 PATH
                Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告: 刷新环境变量时出错: {ex.Message}");
            }
        }

        // 实现类似 which 命令的功能，查找可执行文件路径
        public static string Which(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return null;

            // 从环境变量获取所有PATH路径
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return null;

            string[] paths = pathEnv.Split(Path.PathSeparator);

            // 获取可执行文件扩展名
            string[] extensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';')
                ?? new string[] { ".exe", ".bat", ".cmd", ".com" };

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    if (Path.HasExtension(command))
                    {
                        // 如果命令已经有扩展名，直接检查
                        var fullPath = Path.Combine(path, command);
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                    else
                    {
                        // 尝试常见的可执行文件扩展名
                        foreach (var ext in extensions)
                        {
                            if (string.IsNullOrEmpty(ext))
                                continue;

                            var fullPath = Path.Combine(path, command + ext);
                            if (File.Exists(fullPath))
                                return fullPath;
                        }
                    }
                }
                catch
                {
                    // 忽略无效路径错误
                    continue;
                }
            }
            return null;
        }

        // PowerShell 命令通用封装，实时输出内容，返回错误码和输出
        public static (int exitCode, string output) RunPsCommand(string command)
        {

            Console.WriteLine("执行命令: " + command);

            string psPath = Which("pwsh") ?? Which("powershell");

            if (string.IsNullOrEmpty(psPath))
            {
                Console.WriteLine("错误: 未找到 PowerShell！");
                return (-1, string.Empty);
            }

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = psPath;
            process.StartInfo.Arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -Command \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            var outputBuilder = new StringBuilder();
            //var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine("> " + e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine("× " + e.Data);
                    //errorBuilder.AppendLine(e.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return (process.ExitCode, outputBuilder.ToString().Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令时发生异常: {ex.Message}");
                return (-1, string.Empty);
            }
            finally
            {
                process?.Dispose();
            }
        }


        // 生成是否XXX+倒计时提示，如果 5 秒内无响应则返回true
        // 返回 true 表示用户确认，false 表示取消
        public static bool PromptYN(string message, int timeoutSeconds = 5)
        {
            bool userConfirmed = false;
            bool userResponded = false;
            int currentCount = timeoutSeconds;

            // 初始提示
            string basePrompt = $"是否{message}? (Y/N，默认 {{0}} 秒后自动{message}): ";
            Console.Write(string.Format(basePrompt, currentCount));

            // 倒计时循环，每100ms检查一次输入
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds && !userResponded)
            {
                // 检测键盘输入
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    userResponded = true;

                    if (key.Key == ConsoleKey.Y)
                    {
                        userConfirmed = true;
                        Console.WriteLine("Y");
                        break;
                    }
                    else if (key.Key == ConsoleKey.N)
                    {
                        userConfirmed = false;
                        Console.WriteLine("N");
                        break;
                    }
                    else
                    {
                        // 无效输入，给用户明确反馈
                        userResponded = false;

                        // 清除当前行
                        //Console.Write("\r" + new string(' ', Math.Min(Console.WindowWidth - 1, 80)) + "\r");

                        // 显示错误提示
                        Console.Write($"无效输入 '{key.KeyChar}'，请按 Y 或 N: ");

                        // 给用户时间看到错误提示
                        System.Threading.Thread.Sleep(800);

                        // 重置开始时间，给用户重新输入的机会
                        startTime = DateTime.Now;
                        currentCount = timeoutSeconds;
                        continue;
                    }
                }

                // 更新倒计时显示（每秒更新一次）
                int newCount = timeoutSeconds - (int)(DateTime.Now - startTime).TotalSeconds;
                if (newCount != currentCount && newCount > 0)
                {
                    currentCount = newCount;
                    Console.Write($"\r{string.Format(basePrompt, currentCount)}");
                }

                System.Threading.Thread.Sleep(100); // 减少延迟，提高响应性
            }

            // 如果超时未响应，默认确认
            if (!userResponded)
            {
                userConfirmed = true;
                // 清除当前行并显示结果
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                Console.WriteLine($"没有检测到输入，自动{message}");
            }

            return userConfirmed;
        }

        // 生成按任意键继续+倒计时提示，如果 5 秒内无响应则返回true
        public static bool PromptAnyKey(string message, int timeoutSeconds = 5)
        {
            bool keyPressed = false;
            int currentCount = timeoutSeconds;

            // 初始提示
            string basePrompt = $"按任意键{message}（默认 {{0}} 秒后自动{message}）：";
            Console.Write(string.Format(basePrompt, currentCount));

            // 倒计时循环，每100ms检查一次输入
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds && !keyPressed)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    keyPressed = true;
                    // 清除当前行并显示结果
                    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                    Console.WriteLine("按键已被检测，继续执行...");
                    break;
                }

                // 更新倒计时显示（每秒更新一次）
                int newCount = timeoutSeconds - (int)(DateTime.Now - startTime).TotalSeconds;
                if (newCount != currentCount && newCount > 0)
                {
                    currentCount = newCount;
                    Console.Write($"\r{string.Format(basePrompt, currentCount)}");
                }

                System.Threading.Thread.Sleep(100); // 减少延迟，提高响应性
            }

            if (!keyPressed)
            {
                // 清除当前行并显示结果
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                Console.WriteLine("没有检测到按键，自动继续执行...");
            }

            return keyPressed;
        }
    }
}
