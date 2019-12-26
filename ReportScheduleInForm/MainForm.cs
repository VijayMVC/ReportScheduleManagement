using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EncryptStringSample;
using System.Configuration;
using System.Text;
using System.Net.Mail;
using System.Globalization;
using System.Net;
using MySql.Data.MySqlClient;
using System.Security;

namespace ReportScheduleInForm
{
    public partial class MainForm : MetroForm
    {
        public MainForm()
        {
            bool success = Int32.TryParse(ConfigurationManager.AppSettings.Get("command_timeout"), out command_timeout);

            if (!success)
            {
                //Полчаса на выполнение запроса
                command_timeout = 1800;
            }
            InitializeComponent();
            this.StyleManager = metroStyleManager1;
        }

        string password = ConfigurationManager.AppSettings.Get("password");
        int command_timeout;

        private readonly string smtpFrom = ConfigurationManager.AppSettings["smtp:from"];

        //Проверка задания на дедлайн
        private bool CheckDeadline(Wishes w)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if (System.DateTime.Compare(w.wish_deadline, System.DateTime.Now) <= 0)
                {
                    WriteOnStory("Задание на выгрузку отчета \"" + w.wish_report_type_name + "\" провалено. Прошел срок исполнения.");
                    if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done").Count() != 0)
                    {
                        //Всем не завершенным задачам ставим статус "провала"
                        foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail"))
                        {
                            t.task_last_error_text = t.task_last_error_text ?? "";
                            t.task_last_error_text = "Прошел срок выполнения выгрузки отчета. " + t.task_last_error_text;
                            t.task_status = "fail";
                            WriteOnStory("Прошел срок выполнения выгрузки отчета \"" + w.wish_report_type_name + "\" для " + db.Places.Where(x => x.place_id == t.task_place_id).FirstOrDefault().place_name);
                        }
                        db.SaveChanges();
                    }
                    return false;
                }
            }
            return true;
        }

        //Проверка на успешное завершение задания
        private bool CheckWishDone(Wishes w)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail").Count() == 0)
                {
                    WriteOnStory("Задание на выгрузку отчета \"" + w.wish_report_type_name + "\" завершено.");

                    return true;
                }
            }

            return false;
        }

        private bool CheckWishFail(Wishes w)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status == "fail").Count() == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetWishes()
        {
            System.Threading.Thread.Sleep(1000);

            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                try
                {
                    Wishes w = null;
                    foreach (Wishes item in db.Wishes.Where(x => x.wish_status != "not_ready" && x.wish_status != "done" && x.wish_status != "fail" && x.wish_status != "work"))
                    {
                        //Задача в ожидании
                        if (item.wish_status == "wait")
                        {
                            //Проверка прошло ли время
                            foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == item.wish_id && ((x.task_status == "wait") || (x.task_status == "new"))))
                            {
                                if (System.DateTime.Compare(t.task_startdate, System.DateTime.Now) <= 0)
                                {
                                    w = item;
                                    break;
                                }
                                else
                                {
                                    if (t.task_status == "new")
                                    {
                                        t.task_status = "wait";
                                    }
                                }
                            }
                        }
                        else
                        {
                            w = item;
                        }
                        if (w != null) break;
                    }
                    db.SaveChanges();

                    if (w != null)
                    {
                        if (w.wish_status != "wait")
                        {
                            WriteOnStory("Найдено задание на выгрузку отчета \"" + w.wish_report_type_name + "\".");
                        }

                        //Проверка задачи на дедлайн
                        w.wish_status = CheckDeadline(w) ? "work" : "fail";
                        db.SaveChanges();

                        if (w.wish_status == "fail")
                        {
                            SendNotify(w.wish_id);
                            return;
                        }

                        Task.Run(() => doWork(w));
                    }
                }
                catch
                {
                    WriteOnStory("Не удалось подключиться к БД ReportSchedule. Попытка подключения через 1 минуту!");
                    System.Threading.Thread.Sleep(60000);
                    return;
                }
            }
        }

        private void doWork(Wishes w)
        {
            ReportRequestAsync(w);
        }

        private async void ReportRequestAsync(Wishes w)
        {
            var tasks = new List<Task>();
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail"))
                {
                    if (System.DateTime.Compare(t.task_startdate, System.DateTime.Now) > 0)
                    {
                        t.task_status = "wait";
                        continue;
                    }

                    t.task_number_attempts = t.task_number_attempts ?? 0;

                    //Превышено количество попыток
                    if ((w.wish_total_attempts != null) && (t.task_number_attempts >= w.wish_total_attempts))
                    {
                        t.task_last_error_text = t.task_last_error_text ?? "";
                        t.task_last_error_text = "Исчерпано количество попыток выгрузки отчета. " + t.task_last_error_text;
                        t.task_status = "fail";
                        continue;
                    }

                    tasks.Add(Task.Run(() => ReportRequest(t.task_id, w.wish_report_type_xml)));
                }
                db.SaveChanges();

                Task task_wait = Task.WhenAll(tasks);

                try
                {
                    await task_wait;
                }
                catch { }

                Wishes wish = db.Wishes.Where(x => x.wish_id == w.wish_id).FirstOrDefault();
                wish.wish_status = CheckWishDone(wish) ? CheckWishFail(wish) ? "fail": "done" : "wait";
                db.SaveChanges();
                if ((wish.wish_status == "done") || (wish.wish_status == "fail"))
                {
                    try
                    {
                        SendNotify(wish.wish_id);
                    }
                    catch
                    {
                        WriteOnStory("Не удалось отправить письмо по email! Что-то с почтовым сервером!");
                    }
                }
            }
        }

        private void ReportRequest(int task_id, string report_xml)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                Tasks task = db.Tasks.Where(x => x.task_id == task_id).FirstOrDefault();
                try
                {
                    task.task_number_attempts++;
                    task.task_status = "work";
                    db.SaveChanges();

                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(report_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        Places place = db.Places.Where(x => x.place_id == task.task_place_id).FirstOrDefault();

                        string report_data_xml = String.Empty;
                        string conn_string = StringCipher.Decrypt(place.place_connection, password);
                        string cmd_text = report.SelectCommand;

                        DataTable table_result = new DataTable();

                        switch (place.place_type_DB)
                        {
                            case "MS SQL":
                                using (SqlConnection sqlConnection = new SqlConnection(conn_string))
                                {
                                    sqlConnection.Open();
                                    SqlCommand sqlCmd = new SqlCommand(cmd_text, sqlConnection);
                                    sqlCmd.CommandTimeout = command_timeout;
                                    foreach (var p in report.Parameters)
                                    {
                                        sqlCmd.Parameters.AddWithValue(p.ParameterName, p.ParameterValue);
                                    }

                                    SqlDataAdapter sda = new SqlDataAdapter(sqlCmd);
                                    sda.Fill(table_result);
                                }
                                break;
                            case "MySQL":
                                using (MySqlConnection sqlConnection = new MySqlConnection(conn_string))
                                {
                                    sqlConnection.Open();
                                    MySqlCommand sqlCmd = new MySqlCommand(cmd_text, sqlConnection);
                                    sqlCmd.CommandTimeout = command_timeout;
                                    foreach (var p in report.Parameters)
                                    {
                                        sqlCmd.Parameters.AddWithValue(p.ParameterName, p.ParameterValue);
                                    }

                                    MySqlDataAdapter sda = new MySqlDataAdapter(sqlCmd);
                                    sda.Fill(table_result);
                                }
                                break;
                            default:
                                throw new Exception("В местоположении \"" + place.place_name + "\" ошибка в указании БД.");
                        }

                        foreach (DataRow row in table_result.Rows)
                        {
                            report_data_xml += @"<row>";
                            foreach (var col in report.Columns)
                            {
                                report_data_xml += @"<" + col.ColumnAlias + @">" + SecurityElement.Escape(row[col.ColumnName].ToString()) + @"</" + col.ColumnAlias + @">";
                            }
                            report_data_xml += @"</row>";
                        }

                        Report_data rd = new Report_data();

                        rd.report_data_task_id = task.task_id;
                        rd.report_data_createdate = System.DateTime.Now;
                        rd.report_data_xml = @"<Root>" + report_data_xml + @"</Root>";

                        db.Report_data.Add(rd);

                        task.task_status = "done";
                    }
                }
                catch (Exception ex)
                {
                    task.task_status = "wait";
                    task.task_startdate = System.DateTime.Now.AddMinutes(5);
                    task.task_last_error_text = ex.Message;

                    WriteOnStory("Ошибка при попытки выгрузки отчета: " + ex.Message);
                }
                finally
                {
                    db.SaveChanges();
                }
            }
        }

        private async void MetroButton1_Click(object sender, EventArgs e)
        {
            //Бесконечный цикл выполнения задач
            while (true)
            {
                await Task.Run(() => GetWishes());
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            MetroButton1_Click(this, new EventArgs());
            MetroButton2_Click(this, new EventArgs());
        }

        private void WriteOnStory(string text)
        {
            if (StoryTextBox.InvokeRequired)
            {
                BeginInvoke(new Action(() => StoryTextBox.AppendText(System.DateTime.Now.ToString() + ": " + text + Environment.NewLine)));
            }
            else
            {
                StoryTextBox.AppendText(text + Environment.NewLine);
            }
        }

        private async void MetroButton2_Click(object sender, EventArgs e)
        {
            //Бесконечный цикл отображения результатов
            while (true)
            {
                await Task.Run(() => ShowThreadProgressAsync());
            }
        }

        private async void ShowThreadProgressAsync()
        {
            System.Threading.Thread.Sleep(1000);
            await Task.Run(() => ShowThreadProgress());
        }

        private void ShowThreadProgress()
        {
            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    List<ThreadProgress> data = new List<ThreadProgress>();

                    foreach (var item in db.Wishes.Where(x => x.wish_status == "work" || x.wish_status == "wait"))
                    {
                        switch (item.wish_status)
                        {
                            case "work":
                                data.Add(new ThreadProgress(item.wish_createdate.ToString(), "Ведется работа по выгрузке отчета \"" + item.wish_report_type_name + "\""));
                                break;
                            case "wait":
                                data.Add(new ThreadProgress(item.wish_createdate.ToString(), "Ожидается работа по выгрузке отчета \"" + item.wish_report_type_name + "\""));
                                break;
                        }
                    }

                    if (ThreadGrid.InvokeRequired)
                    {
                        BeginInvoke(new Action(() => { ThreadGrid.DataSource = data; }));
                    }
                    else
                    {
                        ThreadGrid.DataSource = data;
                    }
                }
            }
            catch
            {
                System.Threading.Thread.Sleep(60000);
                return;
            }
        }

        public string GetPathExcelFile(int wish_id)
        {
            string url = ConfigurationManager.AppSettings.Get("webservice");
            WebRequest request = WebRequest.Create(url + wish_id.ToString());
            request.Method = "GET";
            WebResponse response = request.GetResponse();

            string responseFromServer;
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                responseFromServer = reader.ReadToEnd();
            }

            responseFromServer = responseFromServer.Substring(0, responseFromServer.LastIndexOf("</"));
            responseFromServer = responseFromServer.Substring(responseFromServer.IndexOf(">") + 1);

            return responseFromServer;
        }

        public void SendNotify(int wish_id)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                Wishes w = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault();

                string email = db.Users.Where(x => x.user_id == w.wish_user_id).SingleOrDefault().user_email;
                string subject = w.wish_report_type_name;

                StringBuilder param = new StringBuilder();

                XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));
                using (TextReader reader = new StringReader(w.wish_report_type_xml))
                {
                    ReportModel report = (ReportModel)serializer.Deserialize(reader);

                    if (report.Parameters.Count != 0)
                    {
                        foreach (var p in report.Parameters)
                        {
                            param.AppendLine("\t" + p.ParameterAlias + " : " + p.ParameterValue);
                        }
                    }
                    else
                    {
                        param.AppendLine("\t нет");
                    }
                }

                var ru = CultureInfo.GetCultureInfo("ru-RU");
                var body = new StringBuilder();

                if (db.Tasks.Where(x => x.task_wish_id == wish_id && x.task_status == "done").Count() == 0)
                {
                    body.AppendLine("ПРОВАЛ!!");
                    body.AppendLine().AppendLine("ПРОВАЛЕНО задание на выгрузку отчета \"" + w.wish_report_type_name + "\", созданному Вами " + w.wish_createdate.Day.ToString() + " " + ru.DateTimeFormat.MonthGenitiveNames[w.wish_createdate.Month - 1] + " " + w.wish_createdate.Year.ToString() + " году в " + w.wish_createdate.ToString("HH:mm") + ".");
                    body.AppendLine().AppendLine("Никаких данных извлечь не удалось. Для более подробной информации об ошибке зайдите на страницу \"Мониторинг\" в веб-приложении \"Планировщик отчетов\".");
                    body.AppendLine().AppendLine("ИНФОРМАЦИЯ:");
                    body.AppendLine("Параметры:");
                    body.AppendLine(param.ToString());
                }
                else
                {
                    body.AppendLine("Тук-тук!");
                    body.AppendLine().AppendLine("С пылу с жару готов отчет \"" + w.wish_report_type_name + "\" по заданию, созданному Вами " + w.wish_createdate.Day.ToString() + " " + ru.DateTimeFormat.MonthGenitiveNames[w.wish_createdate.Month - 1] + " " + w.wish_createdate.Year.ToString() + " году в " + w.wish_createdate.ToString("HH:mm"));
                    body.AppendLine().AppendLine("ИНФОРМАЦИЯ:");
                    body.AppendLine("Параметры:");
                    body.AppendLine(param.ToString());
                    body.AppendLine().AppendLine("Скачать отчет Вы можете по ссылке: " + GetPathExcelFile(wish_id));
                }

                body.AppendLine().AppendLine("С уважением, центр «Мои Документы».");
                body.AppendLine().AppendLine("---");
                body.AppendLine("Данное сообщение сформировано автоматически. Пожалуйста, не отвечайте на него.");
                body.AppendLine("УВЕДОМЛЕНИЕ О КОНФИДЕНЦИАЛЬНОСТИ: Это электронное сообщение и любые документы, приложенные к нему, содержат конфиденциальную информацию. Настоящим уведомляем Вас о том, что если это сообщение не предназначено Вам, использование, копирование, распространение информации, содержащейся в настоящем сообщении, а также осуществление любых действий на основе этой информации, строго запрещено. Если Вы получили это сообщение по ошибке, пожалуйста, сообщите об этом отправителю по электронной почте и удалите это сообщение.");
                body.AppendLine();

                SendMail(email, subject, body);
            }
        }

        public void SendMail(string email, string subject, StringBuilder text)
        {
            var mail = MailHelper.GetInstance();

            var message = new MailMessage();
            message.From = new MailAddress(smtpFrom);
            message.ReplyToList.Add(new MailAddress("grizzled@mfcsakha.ru", "no-reply"));
            message.Subject = subject;
            message.Body = text.ToString();
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = false;

            message.To.Add(new MailAddress(email));

            try
            {
                mail.Send(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            StoryTextBox.Clear();
        }

        //Закодировать строку в текстбоксе
        private void MetroButton3_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(metroTextBox1.Text))
            {
                metroTextBox1.Text = StringCipher.Encrypt(metroTextBox1.Text, password);
            }
        }
    }

    public class ThreadProgress
    {
        public ThreadProgress(string cdt, string s)
        {
            createdatetime = cdt;
            status = s;
        }

        public string createdatetime { get; set; }
        public string status { get; set; }
    }
}
