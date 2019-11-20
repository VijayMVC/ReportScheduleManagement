using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReportScheduleInWeb.Controllers
{
    public class ReportController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Report
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim(), User_id = x.user_id }).ToList();

            ViewBag.Authors = new SelectList(users, "User_id", "Fullname");
            return View();
        }

        public JsonResult GetPlacesByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Places.Select(x => x.place_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> PlaceList = db.Report_place_relation.Where(x => x.rpr_report_type_id == ReportTypeId).Select(x => x.rpr_place_id).ToList();
                    return Json(PlaceList, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetGroupsByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Report_groups.Select(x => x.report_group_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> GroupList = db.Report_group_relation.Where(x => x.rgr_report_id == ReportTypeId).Select(x => x.rgr_report_group_id).ToList();
                    return Json(GroupList, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetUsersByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Users.Select(x => x.user_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> UserList = db.Report_user_relation.Where(x => x.rur_report_type_id == ReportTypeId).Select(x => x.rur_user_id).ToList();
                    return Json(UserList, JsonRequestBehavior.AllowGet);
            }
        }

    }
}