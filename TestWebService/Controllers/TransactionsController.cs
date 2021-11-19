using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TestWebService.Models;
using TxnLogger.Sender;
using TxnLogger.Sender.Dto;

namespace TestWebService.Controllers
{
	public class TransactionsController : Controller
	{
		private static MultiSearchTransactionData GetMultiSearchTransactionData()
		{
			var sessionId = 0;
			var ipAddress = "127.0.0.1";
			var customerId = "demo";
			var groupId = "main";
			var profId = "eds";
			var userId = 0;
			var cookieGuid = Guid.NewGuid().ToString();

			string[] transactionSpecificParameters = { "p1", "p2" };

			return new MultiSearchTransactionData(ipAddress, sessionId, customerId, groupId, profId, userId, cookieGuid,
				DateTime.Now, new List<SearchProperties> { new SearchProperties { NumberOfResults = 100, ProductCode = "code", SearchDuration = 100}},
				transactionSpecificParameters
			);
		}

		private static readonly Statistics Statistics = new Statistics();


		public ActionResult ResponseTime()
		{
			return Json(Statistics.GetResponseTime(), JsonRequestBehavior.AllowGet);
		}

		public void Clear()
		{
			Statistics.Clear();
			TransactionSender.ClearMultiSearchTransactionsDiagnostic();
		}

		public string Index()
		{

			var data = GetMultiSearchTransactionData();
			Statistics.LogTransactionAndMeasureResponseTime( () => TransactionSender.Instance().LogMultiSearchTransaction(data));
			
			return $"Multi search transaction sent {data.CookieGUID}";
		}

		public ActionResult Diagnostic()
		{
			return Json(TransactionSender.GetMultiSearchTransactionsDiagnostics(), JsonRequestBehavior.AllowGet);
		}
	}
}
