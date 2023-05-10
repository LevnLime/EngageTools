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
			IsDebugEnabled = true;
		}
		public static bool IsDebugEnabled { get; private set; }

		public static void WriteLine(string message)
		{
			if (IsDebugEnabled)
			{
				Console.WriteLine(message);
			}
		}
	}
}
