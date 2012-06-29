using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using WatiN.Core;

namespace SimSimi
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SetUnsafeHeaderParsing();
            var client = new WebClient();

            client.Headers.Add("Accept-Charset", "UTF-8,*;q=0.5");
            client.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4");


            while (true)
            {
                var path = Guid.NewGuid() + ".txt";
                try
                {
                    Chat(client, path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    if (new FileInfo(path).Length == 0)
                        File.Delete(path);
                }
                catch (Exception)
                { }
            }
        }

        private static void Chat(WebClient client, string path)
        {
            using (File.Create(path))
            {
            }

            using (var browser = new IE("http://widget.chatvdvoem.ru/iframe?mode=production&height=600"))
            {
                browser.Link(Find.ById("chat_start")).Click();

                var lastAnswer = string.Empty;
                var answer = string.Empty;

                while (true)
                {
                    var i = 0;

                    while (string.Equals(lastAnswer, answer,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        Thread.Sleep(7000);

                        i++;

                        if (i > 4)
                        {
                            browser.ForceClose();
                            return;
                        }

                        var froms = browser.Elements.Filter(p => p.ClassName == "messageFrom");

                        if (froms.Count == 0)
                            continue;

                        answer = froms.Last().Text;

                        answer = answer.Substring(6);
                    }

                    lastAnswer = answer;

                    if (BlackListed(answer))
                        break;

                    var question = GetAnswer(client, answer);

                    File.AppendAllLines(path, new[] { answer, question });

                    browser.TextField(Find.ByName("text")).TypeText(question);

                    Thread.Sleep(2000);

                    browser.Button(Find.ById("text_send")).Click();
                }

                browser.ForceClose();
            }
        }

        private static bool BlackListed(string answer)
        {
            return
                string.IsNullOrEmpty(answer) ||
                answer.Contains("для вирта") ||
                answer.Contains("http") ||
                answer.Contains("Я разделил твое тело");
        }

        private static string GetAnswer(WebClient client, string q)
        {
            var query = HttpUtility.UrlEncode(q).Replace("+", "%2B");
            var json = client.DownloadString(
                    string.Format("http://www.simsimi.com/func/req?msg={0}&lc=ru", query));

            var jss = new JavaScriptSerializer();
            var dict = jss.Deserialize<Dictionary<string, string>>(json);
            var anscp = dict["sentence_resp"];

            var answb = Encoding.GetEncoding(1252).GetBytes(anscp);
            return Encoding.UTF8.GetString(answb);
        }

        public static bool SetUnsafeHeaderParsing()
        {
            Assembly oAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
            if (oAssembly != null)
            {
                Type oAssemblyType = oAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (oAssemblyType != null)
                {

                    object oInstance = oAssemblyType.InvokeMember("Section",
                      BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });

                    if (oInstance != null)
                    {
                        FieldInfo objFeild = oAssemblyType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (objFeild != null)
                        {
                            objFeild.SetValue(oInstance, true);
                            return true;
                        }
                    }
                }
            }
            return false;
        }


    }
}