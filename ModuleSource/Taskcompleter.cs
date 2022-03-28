namespace edpbot;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

public class Taskcompleter
{
    static Random rand;

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

    public void Run(string sessionId, bool stealth, bool debug)
    {
        if (stealth)
        {
            Console.WriteLine("Stealth mode is not available in this version, switching to regular mode...");
        }

        rand = new Random();

        Console.WriteLine("Getting tasks...");
        Thread.Sleep(1000);
        Console.WriteLine("Please do NOT open education perfect!");
        Thread.Sleep(500);

        string tasksresponse = MakeWebRequest("https://services.languageperfect.com/json.rpc?" +
            "target=nz.co.LanguagePerfect.Services.PortalsAsync.App." +
            "AppServicesPortal.GetCurrentTasksForUser",
            "{\"id\":" + rand.Next(1111, 9999) + ",\"method\":\"nz.co.LanguagePerfect.Services." +
            "PortalsAsync.App.AppServicesPortal.GetCurrentTasksForUser\"," +
            "\"params\":[" + sessionId + "]}", "POST");

        if (debug)
        {
            Console.WriteLine(tasksresponse);
        }

        Console.WriteLine();

        string[] taskTypes = new string[3] { "EarnPointsTasks", "LearnContentTasks", "CompleteActivityTasks" };

        string scoreDataSet = "0";
        string taskId = "0";

        int taskCount = 0;
        int subjectId = 0;

        List<string> targetContentTitles = new();
        List<string> taskModules = new();
        List<string> taskLists = new();
        List<string> subjectIds = new();
        List<JToken> tasks = new();

        for (int i = 0; i < taskTypes.Length; i++)
        {
            JObject taskjson = JObject.Parse(tasksresponse);
            JToken taskresult = taskjson["result"];
            JToken taskcontent = taskresult[taskTypes[i]];
            if (taskcontent.HasValues == true)
            {
                if (debug)
                {
                    Console.WriteLine(taskTypes[i]);
                }

                foreach (JToken t in taskcontent)
                {
                    taskCount++;
                    
                    tasks.Add(t);

                    targetContentTitles.Add(t["Name"].ToString());
                }
            }
        }

        Console.WriteLine("Preparing tasks...");

        if (taskCount <= 0)
            Console.WriteLine("You have no tasks.");
        else
        {
            Console.WriteLine("You have " + taskCount + " tasks");

            Thread.Sleep(1000);

            Console.WriteLine("\nid name");

            for (int y = 0; y < targetContentTitles.Count; y++)
            {
                Console.WriteLine(y + " " + targetContentTitles[y]);
            }

            Console.WriteLine("\nPlease enter the id of the tasks you want completed, seperated by commas");

            List<int> doTaskIds = new();

            while (true)
            {
                bool continue_ = false;

                string tasksToCompleteStr = GetInput();
                string[] tasksToCompleteArray = tasksToCompleteStr.Split(",");

                if (debug)
                {
                    Console.WriteLine(tasks.Count);
                }

                foreach (string s in tasksToCompleteArray)
                {
                    try
                    {
                        int temp = Convert.ToInt32(s);

                        if (temp > tasks.Count || temp < 0)
                        {
                            continue_ = true;
                        }
                    }
                    catch
                    {
                        continue_ = true;
                    }
                }

                if (continue_)
                {
                    Console.WriteLine("Sorry i didn't understand that");
                    continue;
                }

                foreach (string s in tasksToCompleteArray)
                {
                    doTaskIds.Add(Convert.ToInt32(s));
                }

                break;
            }

            Console.WriteLine("\nDISCLAIMER: Interactive task completion is not available in this version," +
                " please contact me if you would like support added");

            Console.WriteLine("\nCompleting tasks...");

            if (debug)
            {
                Console.WriteLine(tasks.Count);
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                bool continue_ = true;

                foreach (int h in doTaskIds)
                {
                    if (debug)
                    {
                        Console.WriteLine(h);
                    }

                    if (i == h)
                    {
                        continue_ = false;
                    }
                    else if (debug)
                    {
                        Console.WriteLine(h + "was not equal to " + i);
                    }
                }

                if (continue_)
                {
                    continue;
                }

                if (debug)
                {
                    Console.WriteLine("Attempting " + tasks[i]["Name"]);
                }

                if (tasks[i].HasValues == true)
                {
                    string subjectIdStr = tasks[i]["TargetContentIDs"]["Subjects"][0]["SubjectID"].ToString();

                    subjectId = Convert.ToInt32(subjectIdStr);

                    if (debug)
                    {
                        Console.WriteLine(subjectIdStr);
                    }

                    foreach (JToken list in tasks[i]["TargetContentIDs"]["Lists"])
                    {
                        string basicresponse = MakeWebRequest("https://services.languageperfect.com/json.rpc" +
                "?target=nz.co.LanguagePerfect.Services.PortalsAsync.App." +
                "AppServicesPortal.GetActivityBasicInfo",
                "{\"id\":" + rand.Next(1111, 9999) + " ,\"method\":\"nz.co.LanguagePerfect.Services.PortalsAsync." +
                "App.AppServicesPortal.GetActivityBasicInfo\"," +
                "\"params\":[" + sessionId + " ,{\"ActivityID\":" + list["ListID"].ToString()
                + ",\"ModuleID\":" + list["ModuleID"].ToString() + "}]}", "POST");

                        if (debug)
                        {
                            Console.WriteLine(basicresponse);
                        }

                        JObject basicjson = JObject.Parse(basicresponse);
                        JToken basicresult = basicjson["result"];
                        JToken basicact = basicresult["Activity"];
                        JToken basictype = basicact["ActivityType"];

                        if (debug)
                        {
                            Console.WriteLine(basictype.ToString());
                        }
                        
                        if (basictype.ToString() == "1")
                        {
                            string gamedataresponse = MakeWebRequest("https://services.languageperfect.com/json.rpc?" +
                                "target=nz.co.LanguagePerfect.Services.PortalsAsync." +
                                "App.AppServicesPortal.GetPreGameDataForClassicActivity",
                                "{\"id\":" + rand.Next(1111, 9999) + ",\"method\":\"nz.co.LanguagePerfect." +
                                "Services.PortalsAsync.App.AppServicesPortal.GetPreGameDataForClassicActivity\"," +
                                "\"params\":[" + sessionId + ",{ \"ActivityID\":" + list["ListID"].ToString() + "," +
                                "\"DatasetID\":" + scoreDataSet + "}]}", "POST");

                            if (debug)
                            {
                                Console.WriteLine(gamedataresponse);
                            }

                            JObject gamedatajson = JObject.Parse(gamedataresponse);
                            JToken gamedataresult = gamedatajson["result"];
                            JToken gamedatatranslations = gamedataresult["Translations"];

                            List<string> idList = new();

                            for (int k = 0; k < gamedatatranslations.ToObject<JToken[]>().Length; k++)
                            {
                                idList.Add(gamedatatranslations.ToObject<JToken[]>()[k]["ID"].ToString());
                            }

                            string data = "";

                            foreach (string s in idList)
                            {
                                for (int z = 0; z < 5; z++)
                                {
                                    data += "{\"TranslationID\":" + s + ",\"TranslationDirection\":" + (z + 1) + "," +
                                        "\"NewNumberRight\":2,\"NewNumberWrong\":0,\"NewData\":98},";
                                }
                            }
                          
                            data = data.Remove(data.Length - 1, 1);

                            if (debug)
                            {
                                Console.WriteLine(data);
                            }

                            string saveresponse = MakeWebRequest("https://services.languageperfect.com/json.rpc?" +
                                "target=nz.co.LanguagePerfect.Services.PortalsAsync." +
                                "App.AppServicesPortal.StoreActivityProgress",
                                "{\"id\":" + rand.Next(1111, 9999) + ",\"method\":\"nz.co.LanguagePerfect.Services." +
                                "PortalsAsync.App.AppServicesPortal.StoreActivityProgress\"," +
                                "\"params\":[" + sessionId + ",{\"ActivityTypeId\":1," +
                                "\"BaseLanguageId\":6,\"ClientTimezoneOffsetMinutes\":480," +
                                "\"Data\":[" + data + "],\"ListIds\":[" + list["ListID"].ToString() + "]," +
                                "\"RequestId\":\"e599ec3b-8acd-2ef4-242a-084944a69870\"," +
                                "\"TargetLanguageId\":" + subjectId + ",\"ModuleId\":" + list["ModuleID"].ToString() + "}]}", "POST");

                            if (debug)
                            {
                                Console.WriteLine("{\"id\":" + rand.Next(1111, 9999) + ",\"method\":\"nz.co.LanguagePerfect.Services." +
                                "PortalsAsync.App.AppServicesPortal.StoreActivityProgress\"," +
                                "\"params\":[" + sessionId + ",{\"ActivityTypeId\":1," +
                                "\"BaseLanguageId\":6,\"ClientTimezoneOffsetMinutes\":480," +
                                "\"Data\":[" + data + "],\"ListIds\":[" + list["ListID"].ToString() + "]," +
                                "\"RequestId\":\"e599ec3b-8acd-2ef4-242a-084944a69870\"," +
                                "\"TargetLanguageId\":" + subjectId + ",\"ModuleId\":" + list["ModuleID"].ToString() + "}]}");
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine("Ending module...");
    }
}
