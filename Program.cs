#region Usings

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

#endregion

/// <summary>
/// Namespace which houses Education Perfect Bot
/// </summary>

namespace edpbot
{
    /*
     * Education Perfect Bot, by Matthew Bullard
     *
     * This program was intended to automatically complete 
     * Education Perfect (EP) homework so that anyone who uses it
     * wouldn't have to worry about doing it themselves.
     * 
     * This program also has the benefit of working on the
     * three most common operating systems, without requiring
     * an open browser.
     * 
     * Instead, this program exploits a lack of required authentication
     * of web requests made to the EP server.
     * 
     * This means that you can play games, watch youtube,
     * complete other homework or whatever you do, while
     * all your EP tasks are completed automatically.
    */

    using static Utility;
    using static Updater;
    using static InputController;

    /// <summary>
    /// Main class of this program
    /// </summary>

    class Program
    {
        private static string? sessionId;

        public static Dictionary<int, int> subjClass;

        public static bool allowInput, debugging;

        public static string? assemblyDirectory;

        public static string OSString = "";

        static void Main(string[] args)
        {
            Console.WriteLine(HashText("alexander.kervin@student.education.wa.edu.au", "alexander.kervin@student.education.wa.edu.au", SHA1.Create()));
            subjClass = new();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OSString = "Mac";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OSString = "Windows";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OSString = "Linux";
            }
            if (OSString == "")
            {
                CloseProgramFromOS();
            }

            Thread InputCheck = new(MonitorInput);
            InputCheck.Start();

            rand = new Random();
            string local_v = "vUnknown";

            assemblyDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);

            try
            {
                local_v = File.ReadAllText(assemblyDirectory + "/edpbot/data/VERSION.txt");
            }
            catch
            {
                Console.WriteLine("Initialising files...");
                Thread.Sleep(500);
                Directory.CreateDirectory(assemblyDirectory + "/edpbot/data/");

                File.WriteAllText(assemblyDirectory + "/edpbot/data/VERSION.txt",
                    "vUnknown");

                Directory.CreateDirectory(assemblyDirectory + "/edpbot/modules/");

                Thread.Sleep(500);

                Console.WriteLine();
            }

            RunUpdateScan(local_v);

            local_v = File.ReadAllText(assemblyDirectory + "/edpbot/data/VERSION.txt");

            Console.WriteLine($"Education Perfect Bot {local_v}");

            Thread.Sleep(500);

            string username = "";
            string password = "";

            bool usingLoggedCreds = false;

            Console.WriteLine($"Please login to your education perfect account{Environment.NewLine}");

            while (true)
            {
                if (Logger.CanLoadCredentials())
                {
                    while (true)
                    {
                        string logCreds = GetInput("Load saved credentials? ");

                        int userLoadCreds = GetBoolIntFromString(logCreds);

                        if (userLoadCreds == 0)
                        {
                            break;
                        }

                        if (userLoadCreds == 2)
                        {
                            Console.WriteLine("Sorry, i didn't understand that");
                            continue;
                        }
                        
                        username = Logger.LoadCredentials()[0];
                        password = Logger.LoadCredentials()[1];
                        usingLoggedCreds = true;

                        goto login;
                    }
                }

                allowInput = true;
                username = GetInput("Username: ");
                Console.Write("Password: ");
                password = string.Empty;

                ConsoleKey key;

                do
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        Console.Write("\b \b");
                        password = password[0..^1];
                        continue;
                    }

                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Console.Write("*");
                        password += keyInfo.KeyChar;
                    }
                }
                while
                (
                    key != ConsoleKey.Enter
                );

                Console.WriteLine();

            login:

                Console.WriteLine("Logging in...");

                try
                {
                    string loginresponse =
                        MakeWebRequest("https://services.languageperfect.com/json.rpc?target=EP.API.AppLoginPortal.PasswordLogin",
                        "{\"id\":" + rand.Next(1111, 9999) + ",\"method\":\"EP.API.AppLoginPortal.PasswordLogin\"," +
                        "\"params\":[{\"Username\":\"" + username + "\",\"Password\":\"" + password + "\",\"AppId\":6," +
                        "\"AppVersion\":\"OSX 5.0.60982\",\"DeviceInformation\":{\"BrowserName\":\"Safari\"," +
                        "\"BrowserVersion\":\"15.0\",\"OperatingSystemName\":\"macOS\"," +
                        "\"OperatingSystemVersion\":\"10.15.7\",\"PlatformType\":\"desktop\"," +
                        "\"UserAgent\":\"Mozilla / 5.0(Macintosh; Intel Mac OS X 10_15_7) AppleWebKit / 605.1.15(KHTML," +
                        " like Gecko) Version / 15.0 Safari / 605.1.15\",\"ScreenWidth\":1440,\"ScreenHeight\":900}," +
                        "\"ClientFlags\":[\"ALLOW_INACTIVE_USER\"]}]}", "POST");

                    JObject loginjson = JObject.Parse(loginresponse);
                    JToken loginresult = loginjson["result"];
                    JToken jsonsessionid = loginresult["SessionId"];
                    string tempsessionid = jsonsessionid.ToString();

                    foreach (JToken t in loginresult["Subjects"])
                    {
                        try
                        {
                            Convert.ToInt32(t["ClassId"].ToString());
                        }
                        catch
                        {
                            continue;
                        }

                        subjClass.Add(Convert.ToInt32(t["SubjectId"].ToString()), Convert.ToInt32(t["ClassId"].ToString()));
                    }

                    if (tempsessionid == "0")
                    {
                        if (!usingLoggedCreds)
                        {
                            Console.WriteLine("Invalid username or password, please try again");

                            continue;
                        }

                        Console.WriteLine("Clearing invalid credentials...");
                        Logger.DeleteCredentials();
                        usingLoggedCreds = false;

                        continue;
                    }

                    sessionId = tempsessionid;

                    if (UserIsAuthenticated(username))
                    {
                        Console.WriteLine("Login successful!");

                        Console.WriteLine();

                        if (!usingLoggedCreds)
                        {
                            while (true)
                            {
                                string logCreds = GetInput("Save credentials? ");

                                int logCredsBool = GetBoolIntFromString(logCreds);

                                if (logCredsBool == 1)
                                {
                                    Logger.LogCredentials(username, password);
                                    Console.WriteLine("Credentials saved");
                                    Thread.Sleep(200);
                                    break;
                                }

                                if (logCredsBool == 0)
                                {
                                    break;
                                }

                                Console.WriteLine("Sorry, i didn't understand that");
                                continue;
                            }
                        }

                        break;
                    }

                    Console.WriteLine("This account is not whitelisted, if you have a login code, please enter it here: ");

                enterlogincode:

                    string inputCode = GetInput("Login code: ");
                    if (HashText(inputCode, "ASDFZXCVQWER8", SHA1.Create()) == "OWuoyu8u897/AyzusKQktLASh6c=")
                    {
                        Console.WriteLine("Your account has been whitelisted");

                        string existingAuthed = "";

                        if (File.Exists(assemblyDirectory + "/edpbot/data/AUTHED.txt"))
                        {
                            existingAuthed = File.ReadAllText(assemblyDirectory + "/edpbot/data/AUTHED.txt");
                        }

                        File.WriteAllLines(assemblyDirectory + "/edpbot/data/AUTHED.txt", new string[1] {existingAuthed +
                            GetRandomString(200) + HashText(username, username, SHA1.Create()) + GetRandomString(200)});

                        Thread.Sleep(500);
                        goto login;
                    }

                    string retry = GetInput("Try again? ");

                    if (GetBoolIntFromString(retry) == 1)
                    {
                        goto enterlogincode;
                    }

                    Console.WriteLine("Going back to login...");
                    Thread.Sleep(500);

                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Please try again later");
                    continue;
                }
            }

            Console.WriteLine("Command Line:");
            Console.WriteLine("Enter \"help\" to see a list of commands");
            while (true)
            {
                string input = GetInput(">>> ");
                if (input.StartsWith("explain"))
                {
                    string[] inputwords = input.Split();
                    if (inputwords.Length < 2)
                    {
                        Console.WriteLine("You need to enter a command to get the explanation for, usage: explain <command>");
                        continue;
                    }

                    string command = inputwords[1];
                    switch (command)
                    {
                        case "help":
                            Console.WriteLine("Provides a list of commands");
                            continue;
                        case "stealth":
                            Console.WriteLine("Stealth mode is slower than regular" +
                                " mode but makes it much harder" +
                                " for anyone to realise that you are cheating");
                            continue;
                        case "listmodules":
                            Console.WriteLine("Lists available modules to run");
                            continue;
                        case "explain":
                            Console.WriteLine("Provides additional explanation and help for using commands");
                            continue;
                        case "runmodule":
                            Console.WriteLine("Runs the provided module");
                            continue;
                        case "quit":
                            Console.WriteLine("Ends the execution of the program");
                            continue;
                        case "debug":
                            Console.WriteLine("Debugging mode enabled");
                            debugging = true;
                            continue;
                        default:
                            Console.WriteLine($"Cannot get explaination for {command}, as that command does not exist");
                            continue;
                    }
                }

                if (input.StartsWith("runmodule"))
                {
                    string[] inputwords = input.Split();
                    if (inputwords.Length < 2)
                    {
                        Console.WriteLine("You need to enter a module to run, usage: runmodule <module>. You can see a list of modules" +
                            " by typing in the \"listmodules\" command");
                        continue;
                    }

                    string moduleinput = inputwords[1];

                    try
                    {
                        string module = string.Concat(moduleinput[0].ToString().ToUpper(), moduleinput.AsSpan(1));
                        RunModule(module);
                        continue;
                    }
                    catch
                    {
                        Console.WriteLine($"Cannot run module {moduleinput}, as that module does not exist. Run the command" +
                                $" \"listmodules\" to see all available modules");
                        continue;
                    }
                }

                switch (input)
                {
                    case "help":
                        Console.WriteLine($"Available commands: {Environment.NewLine}help{Environment.NewLine}" +
                            $"listmodules{Environment.NewLine}explain{Environment.NewLine}runmodule" +
                            $"{Environment.NewLine}quit");
                        break;
                    case "listmodules":

                        string[] files = Directory.GetFiles(assemblyDirectory + "/edpbot/modules/", "*.dll");

                        foreach (string f in files)
                        {
                            string fName = Path.GetFileName(f);
                            Console.WriteLine(fName[0..^4]);
                        }

                        break;
                    case "quit":
                        Quit();
                        break;
                    default:
                        Console.WriteLine($"Unknown command: {input}, enter \"help\" to see a list of commands");
                        break;
                }
            }
        }

        /// <summary>
        /// Runs the module specified
        /// </summary>
        /// <param name="MODULENAME">Name of module to run</param>

        private static void RunModule(string MODULENAME)
        {
            Assembly assembly;

            assembly = Assembly.LoadFrom(assemblyDirectory + "/edpbot/modules/"
                + MODULENAME[0].ToString().ToUpper() + MODULENAME[1..] + ".dll");

            foreach (Type t in assembly.GetTypes())
            {
                try
                {
                    MethodInfo methodInfo = t.GetMethod("Run");

                    if (!File.Exists(assemblyDirectory + "/edpbot/data/FIRSTMODULE.txt"))
                    {
                        Console.WriteLine("Because this is your first time here, I will use some time to explain");
                        Thread.Sleep(1000);
                        Console.WriteLine("Whenever you run a module, you have the option of running it in Stealth mode");
                        Thread.Sleep(1000);
                        Console.WriteLine("Stealth mode is much slower than regular mode");
                        Thread.Sleep(1000);
                        Console.WriteLine("However, it removes all chances of anyone knowing you are cheating");
                        Thread.Sleep(1000);
                        Console.WriteLine("Stealth mode is the suggested mode for all modules");
                        Thread.Sleep(1000);
                        Console.WriteLine("Because it is so much slower, it is reccomended that you leave this program running in the" +
                            " background for a while");
                        Thread.Sleep(1000);

                        File.WriteAllLines(assemblyDirectory + "/edpbot/data/FIRSTMODULE.txt",
                            new string[1] { GetRandomString(500) });
                    }

                    ParameterInfo[] parameters = methodInfo.GetParameters();

                    if (parameters.Length > 2)
                    {
                        bool useStealth = false;

                        string stealthInput = GetInput("Use stealth mode? (Use command \"explain stealth\" for help) ");

                        if (stealthInput == "d")
                        {
                            debugging = true;
                        }

                        if (GetBoolIntFromString(stealthInput) == 1)
                        {
                            useStealth = true;
                        }

                        string mode = useStealth ? "Stealth" : "regular";

                        Console.WriteLine("Running module " + MODULENAME + " in " + mode + " mode...");
                        Thread.Sleep(500);

                        var obj = Activator.CreateInstance(t);

                        methodInfo.Invoke(obj, new object[3] { sessionId, useStealth, debugging });

                        return;
                    }

                    Console.WriteLine("Running module " + MODULENAME + "...");
                    Thread.Sleep(500);
                    
                    methodInfo.Invoke(t, new object[2] { sessionId, debugging });
                }

                catch (Exception e) { }
            }
        }
    }

    /// <summary>
    /// A class filled with utility functions, to be used in conjunction with the main Program class
    /// </summary>

    class Utility
    {
        public static Random rand;

        /// <summary>
        /// Checks if client is authenticated to run program
        /// </summary>
        /// <param name="username">Username of client</param>
        /// <returns>Whether the user is authenticated to continue</returns>

        public static bool UserIsAuthenticated(string username)
        {
            string authedUsers = ReadFromCloudFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/AUTHEDUSERS.txt");
            string userHash = HashText(username, username, SHA1.Create());

            if (authedUsers.Contains(userHash))
            {
                return true;
            }

            if (File.Exists(Program.assemblyDirectory + "/edpbot/data/AUTHED.txt"))
            {
                if (Compare(File.ReadAllText(Program.assemblyDirectory + "/edpbot/data/AUTHED.txt"),
                    new string[1] { HashText(username, username, SHA1.Create()) }))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetBoolIntFromString(string text)
        {
            if (Compare(text, new string[5] { "n", "no", "nup", "nope", "noo" }))
            {
                return 0;
            }

            if (Compare(text, new string[5] { "y", "ye", "yep", "yup", "yee" }))
            {
                return 1;
            }

            return 2;
        }

        /// <summary>
        /// Gets a random string to make it more difficult to edit saved data
        /// </summary>
        /// <param name="length">Length of random string</param>
        /// <returns>Random string of specified length</returns>

        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Hashes a string with SHA1
        /// </summary>
        /// <param name="text">String to hash</param>
        /// <param name="salt">Salt to use when hashing</param>
        /// <param name="hasher">SHA1 instance</param>
        /// <returns>Hashed string</returns>

        public static string HashText(string text, string salt, SHA1 hasher)
        {
            byte[] textWithSaltBytes = Encoding.UTF8.GetBytes(string.Concat(text, salt));
            byte[] hashedBytes = hasher.ComputeHash(textWithSaltBytes);
            hasher.Clear();
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Provides a more dynamic and shorthand way of getting input from the client
        /// </summary>
        /// <param name="inputtext">Text to write before receiving input</param>
        /// <returns>Input</returns>

        public static string GetInput(string inputtext)
        {
            Console.Write(inputtext);
            string input = Console.ReadLine().ToLower();
            return input;
        }

        public static string GetInput()
        {
            Console.Write("");
            string input = Console.ReadLine().ToLower();
            return input;
        }

        /// <summary>
        /// Make a web request
        /// </summary>
        /// <param name="URL">URL to send request to</param>
        /// <param name="PAYLOAD">Data sent to server in POST/PUT requests</param>
        /// <param name="METHOD">Method to use</param>
        /// <returns>Response from URL</returns>

        public static string MakeWebRequest(string URL, string PAYLOAD, string METHOD)
        {
            WebRequest request = WebRequest.Create(URL);
            request.ContentType = "application/json";
            request.Method = METHOD;
            if (PAYLOAD != "")
            {
                string formParams = PAYLOAD;
                byte[] bytes = Encoding.ASCII.GetBytes(formParams);
                request.ContentLength = bytes.Length;
                using Stream os = request.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
            }
            WebResponse resp = request.GetResponse();
            string response;
            using (StreamReader sr = new(resp.GetResponseStream()))
            {
                response = sr.ReadToEnd();
            }
            return response;
        }

        public static void DownloadFile(string url, string path)
        {
            using WebClient wc = new();
            wc.DownloadFile(url, path);
        }

        /// <summary>
        /// Reads text from a file on the internet
        /// </summary>
        /// <param name="url">URL of the file to read from</param>
        /// <returns>File contents</returns>

        public static string ReadFromCloudFile(string url)
        {
            string content = "";

            using (WebClient wc = new())
            {
                wc.Headers.Add("a", "a");
                try
                {
                    Stream stream = wc.OpenRead(url);
                    StreamReader reader = new(stream);
                    content = reader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return content;
        }

        /// <summary>
        /// Provides an easy way to check that a string is the same/similar to a list of other strings
        /// </summary>
        /// <param name="SUBJECT">Main string to compare</param>
        /// <param name="COMPARATORS">List of strings to compare the main string to</param>
        /// <returns>Boolean value representing if the main string was the same/similar to any of the comparators</returns>

        public static bool Compare(string SUBJECT, string[] COMPARATORS)
        {
            for (int i = 0; i < COMPARATORS.Length; i++)
            {
                if (COMPARATORS[i] == SUBJECT || COMPARATORS[i].Contains(SUBJECT)
                    || SUBJECT.Contains(COMPARATORS[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Quits the program
        /// </summary>

        public static void Quit()
        {
            Console.WriteLine("Closing Program...");
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles closing the program when client's operating system is not supported
        /// </summary>

        public static void CloseProgramFromOS()
        {
            Console.WriteLine("This program does not currently support your operating system. If you would like to have this issue" +
                     " resolved, please contact me at mraem.rmcp.mb@gmail.com and i will get back to you as soon as i can.");
            Thread.Sleep(1);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            Quit();
        }
    }

    /// <summary>
    /// A class filled with functions required for updating the program
    /// </summary>

    class Updater
    {
        /// <summary>
        /// Checks for available updates and then updates the program if possible
        /// </summary>
        /// <param name="LOCAL_V">Version of program stored locally</param>

        public static void RunUpdateScan(string LOCAL_V)
        {
            Console.WriteLine($"Checking for updates...");

            string cloud_v = ReadFromCloudFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/VERSION.txt");
            if (!Compare(cloud_v, new string[1] { LOCAL_V }))
            {
                Console.WriteLine("Updates found!");
                Thread.Sleep(500);

                Update();

                return;
            }

            Console.WriteLine($"Program is up to date{Environment.NewLine}");
        }

        /// <summary>
        /// Updates modules and version number
        /// </summary>

        private static void Update()
        {
            Console.WriteLine("Updating...");

            string[] files = Directory.GetFiles(Program.assemblyDirectory + "/edpbot/modules/", "*.dll");

            foreach (string f in files)
            {
                File.Delete(f);
            }

            string moduleList = ReadFromCloudFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/MODULES.txt");

            string[] availableModules = moduleList.Split();

            availableModules = availableModules.SkipLast(1).ToArray();

            try
            {
                foreach (string m in availableModules)
                {
                    DownloadFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/" + Program.OSString + "/" + m + ".dll",
                        Program.assemblyDirectory + "/edpbot/modules/" + m + ".dll");
                }
            }
            catch
            {
                CloseProgramFromOS();
            }

            File.Delete(Program.assemblyDirectory + "/edpbot/data/VERSION.txt");

            DownloadFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/VERSION.txt",
                Program.assemblyDirectory + "/edpbot/data/VERSION.txt");

            Console.WriteLine("Update completed");
            Console.WriteLine($"CHANGELOG:{Environment.NewLine}");
            Console.WriteLine(ReadFromCloudFile("https://raw.githubusercontent.com/TheFlamingCrab/edpbot/main/CHANGELOG.txt"));
        }
    }

    /// <summary>
    /// The class responsible for most of the communication between the program and the file system
    /// </summary>

    class Logger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DATA"></param>
        /// <param name="MODULENAME"></param>

        public static void LogStealthData(string DATA, string MODULENAME)
        {
            string filePath = Program.assemblyDirectory + "/edpbot/data/stealthdata" + MODULENAME + ".txt";

            File.WriteAllText(filePath, DATA);
        }

        /// <summary>
        /// Encrypts and saves supplied credentials
        /// </summary>
        /// <param name="username">EP Username for client</param>
        /// <param name="password">EP Password for client</param>

        public static void LogCredentials(string username, string password)
        {
            string directoryPath = Program.assemblyDirectory + "/edpbot/data/creds/";

            string hashedUsername = EncryptString(username);
            string hashedPassword = EncryptString(password);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(directoryPath + "1.txt", hashedUsername);
            File.WriteAllText(directoryPath + "2.txt", hashedPassword);
        }

        /// <summary>
        /// Removes saved credentials
        /// </summary>

        public static void DeleteCredentials()
        {
            if (File.Exists(Program.assemblyDirectory + "/edpbot/data/creds/1.txt"))
            {
                File.Delete(Program.assemblyDirectory + "/edpbot/data/creds/1.txt");
                File.Delete(Program.assemblyDirectory + "/edpbot/data/creds/2.txt");
            }
        }

        /// <summary>
        /// Loads saved credentials if available
        /// </summary>
        /// <returns>A list of strings containing the username and password</returns>

        public static string[] LoadCredentials()
        {
            if (File.Exists(Program.assemblyDirectory + "/edpbot/data/creds/1.txt") &&
                File.Exists(Program.assemblyDirectory + "/edpbot/data/creds/2.txt"))
            {
                try
                {
                    string username = DecryptString(File.ReadAllText(Program.assemblyDirectory + "/edpbot/data/creds/1.txt"));
                    string password = DecryptString(File.ReadAllText(Program.assemblyDirectory + "/edpbot/data/creds/2.txt"));

                    return new string[2] { username, password };
                }
                catch { }
            }

            return new string[2] { "", "" };
        }

        /// <summary>
        /// Checks whether saved credentials are available
        /// </summary>
        /// <returns>A boolean value displaying whether credentials are available</returns>

        public static bool CanLoadCredentials()
        {
            if (File.Exists(Program.assemblyDirectory + "/edpbot/data/creds/1.txt") &&
                File.Exists(Program.assemblyDirectory + "/edpbot/data/creds/2.txt"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Decrypts a string from base64
        /// </summary>
        /// <param name="encrString">String to decrypt</param>
        /// <returns>Decrypted string</returns>

        private static string DecryptString(string encrString)
        {
            byte[] b;
            string decrypted;

            try
            {
                b = Convert.FromBase64String(encrString);
                decrypted = System.Text.ASCIIEncoding.ASCII.GetString(b);
            }
            catch
            {
                decrypted = "";
            }

            return decrypted;
        }

        /// <summary>
        /// Encrypts a string with base64
        /// </summary>
        /// <param name="strEncrypted">String to encrypt</param>
        /// <returns>Encrypted string</returns>

        private static string EncryptString(string strEncrypted)
        {
            byte[] b = System.Text.ASCIIEncoding.ASCII.GetBytes(strEncrypted);
            string encrypted = Convert.ToBase64String(b);
            return encrypted;
        }
    }

    /// <summary>
    /// The class which contains input related functions
    /// </summary>

    class InputController
    {
        /// <summary>
        /// Prevents user input when Program.allowInput variable is false
        /// </summary>

        public static void MonitorInput()
        {
            while (true)
            {
                if (Console.KeyAvailable && !Program.allowInput)
                {
                    ConsoleKeyInfo info = Console.ReadKey(intercept: true);

                    if (Program.allowInput)
                    {
                        Console.Write(info.Key);
                    }
                }
            }
        }
    }
}