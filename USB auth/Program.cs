using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using Renci.SshNet;
using System.Threading;
using System.Xml;
//This program has to be built from source in order to change variables located below.
namespace USB_auth
{
    class Program
    {
        static void Main(string[] args)
        {
            string Host = ""; //Hostname
            int Port = 22; //SFTP Port number
            String RemoteFileName = "/var/www/pass/passwd.kdbx"; //Full path to password database on remote server. Make sure the file is named passwd.kdbx. If you want to change this edit the code below
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"passwd\\"); //Directory password database will be stored in on local computer. I recommend leaving this alone unless you know what you're doing!
            String Username = ""; //Username. Setting password can be done further down. If password is already set just press enter when it prompts for password.
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var userFileName = "passwd"; //Filename
            var fileName = Path.Combine(dir, userFileName + ".kdbx"); //Filename with path and extension. DO NOT MODIFY!
            Console.WriteLine("Found existing file: " + fileName);
            if (File.Exists(fileName))
            {
                string backupFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "passwdbackup\\" + userFileName + ".kdbx");
                string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"passwdbackup\\");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                File.Copy(fileName, backupFile, true);
                Console.WriteLine("Database backup succeeded! If file becomes broken do not rerun this program. Backup stored at: \n" + backupFile + "\n");
            }
            if (File.Exists(fileName))
            {
                Console.WriteLine("Replacing file: " + fileName);
            }

            String LocalDestinationFilename = fileName; //Variable override. You can force local download folder here! Only modify if you know what you're doing!

            string pass = ""; //Password. Leave blank unless you want to automatic authentication. Remember it can only be set from source and software like ILSpy can view the password in plaintext!
            bool tryagain = true; //If you want the program not to ask for a password and use the above variable "pass" instead set to false.
            if (tryagain == false)
            {
                using (var sftp = new SftpClient(Host, Port, Username, pass))
                {
                    sftp.Connect();

                    using (var file = File.OpenWrite(LocalDestinationFilename))
                    {
                        sftp.DownloadFile(RemoteFileName, file);
                    }

                    sftp.Disconnect();
                }
            }
            while (tryagain)
            {
                try
                {
                    Console.Write("Password: ");
                    ConsoleKeyInfo key;
                    pass = "";
                    do
                    {
                        key = Console.ReadKey(true);

                        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        {
                            pass += key.KeyChar;
                            Console.Write("*");
                        }
                        else
                        {
                            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                            {
                                pass = pass.Substring(0, (pass.Length - 1));
                                Console.Write("\b \b");
                            }
                        }
                    }
                    while (key.Key != ConsoleKey.Enter);
                    Console.WriteLine();
                    tryagain = false;
                    using (var sftp = new SftpClient(Host, Port, Username, pass))
                    {
                        sftp.Connect();

                        using (var file = File.OpenWrite(LocalDestinationFilename))
                        {
                            sftp.DownloadFile(RemoteFileName, file);
                        }

                        sftp.Disconnect();
                    }
                }
                catch (Renci.SshNet.Common.SshAuthenticationException)
                {
                    Console.WriteLine("Wrong password!");
                    tryagain = true;
                }
            }
            Console.WriteLine("Transfer complete!");
            Console.WriteLine("Opening KeePass!");
            try
            {
                string xmlFile = "KeePass.config.xml"; //KeePass Configuration file name
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlFile);
                xmlDoc.SelectSingleNode("Configuration/Application/LastUsedFile/Path").InnerText = fileName; //Change configuration file to automatically open the downloaded database.
                xmlDoc.Save(xmlFile);


                Process.Start("KeePass.exe");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("Could not open KeePass! Make sure that KeePass is installed in the same directory as this exe!");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Keepass configuration file not found! Make sure that KeePass is installed in the same directory as this exe!");
            }

            Thread.Sleep(3000);

        }
    }
}
