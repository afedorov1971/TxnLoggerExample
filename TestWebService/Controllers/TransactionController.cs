using System;
using System.Web.Mvc;
using TestWebService.Models;
using TxnLogger.Sender.Dto;

namespace TestWebService.Controllers
{
	public class TransactionController : Controller
	{
		private static TransactionData GetTransactionData()
		{
			var sessionId = 0;
			var ipAddress = "127.0.0.1";
			var customerId = "demo";
			var groupId = "main";
			var profId = "eds";
			var userId = 0;
			var activityId = 113;
			var cookieGuid = Guid.NewGuid().ToString();

			string[] transactionSpecificParameters = { "p1", "p2" };

			return new TransactionData(
				ipAddress, sessionId, customerId, groupId, profId, userId, activityId, cookieGuid, DateTime.Now,
				transactionSpecificParameters);
		}

		public object ResponseTime()
		{
			return Json(Statistics.GetResponseTime(), JsonRequestBehavior.AllowGet);
		}

		public void Clear()
		{
			Statistics.Clear();
			TransactionSender.ClearTransactionsDiagnostic();
		}

		private static readonly Statistics Statistics = new Statistics();
		
		public string Index()
		{
			var data = GetTransactionData();
			
    		Statistics.LogTransactionAndMeasureResponseTime(() => TransactionSender.Instance().LogTransaction(data));

			return $"Transaction sent {data.CookieGUID}";
		}

		public ActionResult Diagnostic()
		{
			return Json(TransactionSender.GetTransactionsDiagnostics(), JsonRequestBehavior.AllowGet);
		}
	}
}
