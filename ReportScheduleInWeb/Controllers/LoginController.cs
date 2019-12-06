using ReportScheduleInWeb.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Net.Mail;
using System.Web.Configuration;
using ReportScheduleInWeb.App.Tool;
using System.Collections.Generic;

namespace ReportScheduleInWeb.Controllers
{
    public class LoginController : Controller
    {
        private readonly string smtpFrom = WebConfigurationManager.AppSettings["smtp:from"];

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

                List<int> userRoles = db.User_roles.Where(x => x.userrole_user_id == userDetails.user_id).Select(x => x.userrole_role_id).ToList();

                if (userRoles == null)
                {
                    userModel.LoginErrorMessage = "Пользователю не назначена ни одна роль";
                    ViewBag.Action = "Login";
                    return View("Index", userModel);
                }

                Session["userID"] = userDetails.user_id;
                Session["userSurname"] = userDetails.user_surname;
                Session["userName"] = userDetails.user_name;
                Session["userRoles"] = userRoles;

                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Activate(string user_guid)
        {
            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    Guid guid = Guid.Parse(user_guid);

                    Registered reg = db.Registered.Where(x => x.reg_guid == guid).SingleOrDefault();

                    if (reg == null)
                    {
                        ViewBag.Action = "Не найдено в таблице запроса на регистрацию. Возможно вы уже зарегистрированы.";
                        return View("Index", null);
                    }

                    if (CheckUserLogin(reg.reg_login, 0))
                    {
                        ViewBag.Action = "Пользователь под таким логином уже был зарегистрирован.";
                        return View("Index", null);
                    }

                    if (CheckUserEmail(reg.reg_email, 0))
                    {
                        ViewBag.Action = "Пользователь с таким email уже был зарегистрирован.";
                        return View("Index", null);
                    }

                    Users user = new Users()
                    {
                        user_email = reg.reg_email,
                        user_isdeleted = false,
                        user_login = reg.reg_login,
                        user_password = reg.reg_password,
                        user_surname = reg.reg_surname,
                        user_name = reg.reg_name,
                        user_patronymic = reg.reg_patronymic
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    User_roles userrole = new User_roles()
                    {
                        userrole_user_id = user.user_id,
                        userrole_role_id = 4
                    };

                    db.User_roles.Add(userrole);
                    db.SaveChanges();

                    db.Registered.Remove(reg);
                    db.SaveChanges();

                    List<int> userRoles = db.User_roles.Where(x => x.userrole_user_id == user.user_id).Select(x => x.userrole_role_id).ToList();

                    Session["userID"] = user.user_id;
                    Session["userSurname"] = user.user_surname;
                    Session["userName"] = user.user_name;
                    Session["userRoles"] = userRoles;
                }

                ViewBag.Action = "Login";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpPost]
        public ActionResult Register(LoginViewModel userModel)
        {
            try
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

                    Guid guid = Guid.NewGuid();

                    Registered reg = new Registered()
                    {
                        reg_email = userModel.user_email.Trim(),
                        reg_login = userModel.user_new_login.Trim(),
                        reg_surname = userModel.user_surname,
                        reg_name = userModel.user_name,
                        reg_patronymic = userModel.user_patronymic,
                        reg_password = pass,
                        reg_guid = guid
                    };

                    SendMail(reg.reg_email, guid.ToString());

                    db.Registered.Add(reg);
                    db.SaveChanges();

                    ViewBag.Action = "Registered";
                    return View("Index", userModel);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public ActionResult Forgot(LoginViewModel userModel)
        {
            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    if (userModel.user_email.Trim() == "")
                    {
                        ViewBag.Action = "Forgot";
                        return View("Index", userModel);
                    }

                    if (!CheckUserEmail(userModel.user_email.Trim(), userModel.user_id))
                    {
                        userModel.LoginErrorMessage = "Такой email в системе не зарегистрирован!";
                        ViewBag.Action = "Forgot";
                        return View("Index", userModel);
                    }

                    Users user = db.Users.Where(x => x.user_email == userModel.user_email.Trim()).SingleOrDefault();

                    Guid guid = Guid.NewGuid();

                    Reminded remind = new Reminded()
                    {
                        remind_guid = guid,
                        remind_user_id = user.user_id
                    };

                    SendMailForgot(user.user_id, user.user_email, guid.ToString());

                    db.Reminded.Add(remind);
                    db.SaveChanges();

                    ViewBag.Action = "Forgoted";
                    return View("Index", userModel);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult Remind(string user_guid)
        {
            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    Guid guid = Guid.Parse(user_guid);

                    Reminded remind = db.Reminded.Where(x => x.remind_guid == guid).SingleOrDefault();

                    if (remind == null)
                    {
                        ViewBag.Action = "Не найдено в таблице запроса на восстановление пароля.";
                        return View("Index", null);
                    }

                    if (remind.remind_user_id == 0)
                    {
                        ViewBag.Action = "Неопознанная ошибка при восстановлении пароля. Сделайте запрос на восстановление еще раз";
                        return View("Index", null);
                    }

                    ViewBag.Action = "Remind";
                    ViewBag.RemindGUID = user_guid;
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }

        public void SendMail(string email, string guid)
        {
            var mail = MailHelper.GetInstance();

            var body = new StringBuilder();
            body.AppendLine("Здравствуйте!");
            body.AppendLine().AppendLine("Перейдите по ссылке http://" + Request.Url.Host + Url.Action("Activate","Login",new { user_guid = guid }) + ", чтобы завершить регистрацию и войти в систему.");
            body.AppendLine().AppendLine("С уважением, центр «Мои Документы».");
            body.AppendLine().AppendLine("---");
            body.AppendLine("Данное сообщение сформировано автоматически. Пожалуйста, не отвечайте на него.");
            body.AppendLine("УВЕДОМЛЕНИЕ О КОНФИДЕНЦИАЛЬНОСТИ: Это электронное сообщение и любые документы, приложенные к нему, содержат конфиденциальную информацию. Настоящим уведомляем Вас о том, что если это сообщение не предназначено Вам, использование, копирование, распространение информации, содержащейся в настоящем сообщении, а также осуществление любых действий на основе этой информации, строго запрещено. Если Вы получили это сообщение по ошибке, пожалуйста, сообщите об этом отправителю по электронной почте и удалите это сообщение.");
            body.AppendLine();

            var message = new MailMessage();
            message.From = new MailAddress(smtpFrom);
            message.ReplyToList.Add(new MailAddress("grizzled@mfcsakha.ru", "no-reply"));
            message.Subject = "Регистрация в АИС \"Планировщик отчетов\"";
            message.Body = body.ToString();
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = false;

            message.To.Add(new MailAddress(email));

            try
            {
                mail.Send(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SendMailForgot(int id, string email, string guid)
        {
            var mail = MailHelper.GetInstance();

            var body = new StringBuilder();
            body.AppendLine("Здравствуйте!");
            body.AppendLine().AppendLine("Поступил запрос на восстановление пароля. Перейдите по ссылке http://" + Request.Url.Host + Url.Action("Remind", "Login", new { user_guid = guid }) + ", чтобы ввести новый пароль.");
            body.AppendLine().AppendLine("С уважением, центр «Мои Документы».");
            body.AppendLine().AppendLine("---");
            body.AppendLine("Данное сообщение сформировано автоматически. Пожалуйста, не отвечайте на него.");
            body.AppendLine("УВЕДОМЛЕНИЕ О КОНФИДЕНЦИАЛЬНОСТИ: Это электронное сообщение и любые документы, приложенные к нему, содержат конфиденциальную информацию. Настоящим уведомляем Вас о том, что если это сообщение не предназначено Вам, использование, копирование, распространение информации, содержащейся в настоящем сообщении, а также осуществление любых действий на основе этой информации, строго запрещено. Если Вы получили это сообщение по ошибке, пожалуйста, сообщите об этом отправителю по электронной почте и удалите это сообщение.");
            body.AppendLine();

            var message = new MailMessage();
            message.From = new MailAddress(smtpFrom);
            message.ReplyToList.Add(new MailAddress("grizzled@mfcsakha.ru", "no-reply"));
            message.Subject = "Запрос на восстановление пароля в АИС \"Планировщик отчетов\"";
            message.Body = body.ToString();
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = false;

            message.To.Add(new MailAddress(email));

            try
            {
                mail.Send(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult ChangePassword(UserViewModel model)
        {
            var result = 0;
            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    Guid guid = Guid.Parse(Request.Form["guid"].ToString());

                    Reminded remind = db.Reminded.Where(x => x.remind_guid == guid).SingleOrDefault();

                    if ((remind.remind_user_id > 0) && (model.user_password != null) && (model.user_password.Trim() != ""))
                    {
                        Users Use = db.Users.SingleOrDefault(x => x.user_id == remind.remind_user_id);
                        Use.user_password = encryption(model.user_password);
                        db.SaveChanges();

                        db.Reminded.Remove(remind);
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
    }
}