using Newtonsoft.Json;
using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace ReportScheduleInWeb.Controllers
{
    public class UserController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        public string encryption(String password)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] originalBytes = ASCIIEncoding.Default.GetBytes(password);
            byte[] encodedBytes = md5.ComputeHash(originalBytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < encodedBytes.Length; i++)
                sb.Append(encodedBytes[i].ToString("x2").ToLower());

            return sb.ToString();
        }

        public JsonResult GetUserList()
        {
            List<UserViewModel> UserList = db.Users.Select(x => new UserViewModel
            {
                user_id = x.user_id,
                user_login = x.user_login,
                user_surname = x.user_surname,
                user_name = x.user_name,
                user_patronymic = x.user_patronymic,
                user_isdeleted = x.user_isdeleted,
                user_email = x.user_email,
                user_FIO = (!String.IsNullOrEmpty(x.user_surname) ? x.user_surname : "") + " " + (!String.IsNullOrEmpty(x.user_name) ? x.user_name : "") + " " + (!String.IsNullOrEmpty(x.user_patronymic) ? x.user_patronymic : ""),
                changeable = 1
            }).OrderBy(x => x.user_surname).ThenBy(x => x.user_name).ThenBy(x => x.user_patronymic).ToList();

            foreach (var user in UserList)
            {
                List<string> roles = (from r in db.Roles
                                      join ur in db.User_roles on r.role_id equals ur.userrole_role_id
                                      where ur.userrole_user_id == user.user_id
                                      orderby r.role_id
                                      select r.role_name).ToList<string>();
                user.user_roles = String.Join(", ", roles.ToArray());
            }

            return Json(UserList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserById(int UserId)
        {
            var Usermodel = new UserViewModel();
            if (UserId == 0)
            {
                UserId = Convert.ToInt32(Session["userID"]);
            }
            Users model = db.Users.Where(x => x.user_id == UserId).SingleOrDefault();

            Usermodel.user_id = model.user_id;
            Usermodel.user_login = model.user_login;
            Usermodel.user_password = model.user_password;
            Usermodel.user_isdeleted = model.user_isdeleted;
            Usermodel.user_surname = model.user_surname;
            Usermodel.user_name = model.user_name;
            Usermodel.user_patronymic = model.user_patronymic;
            Usermodel.user_email = model.user_email;

            Usermodel.roles = (from r in db.Roles
                               join ur in db.User_roles on r.role_id equals ur.userrole_role_id
                               where ur.userrole_user_id == UserId
                               orderby r.role_id
                               select r.role_id).ToList<int>();

            Usermodel.report_types = (from rt in db.Report_types
                                      join rur in db.Report_user_relation on rt.report_type_id equals rur.rur_report_type_id
                                      where rur.rur_user_id == UserId
                                      orderby rt.report_type_name
                                      select rt.report_type_id).ToList<int>();

            string value = string.Empty;
            value = JsonConvert.SerializeObject(Usermodel, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ChangePassword(UserViewModel model)
        {
            var result = 0;
            try
            {
                //Пользователь меняет свой пароль - проверяем ввод старого пароля
                if (model.user_id == Convert.ToInt32(Session["userID"]))
                {
                    Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                    if (Use.user_password != encryption(model.user_password_old))
                    {
                        result = 1;
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                }

                if ((model.user_id > 0) && (model.user_password != null) && (model.user_password.Trim() != ""))
                {
                    Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                    Use.user_password = encryption(model.user_password);
                    db.SaveChanges();
                }
                else
                {
                    result = 2;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //Проверка на повтор логина
        public bool CheckUserLogin(string UserLogin, int UserId)
        {
            try
            {
                if (UserId != 0)
                {
                    //Пользователь редактируется, поиск одинакового логина с другим ID
                    if (db.Users.Where(x => x.user_id != UserId && x.user_login == UserLogin).FirstOrDefault() != null)
                    {
                        return true;
                    }
                }
                else
                {
                    //Новый пользователь, поиск одинакового логина
                    if (db.Users.Where(x => x.user_login == UserLogin).FirstOrDefault() != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return false;
        }

        [HttpPost]
        public JsonResult SaveDataInDatabase(UserViewModel model)
        {
            var result = 0;
            try
            {
                //Вытаскиваем выбранные роли и отчеты
                var selroles = Request.Form["user_roles"].Split(',').Select(Int32.Parse).ToList();
                var selreports = Request.Form["report_types"].Split(',').Select(Int32.Parse).ToList();
                var isdeleted = Convert.ToBoolean(Request.Form["user_isdeleted"]);

                //Редактирование пользователя
                if (model.user_id > 0)
                {
                    if (!CheckUserLogin(model.user_login, model.user_id))
                    {
                        Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                        Use.user_login = model.user_login;
                        Use.user_surname = model.user_surname;
                        Use.user_name = model.user_name;
                        Use.user_patronymic = model.user_patronymic;
                        Use.user_isdeleted = isdeleted;
                        Use.user_email = model.user_email;

                        //Роли пользователя ДО редактирования
                        List<User_roles> Ur = new List<User_roles>();
                        foreach (User_roles ur in db.User_roles.Where(x => x.userrole_user_id == model.user_id))
                        {
                            Ur.Add(ur);
                        }

                        foreach (Roles r in db.Roles)
                        {
                            if (selroles.Contains(r.role_id))
                            {
                                if (Ur.Find(x => x.userrole_role_id == r.role_id) == null)
                                {
                                    db.User_roles.Add(new User_roles() { userrole_user_id = model.user_id, userrole_role_id = r.role_id });
                                }
                            }
                            else
                            {
                                if (Ur.Find(x => x.userrole_role_id == r.role_id) != null)
                                {
                                    db.User_roles.Remove(Ur.Find(x => x.userrole_role_id == r.role_id));
                                }
                            }
                        }

                        //Отчеты пользователя ДО редактирования
                        List<Report_user_relation> Rur = new List<Report_user_relation>();
                        foreach (Report_user_relation rur in db.Report_user_relation.Where(x => x.rur_user_id == model.user_id))
                        {
                            Rur.Add(rur);
                        }

                        foreach (Report_types rt in db.Report_types)
                        {
                            if (selreports.Contains(rt.report_type_id))
                            {
                                if (Rur.Find(x => x.rur_report_type_id == rt.report_type_id) == null)
                                {
                                    db.Report_user_relation.Add(new Report_user_relation() { rur_user_id = model.user_id, rur_report_type_id = rt.report_type_id });
                                }
                            }
                            else
                            {
                                if (Rur.Find(x => x.rur_report_type_id == rt.report_type_id) != null)
                                {
                                    db.Report_user_relation.Remove(Rur.Find(x => x.rur_report_type_id == rt.report_type_id));
                                }
                            }
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        result = 1;
                    }
                }
                //Создание нового пользователя
                else
                {
                    if (!CheckUserLogin(model.user_login, 0))
                    {
                        Users Use = new Users();
                        Use.user_login = model.user_login;
                        Use.user_password = encryption(model.user_password);
                        Use.user_surname = model.user_surname;
                        Use.user_name = model.user_name;
                        Use.user_patronymic = model.user_patronymic;
                        Use.user_isdeleted = isdeleted;
                        Use.user_email = model.user_email;
                        db.Users.Add(Use);
                        db.SaveChanges();

                        foreach (Roles r in db.Roles)
                        {
                            if (selroles.Contains(r.role_id))
                            {
                                db.User_roles.Add(new User_roles() { userrole_user_id = Use.user_id, userrole_role_id = r.role_id });
                            }
                        }

                        foreach (Report_types rt in db.Report_types)
                        {
                            if (selreports.Contains(rt.report_type_id))
                            {
                                db.Report_user_relation.Add(new Report_user_relation() { rur_user_id = Use.user_id, rur_report_type_id = rt.report_type_id });
                            }
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        result = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}