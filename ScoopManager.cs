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

        // ��װ Scoop
        public static void InstallScoop()
        {
            // ѯ���û��Ƿ�װ
            Console.Write("�Ƿ�װ Scoop? (Y/N��Ĭ�� 5 ����Զ���װ): ");

            bool shouldInstall = false;
            bool userResponded = false;

            // ����ʱ 5s���ڼ��������
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
                        Console.WriteLine("�û�ȡ����װ");
                        Console.WriteLine("��������˳�...");
                        Console.ReadKey();
                        Environment.Exit(1);
                    }
                }

                if (!userResponded)
                {
                    Console.Write($"\r�Ƿ�װ Scoop? (Y/N��Ĭ�� {i} ����Զ���װ): ");
                    System.Threading.Thread.Sleep(1000);
                }
            }

            if (!userResponded)
            {
                shouldInstall = true;
                Console.WriteLine("\r" + new string(' ', 60));
                Console.WriteLine("�Զ�ȷ�ϰ�װ Scoop");
            }

            if (shouldInstall)
            {
                Console.WriteLine("��ʼ��װ Scoop...");

                Utils.RunPsCommand("(New-Object System.Net.WebClient).DownloadString('http://c.xrgzs.top/c/scoop') | iex");

                // ˢ�»�������
                Utils.RefreshEnvironmentVariables();


                Console.WriteLine("Scoop ����������ˢ�¡�");
            }

            // ����Ƿ�װ�ɹ�
            if (ScoopManager.IsScoopInstalled())
            {
                Console.WriteLine("Scoop ��װ�ɹ���");
            }
            else
            {
                Console.WriteLine("Scoop ��װʧ�ܣ����ֶ���飡");
                Console.WriteLine("��������˳�...");
                Console.ReadKey();
                Environment.Exit(1);
            }
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
            var (installedExitCode, installedVersions) = Utils.RunPsCommand($"(scoop info {appName}).Installed");

            if (installedExitCode == 0 && !string.IsNullOrWhiteSpace(installedVersions))
            {
                // ��ȡ�Ѱ�װ�汾�������ж���汾���û��з��ָ���ȡ���һ����
                var versions = installedVersions.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var installedVersion = versions[versions.Length - 1].Trim();

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