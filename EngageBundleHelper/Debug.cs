using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper
{
	internal static class Debug
	{
		static Debug()
		{
			IsDebugEnabled = false;
		}
		public static bool IsDebugEnabled { get; set; }

		public static void WriteLine(string message)
		{
			if (IsDebugEnabled)
			{
				Console.WriteLine(message);
			}
		}
	}
}
