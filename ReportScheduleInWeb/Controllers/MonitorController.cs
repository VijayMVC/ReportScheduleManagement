using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace ReportScheduleInWeb.Controllers
{
    public class MonitorController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Monitor
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = (x.user_surname ?? "") + (x.user_name ?? "") + (x.user_patronymic ?? ""), User_id = x.user_id }).ToList();

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

            List<WishViewModel> WishList = db.Wishes.OrderByDescending(x => x.wish_createdate)
                                            .Where(x => (user_id != 0) ? (x.wish_user_id == user_id) : (x.wish_user_id == x.wish_user_id))
                                            .Where(x => (report_type_id != 0) ? (x.wish_report_type_name == report_type_name) : x.wish_report_type_name == x.wish_report_type_name)
                                            .Where(x => (!String.IsNullOrEmpty(status)) ? (x.wish_status == status) : x.wish_status == x.wish_status)
                                            .Where(x => (startdate != null) ? (x.wish_createdate >= startdate) : x.wish_createdate == x.wish_createdate)
                                            .Where(x => (enddate != null) ? (x.wish_createdate <= enddate) : x.wish_createdate == x.wish_createdate)
                                            .Select(x => new WishViewModel {
                                                Wish_id = x.wish_id,
                                                Wish_createdate = x.wish_createdate,
                                                Wish_report_type_name = x.wish_report_type_name,
                                                User_id = x.wish_user_id,
                                                Wish_status = x.wish_status
                                            }).ToList();

             return Json(WishList, JsonRequestBehavior.AllowGet);
        }
    }
}