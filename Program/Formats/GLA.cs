using System;
using System.Numerics;
using System.Reflection.PortableExecutable;
using Program.Helpers;

namespace Program.Formats
{
	public class GLA
	{
		public MDXAHeader Header { get; private set; } = default;
		public MDXASkel[] Bones { get; private set; } = default;
		public MDXABone[] BonesData { get; private set; } = null;

		// Mapping between bone/frame IDs and their data indices.
		public Dictionary<(int frame, int bone), int> FrameTable { get; private set; } = null;

		public void Deserialize(Stream source)
		{
			using (BinaryReader reader = new BinaryReader(source))
			{
				// Read header
				Header = MDXAHeader.Deserialize(reader);

				// Read skeleton data
				reader.BaseStream.Seek(Header.OfsSkel, SeekOrigin.Begin);
				Bones = new MDXASkel[Header.NumBones];
				for (int i = 0; i < Header.NumBones; i++)
				{
					Bones[i] = MDXASkel.Deserialize(reader);
				}

				// Build frame/bone data mapping
				FrameTable = new(Header.NumFrames * Header.NumBones);
				for (int i = 0; i < Header.NumFrames; i++)
				{
					for (int j = 0; j < Header.NumBones; j++)
					{
						int iOffsetToIndex = (i * Header.NumBones * 3) + (j * 3);

						reader.BaseStream.Seek(Header.OfsFrames + iOffsetToIndex, SeekOrigin.Begin);
						byte[] index = reader.ReadBytes(3);
						FrameTable[(i, j)] = (index[2] << 16) + (index[1] << 8) + index[0];
					}
				}

				// Determine number of compressed bones by iterating through frames.
				int maxBoneIndex = 0;
				for (int i = 0; i < Header.NumFrames; i++)
				{
					for (int j = 0; j < Header.NumBones; j++)
					{
						int index = FrameTable[(i, j)];

						if (maxBoneIndex < index)
						{
							maxBoneIndex = index;
						}
					}
				}

				// Read compressed bone data
				reader.BaseStream.Seek(Header.OfsCompBonePool, SeekOrigin.Begin);
				BonesData = new MDXABone[maxBoneIndex + 1];
				for (int i = 0; i <= maxBoneIndex; i++)
				{
					byte[] compBone = reader.ReadBytes(14); // mdxaCompQuatBone_t
					BonesData[i] = new MDXABone(compBone); // mdxaBone_t
				}
			}
		}

		public MDXABone GetBoneAtFrame(int frameID, int boneID)
		{
			return BonesData[FrameTable[(frameID, boneID)]];
		}
	}

	// mdxaHeader_t
	public struct MDXAHeader
	{
		// Source: https://github.com/JACoders/OpenJK/blob/master/codemp/rd-common/mdx_format.h
		public int Ident; // "IDP3" = MD3, "RDM5" = MDR, "2LGA"(GL2 Anim) = MDXA
		public int Version; // 1,2,3 etc as per format revision
		public string Name; // GLA name (eg "skeletons/marine")	// note: extension missing
		public float Scale; // will be zero if build before this field was defined, else scale it was built with

		// Frames and bones are shared by all levels of detail
		public int NumFrames;
		public int OfsFrames; // points at mdxaFrame_t array
		public int NumBones; // (no offset to these since they're inside the frames array)
		public int OfsCompBonePool; // offset to global compressed-bone pool that all frames use
		public int OfsSkel; // offset to mdxaSkel_t info
		public int OfsEnd; // EOF, which of course gives overall file size

		public static MDXAHeader Deserialize(BinaryReader reader)
		{
			return new MDXAHeader()
			{
				Ident = reader.ReadInt32(),
				Version = reader.ReadInt32(),
				Name = new string(reader.ReadChars(64)), // MAX_QPATH
				Scale = reader.ReadSingle(),
				NumFrames = reader.ReadInt32(),
				OfsFrames = reader.ReadInt32(),
				NumBones = reader.ReadInt32(),
				OfsCompBonePool = reader.ReadInt32(),
				OfsSkel = reader.ReadInt32(),
				OfsEnd = reader.ReadInt32(),
			};
		}
	}

	// mdxaSkel_t
	public struct MDXASkel
	{
		// Source: https://github.com/JACoders/OpenJK/blob/master/codemp/rd-common/mdx_format.h
		public string Name; // name of bone
		public uint Flags;
		public int Parent; // index of bone that is parent to this one, -1 = NULL/root
		public MDXABone BasePoseMat; // base pose
		public MDXABone BasePoseMatInv; // inverse, to save run-time calc
		public int NumChildren; // number of children bones
		public int[] Children; // [mdxaSkel_t->numChildren] (variable sized)

		public static MDXASkel Deserialize(BinaryReader reader)
		{
			MDXASkel skeleton = new MDXASkel()
			{
				Name = new string(reader.ReadChars(64)), // MAX_QPATH
				Flags = reader.ReadUInt32(),
				Parent = reader.ReadInt32(),
				BasePoseMat = reader.ReadMatrix3x4(),
				BasePoseMatInv = reader.ReadMatrix3x4(),
				NumChildren = reader.ReadInt32(),
			};

			skeleton.Children = new int[skeleton.NumChildren];
			for (int i = 0; i < skeleton.NumChildren; i++)
			{
				skeleton.Children[i] = reader.ReadInt32();
			}

			return skeleton;
		}
	}

	// mdxaBone_t
	public struct MDXABone
	{
		public Matrix3x4 Matrix; // float[3][4]

		public MDXABone() {}
		public unsafe MDXABone(byte[] comp) : this()
		{
			Matrix3x4 mat = default;
			fixed (byte* pwInBytes = comp)
			{
				ushort* pwIn = (ushort*)pwInBytes;
				float w = *pwIn++;
				w /= 16383.0f;
				w -= 2.0f;
				float x = *pwIn++;
				x /= 16383.0f;
				x -= 2.0f;
				float y = *pwIn++;
				y /= 16383.0f;
				y -= 2.0f;
				float z = *pwIn++;
				z /= 16383.0f;
				z -= 2.0f;

				float fTx  = 2.0f * x;
				float fTy  = 2.0f * y;
				float fTz  = 2.0f * z;
				float fTwx = fTx * w;
				float fTwy = fTy * w;
				float fTwz = fTz * w;
				float fTxx = fTx * x;
				float fTxy = fTy * x;
				float fTxz = fTz * x;
				float fTyy = fTy * y;
				float fTyz = fTz * y;
				float fTzz = fTz * z;

				// rot...
				mat[0, 0] = 1.0f - (fTyy+fTzz);
				mat[0, 1] = fTxy - fTwz;
				mat[0, 2] = fTxz + fTwy;
				mat[1, 0] = fTxy + fTwz;
				mat[1, 1] = 1.0f - (fTxx+fTzz);
				mat[1, 2] = fTyz - fTwx;
				mat[2, 0] = fTxz - fTwy;
				mat[2, 1] = fTyz +fTwx;
				mat[2, 2] = 1.0f - (fTxx+fTyy);

				// xlat...
				float f = *pwIn++;
				f /= 64;
				f -= 512;
				mat[0, 3] = f;

				f = *pwIn++;
				f /= 64;
				f -= 512;
				mat[1, 3] = f;

				f = *pwIn++;
				f /= 64;
				f -= 512;
				mat[2, 3] = f;
			}

			Matrix = mat;
		}

		public static implicit operator MDXABone(Matrix3x4 value) => new MDXABone() { Matrix = value };
		public static implicit operator Matrix3x4(MDXABone value) => value.Matrix;
	}
}
