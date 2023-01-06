using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Text;

namespace getGeoJson
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text == "")
            {
                MessageBox.Show("Access Token이 비어이있습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            logging("accessToken is " + textBox1.Text);

            logging("준비중 ..");
            string folderPath = Application.StartupPath + @"\result\";
            DirectoryInfo di = new DirectoryInfo(folderPath);
            if(!di.Exists)
            {
                di.Create();
            }
            logging("저장 폴더: " + folderPath);

            logging("1. 전국 단위 가져오기 시작");
            string getSidoUrl = string.Format("/OpenAPI3/addr/stage.json?accessToken={0}&pg_yn=0", textBox1.Text);
            logging("request Url is " + getSidoUrl);
            JArray sidoList = (JArray) Request(Method.Get, getSidoUrl)["result"];
            if(sidoList == null)
            {
                logging("Check accessToken");
                return;
            }
            File.WriteAllText(folderPath + "sido.json", (string?)sidoList.ToString(), Encoding.UTF8);
            logging("1. 전국 단위 가져오기 끝");

            logging("2. 구 단위 가져오기 시작");
            foreach(JObject sido in sidoList)
            {
                logging(sido["full_addr"] + " 가져오기 시작");
                string getGuGunUrl = string.Format("/OpenAPI3/boundary/hadmarea.geojson?accessToken={0}&year=2022&adm_cd={1}", textBox1.Text, sido["cd"]);
                JObject guGunObject = Request(Method.Get, getGuGunUrl);
                File.WriteAllText(folderPath + sido["cd"] +".json", (string?)guGunObject.ToString(), Encoding.UTF8);
                string text = File.ReadAllText(folderPath + sido["cd"] + ".json");
                text = text.Replace(sido["full_addr"] + " ", "");
                File.WriteAllText(folderPath + sido["cd"] + ".json", text, Encoding.UTF8);

                JArray guGunFeatures = (JArray) guGunObject["features"];

                foreach (JObject guGunTest in guGunFeatures)
                {
                    JObject properties = (JObject)guGunTest["properties"];
                    logging(properties["adm_nm"] + " 가져오기 시작");
                    string getDongUrl = string.Format("/OpenAPI3/boundary/hadmarea.geojson?accessToken={0}&year=2022&adm_cd={1}", textBox1.Text, properties["adm_cd"]);
                    JObject dongObject = Request(Method.Get, getDongUrl);
                    File.WriteAllText(folderPath + properties["adm_cd"] + ".json", (string?)dongObject.ToString(), Encoding.UTF8);

                    string text2 = File.ReadAllText(folderPath + properties["adm_cd"] + ".json");
                    text2 = text2.Replace(properties["adm_nm"] + " ", "");
                    File.WriteAllText(folderPath + properties["adm_cd"] + ".json", text2, Encoding.UTF8);

                    logging(properties["adm_nm"] + " 가져오기 끝");
                }
                logging(sido["full_addr"] + " 가져오기 끝");
            }
            logging("2. 구 단위 가져오기 끝");
        }

        private void logging(string text)
        {
            textBox2.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + text + "\r\n");
        }

        public static JObject? Request(Method method, string path)
        {
            RestClient client = new RestClient("https://sgisapi.kostat.go.kr");
            RestRequest req = new RestRequest(path, Method.Get);

            RestResponse res = client.Execute(req);
            if(res.Content != null) { 
                JObject json = JObject.Parse(res.Content);
                return json;
            } else
            {
                return null;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var ps = new ProcessStartInfo("https://sgis.kostat.go.kr/developer/html/main.html")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
    }
}