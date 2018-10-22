using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DupeDetecter2000
{
	struct CheckedFile
	{
		public string path, md5;
		public long size;
		public DateTime creation;

		public CheckedFile(string path, string md5, long size, DateTime c)
		{
			this.path = path;
			this.md5 = md5;
			this.size = size;
			creation = c;
		}
	}

	class FileComparerator : IEqualityComparer<CheckedFile>
	{
		public bool Equals(CheckedFile x, CheckedFile y)
		{
			// Two items are equal if their keys are equal.
			return x.md5 == y.md5;
		}

		public int GetHashCode(CheckedFile obj)
		{
			return obj.md5.GetHashCode();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.Write("Please input your Path: ");
			DirectoryInfo path = new DirectoryInfo(Console.ReadLine());
			Console.WriteLine("Scanning directory and sub dirs");
			Console.WriteLine("Updates per 100 file(s)");
			List<CheckedFile> files = ScanDeep(path);
			Console.WriteLine("Detecting duplicates by hash");
			List<CheckedFile> dupelist = DupeChecker(files);
			Console.WriteLine("Duplicates found: " + dupelist.Count);
			Console.WriteLine("You'll now be prompted to choose the action to execute");
			Console.WriteLine("Simply press the indicated Letter");
			Console.WriteLine("Write dupelist to file [S] ");
			Console.WriteLine("Delete oldest dupe [D]");
			Console.WriteLine("If you just want to quit, press enter or any non indicated button");

			switch (Console.ReadKey().Key)
			{
				case ConsoleKey.S:
					Console.WriteLine("Saving dupelist");
					using (StreamWriter sw = File.CreateText("./Dupelist.json"))
					{
						JsonSerializer serializer = new JsonSerializer
						{
							Formatting = Formatting.Indented
						};
						serializer.Serialize(sw, dupelist);
					}
					break;
				case ConsoleKey.D:
					Console.WriteLine("Deleting oldest known dupes");
					dupelist.Sort((x, y) => x.creation.CompareTo(y.creation));
					List<CheckedFile> list = dupelist.Distinct(new FileComparerator()).ToList();

					foreach (var keep in list)
					{
						foreach (var delete in dupelist)
						{
							if (keep.creation != delete.creation && keep.md5 == delete.md5 && keep.path != delete.path)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("DELETING " + delete.creation + " --- " + delete.path);
								if (File.Exists(delete.path))
									File.Delete(delete.path);
								Console.ForegroundColor = ConsoleColor.Green;
								Console.WriteLine(" KEEPING " + keep.creation + " --- " + keep.path);
							}
						}
					}
					break;
				default:
					break;
			}
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Done, press anykey to close");
			Console.ReadKey();
		}
		static List<CheckedFile> DupeChecker(List<CheckedFile> list)
		{
			List<CheckedFile> dupelist = new List<CheckedFile>();
			var dupemd5 = from p in list group p by p.md5 into g where g.Count() > 1 select g.Key;

			foreach (var dupe in dupemd5)
				foreach (var file in list.FindAll(p => p.md5 == dupe))
					dupelist.Add(file);
			return dupelist;
		}

		static List<CheckedFile> ScanDeep(DirectoryInfo path)
		{
			List<CheckedFile> files = new List<CheckedFile>();
			FileInfo[] tempfiles = null;
			try
			{
				tempfiles = path.GetFiles("*.*", SearchOption.AllDirectories);
				Console.WriteLine("Files found: " + tempfiles.Count());
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			if (tempfiles != null)
			{
				Console.WriteLine("Now Building hash database");
				int i = 0;

				foreach (var fi in tempfiles)
				{
					if (i % 100 == 1)
						Console.WriteLine($" {i}/{tempfiles.Count()} - {fi.Name}");
					files.Add(new CheckedFile(fi.FullName, CalculateMD5(fi.FullName), fi.Length, fi.CreationTime));
					i++;
				};
				Console.WriteLine("Done Hashing");
			}
			return files;
		}
		static string CalculateMD5(string filename)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(filename))
				{
					var hash = md5.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToLower();
				}
			}
		}
	}
}
