using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;
using EncryptStringSample;
using Newtonsoft.Json;
using ReportScheduleInWeb.Models;

namespace ReportScheduleInWeb.Controllers
{
    public class WishController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Wish
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim(), User_id = x.user_id }).ToList();

            ViewBag.Wish_users = new SelectList(users, "User_id", "Fullname");
            return View();
        }

        public JsonResult GetParameters(int ReportTypeId)
        {
            List<ParameterType> ParameterList = new List<ParameterType>();

            string report_xml = db.Report_types.Where(x => x.report_type_id == ReportTypeId).SingleOrDefault().report_type_xml ?? "";

            if (!String.IsNullOrEmpty(report_xml))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                using (TextReader reader = new StringReader(report_xml))
                {
                    ReportModel report = (ReportModel)serializer.Deserialize(reader);

                    foreach (var p in report.Parameters)
                    {
                        ParameterList.Add(new ParameterType
                        {
                            Name = p.ParameterName,
                            Alias = p.ParameterAlias,
                            Type = p.ParameterDataType
                        });
                    }
                }
            }
            return Json(ParameterList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPlacesByPlaceTypeId(int PlaceTypeId)
        {
            List<int> PlaceList = db.Place_type_relation.Where(x => x.ptr_place_type_id == PlaceTypeId).Select(x => x.ptr_place_id).ToList();

            if (PlaceTypeId == 0)
            {
                List<int> EmptyList = new List<int>();
                return Json(EmptyList, JsonRequestBehavior.AllowGet);
            }
            return Json(PlaceList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetReportTypes(int UserId)
        {
            List<int> userRoles = (List<int>)Session["userRoles"];
            //Суперадмин или админ
            if (userRoles.Contains(1) || userRoles.Contains(2))
            {
                List<ReportType> ReportTypeList = (from rt in db.Report_types
                                                   join rgr in db.Report_group_relation on rt.report_type_id equals rgr.rgr_report_id
                                                   join rg in db.Report_groups on rgr.rgr_report_group_id equals rg.report_group_id
                                                   orderby rg.report_group_name, rt.report_type_name
                                                   select new ReportType
                                                   {
                                                       report_type_id = rt.report_type_id.ToString(),
                                                       report_type_name = rt.report_type_name,
                                                       report_group_name = rg.report_group_name
                                                   }).ToList();
                return Json(ReportTypeList, JsonRequestBehavior.AllowGet);
            }
            else
            {
                List<ReportType> ReportTypeList = (from rt in db.Report_types
                                                   join rur in db.Report_user_relation on rt.report_type_id equals rur.rur_report_type_id
                                                   join rgr in db.Report_group_relation on rt.report_type_id equals rgr.rgr_report_id
                                                   join rg in db.Report_groups on rgr.rgr_report_group_id equals rg.report_group_id
                                                   where rur.rur_user_id == UserId
                                                   orderby rg.report_group_name, rt.report_type_name
                                                   select new ReportType
                                                   {
                                                       report_type_id = rt.report_type_id.ToString(),
                                                       report_type_name = rt.report_type_name,
                                                       report_group_name = rg.report_group_name
                                                   }).ToList();
                return Json(ReportTypeList, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult GetPlaces(int ReportTypeId)
        {
            List<Place> PlaceList = (from p in db.Places
                                    join rpr in db.Report_place_relation on p.place_id equals rpr.rpr_place_id
                                    where rpr.rpr_report_type_id == ReportTypeId
                                    select new Place
                                    {
                                        Place_id = p.place_id.ToString(),
                                        Place_name = p.place_name
                                    }).ToList();

            return Json(PlaceList, JsonRequestBehavior.AllowGet);
        }

        public class parameters
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class places
        {
            public int id { get; set; }
            public string startdate { get; set; }
        }

        [HttpPost]
        public JsonResult AddWish()
        {
            var result = 0;
            try
            {
                //Вытаскиваем данные из запроса
                int report_type_id = Convert.ToInt32(Request.Form["report_type_id"]);
                int user_id = Convert.ToInt32(Request.Form["user_id"]);
                string report_type_name = db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_name;
                DateTime deadlineValue = DateTime.ParseExact(Request.Form["deadlineValue"].ToString(), "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                int? attemptsCount = Request.Form["attemptsCount"].ToString() != "null" ? Convert.ToInt32(Request.Form["attemptsCount"].ToString()) : (int?)null;
                List<parameters> ParamList = JsonConvert.DeserializeObject<List<parameters>>(Request.Form["parameters"].ToString());
                List<places> PlaceList = JsonConvert.DeserializeObject<List<places>>(Request.Form["places"].ToString());

                //Доступы к просмотру
                int[] Access_users = Array.ConvertAll(Request.Form.GetValues("access_user"), s => int.Parse(s));

                string wish_report_type_xml = String.Empty;

                string report_type_xml = db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_xml ?? "";

                List<ParameterType> ParameterList = new List<ParameterType>();

                if (!String.IsNullOrEmpty(report_type_xml))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(report_type_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        foreach (var p in report.Parameters)
                        {
                            ParameterList.Add(new ParameterType
                            {
                                Name = p.ParameterName,
                                Alias = p.ParameterAlias,
                                Type = p.ParameterDataType,
                                Value = 
                                    (p.ParameterDataType == "startdate") || (p.ParameterDataType == "enddate")
                                    ? DateTime.ParseExact(ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss") 
                                    : ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value
                            });
                        }

                        wish_report_type_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
                        wish_report_type_xml += "<TableDataSource SelectCommand=\"" + SecurityElement.Escape(report.SelectCommand) + "\">";
                        foreach (var c in report.Columns)
                        {
                            wish_report_type_xml += "<Column Name=\"" + c.ColumnName + "\" Type=\"" + c.ColumnType + "\" Alias=\"" + c.ColumnAlias + "\" />";
                        }
                        if (ParamList.Count != 0)
                        {
                            foreach (var p in ParameterList)
                            {
                                wish_report_type_xml += "<CommandParameter Name=\"" + p.Name + "\" Alias=\"" + p.Alias + "\" DataType=\"" + p.Type + "\" Value=\"" + p.Value + "\" />";
                            }
                        }
                        wish_report_type_xml += "</TableDataSource>";
                    }
                }
                else
                {
                    result = 1;
                }

                //Создаем задание
                Wishes Wish = new Wishes();
                Wish.wish_createdate = Convert.ToDateTime(DateTime.Now);
                Wish.wish_deadline = deadlineValue;
                Wish.wish_total_attempts = attemptsCount;
                Wish.wish_report_type_name = report_type_name;
                Wish.wish_status = "not_ready";
                Wish.wish_report_type_xml = wish_report_type_xml;
                Wish.wish_user_id = user_id;
                db.Wishes.Add(Wish);
                db.SaveChanges();

                //Создаем задачи
                foreach (var p in PlaceList)
                {
                    Tasks Task = new Tasks();
                    Task.task_startdate = DateTime.ParseExact(p.startdate, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    Task.task_wish_id = Wish.wish_id;
                    Task.task_place_id = p.id;
                    Task.task_number_attempts = 0;
                    Task.task_status = "new";
                    Task.task_last_error_text = null;
                    db.Tasks.Add(Task);
                }

                //Создаем доступы
                if (Access_users.Length != 0)
                {
                    Wish_report_relation wrr = new Wish_report_relation();
                    wrr.wrr_wish_id = Wish.wish_id;
                    wrr.wrr_report_type_id = report_type_id;
                    wrr.wrr_access_type = (short)((Access_users[0] == -1) ? 1 : (Access_users[0] == 0) ? 0 : 2);
                    db.Wish_report_relation.Add(wrr);
                    db.SaveChanges();

                    //Конкретные пользователи
                    if (wrr.wrr_access_type == 2)
                    {
                        foreach (int item in Access_users)
                        {
                            Wish_user_relation wur = new Wish_user_relation();
                            wur.wur_wish_id = Wish.wish_id;
                            wur.wur_user_id = item;
                            db.Wish_user_relation.Add(wur);
                        }
                        db.SaveChanges();
                    }
                }
                //Доступ для всех
                else
                {
                    Wish_report_relation wrr = new Wish_report_relation();
                    wrr.wrr_wish_id = Wish.wish_id;
                    wrr.wrr_report_type_id = report_type_id;
                    wrr.wrr_access_type = 0;
                    db.Wish_report_relation.Add(wrr);
                    db.SaveChanges();
                }

                Wish.wish_status = "new";
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult RepeatWish()
        {
            var result = -1;
            try
            {
                //Вытаскиваем данные из запроса
                int wish_id = Convert.ToInt32(Request.Form["wish_id"]);
                int user_id = Convert.ToInt32(Request.Form["user_id"]);
                int repeat_type = Convert.ToInt32(Request.Form["repeat_type"]);

                DateTime deadlineValue = DateTime.ParseExact(Request.Form["deadlineValue"].ToString(), "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                int? attemptsCount = Request.Form["attemptsCount"].ToString() != "null" ? Convert.ToInt32(Request.Form["attemptsCount"].ToString()) : (int?)null;
                List<parameters> ParamList = JsonConvert.DeserializeObject<List<parameters>>(Request.Form["parameters"].ToString());
                List<places> PlaceList = JsonConvert.DeserializeObject<List<places>>(Request.Form["places"].ToString());

                Wishes w = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault();

                int report_type_id = db.Wish_report_relation.Where(x => x.wrr_wish_id == wish_id).SingleOrDefault().wrr_report_type_id;

                //Доступы к просмотру
                int[] Access_users = Array.ConvertAll(Request.Form.GetValues("access_user"), s => int.Parse(s));

                string wish_report_type_xml = w.wish_report_type_xml;

                string new_xml = String.Empty;

                List<ParameterType> ParameterList = new List<ParameterType>();

                if (!String.IsNullOrEmpty(wish_report_type_xml))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(wish_report_type_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        foreach (var p in report.Parameters)
                        {
                            ParameterList.Add(new ParameterType
                            {
                                Name = p.ParameterName,
                                Alias = p.ParameterAlias,
                                Type = p.ParameterDataType,
                                Value =
                                    (p.ParameterDataType == "startdate") || (p.ParameterDataType == "enddate")
                                    ? DateTime.ParseExact(ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss")
                                    : ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value
                            });
                        }

                        new_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
                        new_xml += "<TableDataSource SelectCommand=\"" + SecurityElement.Escape(report.SelectCommand) + "\">";
                        foreach (var c in report.Columns)
                        {
                            new_xml += "<Column Name=\"" + c.ColumnName + "\" Type=\"" + c.ColumnType + "\" Alias=\"" + c.ColumnAlias + "\" />";
                        }
                        if (ParamList.Count != 0)
                        {
                            foreach (var p in ParameterList)
                            {
                                new_xml += "<CommandParameter Name=\"" + p.Name + "\" Alias=\"" + p.Alias + "\" DataType=\"" + p.Type + "\" Value=\"" + p.Value + "\" />";
                            }
                        }
                        new_xml += "</TableDataSource>";
                    }
                }
                else
                {
                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                //Обновляем задание
                if (repeat_type == 0)
                {
                    w.wish_createdate = Convert.ToDateTime(DateTime.Now);
                    w.wish_deadline = deadlineValue;
                    w.wish_total_attempts = attemptsCount;
                    w.wish_status = "not_ready";
                    w.wish_user_id = user_id;
                    db.SaveChanges();

                    //Обновляем задачи
                    foreach (var p in PlaceList)
                    {
                        Tasks Task = db.Tasks.Where(x => x.task_wish_id == wish_id && x.task_place_id == p.id).SingleOrDefault();
                        Task.task_startdate = DateTime.ParseExact(p.startdate, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                        Task.task_number_attempts = 0;
                        Task.task_status = "new";
                        Task.task_last_error_text = null;
                    }
                    db.SaveChanges();

                    //Обновляем доступы
                    Wish_report_relation wrr = db.Wish_report_relation.Where(x => x.wrr_wish_id == wish_id).SingleOrDefault();

                    if (Access_users.Length != 0)
                    {
                        wrr.wrr_access_type = (short)((Access_users[0] == -1) ? 1 : (Access_users[0] == 0) ? 0 : 2);
                        db.SaveChanges();

                        //Конкретные пользователи
                        if (wrr.wrr_access_type == 2)
                        {
                            //Пользователи ДО редактирования
                            List<Wish_user_relation> Wur = new List<Wish_user_relation>();
                            foreach (Wish_user_relation wur in db.Wish_user_relation.Where(x => x.wur_wish_id == wish_id))
                            {
                                Wur.Add(wur);
                            }

                            foreach (Users u in db.Users)
                            {
                                if (Access_users.Contains(u.user_id))
                                {
                                    if (Wur.Find(x => x.wur_user_id == u.user_id) == null)
                                    {
                                        db.Wish_user_relation.Add(new Wish_user_relation() { wur_wish_id = (int)wish_id, wur_user_id = u.user_id });
                                    }
                                }
                                else
                                {
                                    if (Wur.Find(x => x.wur_user_id == u.user_id) != null)
                                    {
                                        db.Wish_user_relation.Remove(Wur.Find(x => x.wur_user_id == u.user_id));
                                    }
                                }
                            }
                            db.SaveChanges();
                        }
                    }
                    //Доступ для всех
                    else
                    {
                        wrr.wrr_access_type = 0;
                        db.SaveChanges();
                    }

                    w.wish_status = "new";
                    db.SaveChanges();

                    result = wish_id;
                }
                else
                {
                    //Создаем задание
                    Wishes Wish = new Wishes();
                    Wish.wish_createdate = Convert.ToDateTime(DateTime.Now);
                    Wish.wish_deadline = deadlineValue;
                    Wish.wish_total_attempts = attemptsCount;
                    Wish.wish_report_type_name = w.wish_report_type_name;
                    Wish.wish_status = "not_ready";
                    Wish.wish_report_type_xml = new_xml;
                    Wish.wish_user_id = user_id;
                    db.Wishes.Add(Wish);
                    db.SaveChanges();

                    //Создаем задачи
                    foreach (var p in PlaceList)
                    {
                        Tasks Task = new Tasks();
                        Task.task_startdate = DateTime.ParseExact(p.startdate, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture); ;
                        Task.task_wish_id = Wish.wish_id;
                        Task.task_place_id = p.id;
                        Task.task_number_attempts = 0;
                        Task.task_status = "new";
                        Task.task_last_error_text = null;
                        db.Tasks.Add(Task);
                    }

                    //Создаем доступы
                    if (Access_users.Length != 0)
                    {
                        Wish_report_relation wrr = new Wish_report_relation();
                        wrr.wrr_wish_id = Wish.wish_id;
                        wrr.wrr_report_type_id = report_type_id;
                        wrr.wrr_access_type = (short)((Access_users[0] == -1) ? 1 : (Access_users[0] == 0) ? 0 : 2);
                        db.Wish_report_relation.Add(wrr);
                        db.SaveChanges();

                        //Конкретные пользователи
                        if (wrr.wrr_access_type == 2)
                        {
                            foreach (int item in Access_users)
                            {
                                Wish_user_relation wur = new Wish_user_relation();
                                wur.wur_wish_id = Wish.wish_id;
                                wur.wur_user_id = item;
                                db.Wish_user_relation.Add(wur);
                            }
                            db.SaveChanges();
                        }
                    }
                    //Доступ для всех
                    else
                    {
                        Wish_report_relation wrr = new Wish_report_relation();
                        wrr.wrr_wish_id = Wish.wish_id;
                        wrr.wrr_report_type_id = report_type_id;
                        wrr.wrr_access_type = 0;
                        db.Wish_report_relation.Add(wrr);
                        db.SaveChanges();
                    }

                    Wish.wish_status = "new";
                    db.SaveChanges();
                    result = Wish.wish_id;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}