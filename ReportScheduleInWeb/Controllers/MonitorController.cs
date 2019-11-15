using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}