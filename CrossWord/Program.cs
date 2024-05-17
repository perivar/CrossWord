using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog;

using CrossWord.Scraper.Extensions;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace CrossWord
{
	static class Program
	{
		public static int Main(string[] args)
		{
			var configuration = new ConfigurationBuilder()
									.SetBasePath(Directory.GetCurrentDirectory())
									.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
									.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
									.AddCommandLine(args)
									.AddEnvironmentVariables()
									.Build();

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(configuration)
				.CreateLogger();

			// output æøå properly to console
			Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;

			Log.Information("Starting Puzzle generator app ver. {0} ", "1.0");

			if (!ParseInput(args, out string inputFile, out string outputFile, out string dictionaryFile, out string? puzzle))
			{
				return 1;
			}

			try
			{
				if (outputFile.Equals("database"))
				{
					const string CONNECTION_STRING_KEY = "DefaultConnection";
					var connectionString = configuration.GetConnectionString(CONNECTION_STRING_KEY);
					if (string.IsNullOrEmpty(connectionString))
					{
						throw new Exception(string.Format($"Cannot use database without a configured connection string! (looking for key: {CONNECTION_STRING_KEY})"));
					}
					else
					{
						var doSQLDebug = configuration.GetBoolValue("DoSQLDebug", false);
						OutputToDatabase(connectionString, doSQLDebug, dictionaryFile);
					}
					return 0;
				}
			}
			catch (Exception e)
			{
				Log.Error(e, $"Cannot load {dictionaryFile} into database.");
				return 2;
			}

			ICrossBoard board;
			try
			{
				if (inputFile.StartsWith("http"))
				{
					board = CrossBoardCreator.CreateFromUrlAsync(inputFile).Result;
				}
				else
				{
					board = CrossBoardCreator.CreateFromFileAsync(inputFile).Result;
				}
			}
			catch (Exception e)
			{
				Log.Error(e, $"Cannot load crossword layout from file {inputFile}.");
				return 3;
			}

			ICrossDictionary dictionary;
			try
			{
				if (dictionaryFile.Equals("database"))
				{
					const string CONNECTION_STRING_KEY = "DefaultConnection";
					var connectionString = configuration.GetConnectionString(CONNECTION_STRING_KEY);
					if (string.IsNullOrEmpty(connectionString))
					{
						throw new Exception(string.Format($"Cannot use database without a configured connection string! (looking for key: {CONNECTION_STRING_KEY})"));
					}
					else
					{
						var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
						dictionary = new DatabaseDictionary(connectionString, board.MaxWordLength, loggerFactory, configuration);
					}
				}
				else
				{
					dictionary = new Dictionary(dictionaryFile, board.MaxWordLength);
				}
			}
			catch (Exception e)
			{
				Log.Error(e, $"Cannot load dictionary from file {dictionaryFile}.");
				return 4;
			}

			if (outputFile.Equals("signalr"))
			{
				// generate and send to signalr hub
				// https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-cancel-a-task-and-its-children
				var tokenSource = new CancellationTokenSource();
				var cancellationToken = tokenSource.Token;

				var task = Task.Run(async () =>
				{
					var signalRHubURL = configuration["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";
					await Generator.GenerateCrosswordsSignalRAsync(board, dictionary, puzzle, signalRHubURL, cancellationToken);

				}, cancellationToken);

				// Request cancellation from the UI thread.
				char ch = Console.ReadKey().KeyChar;
				if (ch == 'c' || ch == 'C')
				{
					tokenSource.Cancel();
					Log.Information("Task cancellation requested from command line.");

					// Optional: Observe the change in the Status property on the task.
					// It is not necessary to wait on tasks that have canceled. However,
					// if you do wait, you must enclose the call in a try-catch block to
					// catch the TaskCanceledExceptions that are thrown. If you do
					// not wait, no exception is thrown if the token that was passed to the
					// Task.Run method is the same token that requested the cancellation.
				}

				try
				{
					// wait until the task is done
					task.Wait();
				}
				catch (Exception e)
				{
					Log.Error(e, $"Failed generating crossword asynchronously");
					return 5;
				}
				finally
				{
					tokenSource.Dispose();
				}
			}
			else
			{
				ICrossBoard? resultBoard;
				try
				{
					resultBoard = puzzle != null
						? Generator.GenerateFirstCrossWord(board, dictionary, puzzle)
						: Generator.GenerateFirstCrossWord(board, dictionary);
				}
				catch (Exception e)
				{
					Log.Error(e, $"Generating crossword has failed.");
					return 6;
				}

				if (resultBoard == null)
				{
					Log.Error("No solution has been found.");
					return 7;
				}

				try
				{
					SaveResultToFile(outputFile, resultBoard, dictionary);
				}
				catch (Exception e)
				{
					Log.Error(e, $"Saving result crossword to file {outputFile} has failed.");
					return 8;
				}
			}
			return 0;
		}

		private static WordHintDbContext GetDatabase(string connectionString, bool doSQLDebug)
		{
			if (doSQLDebug)
			{
				Log.Debug("Getting WordHintDbContext using DesignTimeDbContextFactory (SQL debugging).");
				var dbContextFactory = new DesignTimeDbContextFactory();
				return dbContextFactory.CreateDbContext(connectionString, Log.Logger);
			}
			else
			{
				Log.Debug("Getting WordHintDbContext using DbContextOptionsBuilder.");
				var options = new DbContextOptionsBuilder<WordHintDbContext>();
				options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
				return new WordHintDbContext(options.Options);
			}
		}
		private static void OutputToDatabase(string connectionString, bool doSQLDebug, string dictionaryFile)
		{
			string source = Path.GetFileName(dictionaryFile);

			var textinfo = CultureInfo.CurrentCulture.TextInfo;
			var sourceClean = Regex.Replace(source, @"\W", " ");
			var title = textinfo.ToTitleCase(sourceClean);

			// set admin user
			var adminUser = new User()
			{
				FirstName = "",
				LastName = title,
				UserName = source
			};

			var db = GetDatabase(connectionString, doSQLDebug);

			// setup database
			// You would either call EnsureCreated() or Migrate(). 
			// EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
			// It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
			// db.Database.EnsureDeleted();
			// db.Database.EnsureCreated();

			// Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
			db.Database.Migrate();

			// check if admin user already exists
			var existingUser = db.DictionaryUsers.Where(u => u.FirstName == adminUser.FirstName).FirstOrDefault();
			if (existingUser != null)
			{
				adminUser = existingUser;
			}
			else
			{
				db.DictionaryUsers.Add(adminUser);
				db.SaveChanges();
			}

			// disable tracking to speed things up
			// note that this doesn't load the virtual properties, but loads the object ids after a save
			db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

			// this works when using the same user for all words.
			db.ChangeTracker.AutoDetectChangesEnabled = false;

			bool isDebugging = false;
#if DEBUG
			isDebugging = true;
#endif

			if (Path.GetExtension(dictionaryFile).ToLower().Equals(".json"))
			{
				// read json files
				using StreamReader r = new(dictionaryFile);
				var json = r.ReadToEnd();
				var jobj = JObject.Parse(json);

				var totalCount = jobj.Properties().Count();
				int count = 0;
				foreach (var item in jobj.Properties())
				{
					count++;

					var wordText = item.Name;
					var relatedArray = item.Values().Select(a => a.Value<string>());

					WordDatabaseService.AddToDatabase(db, source, adminUser, wordText, relatedArray);

					if (isDebugging)
					{
						// in debug mode the Console.Write \r isn't shown in the output console
						Console.WriteLine("[{0}] / [{1}]", count, totalCount);
					}
					else
					{
						Console.Write("\r[{0}] / [{1}]", count, totalCount);
					}
				}
				Console.WriteLine("Done!");
			}
			else if (Path.GetExtension(dictionaryFile).ToLower().Equals(".dat"))
			{
				// read first line from the file using default encoding
				Encoding? encoding = null;
				using (StreamReader reader = new(dictionaryFile))
				{
					var encodingString = reader.ReadLine();
					if (!string.IsNullOrEmpty(encodingString))
					{
						encoding = Encoding.GetEncoding(encodingString);
					}
				}

				if (encoding == null)
				{
					throw new Exception("Could not detect file encoding!");
				}
				else
				{
					Console.WriteLine("Detected file encoding: {0}", encoding.HeaderName);
				}

				using (StreamReader reader = new(dictionaryFile, encoding))
				{
					string linePattern = @"\|(\d+)$"; // Regex pattern to detect lines ending with |<number>
					int lineCounter = 0;
					int count = 0;
					int totalCount = 0; // unknown
					while (!reader.EndOfStream)
					{
						string? line = reader.ReadLine();
						lineCounter++;

						// skip first line
						if (lineCounter == 1) continue;

						string wordText = "";
						int numberOfLinesToRead = 0;
						if (!string.IsNullOrEmpty(line))
						{
							Match match = Regex.Match(line, linePattern);
							if (match.Success)
							{
								count++;

								wordText = line.Substring(0, match.Index);
								numberOfLinesToRead = int.Parse(match.Groups[1].Value);

								var relatedArray = new HashSet<string>();
								for (int i = 0; i < numberOfLinesToRead; i++)
								{
									string? l = reader.ReadLine();
									if (!string.IsNullOrEmpty(l))
									{
										var synonyms = l.Split('|').Skip(1); // ignore the first "-" entry
										foreach (string synonym in synonyms)
										{
											relatedArray.Add(synonym);
										}
									}
								}

								// Define a regex pattern to match "(word)"
								string pattern = @"^\([^)]+\)\s*";
								wordText = Regex.Replace(wordText, pattern, ""); // Replace the pattern with an empty string

								// Console.WriteLine("{0} {1}:{2}", lineCounter, word, string.Join(",", synonyms));
								WordDatabaseService.AddToDatabase(db, source, adminUser, wordText, relatedArray);

								if (isDebugging)
								{
									// in debug mode the Console.Write \r isn't shown in the output console
									Console.WriteLine("[{0}] / [{1}]", count, totalCount);
								}
								else
								{
									Console.Write("\r[{0}] / [{1}]", count, totalCount);
								}
							}
						}
					}
				}
				Console.WriteLine("Done!");
			}
		}

		static bool ParseInput(IEnumerable<string> args, out string inputFile, out string outputFile, out string dictionary, out string? puzzle)
		{
			bool help = false;
			string? i = null, o = null, p = null, d = null;
			var optionSet = new NDesk.Options.OptionSet
								{
									{ "i|input=", "(input file)", v => i = v },
									{ "d|dictionary=", "(dictionary)", v => d = v },
									{ "o|output=", "(output file)", v => o = v },
									{ "p|puzzle=", "(puzze)", v => p = v },
									{ "h|?|help", "(help)", v => help = v != null },
								};
			var unparsed = optionSet.Parse(args);
			inputFile = i;
			outputFile = o;
			puzzle = p;
			dictionary = d;
			// support not having mandatory input file if we write to a database, then only directory file is required
			if ("database".Equals(o) && !string.IsNullOrEmpty(d))
			{
				return true;
			}
			else if (help || unparsed.Count > 1 || string.IsNullOrEmpty(i) ||
				string.IsNullOrEmpty(o) || string.IsNullOrEmpty(d))
			{
				optionSet.WriteOptionDescriptions(Console.Out);
				return false;
			}
			return true;
		}

		static void SaveResultToFile(string outputFile, ICrossBoard resultBoard, ICrossDictionary dictionary)
		{
			Console.WriteLine($"Solution has been written to file {outputFile}.");
			using var writer = new StreamWriter(new FileStream(outputFile, FileMode.Create));
			resultBoard.WriteTo(writer);
			resultBoard.WritePatternsTo(writer, dictionary);
		}

		static IEnumerable<string> ReadLines(string filePath, Encoding encoding)
		{
			using (StreamReader reader = new(filePath, encoding))
			{
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					yield return line;
				}
			}
		}
	}
}