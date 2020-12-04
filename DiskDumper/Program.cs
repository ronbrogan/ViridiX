using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ViridiX.Linguist;
using ViridiX.Linguist.FileSystem;

namespace DiskDumper
{
    class Program
    {
        static XboxFileSystem xbfs;

        static void Main(string[] args)
        {
            string ipString = "";

            if(args.Length == 1)
            {
                ipString = args[0];
            }

            IPAddress ip = null;

            while (ip == null)
            {
                if(IPAddress.TryParse(ipString, out ip))
                {
                    break;
                }
                else
                {
                    if(ipString != "")
                        Console.Error.WriteLine($"Unable to parse {ipString} as an IP Address");

                    Console.Write("Enter Xbox IP: ");
                    ipString = Console.ReadLine();
                }
            }

            var xbox = new Xbox(new ConsoleLogger());
            xbox.Connect(ip, ViridiX.Linguist.Network.XboxConnectionOptions.PerformanceMode);
            xbox.CommandSession.SendBufferSize = 80192;
            xbox.CommandSession.ReceiveBufferSize = 80192;
            xbfs = xbox.FileSystem;

            var localRoot = Path.Combine(Directory.GetCurrentDirectory(), ip.ToString());

            foreach(var drive in xbfs.DriveLetters)
            {
                DownloadDrive(drive, localRoot);
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static void DownloadDrive(string drive, string local)
        {
            var directory = xbfs.GetDirectoryList(drive);
            var localDriveDir = Path.Combine(local, drive.Substring(0, 1));

            Directory.CreateDirectory(localDriveDir);
            DownloadDirectory(directory, localDriveDir);
        }

        static void DownloadDirectory(List<XboxFileInformation> directory, string local)
        {
            foreach(var entry in directory)
            {
                if(entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    var subdir = xbfs.GetDirectoryList(entry.FullName);
                    var localSubdir = Path.Combine(local, entry.Name);
                    Directory.CreateDirectory(localSubdir);
                    DownloadDirectory(subdir, localSubdir);
                }
                else
                {
                    DownloadFile(entry, local);
                }
            }
        }

        static void DownloadFile(XboxFileInformation file, string local)
        {
            var localName = Path.Combine(local, file.Name);

            if (File.Exists(localName))
                return;

            xbfs.DownloadFile(file.FullName, localName);
        }
    }
}
