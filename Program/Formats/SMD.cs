using System;
using System.Numerics;

namespace Program.Formats
{
	public class SMD
	{
		public SMD() {}

		public SMDNode[] Nodes { get; set; }
		public SMDFrame[] Skeleton { get; set; }

		public SMD(GLA gla) : this()
		{
			// Build node tree
			Nodes = new SMDNode[gla.Header.NumBones];
			for (int i = 0; i < gla.Header.NumBones; i++)
			{
				var bone = gla.Bones[i];

				Nodes[i] = new SMDNode()
				{
					ID = i,
					Name = bone.Name.Replace("\0", string.Empty),
					ParentID = bone.Parent
				};
			}

			// Build per-frame data
			Skeleton = new SMDFrame[gla.Header.NumFrames];
			for (int i = 0; i < gla.Header.NumFrames; i++)
			{
				// Bone positions
				var positions = new SMDBonePosition[gla.Header.NumBones];
				for (int j = 0; j < gla.Header.NumBones; j++)
				{
					var boneMatrix = (Matrix4x4)gla.Bones[j].BasePoseMat.Matrix * (Matrix4x4)gla.GetBoneAtFrame(i, j).Matrix;

					positions[j] = new SMDBonePosition()
					{
						BoneID = j,
						Rotation = new Vector3(boneMatrix.M31, boneMatrix.M32, boneMatrix.M33) * (MathF.PI / 180f),
						Position = new Vector3(boneMatrix.M41, boneMatrix.M42, boneMatrix.M43),
					};
				}

				// Frame metadata
				Skeleton[i] = new SMDFrame()
				{
					Time = i * (1 / 30), // Assume 30fps for now
					BonePositions = positions
				};
			}
		}

		public void Serialize(Stream dest)
		{
			using (StreamWriter writer = new StreamWriter(dest))
			{
				writer.WriteLine("version 1");

				// Write bones/nodes
				writer.WriteLine("nodes");
				foreach (var node in Nodes)
				{
					writer.WriteLine($"{node.ID} \"{node.Name}\" {node.ParentID}");
				}
				writer.WriteLine("end");

				// Write skeleton
				writer.WriteLine("skeleton");
				foreach (var frame in Skeleton)
				{
					writer.WriteLine($"time {frame.Time}");
					foreach (var bone in frame.BonePositions)
					{
						writer.WriteLine($"{bone.BoneID} {bone.Position.X:0.######} {bone.Position.Y:0.######} {bone.Position.Y:0.######} " +
							$"{bone.Rotation.X:0.######} {bone.Rotation.Y:0.######} {bone.Rotation.Y:0.######}");
					}
				}
				writer.WriteLine("end");
			}
		}
	}

	public struct SMDNode
	{
		public int ID;
		public string Name;
		public int ParentID;
	}

	public struct SMDFrame
	{
		public float Time;
		public SMDBonePosition[] BonePositions;
	}

	public struct SMDBonePosition
	{
		public int BoneID;
		public Vector3 Position;
		public Vector3 Rotation;
	}
}
