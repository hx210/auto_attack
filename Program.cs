using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;


class cppddl
{
    [DllImport("League.dll")]
    public static extern void SendKey(UInt32 a);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int a);
    [DllImport("kernel32.dll")]
    public static extern ulong GetTickCount64();
}
class Program
{
    static float atkSpeed = 0;
    static ulong lastAttackTime = 0, lastMoveTime = 0;
    static async Task Main(string[] args)
    {   
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("必须以管理员模式运行，否则无效!!\n");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("游戏设置->热键->玩家移动点击X   快捷攻击型移动Z");
        Console.WriteLine("使用方法：直接开着程序上游戏，按Capslock（大写锁定）键即可丝滑走A");
        Console.WriteLine("模拟走砍，周围有小兵的时候可能会A错目标\n");
        Console.ForegroundColor = ConsoleColor.Cyan;

        Thread threadUpdate = new Thread(async () =>
        {
            while (true)
            {
                atkSpeed = await GetAttackSpeed();
                //Console.WriteLine(atkSpeed);
                Thread.Sleep(200);
            }
        });
        threadUpdate.Start();

        Thread threadAttack = new Thread(async () =>
        {
            while (true)
            {
                if (cppddl.GetAsyncKeyState(0x14) != 0 && atkSpeed!=-1)
                {
                    float atkInterval, prev;
                    atkInterval = 1.0f / atkSpeed * 1000;
                    prev = atkInterval * 0.43f;
                    if (cppddl.GetTickCount64() - lastAttackTime > atkInterval)
                    {
                        cppddl.SendKey(44);
                        lastAttackTime = cppddl.GetTickCount64();
                        Thread.Sleep((int)prev);
                    }
                    else if (cppddl.GetTickCount64() - lastMoveTime >= 30)
                    {
                        cppddl.SendKey(45);
                        lastMoveTime = cppddl.GetTickCount64();
                    }
                }

                Thread.Sleep(5);
            }
        });
        threadAttack.Start();
        Console.ReadKey();
    }

    static async Task<float> GetAttackSpeed()
    {
        string url = "https://127.0.0.1:2999/liveclientdata/allgamedata";

        using (var httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using (var httpClient = new HttpClient(httpClientHandler))
            {
                try
                {
                    string htmlContent = await httpClient.GetStringAsync(url);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(htmlContent);

                    string jsonData = ExtractTextFromHtml(doc.DocumentNode);
                    JObject json = JObject.Parse(jsonData);

                    float attackSpeed = (float)json["activePlayer"]["championStats"]["attackSpeed"];

                    return attackSpeed;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Error occurred: " + ex.Message);
                    return -1;
                }
            }
        }
    }

    static string ExtractTextFromHtml(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            return node.InnerText.Trim();
        }

        string text = "";
        foreach (var childNode in node.ChildNodes)
        {
            text += ExtractTextFromHtml(childNode);
        }

        return text;
    }
}
