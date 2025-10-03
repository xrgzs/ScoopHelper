using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text;

namespace ScoopHelper
{
    public static class ScoopManager
    {
        // 缓存 Scoop 安装目录
        private static string _scoopInstallDirectory;

        // 获取 Scoop 安装目录（优先环境变量，其次默认路径），不存在则返回 null
        public static string GetScoopInstallDirectory()
        {
            if (_scoopInstallDirectory != null)
                return _scoopInstallDirectory;

            string scoopEnv = Environment.GetEnvironmentVariable("SCOOP", EnvironmentVariableTarget.User);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string defaultScoopDir = Path.Combine(userProfile, "scoop");

            string scoopDir = !string.IsNullOrEmpty(scoopEnv) ? scoopEnv : defaultScoopDir;
            _scoopInstallDirectory = Directory.Exists(scoopDir) ? scoopDir : null;
            return _scoopInstallDirectory;
        }

        // 判断 Scoop 是否安装
        public static bool IsScoopInstalled()
        {
            bool directoryExists = GetScoopInstallDirectory() != null;
            bool scoopInPath = Utils.Which("scoop") != null;

            return directoryExists && scoopInPath;
        }

        // 安装 Scoop
        public static void InstallScoop()
        {
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
                        Environment.Exit(1);
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


                Console.WriteLine("Scoop 环境变量已刷新。");
            }

            // 检查是否安装成功
            if (ScoopManager.IsScoopInstalled())
            {
                Console.WriteLine("Scoop 安装成功！");
            }
            else
            {
                Console.WriteLine("Scoop 安装失败，请手动检查！");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }


        // 安装指定软件
        public static bool InstallApp(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                Console.WriteLine("应用名不能为空。");
                return false;
            }

            if (!IsScoopInstalled())
            {
                Console.WriteLine("错误: Scoop 未安装！");
                return false;
            }

            // 更新 Scoop
            Console.WriteLine("正在更新 Scoop...");
            var (updateExitCode, _) = Utils.RunPsCommand("scoop update");
            if (updateExitCode != 0)
            {
                Console.WriteLine("Scoop 更新失败，但将继续尝试安装...");
            }
            else
            {
                Console.WriteLine("Scoop 更新完成！");
            }

            // 检查应用是否存在
            Console.WriteLine($"正在检查 {appName} 应用是否存在...");
            var (exitCode, latestVersion) = Utils.RunPsCommand($"(scoop info {appName}).Version");

            if (exitCode != 0 || string.IsNullOrWhiteSpace(latestVersion))
            {
                Console.WriteLine($"应用 {appName} 不存在或无法获取信息，无法安装！");
                return false;
            }

            Console.WriteLine($"{appName} 应用存在，最新版本: {latestVersion}");

            // 检查应用是否已安装
            Console.WriteLine("正在检查应用是否已安装...");
            var (installedExitCode, installedVersions) = Utils.RunPsCommand($"(scoop info {appName}).Installed");

            if (installedExitCode == 0 && !string.IsNullOrWhiteSpace(installedVersions))
            {
                // 提取已安装版本（可能有多个版本，用换行符分隔，取最后一个）
                var versions = installedVersions.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var installedVersion = versions[versions.Length - 1].Trim();

                // 应用已安装
                if (installedVersion == latestVersion)
                {
                    Console.WriteLine($"{appName} 已安装版本: {installedVersion}，为最新版本，无需安装或升级！");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{appName} 已安装版本: {installedVersion}，但不是最新版本: {latestVersion}，正在升级...");
                    var (upgradeExitCode, _) = Utils.RunPsCommand($"scoop update {appName}");

                    if (upgradeExitCode == 0)
                    {
                        Console.WriteLine($"{appName} 升级成功！");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"{appName} 升级失败！");
                        return false;
                    }
                }
            }
            else
            {
                // 应用未安装
                Console.WriteLine($"{appName} 未安装，正在安装...");
                var (installExitCode, _) = Utils.RunPsCommand($"scoop install {appName}");

                if (installExitCode == 0)
                {
                    Console.WriteLine($"{appName} 安装成功！");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{appName} 安装失败！");
                    return false;
                }
            }
        }
    }
}