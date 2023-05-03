using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace kursinis { 
    
    public class Manage {
            public static void RunAsAdmin(string program, string file)
            {
                var currentPath = Directory.GetCurrentDirectory();
                var regFilePath = Path.Combine(currentPath, file);

                var startInfo = new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = program == "cmd.exe" ? $"/c {file}" : $"/s \"{regFilePath}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var process = new Process
                {
                    StartInfo = startInfo
                };
                process.Start();
            }

        public static string CheckDB(string hash, string method)
            {
                string response = "";
                string conString = "server=194.31.55.174;port=3306;username=test_virus;password=fc2a6br9iyKXkhSI;database=test_virus";

                using (MySqlConnection connection = new MySqlConnection(conString))
                {
                    connection.Open();
                    string sql = "SELECT EXISTS(SELECT * FROM hash WHERE hash = @hash)";

                    if (method == "onefile")
                    {
                        MySqlCommand cmd = new MySqlCommand(sql, connection);
                        cmd.Parameters.AddWithValue("@hash", hash);
                        var result = Convert.ToInt32(cmd.ExecuteScalar());

                        response = "{\"status\":\"" + (result > 0 ? "Danger" : "Safe") + "\",\"message\":\"This program " + (result > 0 ? "contains a virus, program is being removed" : "does not contain a virus") + "\",\"removed\":\"Removed files: " + result + "\"}";
                        if (result > 0) File.Delete(hash);
                    }
                    else if (method == "multiple")
                    {
                        string[] lines = File.ReadAllLines($"{System.IO.Directory.GetCurrentDirectory()}\\keys");
                        int malicious = 0;
                        foreach (string line in lines)
                        {
                            Console.WriteLine(line);
                            
                            MySqlCommand cmd = new MySqlCommand(sql, connection);
                            cmd.Parameters.AddWithValue("@hash", line);
                            var result = Convert.ToInt32(cmd.ExecuteScalar());

                            if (result > 0)  malicious++;
                        }

                        response = "{\"status\":\"" + (malicious == 0 ? "Safe" : "Danger") + "\",\"message\":\"This program " + (malicious == 0 ? "does not contain" : "contains") + " a virus\",\"removed\":\"Removed files: " + malicious + "\"}";
                    }
                    else
                    {
                        //Something if none
                    }
                    return response;
                }
            }

    }

    class Program
    {
        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:6969/");
            listener.Start();
            Console.WriteLine("Listening for POST requests on port 6969...");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "OPTIONS")
                {
                    Console.WriteLine("Received a preflight request!");

                    HttpListenerResponse response = context.Response;
                    response.Headers.Add("Access-Control-Allow-Origin", "https://samarata.trusty.lt");
                    response.Headers.Add("Access-Control-Allow-Methods", "POST");
                    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                    response.StatusCode = 200;
                    response.StatusDescription = "OK";
                    response.Close();
                }
                else if (request.HttpMethod == "POST")
                {
                    Console.WriteLine("Received a POST request!");
                    string response_web = "";

                    using (StreamReader reader = new StreamReader(request.InputStream))
                    {
                        string body = reader.ReadToEnd();
                        if (body.Contains("optimize"))
                        {
                            response_web = "Optimization request recieved successfully.";
                            Manage.RunAsAdmin("regedit.exe", "\\p1.reg");
                            Manage.RunAsAdmin("cmd.exe", $"{System.IO.Directory.GetCurrentDirectory()}\\debloat.bat");
                        }
                        else if (body.Contains("regen"))
                        {
                            response_web = "Regen request recieved successfully.";
                            Manage.RunAsAdmin("cmd.exe", $"for /r %f in (*.exe) do @(CertUtil -hashfile \"%f\" MD5 | find /i /v \"md5\" | find /i /v \"certutil\") >> {System.IO.Directory.GetCurrentDirectory()}\\keys");
                        }
                        else if (body.Contains("scan"))
                        {
                            response_web = Manage.CheckDB(null, "multiple");


                        }
                        else if (body.Contains("one_file"))
                        {
                            string hash = body.Replace("[one_file]", "");
                            response_web = Manage.CheckDB(hash, "onefile");
                        }
                        else
                        {
                            response_web = "The request does not contain 'optimize', 'regen', 'scan', or 'one_file'.";
                            Console.WriteLine("The request does not contain 'optimize', 'regen', 'scan', or 'one_file'.");
                        }
                    }

                    HttpListenerResponse response = context.Response;
                    response.Headers.Add("Access-Control-Allow-Origin", "https://samarata.trusty.lt"); // Allow requests only from samarata.trusty.lt
                    response.StatusCode = 200;
                    response.StatusDescription = "OK";
                    string responseString = response_web;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    response.Close();
                }
            }
        }
    }
}
