using System;
using System.IO;
using System.Reflection;
using Program.Formats;

namespace Program
{
	public class Program
	{
		static void Main(string[] args)
		{
			// Print usage.
			if (args.Length != 2)
			{
				Console.WriteLine($"Usage: {Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)} [input-path] [output-path]");
				Console.WriteLine("input-path:\n\t The path to the input GLA file.");
				Console.WriteLine("output-path:\n\t The path to the output SMD file.");
				return;
			}

			using (Stream source = File.OpenRead(args[0]))
			using (Stream dest = File.Open(args[1], FileMode.Create))
			{
				GLA gla = new GLA();
				gla.Deserialize(source);

				SMD smd = new SMD(gla);
				smd.Serialize(dest);
			}
		}
	}
}