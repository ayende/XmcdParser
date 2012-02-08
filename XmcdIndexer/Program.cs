using System;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using XmcdParser;
using Directory = System.IO.Directory;
using System.Linq;

namespace XmcdIndexer
{
	class Program
	{
		private const string _indexPath = @"z:\CDDBTestIdx";
		private static IndexWriter _indexWriter;
		private static readonly Document ReusableDoc = new Document();
		private static Parser _parser = new Parser();

		static void Main(string[] args)
		{
			_indexWriter = new IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), new StandardAnalyzer(), true,
			                               IndexWriter.MaxFieldLength.UNLIMITED);
			_indexWriter.SetUseCompoundFile(false);

			var sw = Stopwatch.StartNew();

			var i = 0;
			const string start = @"Z:\freedb-complete-20120201\rock\42124f16";
			foreach (var directory in Directory.GetDirectories(@"Z:\freedb-complete-20120201"))
			{
				foreach (var file in Directory.EnumerateFiles(directory))
				{
					if (String.CompareOrdinal(file, start) < 0)
						continue;
					try
					{
						var disk = _parser.Parse(file);
						AddDocShortVersion(disk); // TODO: we can do this in batches, and trigger another thread to do that

						if (i++%1000 == 0)
							Console.Write("\r{0} {1:#,#}           ", file, i);
					}
					catch (Exception e)
					{
						Console.WriteLine();
						Console.WriteLine(file);
						Console.WriteLine(e);
						_indexWriter.Close();
						return;
					}
				}
			}

			sw.Stop();
			Console.WriteLine("Elapsed: " + sw.Elapsed);

			_indexWriter.SetUseCompoundFile(true);
			_indexWriter.Optimize();
			_indexWriter.Close(true);
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
			ReusableDoc.Add(fields[0]);

			fields[1].SetValue(disk.Title);
			ReusableDoc.Add(fields[1]);

			fields[2].SetValue(disk.DiskIds.FirstOrDefault() ?? Guid.NewGuid().ToString());
			ReusableDoc.Add(fields[2]);

			_indexWriter.AddDocument(ReusableDoc);
		}
	}
}
