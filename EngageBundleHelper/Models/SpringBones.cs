using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EngageBundleHelper.Models
{
	/// <summary>
	/// Represents a SpringBone component from Unity
	/// </summary>
	public class SpringBone
	{
		public record AngleLimits
		{
			public required bool Active { get; init; }
			public required float Min { get; init; }
			public required float Max { get; init; }
		}

		public required bool Enabled { get; init; }
		public required int Index { get; init; }
		public required bool EnabledJobSystem { get; init; }
		public required float StiffnessForce { get; init; }
		public required float DragForce { get; init; }
		public required Vector3 SpringForce { get; init; }
		public required float WindInfluence { get; init; }
		public required float AngularStiffness { get; init; }
		public required AngleLimits AngleLimitsY { get; init; }
		public required AngleLimits AngleLimitsZ { get; init; }
		public required float Radius { get; init; }

		public static SpringBone CreateModelFromBaseField(AssetTypeValueField baseField)
		{
			AssetTypeValueField yAngleLimits = baseField["yAngleLimits"];
			AssetTypeValueField zAngleLimits = baseField["zAngleLimits"];

			return new SpringBone()
			{
				Enabled = baseField["m_Enabled"].AsBool,
				Index = baseField["index"].AsInt,
				EnabledJobSystem = baseField["enabledJobSystem"].AsBool,
				StiffnessForce = baseField["stiffnessForce"].AsFloat,
				DragForce = baseField["dragForce"].AsFloat,
				SpringForce = ModelsExtensions.CreateVector3ModelFromField(baseField["springForce"]),
				WindInfluence = baseField["windInfluence"].AsFloat,
				AngularStiffness = baseField["angularStiffness"].AsFloat,
				AngleLimitsY = new AngleLimits()
				{
					Active = yAngleLimits["active"].AsBool,
					Min = yAngleLimits["min"].AsFloat,
					Max = yAngleLimits["max"].AsFloat
				},
				AngleLimitsZ = new AngleLimits()
				{
					Active = zAngleLimits["active"].AsBool,
					Min = zAngleLimits["min"].AsFloat,
					Max = zAngleLimits["max"].AsFloat
				},
				Radius = baseField["radius"].AsFloat
			};
		}

		public void UpdateBaseFieldFromModel(AssetTypeValueField baseField)
		{
			AssetTypeValueField yAngleLimits = baseField["yAngleLimits"];
			AssetTypeValueField zAngleLimits = baseField["zAngleLimits"];

			baseField["m_Enabled"].AsBool = Enabled;
			baseField["index"].AsInt = Index;
			baseField["enabledJobSystem"].AsBool = EnabledJobSystem;
			baseField["stiffnessForce"].AsFloat = StiffnessForce;
			baseField["dragForce"].AsFloat = DragForce;
			SpringForce.UpdateFieldFromModel(baseField["springForce"]);
			baseField["windInfluence"].AsFloat = WindInfluence;
			baseField["angularStiffness"].AsFloat= AngularStiffness;
			yAngleLimits["active"].AsBool = AngleLimitsY.Active;
			yAngleLimits["min"].AsFloat = AngleLimitsY.Min;
			yAngleLimits["max"].AsFloat = AngleLimitsY.Max;
			zAngleLimits["active"].AsBool = AngleLimitsZ.Active;
			zAngleLimits["min"].AsFloat = AngleLimitsZ.Min;
			zAngleLimits["max"].AsFloat = AngleLimitsZ.Max;
			baseField["radius"].AsFloat = Radius;
		}
	}

	public class SpringJobManager
	{
		public required bool Enabled { get; init; }
		public required bool OptimizeTransform { get; init; }
		public required bool IsPaused { get; init; }
		public required int SimulationFrameRate { get; init; }
		public required float DynamicRatio { get; init; }
		public required Vector3 Gravity {  get; init; }
		public required float Bounce { get; init; }
		public required float Friction { get; init; }
		public required float Time { get; init; }
		public required bool EnableAngleLimits { get; init; }
		public required bool EnableCollision { get; init; }
		public required bool EnableLengthLimits { get; init; }
		public required bool CollideWithGround { get; init; }
		public required float GroundHeight { get; init; }
		public required bool WindDisabled { get; init; }
		public required float WindInfluence { get; init; }
		public required Vector3 WindPower { get; init; }
		public required Vector3 WindDir { get; init; }
		public required Vector3 DistanceRate { get; init; }
		public required bool AutomaticReset { get; init; }
		public required float ResetDistance { get ; init; }
		public required float ResetAngle { get; init; }

		public static SpringJobManager CreateModelFromBaseField(AssetTypeValueField baseField)
		{
			return new SpringJobManager()
			{
				Enabled = baseField["m_Enabled"].AsBool,
				OptimizeTransform = baseField["optimizeTransform"].AsBool,
				IsPaused = baseField["isPaused"].AsBool,
				SimulationFrameRate = baseField["simulationFrameRate"].AsInt,
				DynamicRatio = baseField["dynamicRatio"].AsFloat,
				Gravity = ModelsExtensions.CreateVector3ModelFromField(baseField["gravity"]),
				Bounce = baseField["bounce"].AsFloat,
				Friction = baseField["friction"].AsFloat,
				Time = baseField["time"].AsFloat,
				EnableAngleLimits = baseField["enableAngleLimits"].AsBool,
				EnableCollision = baseField["enableCollision"].AsBool,
				EnableLengthLimits = baseField["enableLengthLimits"].AsBool,
				CollideWithGround = baseField["collideWithGround"].AsBool,
				GroundHeight = baseField["groundHeight"].AsFloat,
				WindDisabled = baseField["windDisabled"].AsBool,
				WindInfluence = baseField["windInfluence"].AsFloat,
				WindPower = ModelsExtensions.CreateVector3ModelFromField(baseField["windPower"]),
				WindDir = ModelsExtensions.CreateVector3ModelFromField(baseField["windDir"]),
				DistanceRate = ModelsExtensions.CreateVector3ModelFromField(baseField["distanceRate"]),
				AutomaticReset = baseField["automaticReset"].AsBool,
				ResetDistance = baseField["resetDistance"].AsFloat,
				ResetAngle = baseField["resetAngle"].AsFloat
			};
		}

		public void UpdateBaseFieldFromModel(AssetTypeValueField baseField)
		{
			baseField["m_Enabled"].AsBool = Enabled;
			baseField["optimizeTransform"].AsBool = OptimizeTransform;
			baseField["isPaused"].AsBool = IsPaused;
			baseField["simulationFrameRate"].AsInt = SimulationFrameRate;
			baseField["dynamicRatio"].AsFloat = DynamicRatio;
			Gravity.UpdateFieldFromModel(baseField["gravity"]);
			baseField["bounce"].AsFloat = Bounce;
			baseField["friction"].AsFloat = Friction;
			baseField["time"].AsFloat = Time;
			baseField["enableAngleLimits"].AsBool = EnableAngleLimits;
			baseField["enableCollision"].AsBool = EnableCollision;
			baseField["enableLengthLimits"].AsBool = EnableLengthLimits;
			baseField["collideWithGround"].AsBool = CollideWithGround;
			baseField["groundHeight"].AsFloat = GroundHeight;
			baseField["windDisabled"].AsBool = WindDisabled;
			baseField["windInfluence"].AsFloat = WindInfluence;
			WindPower.UpdateFieldFromModel(baseField["windPower"]);
			WindDir.UpdateFieldFromModel(baseField["windDir"]);
			DistanceRate.UpdateFieldFromModel(baseField["distanceRate"]);
			baseField["automaticReset"].AsBool = AutomaticReset;
			baseField["resetDistance"].AsFloat = ResetDistance;
			baseField["resetAngle"].AsFloat = ResetAngle;
		}
	}

	/// <summary>
	/// Utility class for SpringBone related code
	/// </summary>
	public static class SpringBones
	{
		/// <summary>
		/// Determine whether the baseField has a SpringBone component or SpringJobManager component (or neither), and return create a model for it
		/// </summary>
		public static (SpringBone?, SpringJobManager?) CreateModelFromBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInst, AssetTypeValueField baseField)
		{
			SpringBone? springBoneModel = null;
			SpringJobManager? springJobManagerModel = null;

			AssetTypeValueField components = baseField["m_Component"]["Array"];
			if (components.Children.Count > 1)
			{
				// For now, assume that there can only be one Spring Bone related component and that component is in the second slot
				// Note: If we ever encounter a case where something entirely different is in this second component slot, we'll need to fix our logic to account for that
				AssetTypeValueField secondComponentPPtr = components[1]["component"];
				AssetExternal springComponent = assetsManager.GetExtAsset(assetsFileInst, secondComponentPPtr);

				// Follow another pointer to the m_Script and find the name of the script component
				string scriptName = String.Empty;
				AssetTypeValueField scriptPPtr = springComponent.baseField["m_Script"];
				if (!scriptPPtr.IsDummy)
				{
					AssetExternal scriptComponent = assetsManager.GetExtAsset(assetsFileInst, scriptPPtr);
					scriptName = scriptComponent.baseField["m_Name"].AsString;
				}
				// else, there is no m_Script property, meaning that this component can't be a Spring Bone related component (which are all Script based)

				if (scriptName == "SpringBone")
				{
					springBoneModel = SpringBone.CreateModelFromBaseField(springComponent.baseField);
				}
				else if (scriptName == "SpringJobManager")
				{
					springJobManagerModel = SpringJobManager.CreateModelFromBaseField(springComponent.baseField);
				}
			}
			// else, there are no Spring Bone related components because the Transform component should be the first component

			return (springBoneModel, springJobManagerModel);
		}
	}
}
