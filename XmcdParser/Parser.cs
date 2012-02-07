// //-----------------------------------------------------------------------
// // <copyright company="Hibernating Rhinos LTD">
// //     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// // </copyright>
// //-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace XmcdParser
{
	public class Parser
	{
		readonly List<Tuple<Regex,Action<Disk, MatchCollection>>> actions = new List<Tuple<Regex, Action<Disk, MatchCollection>>>();

		public Parser()
		{
			Add(@"^\#\s+xmcd", (disk, collection) =>
			{
				if (collection.Count == 0)
					throw new InvalidDataException("Not an XMCD file");
			});

			Add(@"\# \s* (\d+)", (disk, collection) => 
				disk.TrackFramesOffsets.Add(int.Parse(collection[0].Groups[1].Value)));

			Add(@"Disc \s+ length \s*: \s* (\d+)", (disk, collection) =>
			                                                  disk.DiskLength = int.Parse(collection[0].Groups[1].Value)
				);

			Add("DISCID=(.+)", (disk, collection) =>
			                   disk.DiskIds.AddRange(collection[0].Groups[1].Value.Split(new[] {","},
			                                                                             StringSplitOptions.RemoveEmptyEntries)));

			Add("DTITLE=(.+)", (disk, collection) =>
			{
				var parts = collection[0].Groups[1].Value.Split(new[] {"/"}, 2, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 2)
				{
					disk.Artist = parts[0];
					disk.Title = parts[1];
				}
				else
				{
					disk.Title = parts[0];
				}
			});

			Add(@"DYEAR=(\d+)", (disk, collection) =>
			{
				if (collection.Count == 0)
					return;
				var value = collection[0].Groups[1].Value;
				if(value.Length > 4) // there is data like this
				{
					value = value.Substring(value.Length - 4);
				}
				disk.Year = int.Parse(value);
			}
			);

			Add(@"DGENRE=(.+)", (disk, collection) =>
			{
				if (collection.Count == 0)
					return;
				disk.Genre = collection[0].Groups[1].Value;
			}
			);

			Add(@"TTITLE\d+=(.+)", (disk, collection) =>
			{
				foreach (Match match in collection)
				{
					disk.Tracks.Add(match.Groups[1].Value);
				}	
			});

			Add(@"(EXTD\d*)=(.+)", (disk, collection) =>
			{
				foreach (Match match in collection)
				{
					disk.Attributes[match.Groups[1].Value] = match.Groups[2].Value;
				}
			});
		}

		private void Add(string regex, Action<Disk, MatchCollection> action)
		{
			var key = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
			actions.Add(Tuple.Create(key, action));
		}

		public Disk Parse(string file)
		{
			var disk = new Disk();
			var text = File.ReadAllText(file);
			foreach (var action in actions)
			{
				var collection = action.Item1.Matches(text);
				try
				{
					action.Item2(disk, collection);
				}
				catch (Exception e)
				{
					Console.WriteLine();
					Console.WriteLine(file);
					Console.WriteLine(action.Item1);
					Console.WriteLine(e);
					throw;
				}
			}

			return disk;
		}
	}
}