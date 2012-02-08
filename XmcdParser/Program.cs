using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;
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
				var count = 0;
				Action<Disk> addToBatch = diskToAdd =>
				{
					session.Store(diskToAdd);
					count += 1;
					if (count < 512) 
						return;

					session.SaveChanges();
					session = store.OpenSession();
					count = 0;
				};

				int i = 0;
				var parser = new Parser();
				var buffer = new byte[1024*1024];// more than big enough for all files

				var sp = Stopwatch.StartNew();

				using (var bz2 = new BZip2InputStream(File.Open(@"D:\Data\freedb-complete-20120101.tar.bz2", FileMode.Open)))
				using (var tar = new TarInputStream(bz2))
				{
					TarEntry entry;
					while((entry=tar.GetNextEntry()) != null)
					{
						if(entry.Size == 0 || entry.Name == "README" || entry.Name == "COPYING")
							continue;
						var readSoFar = 0;
						while(true)
						{
							var read = tar.Read(buffer, readSoFar, ((int) entry.Size) - readSoFar);
							if (read == 0)
								break;

							readSoFar += read;
						}
						// we do it in this fashion to have the stream reader detect the BOM / unicode / other stuff
						// so we can read the values properly
						var fileText = new StreamReader(new MemoryStream(buffer,0, readSoFar)).ReadToEnd();
						try
						{
							var disk = parser.Parse(fileText);
							addToBatch(disk);
							if (i++ % 1000 == 0)
								Console.Write("\r{0} {1:#,#}  {2}         ", entry.Name, i, sp.Elapsed);
						}
						catch (Exception e)
						{
							Console.WriteLine();
							Console.WriteLine(entry.Name);
							Console.WriteLine(e);
							return;
						}
					}
				}
				session.SaveChanges();

				Console.WriteLine();
				Console.WriteLine("Done in {0}", sp.Elapsed);
			}
		}
	}
}