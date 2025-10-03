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
            var (installedExitCode, installedVersion) = Utils.RunPsCommand($"(scoop info {appName}).Installed");

            if (installedExitCode == 0 && !string.IsNullOrWhiteSpace(installedVersion))
            {
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