using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace CrossWord.Scraper
{
    public class NorwegianSynonymsScraper
    {
        private readonly TextWriter writer = null;
        private readonly string connectionString = null;
        private readonly string signalRHubURL = null;
        private readonly string source = null;

        const string JSON_URL = "https://raw.githubusercontent.com/ltgoslo/norwegian-synonyms/master/norwegian-synonyms.json";

        public NorwegianSynonymsScraper(string connectionString, string signalRHubURL, int startLetterCount, int endLetterCount, bool doContinueWithLastWord)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "norwegian-synonyms.json";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(this.signalRHubURL, startLetterCount.ToString());
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            if (startLetterCount != endLetterCount)
            {
                Log.Warning("This only support one thread and cannot have several at the same time! Ensure the start and ent letter count is the same. Quiting!");
                writer.WriteLine("This only support one thread and cannot have several at the same time! Ensure the start and ent letter count is the same. Quiting!");
                return;
            }

            DoScrape(source, doContinueWithLastWord);
        }

        private void DoScrape(string source, bool doContinueWithLastWord)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
                string lastWordString = null;
                if (doContinueWithLastWord)
                {
                    lastWordString = WordDatabaseService.GetLastWordFromSource(db, source);
                }

                // Note! 
                // the user needs to be added before we disable tracking and disable AutoDetectChanges
                // otherwise this will crash

                // set admin user
                var adminUser = new User()
                {
                    FirstName = "",
                    LastName = "Admin",
                    UserName = "admin"
                };

                // check if user already exists
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

                // this doesn't seem to work when adding new users all the time
                db.ChangeTracker.AutoDetectChangesEnabled = false;

                ReadWordsFromUrl(db, adminUser, lastWordString);
            }
        }

        private void ReadWordsFromUrl(WordHintDbContext db, User adminUser, string lastWord)
        {
            using (WebClient client = new WebClient())
            using (Stream stream = client.OpenRead(JSON_URL))
            using (StreamReader streamReader = new StreamReader(stream))

            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                reader.SupportMultipleContent = true;

                string currentValue = null;
                List<string> currentList = null;
                int totalCount = 25000;
                int count = 0;

                bool hasFound = false;

                var serializer = new JsonSerializer();
                while (reader.Read())
                {
                    // output the stream one chunk at a time
                    // Log.Information(string.Format("{0,-12}  {1}",
                    //         reader.TokenType.ToString(),
                    //         reader.Value != null ? reader.Value.ToString() : "(null)"));

                    switch (reader.TokenType)
                    {
                        // JsonToken.StartObject = deserialize only when there's "{" character in the stream
                        case JsonToken.StartObject:
                            break;

                        // JsonToken.PropertyName = deserialize only when there's a "text": in the stream
                        case JsonToken.PropertyName:
                            currentValue = reader.Value.ToString();
                            break;

                        // JsonToken.String = deserialize only when there's a "text" in the stream
                        case JsonToken.String:
                            currentList.Add(reader.Value.ToString());
                            break;

                        // JsonToken.StartArray = deserialize only when there's "[" character in the stream
                        case JsonToken.StartArray:
                            currentList = new List<string>();
                            break;

                        // JsonToken.EndArray = deserialize only when there's "]" character in the stream
                        case JsonToken.EndArray:
                            count++;

                            // skip until we reach last word beginning
                            if (lastWord != null)
                            {
                                if (currentValue.ToUpperInvariant().Equals(lastWord))
                                {
                                    hasFound = true;
                                }
                            }
                            else
                            {
                                hasFound = true;
                            }

                            // store to database
                            if (hasFound)
                            {
                                // update that we are processing this word, ignore length and comment
                                WordDatabaseService.UpdateState(db, source, new Word() { Value = currentValue.ToUpper(), Source = source, CreatedDate = DateTime.Now }, writer, true);

                                // disable storing state since we are doing it manually above
                                WordDatabaseService.AddToDatabase(db, source, adminUser, currentValue, currentList, writer, false);

                                // writer?.WriteLine("Added '{0} => {1}'", currentValue, string.Join(",", currentList));
                                if ((count % 10) == 0) writer?.WriteLine("[{0}] / [{1}]", count, totalCount);
                            }

                            //  and reset
                            currentList = null;
                            currentValue = null;
                            break;

                        // JsonToken.EndObject = deserialize only when there's "}" character in the stream
                        case JsonToken.EndObject:
                            currentList = null;
                            currentValue = null;
                            break;
                    }
                }
            }

            /* 
            // reading the whole thing took approx the same time as the streaming version
            {
                var json = streamReader.ReadToEnd();
                var jobj = JObject.Parse(json);

                var totalCount = jobj.Properties().Count();
                int count = 0;
                foreach (var item in jobj.Properties())
                {
                    count++;

                    var currentValue = item.Name;
                    var currentList = item.Values().Select(a => a.Value<string>());

                    WordDatabaseService.AddToDatabase(db, source, adminUser, currentValue, currentList);

                    // if (writer != null) writer.WriteLine("Added '{0} => {1}'", currentValue, string.Join(",", currentList));
                    if (writer != null) writer.WriteLine("[{0}] / [{1}]", count, totalCount);
                }
            }
            */
        }
    }
}