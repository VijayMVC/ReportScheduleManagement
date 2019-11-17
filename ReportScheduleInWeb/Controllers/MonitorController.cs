using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ReportScheduleInWeb.Controllers
{
    public class MonitorController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Monitor
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim(), User_id = x.user_id }).ToList();

            var statuses = new[] { new SelectListItem { Value = "new", Text = "новое" },
                                   new SelectListItem { Value = "work", Text = "в работе" },
                                   new SelectListItem { Value = "wait", Text = "ожидание" },
                                   new SelectListItem { Value = "fail", Text = "завершено с ошибкой" },
                                   new SelectListItem { Value = "done", Text = "завершено" }
            };

            ViewBag.Authors = new SelectList(users, "User_id", "Fullname");
            ViewBag.Statuses = new SelectList(statuses, "Value", "Text");
            return View();
        }

        [HttpPost]
        public JsonResult SearchWishes()
        {
            int user_id = (Request.Form["search_user_id"] == "") ? 0 : Convert.ToInt32(Request.Form["search_user_id"]);
            int report_type_id = (Request.Form["search_report_type_id"] == "") ? 0 : Convert.ToInt32(Request.Form["search_report_type_id"]);
            string report_type_name = (report_type_id != 0) ? db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_name : String.Empty;
            string status = Request.Form["search_status"].Trim();
            DateTime? startdate = (Request.Form["search_startdate"] != "") ? DateTime.ParseExact(Request.Form["search_startdate"] + " 00:00:00", "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture) : (DateTime?)null;
            DateTime? enddate = (Request.Form["search_enddate"] != "") ? (DateTime.ParseExact(Request.Form["search_enddate"] + " 00:00:00", "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture)).AddDays(1) : (DateTime?)null;
            List<int> places = new List<int>(Request.Form["search_places"].Split(',').Select(Int32.Parse).ToList());

            if ((user_id != 0) || (report_type_id != 0) || (!String.IsNullOrEmpty(status)) || (startdate != null) || (enddate != null) || (places[0] != 0))
            {
                List<int> wish_places = new List<int>();
                if (places[0] != 0)
                {
                    wish_places = db.Tasks.Where(x => places.Contains(x.task_place_id)).Select(x => x.task_wish_id).ToList();
                }

                List<WishViewModel> WishList = db.Wishes.OrderByDescending(x => x.wish_createdate)
                                                .Where(x => (user_id != 0) ? (x.wish_user_id == user_id) : (x.wish_user_id == x.wish_user_id))
                                                .Where(x => (report_type_id != 0) ? (x.wish_report_type_name == report_type_name) : x.wish_report_type_name == x.wish_report_type_name)
                                                .Where(x => (!String.IsNullOrEmpty(status)) ? (x.wish_status == status) : x.wish_status == x.wish_status)
                                                .Where(x => (startdate != null) ? (x.wish_createdate >= startdate) : x.wish_createdate == x.wish_createdate)
                                                .Where(x => (enddate != null) ? (x.wish_createdate <= enddate) : x.wish_createdate == x.wish_createdate)
                                                .Where(x => (wish_places.Count != 0) ? (wish_places.Contains(x.wish_id)) : x.wish_id == x.wish_id)
                                                .Select(x => new WishViewModel
                                                {
                                                    Wish_id = x.wish_id,
                                                    Wish_createdate = x.wish_createdate,
                                                    Wish_report_type_name = x.wish_report_type_name,
                                                    User_id = x.wish_user_id,
                                                    Wish_status = x.wish_status
                                                }).ToList();

                foreach (var w in WishList)
                {
                    w.User_name = db.Users.Where(x => x.user_id == w.User_id).Select(x => ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim()).SingleOrDefault();
                    switch (w.Wish_status)
                    {
                        case "new":
                            w.Wish_status = "новое";
                            break;
                        case "work":
                            w.Wish_status = "в работе";
                            break;
                        case "wait":
                            w.Wish_status = "ожидание";
                            break;
                        case "fail":
                            w.Wish_status = "завершено с ошибкой";
                            break;
                        case "done":
                            w.Wish_status = "завершено";
                            break;
                        default :
                            w.Wish_status = "неизвестно";
                            break;
                    }
                }

                return Json(WishList, JsonRequestBehavior.AllowGet);
            }

            return Json(new List<WishViewModel>(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetParameters(int WishId)
        {
            List<ParameterType> ParameterList = new List<ParameterType>();

            string wish_report_xml = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_report_type_xml ?? "";
            DateTime wish_deadline = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_deadline;
            int? wish_attempts = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_total_attempts;

            if (!String.IsNullOrEmpty(wish_report_xml))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                using (TextReader reader = new StringReader(wish_report_xml))
                {
                    ReportModel report = (ReportModel)serializer.Deserialize(reader);

                    foreach (var p in report.Parameters)
                    {
                        ParameterList.Add(new ParameterType
                        {
                            Name = p.ParameterName,
                            Alias = p.ParameterAlias,
                            Type = p.ParameterDataType,
                            Value = (p.ParameterDataType == "startdate") || (p.ParameterDataType == "enddate") || (p.ParameterDataType == "date") ? DateTime.ParseExact(p.ParameterValue, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString() : p.ParameterValue
                        });
                    }

                    ParameterList.Add(new ParameterType
                    { 
                        Name = "deadline",
                        Alias = "deadline",
                        Type = "deadline",
                        Value = wish_deadline.ToString()
                    });

                    ParameterList.Add(new ParameterType
                    {
                        Name = "attempts",
                        Alias = "attempts",
                        Type = "attempts",
                        Value = wish_attempts == null ? "-" : wish_attempts.ToString()
                    });
                }
            }
            return Json(ParameterList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTasks(int WishId)
        {
            List<TaskViewModel> TaskList = db.Tasks.Where(x => x.task_wish_id == WishId).Select(x => new TaskViewModel
            {
                Task_id = x.task_id,
                Task_wish_id = x.task_wish_id,
                Task_place_id = x.task_place_id,
                Task_startdate = x.task_startdate,
                Task_number_attempts = x.task_number_attempts,
                Task_last_error_text_short = x.task_last_error_text == null ? "-" : x.task_last_error_text.Length > 40 ? x.task_last_error_text.Substring(0, 35) + "..." : x.task_last_error_text,
                Task_last_error_text = x.task_last_error_text,
                Task_status = x.task_status
            }).ToList();

            foreach (var t in TaskList)
            {
                t.Task_place_name = db.Places.Where(x => x.place_id == t.Task_place_id).Select(x => x.place_name).SingleOrDefault();
                switch (t.Task_status)
                {
                    case "new":
                        t.Task_status = "новое";
                        break;
                    case "work":
                        t.Task_status = "в работе";
                        break;
                    case "wait":
                        t.Task_status = "ожидание";
                        break;
                    case "fail":
                        t.Task_status = "завершено с ошибкой";
                        break;
                    case "done":
                        t.Task_status = "завершено";
                        break;
                    default:
                        t.Task_status = "неизвестно";
                        break;
                }
            }

            return Json(TaskList.OrderBy(x => x.Task_status).ThenBy(x => x.Task_place_name), JsonRequestBehavior.AllowGet);
        }
    }
}