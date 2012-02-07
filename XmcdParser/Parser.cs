// //-----------------------------------------------------------------------
// // <copyright company="Hibernating Rhinos LTD">
// //     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// // </copyright>
// //-----------------------------------------------------------------------
using System;
using System.IO;

namespace XmcdParser
{
	public class Parser
	{
		public Disk Parse(string file)
		{
			var disk = new Disk();
			State state = new InitialState();

			state.ChangeState = newState =>
			{
				newState.ChangeState = state.ChangeState;
				newState.Disk = disk;
				state = newState;
			};

			foreach (var line in File.ReadLines(file))
			{
				try
				{
					state.ReadLine(line);
				}
				catch (Exception e)
				{
					throw new InvalidDataException("Could not parse: " + line, e);
				}
			}

			return disk;
		}

		#region Nested type: DiskLengthState

		public class DiskLengthState : State
		{
			public override void ReadLine(string line)
			{
				var indexOf = line.IndexOf("Disc length:", StringComparison.InvariantCultureIgnoreCase);
				if (indexOf != -1)
				{
					var start = indexOf + "Disc length: ".Length;
					var trimmed = line.Substring(start).Trim();
					var end = trimmed.IndexOf(" ");
					string len = end != -1 ? trimmed.Substring(0, end) : trimmed;
					try
					{
						Disk.DiskLength = int.Parse(len);
					}
					catch (Exception e)
					{
						throw;
					}
					ChangeState(new ReadDiskInformationState());
				}
			}
		}

		#endregion

		#region Nested type: InitialState

		public class InitialState : State
		{
			public override void ReadLine(string line)
			{
				if (line.IndexOf("xmcd", StringComparison.InvariantCultureIgnoreCase) == -1)
					throw new InvalidDataException("Not a valid XMCD file");

				ChangeState(new TrackFrameOffsetState());
			}
		}

		#endregion

		#region Nested type: ReadDiskInformationState

		public class ReadDiskInformationState : State
		{
			public override void ReadLine(string line)
			{
				if (line.StartsWith("#"))
					return;

				var split = line.Split(new[] {"="}, 2, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length != 2)
					return;

				var name = split[0];
				var value = split[1].Trim();
				if (value.Length == 0)
					return;

				switch (name.ToUpperInvariant())
				{
					case "DISCID":
						Disk.DiskIds.AddRange(value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));
						break;
					case "DTITLE":
						var parts = value.Split(new[] {'/'}, 2, StringSplitOptions.RemoveEmptyEntries);
						if(parts.Length == 2)
						{
							Disk.Artist = parts[0].Trim();
							Disk.Title = parts[1].Trim();
						}
						else
						{
							Disk.Title = value;
						}
						break;
					case "DYEAR":
						Disk.Year = int.Parse(value);
						break;
					case "DGENRE":
						Disk.Genre = value;
						break;
					default:
						if (name.StartsWith("TTITLE"))
						{
							Disk.Tracks.Add(value);
						}
						else
						{
							Disk.Attributes[name] = value;
						}
						break;
				}
			}
		}

		#endregion

		#region Nested type: State

		public abstract class State
		{
			public Action<State> ChangeState { get; set; }

			public Disk Disk { get; set; }

			public abstract void ReadLine(string line);
		}

		#endregion

		#region Nested type: TrackFrameOffsetState

		public class TrackFrameOffsetState : State
		{
			private bool readingLengths;

			public override void ReadLine(string line)
			{
				if (readingLengths)
				{
					var trim = line.Substring(1).Trim();
					
					int result;
					if (int.TryParse(trim, out result) == false)
					{
						var diskLengthState = new DiskLengthState();
						ChangeState(diskLengthState);
						diskLengthState.ReadLine(line);
						return;
					}
					Disk.TrackFramesOffsets.Add(result);
					return;
				}

				if (line.IndexOf("Track frame offsets:", StringComparison.InvariantCultureIgnoreCase) == -1)
					return;

				readingLengths = true;
			}
		}

		#endregion
	}
}