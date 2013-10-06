using System;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;
using Raven.Client.Document;

namespace XmcdParser
{
	class Program
	{
		private const int BatchSize = 24;
		static void Main()
		{
			using (var store = new DocumentStore
			{
				Url = "http://localhost:8080",
				DefaultDatabase = "freedb"
			}.Initialize())
			{
			    var sp = Stopwatch.StartNew();
                using (var insert = store.BulkInsert())
                {
                    insert.Report += Console.WriteLine;
                    ParseDisks(insert);
                }

				Console.WriteLine();
				Console.WriteLine("Done in {0}", sp.Elapsed);
			}
		}

		private static void ParseDisks(BulkInsertOperation insert)
		{
			int i = 0;
			var parser = new Parser();
			var buffer = new byte[1024*1024];// more than big enough for all files

            using (var bz2 = new BZip2InputStream(File.Open(@"C:\Users\Ayende\Downloads\freedb-complete-20130901.tar.bz2", FileMode.Open)))
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
						insert.Store(disk);
					}
					catch (Exception e)
					{
						Console.WriteLine();
						Console.WriteLine(entry.Name);
						Console.WriteLine(e);
					}
				}
			}
		}
	}
}