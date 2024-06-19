using System;
namespace JobApplicationLibrary
{
	public class ApplicationEvaluator
	{
		private const int minAge = 18;
		public ApplicationResult Evaluate (JobApplication form)
		{
			if (form.Application.Age < minAge)
				return ApplicationResult.AutoRejected;
			return ApplicationResult.AutoAccepted;

		}
		
	}

	public enum ApplicationResult
	{
		AutoRejected,
		TransferredToHr,
		TransferredToLead,
		TransferredTCto,
		AutoAccepted
	}
}

