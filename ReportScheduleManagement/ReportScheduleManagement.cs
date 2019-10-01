using System.ServiceProcess;
using System.Timers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;

namespace ReportScheduleManagement
{
    public partial class ReportScheduleManagement : ServiceBase
    {
        private int eventId = 1;
        
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public ReportScheduleManagement()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("Report Schedule Management"))
            {
                System.Diagnostics.EventLog.CreateEventSource("Report Schedule Management", "Report Schedule Management Log");
            }
            eventLog1.Source = "Report Schedule Management";
            eventLog1.Log = "Report Schedule Management Log";
        }

        protected override void OnStart(string[] args)
        {
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("Начало работы службы");

            Timer timer = new Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            try
            {
                eventLog1.WriteEntry("Сканируем таблицу запроса отчетов", EventLogEntryType.Information, eventId++);
                var task = WorkAsync();
            }
            catch(Exception ex)
            {
                eventLog1.WriteEntry("Произошла ошибка: " + ex, EventLogEntryType.Information, eventId++);
            }
        }

        private async Task WorkAsync()
        {
            using (var db = new ReportScheduleEntities())
            {
                foreach (Wishes w in db.Wishes.Where(x => x.wish_status != "done" && x.wish_status != "fail"))
                {

                    eventLog1.WriteEntry("Нашли задание wish_id = " + w.wish_id.ToString(), EventLogEntryType.Information, eventId++);

                    //Прошел срок исполнения задачи
                    if (w.wish_deadline <= System.DateTime.Now)
                    {
                        if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done").Count() != 0)
                        {
                            w.wish_status = "fail";

                            //Всем не завершенным задачам ставим статус "провала"
                            foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail"))
                            {
                                t.task_last_error_text = "Прошел срок выполнения выгрузки отчета";
                                t.task_status = "fail";
                            }
                        }
                    }
                    else
                    {

                        eventLog1.WriteEntry("Точка 2", EventLogEntryType.Information, eventId++);

                        w.wish_status = "work";

                        foreach (Tasks t in db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done" && x.task_status != "fail" && x.task_status != "work"))
                        {

                            eventLog1.WriteEntry("Точка 3", EventLogEntryType.Information, eventId++);

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

                            eventLog1.WriteEntry("Точка 4", EventLogEntryType.Information, eventId++);


                            await Task.Run(() => ReportRequest(t.task_id, w.wish_report_type_xml));

                        }

                        if (db.Tasks.Where(x => x.task_wish_id == w.wish_id && x.task_status != "done").Count() == 0)
                        {
                            w.wish_status = "done";
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        private void ReportRequest(int task_id, string report_xml)
        {
            using (var db = new ReportScheduleEntities())
            {
                Tasks task = db.Tasks.Where(x => x.task_id == task_id).FirstOrDefault();
                try
                {
                    task.task_number_attempts++;
                    task.task_status = "work";

                    eventLog1.WriteEntry("Точка 5", EventLogEntryType.Information, eventId++);


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
                    eventLog1.WriteEntry("Ошибка при попытки выгрузки отчета", EventLogEntryType.Information, eventId++);
                }
                finally
                {
                    db.SaveChanges();
                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Остановка работы службы");

            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("Продолжение работы службы");
        }

        protected override void OnPause()
        {
            eventLog1.WriteEntry("Служба приостановлена");
        }

        protected override void OnShutdown()
        {
            eventLog1.WriteEntry("Служба выключена");
        }
    }
}
