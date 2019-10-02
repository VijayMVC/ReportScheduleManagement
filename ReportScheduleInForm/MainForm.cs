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

namespace ReportScheduleInForm
{
    public partial class MainForm : MetroForm
    {
        public MainForm()
        {
            InitializeComponent();
            this.StyleManager = metroStyleManager1;
        }

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

        private void GetWishes()
        {
            System.Threading.Thread.Sleep(1000);

            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                Wishes w = null;
                foreach (Wishes item in db.Wishes.Where(x => x.wish_status != "not_ready" && x.wish_status != "done" && x.wish_status != "fail" && x.wish_status != "work"))
                {
                    //Задача в ожидании
                    if (item.wish_status == "wait")
                    {
                        //Проверка прошло ли время
                        foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == item.wish_id && x.task_status == "wait"))
                        {
                            if (System.DateTime.Compare(t.task_startdate, System.DateTime.Now) <= 0)
                            {
                                w = item;
                                break;
                            }
                        }
                    }
                    else
                    {
                        w = item;
                    }
                    if (w != null) break;
                }

                if (w != null)
                {
                    if (w.wish_status != "wait")
                    {
                        WriteOnStory("Найдено задание на выгрузку отчета \"" + w.wish_report_type_name + "\".");
                    }

                    //Проверка задачи на дедлайн
                    w.wish_status = CheckDeadline(w) ? "work" : "fail";
                    db.SaveChanges();

                    if (w.wish_status == "fail") return;

                    doWorkAsync(w);
                }
            }
        }

        private async void doWorkAsync(Wishes w)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail" && x.task_status != "work"))
                {
                    if (System.DateTime.Compare(t.task_startdate, System.DateTime.Now) > 0)
                    {
                        t.task_status = "wait";
                        break;
                    }

                    t.task_number_attempts = t.task_number_attempts ?? 0;

                    //Превышено количество попыток
                    if ((w.wish_total_attempts != null) && (t.task_number_attempts >= w.wish_total_attempts))
                    {
                        t.task_last_error_text = t.task_last_error_text ?? "";
                        t.task_last_error_text = "Исчерпано количество попыток выгрузки отчета. " + t.task_last_error_text;
                        t.task_status = "fail";
                        break;
                    }

                    await Task.Run(() => ReportRequest(t.task_id, w.wish_report_type_xml));
                    //ReportRequestAsync(t.task_id, w.wish_report_type_xml);
                }
                db.SaveChanges();

                Wishes wish = db.Wishes.Where(x => x.wish_id == w.wish_id).FirstOrDefault();
                wish.wish_status = CheckWishDone(w) ? "done" : "wait";
                db.SaveChanges();
            }
        }

        private async void ReportRequestAsync(int task_id, string report_xml)
        {
            await Task.Run(() => ReportRequest(task_id, report_xml));
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

                        string report_data_xml = String.Empty;
                        string conn_string = StringCipher.Decrypt(db.Places.Where(x => x.place_id == task.task_place_id).FirstOrDefault().place_connection, db.Places.Where(x => x.place_id == task.task_place_id).FirstOrDefault().place_name);
                        string cmd_text = report.SelectCommand;

                        DataTable table_result = new DataTable();

                        using (SqlConnection sqlConnection = new SqlConnection(conn_string))
                        {
                            sqlConnection.Open();
                            SqlCommand sqlCmd = new SqlCommand(cmd_text, sqlConnection);
                            foreach (var p in report.Parameters)
                            {
                                sqlCmd.Parameters.AddWithValue(p.ParameterName, p.ParameterValue);
                            }

                            SqlDataAdapter sda = new SqlDataAdapter(sqlCmd);
                            sda.Fill(table_result);
                        }

                        foreach (DataRow row in table_result.Rows)
                        {
                            report_data_xml += @"<row>";
                            foreach (var col in report.Columns)
                            {
                                report_data_xml += @"<" + col.ColumnAlias + @">" + row[col.ColumnName].ToString() + @"</" + col.ColumnAlias + @">";
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
