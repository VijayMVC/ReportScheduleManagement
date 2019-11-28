using System.Net;
using System.Net.Mail;
using System.Web.Configuration;

namespace ReportScheduleInForm
{
	public static class MailHelper
	{
		private static readonly string smtpHost = WebConfigurationManager.AppSettings["smtp:host"];
		private static readonly string smtpUser = WebConfigurationManager.AppSettings["smtp:user"];
		private static readonly string smtpPass = WebConfigurationManager.AppSettings["smtp:password"];

		public static SmtpClient GetInstance()
		{
			var instance = new SmtpClient(smtpHost);
			instance.Credentials = new NetworkCredential(smtpUser, smtpPass);

			return instance;
		}
	}
}