/* This is a BBSLink client written in C# for Win32 (and maybe Mono?) platforms.
 * 
 * Written By: Wayne Smith
 * Website: https://www.archaicbinary.net
 * BBS: telnet://bbs.archaicbinary.net
 * 
 * Note: This was written in an hour and a half, if you see OMFG WHY WHY stuff lemme know a better way.
 * 
 * Some functions were COPIED FROM Rick Parrish's TelnetDoor. I am also using the C# RMLib as I do
 * in all my doors I write.
 */

using RandM.RMLib;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ArchaicBinary.BBSLink
{
    class Program
    {
        // This stuff is from DOOR32.SYS
        static int socketHandle = -1;
        static int currentNode = 1;

        // Telnet Port on BBSLink
        static int _Port = 23;

        // BBSLink Stuff
        static string hostURL = "games.bbslink.net";
        static string sysCode = "";
        static string authCode = "";
        static string schemeCode = "";
        static string doorCode = "";

        static readonly Random rng = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        // My stuff to init
        static TcpConnection _Server;
        static RMDoor Door;

        private static void Main(string[] args)
        {
            try
            {
                using (Door = new RMDoor())
                {
                    // How long we can idle in this door until the door kit kicks out the user
                    // In this case we should probably rely on the BBSLink server and/or the door
                    // the user is currently running on BBSLink. So I'll set for 10 minutes.
                    Door.Session.MaxIdle = 600;

                    // Load the TCP socket and the node from DOOR32.SYS into variables
                    socketHandle = Door.DropInfo.SocketHandle;
                    currentNode = Door.DropInfo.Node;

                    // I'm pretty sure we need to strip Line Feeds (LORD does that . (period) stuff)
                    Door.StripLF = true;

                    // Parse CLPs
                    HandleCLPs(Environment.GetCommandLineArgs());

                    // If all CLPs are given then we go to it, there is no sanity checking :(
                    if (!sysCode.Equals("") || !authCode.Equals("") || !schemeCode.Equals("") || !doorCode.Equals(""))
                    {
                        // This is a gamesrv thing, the RecPos starts at ZERO and the Users start at 1
                        // This might throw off the numbers we tell BBSLink <-> Our BBS, but it will always still line up fine
                        int userNumber = Door.DropInfo.RecPos + 1;

                        // That random 6 digit key
                        string xkey = RandomString(6);

                        // Objecttttttssssssssssssss
                        Stream objStream;
                        StreamReader objStreamReader;
                        System.Text.Encoding systemEncoding = System.Text.Encoding.GetEncoding("utf-8");

                        // Build the URL to hit and get data back this returns token variable
                        string strURL = "http://" + hostURL + "/token.php?key=" + xkey;
                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(strURL);
                        webRequest.Method = "GET";
                        HttpWebResponse getResponse = null;
                        getResponse = (HttpWebResponse)webRequest.GetResponse();

                        objStream = getResponse.GetResponseStream();
                        objStreamReader = new StreamReader(objStream, systemEncoding, true);
                        string token = objStreamReader.ReadToEnd();

                        // Build the next URL to hit and get data back this returns testResponse variable
                        strURL = "http://" + hostURL + "/auth.php?key=" + xkey;
                        webRequest = (HttpWebRequest)WebRequest.Create(strURL);
                        webRequest.Method = "GET";
                        webRequest.Headers.Add("X-User", userNumber.ToString());
                        webRequest.Headers.Add("X-System", sysCode);
                        webRequest.Headers.Add("X-Auth", CreateMD5(authCode + token));
                        webRequest.Headers.Add("X-Code", CreateMD5(schemeCode + token));
                        webRequest.Headers.Add("X-Rows", "25");
                        webRequest.Headers.Add("X-Key", xkey);
                        webRequest.Headers.Add("X-Door", doorCode);
                        webRequest.Headers.Add("X-Token", token);
                        getResponse = null;
                        getResponse = (HttpWebResponse)webRequest.GetResponse();

                        objStream = getResponse.GetResponseStream();
                        objStreamReader = new StreamReader(objStream, systemEncoding, true);
                        string testResponse = objStreamReader.ReadToEnd();

                        // Test the testResponse against some strings.
                        if (testResponse == "complete")
                        {
                            Connect();
                        }
                        else
                        {
                            // BOOM!
                            Door.WriteLn("Something went wrong...");
                            Door.WriteLn("Got Back: " + testResponse);
                        }
                    }
                    else
                    {
                        // BOOM! You did not give me what I needed
                        Console.WriteLine("This door requires the switches -D, -W, -X, -Y, and -Z.");
                        Console.WriteLine("All of these must be present.");
                    }

                    // Pause before quitting
                    Door.ClearBuffers();
                    Door.Dispose();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("bbslink.txt", ex.ToString() + Environment.NewLine);
            }
        }

        // This part was taken from Rick Parrish TelnetDoor almost direct copy pase, with some stuff removed.
        private static void Connect()
        {
            _Server = new TelnetConnection();

            if (_Server.Connect(hostURL, _Port))
            {
                bool CanContinue = true;

                if (CanContinue)
                {
                    Door.PipeWrite = false;
                    bool UserAborted = false;
                    while (!UserAborted && Door.Carrier && _Server.Connected)
                    {
                        bool Yield = true;

                        // See if the server sent anything to the client
                        if (_Server.CanRead())
                        {
                            Door.Write(_Server.ReadString());
                            Yield = false;
                        }

                        // See if the client sent anything to the server
                        if (Door.KeyPressed())
                        {
                            string ToSend = "";
                            while (Door.KeyPressed())
                            {
                                byte B = (byte)Door.ReadByte();
                                ToSend += (char)B;
                            }
                            _Server.Write(ToSend);

                            Yield = false;
                        }

                        // See if we need to yield
                        if (Yield) Crt.Delay(1);
                    }
                    Door.PipeWrite = true;
                }
            }
            else
            {
                Door.WriteLn("Looks like the BBSLink server isn't online, please try back later.");
            }
        }

        private static void HandleCLPs(string[] args)
        {
            foreach (string Arg in args)
            {
                if ((Arg.Length >= 2) && ((Arg[0] == '/') || (Arg[0] == '-')))
                {
                    char Key = Arg.ToUpper()[1];
                    string Value = Arg.Substring(2);

                    switch (Key)
                    {
                        case 'W':
                            doorCode = Value;
                            break;
                        case 'X':
                            sysCode = Value;
                            break;
                        case 'Y':
                            authCode = Value;
                            break;
                        case 'Z':
                            schemeCode = Value;
                            break;
                    }
                }
            }
        }

        // Generic random string at any size from chars
        private static string RandomString(int size)
        {
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = chars[rng.Next(chars.Length)];
            }
            return new string(buffer);
        }

        // Generic MD5 stuff
        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
