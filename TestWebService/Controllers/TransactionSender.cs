using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using JetBrains.Lifetimes;
using TxnLogger.Reporting;
using TxnLogger.Sender.Buffering;
using TxnLogger.Sender.Facade.SenderBuilder;
using TxnLogger.Sender.Facade.SenderBuilder.Config;

namespace TestWebService.Controllers
{
	public class TransactionSender
	{
		private static readonly object Lock = new object();
		
		private static ITransactionSender _sender;

		private class CustomReporter : IStatisticReporter
		{
			private long _sentMessages;
			private long _deliveredToServiceMessages;

			public void ReportElapsedLogTransaction(TimeSpan elapsedTime)
			{
				//throw new NotImplementedException();
			}

			public void ReportElapsedLogMultiSearchTransaction(TimeSpan elapsedTime)
			{
				//throw new NotImplementedException();
			}

			public void IncrementReceivedMessagesNumber()
			{
				Interlocked.Increment(ref _sentMessages);
			}

			public void IncrementSentMessagesNumber()
			{
				Interlocked.Increment(ref _deliveredToServiceMessages);
			}
		}

		private static readonly CustomReporter Reporter = new CustomReporter();


		private class DiagnosticReporter : IBufferingDiagnostic
		{
			private readonly object _lock = new object();

			private class Report
			{
				public long Failed { get; set; }
				public long Ok { get; set; }

				public long SavedToDisk { get; set; }

				public long ResentOk { get; set; }
				public long ResentFailed { get; set; }
				public long FailedToSaveToDisk { get; set; }

				public void Clear()
				{
					Failed = 0;
					Ok = 0;
					SavedToDisk = 0;
					ResentOk = 0;
					ResentFailed = 0;
					FailedToSaveToDisk = 0;
					FailedIds.Clear();
				}

				public HashSet<string> FailedIds { get; } = new HashSet<string>();

				public object GetReport()
				{
					return new
					{
						Failed = Failed,
						Sent = Ok,
						Saved = SavedToDisk,
						ResentOk = ResentOk,
						ResentFail = ResentFailed,
						//	LeftFailed = _failedGuids.Count,
						FailedToSaveToDisk = FailedToSaveToDisk
					};
				}
			}

			private readonly Report _transactionsReport = new Report();
			private readonly Report _multiSearchTransactionsReport = new Report();
			
			public object GetTransactionsReport()
			{
				lock (_lock)
				{
					return _transactionsReport.GetReport();
				}
			}

			public object GetMultiSearchTransactionsReport()
			{
				lock (_lock)
				{
					return _multiSearchTransactionsReport.GetReport();
				}
			}

			public void ClearTransactionsReport()
			{
				lock (_lock)
				{
					_transactionsReport.Clear();
				}
			}

			public void ClearMultiSearchTransactionsReport()
			{
				lock (_lock)
				{
					_multiSearchTransactionsReport.Clear();
				}
			}

			private Report GetReportByMessageType(string messageType) =>
				messageType == "Single" ? _transactionsReport : _multiSearchTransactionsReport;

			public void OnLogMultiSearchTransaction(LogMultiSearchTransactionData data)
			{
				lock (_lock)
				{
					if (data.Response.HasError)
					{
						if (data.IsFromBuffer)
						{
							_multiSearchTransactionsReport.ResentFailed++;
						}
						else
						{
							_multiSearchTransactionsReport.Failed++;
						}
					}
					else
					{
						if (data.IsFromBuffer)
						{
							_multiSearchTransactionsReport.ResentOk++;
						}
						else
						{
							_multiSearchTransactionsReport.Ok++;
						}
					}
				}
			}

			public void OnLogTransaction(LogTransactionData data)
			{
				lock (_lock)
				{
					if (data.Response.HasError)
					{
						if (data.IsFromBuffer)
						{
							_transactionsReport.ResentFailed++;
						}
						else
						{
							_transactionsReport.Failed++;
						}
					}
					else
					{
						if (data.IsFromBuffer)
						{
							_transactionsReport.ResentOk++;
						}
						else
						{
							_transactionsReport.Ok++;
						}
					}
				}
			}

			public void OnWriteTransactionMessage(SaveTransactionToFileData data)
			{
				lock (_lock)
				{
					var report = GetReportByMessageType(data.MessageType);

					if (data.IsOk)
					{
						report.SavedToDisk++;
					}
					else
					{
						report.FailedToSaveToDisk++;
					}
				}
			}

			public void OnCreateTxnBufferFile(CreateBufferFileData data)
			{
				//throw new NotImplementedException();
			}
		}

		private static readonly DiagnosticReporter Diagnostic = new DiagnosticReporter();

		public static object GetTransactionsDiagnostics()
		{
			return Diagnostic.GetTransactionsReport();
		}

		public static object GetMultiSearchTransactionsDiagnostics()
		{
			return Diagnostic.GetMultiSearchTransactionsReport();
		}

		public static void ClearTransactionsDiagnostic()
		{
			Diagnostic.ClearTransactionsReport();
		}

		public static void ClearMultiSearchTransactionsDiagnostic()
		{
			Diagnostic.ClearMultiSearchTransactionsReport();
		}

		public static void Initialize()
		{
			lock (Lock)
			{
				if (_sender != null)
				{
					return ;
				}

				var restBaseUrl = ConfigurationManager.AppSettings["TransactionServiceBaseAddress"];
				//"https://gateway-classictxn-dev.eks.ehost-devqa.eislz.com/public";


				var targetServicePath = "activemq:failover:(tcp://ads1-txnlogamq.epnet.com:61616?keepAlive=true)";

                _sender = TxnSenderBuilder.New(TimeSpan.FromSeconds(15),
					new RestClientConfig(
						new Uri($"{restBaseUrl}/transaction"), // TxnRestSingleUrl
						new Uri($"{restBaseUrl}/transactions") // TxnRestBatchUrl
					),
					new AmqClientConfig(targetServicePath // TxnAmqConnection
					),
					new BufferizationConfig("C:\\Temp\\TxnLogger", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)))
					.WithEnvironment(() => true)
					//.WithoutFireAndForget()
					//.WithStatisticReporter(Reporter)
					.WithDiagnostic(Diagnostic)
					.Create(Lifetime.Eternal, TimeSpan.FromMilliseconds(500));			}
		}

		public static ITransactionSender Instance() => _sender;
	}
}