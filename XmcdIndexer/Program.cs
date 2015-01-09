using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using XmcdParser;
using System.Linq;

namespace XmcdIndexer
{
	class Program
	{
		private const string _indexPath = @"D:\Data\ManualIndex";
		private static IndexWriter _indexWriter;
		private static readonly Document ReusableDoc = new Document();

		private static readonly List<Disk> disks = new List<Disk>();

		static void Main()
		{
			_indexWriter = new IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, IndexWriter.MaxFieldLength.UNLIMITED);			

			// TODO: we can do this in batches, and trigger another thread to do that
			var sw = ParseDisks(disk => disks.Add(disk)); 
			
			Console.WriteLine("Elapsed: " + sw.Elapsed);

			sw.Restart();

			foreach (var disk in disks)
			{
				AddDocShortVersion(disk);
			}


			_indexWriter.Optimize();
			_indexWriter.Close(true);

			Console.WriteLine("Elapsed: " + sw.Elapsed);

		}

		private static Stopwatch ParseDisks(Action<Disk> addToBatch)
		{
			int i = 0;
			var parser = new Parser();
			var buffer = new byte[1024 * 1024];// more than big enough for all files

			var sp = Stopwatch.StartNew();

			using (var bz2 = new BZip2InputStream(File.Open(@"D:\Data\freedb-complete-20120101.tar.bz2", FileMode.Open)))
			using (var tar = new TarInputStream(bz2))
			{
				TarEntry entry;
				while ((entry = tar.GetNextEntry()) != null)
				{
					if (entry.Size == 0 || entry.Name == "README" || entry.Name == "COPYING")
						continue;
					var readSoFar = 0;
					while (true)
					{
						var read = tar.Read(buffer, readSoFar, ((int)entry.Size) - readSoFar);
						if (read == 0)
							break;

						readSoFar += read;
					}
					// we do it in this fashion to have the stream reader detect the BOM / unicode / other stuff
					// so we can read the values properly
					var fileText = new StreamReader(new MemoryStream(buffer, 0, readSoFar)).ReadToEnd();
					try
					{
						var disk = parser.Parse(fileText);
						addToBatch(disk);
						if (i++ % 1000 == 0)
							Console.Write("\r{0} {1:#,#}  {2}         ", entry.Name, i, sp.Elapsed);

						if (i % 50000 == 0)
							return sp;
					}
					catch (Exception e)
					{
						Console.WriteLine();
						Console.WriteLine(entry.Name);
						Console.WriteLine(e);
						return sp;
					}
				}
			}
			return sp;
		}

		static readonly Field[] fields = new[]
		{
			new Field("artist", "a", Field.Store.NO, Field.Index.ANALYZED_NO_NORMS),
			new Field("title", "a", Field.Store.NO, Field.Index.ANALYZED_NO_NORMS),
			new Field("__document_id", "a", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS)
	};

		private static void AddDocShortVersion(Disk disk)
		{
			ReusableDoc.GetFields().Clear();

			fields[0].SetValue(disk.Artist);
			if (string.IsNullOrEmpty(disk.Artist) == false)
				ReusableDoc.Add(fields[0]);

			fields[1].SetValue(disk.Title);
			if(string.IsNullOrEmpty(disk.Title) == false)
				ReusableDoc.Add(fields[1]);

			fields[2].SetValue(disk.DiskIds.FirstOrDefault() ?? Guid.NewGuid().ToString());
			ReusableDoc.Add(fields[2]);

			_indexWriter.AddDocument(ReusableDoc);
		}
	}
}
