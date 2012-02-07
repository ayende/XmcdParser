using System;
using System.IO;
using System.Linq;
using System.Text;
using Raven.Client.Document;

namespace XmcdParser
{
	class Program
	{
		static void Main()
		{
			using (var store = new DocumentStore
			{
				Url = "http://localhost:8080"
			}.Initialize())
			{
				var session = store.OpenSession();
				int count = 0;
				Action<Disk> addToBatch = diskToAdd =>
				{
					session.Store(diskToAdd);
					count += 1;
					if (count%512 == 0)
					{
						session.SaveChanges();
						session = store.OpenSession();
						count = 0;
					}
				};

				int i = 0;
				var parser = new Parser();
				var start = @"D:\Data\freedb-complete-20120101\rock\42124f16";
				foreach (var directory in Directory.GetDirectories(@"D:\Data\freedb-complete-20120101"))
				{
					foreach (var file in Directory.EnumerateFiles(directory))
					{
						if (file.CompareTo(start) < 0)
							continue;
						try
						{
							var disk = parser.Parse(file);
							addToBatch(disk);
							if (i++ % 1000 == 0)
								Console.Write("\r{0} {1:#,#}           ", file, i);
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
				session.SaveChanges();
			}
		}
	}
}