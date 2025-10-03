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
                Console.WriteLine($"警告：刷新环境变量时出错: {ex.Message}");
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
                    Console.WriteLine(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine("错误: " + e.Data);
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

    }
}
