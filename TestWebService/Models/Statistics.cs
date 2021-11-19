using System;
using System.Diagnostics;
using TxnLogger.Sender;

namespace TestWebService.Models
{
	public class Statistics
	{
		private long _nRequests;
		private double _responseTotalTimeInMilliseconds;

		private readonly object _lockResponseTime = new object();

		public void Clear()
		{
			lock (_lockResponseTime)
			{
				_nRequests = 0;
				_responseTotalTimeInMilliseconds = 0;
			}
		}

		public LoggerResponse LogTransactionAndMeasureResponseTime(Antlr.Runtime.Misc.Func<LoggerResponse> f)
		{
			var watch = new Stopwatch();
			watch.Start();
			var response = f();
			watch.Stop();

			UpdateResponseTime(watch.Elapsed.TotalMilliseconds);

			return response;
		}

		private void UpdateResponseTime(double elapsedMilliseconds)
		{
			lock (_lockResponseTime)
			{
				_nRequests++;
				_responseTotalTimeInMilliseconds += elapsedMilliseconds;
			}
		}

		public object GetResponseTime()
		{
			lock (_lockResponseTime)
			{
				double average = Math.Round(_nRequests == 0 ? 0 : _responseTotalTimeInMilliseconds / _nRequests, 6);

				return new
				{
					NumberOfRequests = _nRequests,
					TotalTimeInMilliseconds = Math.Round(_responseTotalTimeInMilliseconds, 6),
					AverageResponseTimeInMilliseconds = average
				};
			}
		}
	}
}