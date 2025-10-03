using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text;

namespace ScoopHelper
{
    public static class ScoopManager
    {
        // ���� Scoop ��װĿ¼
        private static string _scoopInstallDirectory;

        // ��ȡ Scoop ��װĿ¼�����Ȼ������������Ĭ��·�������������򷵻� null
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

        // �ж� Scoop �Ƿ�װ
        public static bool IsScoopInstalled()
        {
            bool directoryExists = GetScoopInstallDirectory() != null;
            bool scoopInPath = Utils.Which("scoop") != null;

            return directoryExists && scoopInPath;
        }
      
        // ��װָ�����
        public static bool InstallApp(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                Console.WriteLine("Ӧ��������Ϊ�ա�");
                return false;
            }

            if (!IsScoopInstalled())
            {
                Console.WriteLine("����: Scoop δ��װ��");
                return false;
            }

            // ���� Scoop
            Console.WriteLine("���ڸ��� Scoop...");
            var (updateExitCode, _) = Utils.RunPsCommand("scoop update");
            if (updateExitCode != 0)
            {
                Console.WriteLine("Scoop ����ʧ�ܣ������������԰�װ...");
            }
            else
            {
                Console.WriteLine("Scoop ������ɣ�");
            }

            // ���Ӧ���Ƿ����
            Console.WriteLine($"���ڼ�� {appName} Ӧ���Ƿ����...");
            var (exitCode, latestVersion) = Utils.RunPsCommand($"(scoop info {appName}).Version");

            if (exitCode != 0 || string.IsNullOrWhiteSpace(latestVersion))
            {
                Console.WriteLine($"Ӧ�� {appName} �����ڻ��޷���ȡ��Ϣ���޷���װ��");
                return false;
            }

            Console.WriteLine($"{appName} Ӧ�ô��ڣ����°汾: {latestVersion}");

            // ���Ӧ���Ƿ��Ѱ�װ
            Console.WriteLine("���ڼ��Ӧ���Ƿ��Ѱ�װ...");
            var (installedExitCode, installedVersion) = Utils.RunPsCommand($"(scoop info {appName}).Installed");

            if (installedExitCode == 0 && !string.IsNullOrWhiteSpace(installedVersion))
            {
                // Ӧ���Ѱ�װ
                if (installedVersion == latestVersion)
                {
                    Console.WriteLine($"{appName} �Ѱ�װ�汾: {installedVersion}��Ϊ���°汾�����谲װ��������");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{appName} �Ѱ�װ�汾: {installedVersion}�����������°汾: {latestVersion}����������...");
                    var (upgradeExitCode, _) = Utils.RunPsCommand($"scoop update {appName}");

                    if (upgradeExitCode == 0)
                    {
                        Console.WriteLine($"{appName} �����ɹ���");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"{appName} ����ʧ�ܣ�");
                        return false;
                    }
                }
            }
            else
            {
                // Ӧ��δ��װ
                Console.WriteLine($"{appName} δ��װ�����ڰ�װ...");
                var (installExitCode, _) = Utils.RunPsCommand($"scoop install {appName}");

                if (installExitCode == 0)
                {
                    Console.WriteLine($"{appName} ��װ�ɹ���");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{appName} ��װʧ�ܣ�");
                    return false;
                }
            }
        }
    }
}