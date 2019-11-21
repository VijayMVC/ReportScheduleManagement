using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ReportScheduleInWeb.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            ViewBag.Action = "Login";
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

        [HttpPost]
        public ActionResult Authorize(LoginViewModel userModel)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if ((userModel.user_password == null) || (userModel.user_password.Trim() == ""))
                {
                    ViewBag.Action = "Login";
                    return View("Index", userModel);
                }
                string pass = encryption(userModel.user_password);
                var userDetails = db.Users.Where(x => x.user_login == userModel.user_login && x.user_password == pass).FirstOrDefault();
                if (userDetails != null && userDetails.user_isdeleted == true)
                {
                    userModel.LoginErrorMessage = "Данный пользователь удалён";
                    ViewBag.Action = "Login";
                    return View("Index", userModel);
                }
                if (userDetails == null)
                {
                    userModel.LoginErrorMessage = "Неправильные логин или пароль";
                    ViewBag.Action = "Login";
                    return View("Index", userModel);
                }
                else
                {
                    Session["userID"] = userDetails.user_id;
                    Session["userSurname"] = userDetails.user_surname;
                    Session["userName"] = userDetails.user_name;

                    return RedirectToAction("Index", "Home");
                }
            }
        }

        [HttpPost]
        public ActionResult Register(LoginViewModel userModel)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if ((userModel.user_new_login == null) || (userModel.user_new_password.Trim() == "") || (userModel.user_confirm_password.Trim() == "") || (userModel.user_email.Trim() == ""))
                {
                    ViewBag.Action = "Register";
                    return View("Index", userModel);
                }

                if (CheckUserLogin(userModel.user_new_login.Trim(), userModel.user_id))
                {
                    userModel.LoginErrorMessage = "Такой логин уже существует!";
                    ViewBag.Action = "Register";
                    return View("Index", userModel);
                }

                if (CheckUserEmail(userModel.user_email.Trim(), userModel.user_id))
                {
                    userModel.LoginErrorMessage = "Такой email уже существует!";
                    ViewBag.Action = "Register";
                    return View("Index", userModel);
                }

                string pass = encryption(userModel.user_new_password);

                Users user = new Users()
                {
                    user_isdeleted = false,
                    user_email = userModel.user_email.Trim(),
                    user_login = userModel.user_new_login.Trim(),
                    user_surname = userModel.user_surname,
                    user_name = userModel.user_name,
                    user_patronymic = userModel.user_patronymic,
                    user_password = pass
                };

                db.Users.Add(user);

                User_roles userrole = new User_roles()
                {
                    userrole_user_id = user.user_id,
                    userrole_role_id = 4
                };

                Session["userID"] = user.user_id;
                Session["userSurname"] = user.user_surname;
                Session["userName"] = user.user_name;

                return RedirectToAction("Index", "Home");
            }
        }

        //Проверка на повтор логина
        public bool CheckUserLogin(string UserLogin, int UserId)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
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
            }

            return false;
        }

        //Проверка на повтор email
        public bool CheckUserEmail(string UserEmail, int UserId)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                try
                {
                    if (UserId != 0)
                    {
                        //Пользователь редактируется, поиск одинакового логина с другим email
                        if (db.Users.Where(x => x.user_id != UserId && x.user_email == UserEmail).FirstOrDefault() != null)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //Новый пользователь, поиск одинакового email
                        if (db.Users.Where(x => x.user_email == UserEmail).FirstOrDefault() != null)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return false;
        }

        public ActionResult LogOut()
        {
            int userId = (int)Session["userID"];
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
    }
}