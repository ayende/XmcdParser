using System;
using System.IO;
using System.Linq;
using System.Text;

namespace XmcdParser
{
	class Program
	{
		static void Main()
		{
			int i = 0;
			var parser = new Parser();
			foreach (var directory in Directory.GetDirectories(@"D:\Data\freedb-complete-20120101"))
			{
				foreach (var file in Directory.EnumerateFiles(directory))
				{
					try
					{
						var disk = parser.Parse(file);
						if(i++ % 1000 == 0)
							Console.Write("\r{0} {1}", file, i);
					}
					catch (Exception e)
					{
						Console.WriteLine();
						Console.WriteLine(file);
						Console.WriteLine(e);
						return;
					}				
				}
			}
		}
	}
}
