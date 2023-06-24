using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Models
{
	public static class ModelsExtensions
	{
		public static Vector3 CreateVector3ModelFromField(AssetTypeValueField field)
		{
			return new Vector3(field["x"].AsFloat, field["y"].AsFloat, field["z"].AsFloat);
		}

		public static void UpdateFieldFromModel(this Vector3 vector, AssetTypeValueField field)
		{
			field["x"].AsFloat = vector.X;
			field["y"].AsFloat = vector.Y;
			field["z"].AsFloat = vector.Z;
		}
	}
}
