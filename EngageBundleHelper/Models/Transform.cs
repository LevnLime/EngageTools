using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Models
{
	/// <summary>
	///  Represents a Unity Transform component (the most basic component on all Game Objects)
	/// </summary>
	public class Transform
	{
		public required Vector3 Position { get; init; }
		public required Vector4 Rotation { get; init; }
		public required Vector3 Scale { get; init; }

		public static Transform CreateModelFromBaseField(AssetTypeValueField baseField)
		{
			AssetTypeValueField positionData = baseField["m_LocalPosition"];
			AssetTypeValueField rotationData = baseField["m_LocalRotation"];
			AssetTypeValueField scaleData = baseField["m_LocalScale"];

			Vector3 position = new Vector3(positionData["x"].AsFloat, positionData["y"].AsFloat, positionData["z"].AsFloat);
			Vector4 rotation = new Vector4(rotationData["x"].AsFloat, rotationData["y"].AsFloat, rotationData["z"].AsFloat, rotationData["w"].AsFloat);
			Vector3 scale = new Vector3(scaleData["x"].AsFloat, scaleData["y"].AsFloat, scaleData["z"].AsFloat);

			return new Transform()
			{
				Position = position,
				Rotation = rotation,
				Scale = scale
			};
		}

		public void UpdateBaseFieldFromModel(AssetTypeValueField baseField)
		{
			Vector3 newPosition = Position;
			Vector4 newRotation = Rotation;
			Vector3 newScale = Scale;

			AssetTypeValueField position = baseField["m_LocalPosition"];
			AssetTypeValueField rotation = baseField["m_LocalRotation"];
			AssetTypeValueField scale = baseField["m_LocalScale"];

			position["x"].AsFloat = newPosition.X;
			position["y"].AsFloat = newPosition.Y;
			position["z"].AsFloat = newPosition.Z;
			rotation["x"].AsFloat = newRotation.X;
			rotation["y"].AsFloat = newRotation.Y;
			rotation["z"].AsFloat = newRotation.Z;
			rotation["w"].AsFloat = newRotation.W;
			scale["x"].AsFloat = newScale.X;
			scale["y"].AsFloat = newScale.Y;
			scale["z"].AsFloat = newScale.Z;
		}
	}
}
