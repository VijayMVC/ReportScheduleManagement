using MetroFramework.Forms;
using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
                if (w.wish_deadline <= System.DateTime.Now)
                {
                    WriteOnStory("Задание на выгрузку отчета \"" + w.wish_report_type_name + "\" провалено. Прошел срок исполнения.");
                    if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done").Count() != 0)
                    {
                        //Всем не завершенным задачам ставим статус "провала"
                        foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail"))
                        {
                            t.task_last_error_text = "Прошел срок выполнения выгрузки отчета.";
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
                if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done").Count() == 0)
                {
                    WriteOnStory("Задание на выгрузку отчета \"" + w.wish_report_type_name + "\" успешно завершено.");
                    return true;
                }
            }

            return false;
        }

        private async void doWorkAsync()
        {
            System.Threading.Thread.Sleep(1000);

            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                Wishes w = db.Wishes.Where(x => x.wish_status != "done" && x.wish_status != "fail" && x.wish_status != "work").FirstOrDefault();

                if (w != null)
                {
                    WriteOnStory("Найдено задание на выгрузку отчета \"" + w.wish_report_type_name + "\".");

                    //Проверка задачи на дедлайн
                    w.wish_status = CheckDeadline(w) ? "work" : "fail";
                    db.SaveChanges();

                    await doWork(w);

                    w.wish_status = CheckWishDone(w) ? "done" : "wait";
                    db.SaveChanges();
                }
            }
        }

        private async Task doWork(Wishes w)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail"))
                {
                    if (t.task_startdate > System.DateTime.Now)
                    {
                        t.task_status = "wait";
                        break;
                    }

                    t.task_number_attempts = t.task_number_attempts ?? 0;

                    //Превышено количество попыток
                    if ((w.wish_total_attempts != null) && (t.task_number_attempts >= w.wish_total_attempts))
                    {
                        t.task_status = "fail";
                        break;
                    }

                    await Task.Run(() => ReportRequest(t.task_id, w.wish_report_type_xml));
                }
                db.SaveChanges();
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

                        string report_data_xml = String.Empty;
                        string conn_string = db.Places.Where(x => x.place_id == task.task_place_id).FirstOrDefault().place_connection;
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
            while (true)
            {
                await Task.Run(() => doWorkAsync());
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            MetroButton1_Click(this, new EventArgs());
        }

        private void WriteOnStory(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => StoryTextBox.AppendText(System.DateTime.Now.ToString() + ": " + text + Environment.NewLine)));
            }
            else
            {
                StoryTextBox.AppendText(text + Environment.NewLine);
            }
        }
    }
}
