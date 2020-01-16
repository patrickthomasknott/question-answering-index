using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search.Highlight;
using Lucene.Net;
using Lucene.Net.Analysis;
using Syn.WordNet;
using System.Drawing;


namespace TheApplicationGUI
{
    class NewSystem
    {
        Lucene.Net.Store.Directory luceneIndexDirectory;
        Lucene.Net.Analysis.Analyzer analyzer;
        Lucene.Net.Index.IndexWriter writer;
        Lucene.Net.Search.IndexSearcher searcher;
        Lucene.Net.QueryParsers.QueryParser parser;

        Similarity newSimilarity;

        public static Lucene.Net.Util.Version VERSION = Lucene.Net.Util.Version.LUCENE_30;

        DateTime end;
        DateTime start;

        string passageFieldName;
        string urlFieldName;
        string titleFieldName;
        string passageIdFieldName;


        public string[] urlStopWords = { "www", "org", "wiki", "http", "https", "en", "com", "eu", "htm", "html", "aspx", "yahoo", "gov", "uk", "edu", "misc", "%20", "asp", "co", "article", "biz" };
        char[] splitters = { ' ', '\t', '\'', '"', '-', '(', ')', ',', '’', ' ', ':', ';', '?', '.', '!', '@', '/', '_', '[', ']' };


        public class Passage
        {
            public int is_selected { get; set; }
            public string url { get; set; }
            public string passage_text { get; set; }
            public int passage_ID { get; set; }
        }



        public class RootObject
        {
            public List<Passage> passages { get; set; }
            public int query_id { get; set; }
            public List<string> answers { get; set; }
            public string query_type { get; set; }
            public string query { get; set; }
        }


        /// <summary>
        /// Changes to th scoring function
        /// </summary>
        /// <remarks>
        /// Change term frequency exponent from 0.5 to 2 because the passages are so short.
        /// Increase doc-query coordinating factor again because the passages are so short.
        /// </remarks>
        public class NewSimilarity : DefaultSimilarity
        {
            public override float Tf(float freq)
            {
                return (float)Math.Pow(freq, 2);
            }
            public override float Coord(int overlap, int maxOverlap)
            {
                return 2 * overlap / (float)maxOverlap;
            }

        }


        /// <summary>
        /// Constructor
        /// </summary>
        public NewSystem()
        {
            luceneIndexDirectory = null;
            writer = null;
            analyzer = null;
            newSimilarity = new NewSimilarity();
        }
        public void CreateAnalyser()
        {
            analyzer = new StandardAnalyzer(VERSION);
        }
        public void CleanUpWriter()
        {
            writer.Optimize();
            writer.Flush(true, true, true);
            writer.Dispose();
        }
        /// <summary>
        /// Searches with new similarity function
        /// </summary>
        public void CreateSearcher()
        {
            searcher = new IndexSearcher(luceneIndexDirectory);
            searcher.Similarity = newSimilarity;
        }
        /// <summary>
        /// Changed to search the passage field and the title
        /// </summary>
        public void CreateParser()
        {

            parser = new MultiFieldQueryParser(VERSION, new[] { passageFieldName, titleFieldName }, analyzer);
        }
        public void CleanUpSearch()
        {
            searcher.Dispose();
        }
        /// <summary>
        /// Writes with new similarity function
        /// </summary>
        public void CreateWriter()
        {
            IndexWriter.MaxFieldLength mfl = new IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH);
            writer = new Lucene.Net.Index.IndexWriter(luceneIndexDirectory, analyzer, true, mfl);
            writer.SetSimilarity(newSimilarity);
        }
        public void OpenIndex(string indexLoc)
        {
            indexLoc += "/NewSystemIndexFiles";
            luceneIndexDirectory = Lucene.Net.Store.FSDirectory.Open(indexLoc);
        }

        /// <summary>
        /// For user to input boost values
        /// </summary>
        /// /// <returns>
        /// A float to use as a boost factor, returns 1 if user doesn't want to boost.
        /// </returns>
        public float AskForBoost(string boostTarget)
        {
            float boostFactor = 1;
            while (true)
            {
                string Instruction = string.Format("Enter a number as a boost multiplier for {0}, or leave it as one:", boostTarget);
                string inValue = "";
                string value = "1";
                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        inValue = value;
                    }
                if (inValue == "1")
                {
                    MessageBox.Show(string.Format("You have selected no boost for {0}", boostTarget));
                    break;
                }

                if (float.TryParse(inValue, out boostFactor))
                {
                    MessageBox.Show(string.Format("You have selected a boost of {0} for the {1}", boostFactor, boostTarget));
                    break;
                }
                if (!float.TryParse(inValue, out boostFactor))
                {
                    MessageBox.Show("You need to enter a number for a multiplier or press enter for no boost!!");
                }
            } // end while loop
            return boostFactor;
        } // end AskForBoost


        /// <summary>
        /// To index one dictionary.
        /// </summary>
        public void IndexText(string text, string url, string title, string passageId, float passagesFieldBoost, float titleFieldBoost)
        {

            Lucene.Net.Documents.Field passagesField = new Field(passageFieldName, text, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);

            Lucene.Net.Documents.Field urlField = new Field(urlFieldName, url, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            Lucene.Net.Documents.Field titleField = new Field(titleFieldName, title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);


            Lucene.Net.Documents.Field passageIdField = new Field(passageIdFieldName, passageId, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            Lucene.Net.Documents.Document doc = new Document();
            passagesField.Boost = passagesFieldBoost;
            doc.Add(passagesField);
            doc.Add(urlField);
            titleField.Boost = titleFieldBoost;
            doc.Add(titleField);
            doc.Add(passageIdField);
            writer.AddDocument(doc);
        }


        /// <summary>
        /// Calls the IndexText function for each dictionary 
        /// </summary>
        /// <remarks>
        /// Calls AskBoost for user input boosts to fields Title and Passage text.
        /// Calls function to generate a pseudo-title from the URL
        /// </remarks>
        public void IndexCycle(List<RootObject> listRO, NewSystem myLuceneApp)
        {
            float passagesFieldBoost = AskForBoost("passages field");
            float titleFieldBoost = AskForBoost("title field");
            Cursor.Current = Cursors.WaitCursor;
            Console.WriteLine("The index is being created...");
            start = System.DateTime.Now;
            for (int i = 0; i < listRO.Count; i++) 
            {
                for (int j = 0; j < listRO[i].passages.Count; j++)
                {
                    string text = listRO[i].passages[j].passage_text; 
                    string url = listRO[i].passages[j].url;
                    string title = GenerateTitle(url, myLuceneApp);
                    string passageId = listRO[i].passages[j].passage_ID.ToString();
                    myLuceneApp.IndexText(text, url, title, passageId, passagesFieldBoost, titleFieldBoost);
                }
            }

            end = System.DateTime.Now;
            MessageBox.Show(string.Format("The index creation took {0}", end - start));
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Searches the index for one query
        /// </summary>
        /// <remarks>
        /// Calls query expansion function
        /// </remarks>
        /// /// <returns>
        /// A 2-tuple containing the TopDocs search result object, as well as the Query object for use in highlighting the matching text.
        /// </returns>
        public Tuple<TopDocs, Query> SearchIndex(string querytext, string parserVersion, NewSystem myLuceneApp, WordNetEngine wordNet)
        {

            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            System.Console.WriteLine("Original query: " + querytext);
            querytext = querytext.ToLower();

            if (parserVersion == "Complex")
            {
                querytext = ExpandQuery(myLuceneApp, wordNet, querytext);
            }

            Cursor.Current = Cursors.WaitCursor;
            start = System.DateTime.Now;
            Query query = parser.Parse(querytext);
            query = parser.Parse(querytext);
            System.Console.WriteLine("Parsed query: " + query);
            TopDocs results = searcher.Search(query, 100000);
            System.Console.WriteLine("Number of results is " + results.TotalHits);
            end = System.DateTime.Now;
            MessageBox.Show(string.Format("Time to search: {0}", end - start));
            Cursor.Current = Cursors.Default;
            return Tuple.Create(results, query);
        }

        /// <summary>
        /// Displays the results from one search
        /// </summary>
        /// <remarks>
        /// Output summary details about each result
        /// Outputs matching words with some of the surrounding text (maximum of 20 fragments)
        /// </remarks>
        public void DisplayResults(TopDocs results, int numDocsToDisplay, Query query)
        {
            SimpleHTMLFormatter htmlFormatter = new SimpleHTMLFormatter();
            Highlighter highlighter = new Highlighter(htmlFormatter, new QueryScorer(query));
            int rank = 0;
            foreach (ScoreDoc scoreDoc in results.ScoreDocs)
            {
                rank++;
                if (rank == numDocsToDisplay + 1) break;
                Lucene.Net.Documents.Document doc = searcher.Doc(scoreDoc.Doc);

                string passageContent = doc.Get(passageFieldName);
                TokenStream tokenStream = TokenSources.GetAnyTokenStream(searcher.IndexReader, scoreDoc.Doc, passageFieldName, analyzer);
                TextFragment[] frag = highlighter.GetBestTextFragments(tokenStream, passageContent, false, 10);


                string title = doc.Get(titleFieldName);
                string url = doc.Get(urlFieldName).ToString();
                string passageId = doc.Get(passageIdFieldName).ToString();
                Console.WriteLine("\t Result Ranking: " + rank);
                Console.WriteLine("Title: " + title.ToString());
                Console.WriteLine("Document ID: " + passageId);
                Console.WriteLine("Score: " + scoreDoc.Score);
                double score = scoreDoc.Score;
                Explanation e = searcher.Explain(query, scoreDoc.Doc);
                System.Console.WriteLine(e.ToString());

                Console.WriteLine("URL: " + url + "");

                int fragCount = 0;
                for (int j = 0; j < frag.Length; j++)
                {
                    if (frag[j].Score > 0) fragCount++;
                }
                if (fragCount > 20) fragCount = 20;

                Console.WriteLine("There are {0} fragments of text which match the query:", fragCount);
                for (int j = 0; j < fragCount; j++)
                {
                    if ((frag[j] != null) && (frag[j].Score > 0))
                    {
                        Console.WriteLine("Matching fragment {0}", j + 1);
                        Console.WriteLine((frag[j].ToString()));
                    }
                }
            }
        }

        /// <summary>
        /// Loads a json file into a list of user-defined class, RootObject
        /// </summary>
        public List<RootObject> LoadJson(string dataLoc, string fileName)
        {
            Cursor.Current = Cursors.WaitCursor;
            start = System.DateTime.Now;
            string file = dataLoc + fileName;

            string rawData = File.ReadAllText(file);
            List<RootObject> oldListRO = JsonConvert.DeserializeObject<List<RootObject>>(rawData);
            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to deserialize {0}: {1}", fileName, end - start);
            Cursor.Current = Cursors.Default;
            return oldListRO;
        }


        /// <summary>
        /// Removes non-answer passages
        /// </summary>
        /// <remarks>
        /// Only retains the passages from the source data marked as correct.
        /// NB some queries have multiple correct passages
        /// </remarks>
        /// /// <returns>
        /// The reduced list of RootObjects
        /// </returns>
        public List<RootObject> RemoveBadPassages(List<RootObject> oldListRO)
        {
            Cursor.Current = Cursors.WaitCursor;
            start = System.DateTime.Now;
            List<RootObject> listRO = new List<RootObject>();
            for (var i = 0; i < oldListRO.Count; i++)
            {
                List<Passage> tempListPassages = new List<Passage>();

                for (var j = 0; j < oldListRO[i].passages.Count; j++)
                {
                    if (oldListRO[i].passages[j].is_selected == 1)
                        tempListPassages.Add(oldListRO[i].passages[j]);
                }

                RootObject tempRO = new RootObject() { };
                if (tempListPassages.Count > 0)
                {
                    tempRO.passages = tempListPassages;
                    tempRO.query_id = oldListRO[i].query_id;
                    tempRO.answers = oldListRO[i].answers;
                    tempRO.query_type = oldListRO[i].query_type;
                    tempRO.query = oldListRO[i].query;
                    listRO.Add(tempRO);
                }
            }
            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to remove unnecessary passages: {0}", end - start);
            Cursor.Current = Cursors.Default;
            return listRO;
        }




        /// <summary>
        /// Saves the json file without false answers
        /// </summary>
        /// <remarks>
        /// For saving time during software development
        /// </remarks>
        public void SaveReducedJson(List<RootObject> listRO, string dataLoc)
        {
            Cursor.Current = Cursors.WaitCursor;
            start = System.DateTime.Now;
            string fileName = "reducedCollection.json";
            string file = dataLoc + fileName;
            File.WriteAllText(file, JsonConvert.SerializeObject(listRO));
            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to save reduced JSON: {0}", end - start);
            Cursor.Current = Cursors.Default;
        }



        /// <summary>
        /// Generates a passage pseudo-title from URL
        /// </summary>
        /// <remarks>
        /// Splits URL by a user-designated list of characters
        /// Removes a user-designated stopword list of URL attributes
        /// Replaces youtube URL's titles as Youtube Video
        /// </remarks>
        /// /// <returns>
        /// The pseudo-title
        /// </returns>
        public string GenerateTitle(string url, NewSystem myLuceneApp)
        {
            string[] titleFull = url.ToLower().Split(myLuceneApp.splitters, StringSplitOptions.RemoveEmptyEntries);
            string title = "";
            for (int i = 0; i < titleFull.Length; i++)
            {
                if (!myLuceneApp.urlStopWords.Contains(titleFull[i]))
                    title = title + " " + titleFull[i];
            }
            title = title.Trim();
            if (title.Contains("youtube")) title = "Youtube Video";
            title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);
            return title;
        }



        /// <summary>
        /// Saves trec results
        /// </summary>
        /// <remarks>
        /// Doesn't generate the results
        /// Supplies a default filename
        /// Either creates a new file or appends to an existing file
        /// </remarks>
        public void SaveTrecResults(List<string> trecResultsArray, string dataLoc, string defaultTrecResultsFileName)
        {
            string Instruction = "Enter a file name for the search results file";
            string inValue = "";
            string value = "defaultTrecResultsFileName";
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
            {
                inValue = value;
            }

            string fileTWName = dataLoc + inValue;

            if (File.Exists(fileTWName))
            {
                MessageBox.Show("The results file already exists, so new results will be appended to it");
                using (StreamWriter appendFileTW = new FileInfo(fileTWName).AppendText())
                {
                    appendFileTW.NewLine = "";
                    foreach (string line in trecResultsArray)
                    {
                        appendFileTW.WriteLine(line);
                    }
                }
            }

            if (!File.Exists(fileTWName))
            {
                MessageBox.Show("The results file does not exist, so it will be created.");
                using (StreamWriter newFileTW = new StreamWriter(fileTWName))
                {
                    newFileTW.NewLine = "";
                    foreach (string line in trecResultsArray)
                    {
                        newFileTW.WriteLine(line);
                    }
                }
            }
        }


        /// <summary>
        /// Formats trec results for one query
        /// </summary>
        /// <remarks>
        /// Searches index with one pre-defined question
        /// Formats the results as needed
        /// </remarks>
        /// <returns>
        /// List of strings in required format.
        /// </returns>
        public List<string> GenerateTrecResult(List<RootObject> listRO, NewSystem myLuceneApp, List<string> trecResultsArray, int queryNumber, string parserVersion, WordNetEngine wordNet)
        {

            string question = listRO[queryNumber].query;

            Console.WriteLine("You have chosen the query:\n{0}", question);

            Tuple<TopDocs, Query> searchAnswers = myLuceneApp.SearchIndex(question, parserVersion, myLuceneApp, wordNet);
            TopDocs results = searchAnswers.Item1;
            Query query = searchAnswers.Item2;
            string resultsLine = "";
            int rank = 0;
            foreach (ScoreDoc scoreDoc in results.ScoreDocs)
            {
                rank++;
                Lucene.Net.Documents.Document doc = searcher.Doc(scoreDoc.Doc);
                resultsLine = listRO[queryNumber].query_id.ToString();
                resultsLine += " Q0";
                resultsLine += " " + doc.Get(passageIdFieldName).ToString(); ;
                resultsLine += " " + rank + " " + scoreDoc.Score;
                resultsLine += " n4373545_TeamPat";
                trecResultsArray.Add(resultsLine);
            }
            return trecResultsArray;
        }

        /// <summary>
        /// Creates trec results for user-selected pre-defined queries
        /// </summary>
        /// <remarks>
        /// Searches index with one pre-defined question, input by user
        /// Saves the results after
        /// </remarks>
        public void SelectIndividualTrecResults(NewSystem myLuceneApp, List<RootObject> listRO, List<string> trecResultsArray, string dataLoc, string defaultTrecResultsFileName, int numDocsToDisplay, WordNetEngine wordNet)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
                "You have chosen to manually select supplied queries to build the trec_eval results document~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            int queryNumber;
            int indexUpperLimit = listRO.Count - 1;
            trecResultsArray.Clear();
            string parserVersion = AskIfQueryExpanding();
            while (true)
            {
                string Instruction = string.Format("Enter <exit> to finish or enter an integer between 0 and {0} inclusive to submit a supplied query from the database:", indexUpperLimit);
                string line = "";
                string value = "";
                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    line = value;
                }
                if (line == "exit") break;

                if (int.TryParse(line, out queryNumber))
                {
                    if (queryNumber >= 0 && queryNumber <= indexUpperLimit)
                    {
                        start = System.DateTime.Now;
                        trecResultsArray = myLuceneApp.GenerateTrecResult(listRO, myLuceneApp, trecResultsArray, queryNumber, parserVersion, wordNet);
                        end = System.DateTime.Now;
                        Console.WriteLine("Time to generate trec_eval result for one query: {0}", end - start);
                    }
                    else
                    { Console.WriteLine("The number must be between zero and {0} inclusive!!", indexUpperLimit); }
                }
                else
                {
                    MessageBox.Show("Not an integer!");
                }
            } // end while
            Cursor.Current = Cursors.WaitCursor;
            start = System.DateTime.Now;
            myLuceneApp.SaveTrecResults(trecResultsArray, dataLoc, defaultTrecResultsFileName);
            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Cursor.Current = Cursors.Default;
            MessageBox.Show(string.Format("Time to save trec_eval results file: {0}", end - start));

        }



        /// <summary>
        /// Generates trec results from continuous range of pre-defined queries
        /// </summary>
        /// <remarks>
        /// User selects start and finish query indices
        /// Saves results afterwards
        /// </remarks>
        public void SelectRangeTrecResults(NewSystem myLuceneApp, List<RootObject> listRO, List<string> trecResultsArray, string dataLoc, string defaultTrecResultsFileName, WordNetEngine wordNet)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
    "You have chosen to select a range of supplied queries to build the trec_eval results document~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            trecResultsArray.Clear();
            int startQueryNumber;
            while (true)
                {
                    string Instruction = string.Format("Enter an integer from 0 to {0} inclusive for the starting document in the range of supplied queries to use:", listRO.Count - 2);

                    string inValue = "";
                    string value = "";

                    if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        inValue = value;
                    }

                    if (!int.TryParse(inValue, out startQueryNumber))
                    {
                        MessageBox.Show("Not an integer!");

                    }
                    if (int.TryParse(inValue, out startQueryNumber))
                    {
                        if (startQueryNumber > listRO.Count - 1)
                        {  MessageBox.Show(string.Format("The number must be less than {0}", listRO.Count)); }
                        if (startQueryNumber < 0)
                        {  MessageBox.Show(string.Format("The number must be non-negative")); }


                        if ((startQueryNumber >= 0) && (startQueryNumber < listRO.Count - 1))
                        { break; }
                    }
                } // end while

            while (true)
            {
                string Instruction = string.Format("Enter an integer from {0} to {1} inclusive for the final document in the range of supplied queries to use:>>>>>  ", startQueryNumber + 1, listRO.Count - 1);

                string inValue = "";
                string value = "";
                int endQueryNumber;

                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    inValue = value;
                }

                if (!int.TryParse(inValue, out endQueryNumber))
                {
                    MessageBox.Show("Not an integer!");

                }
                if (int.TryParse(inValue, out endQueryNumber))
                {
                    if (endQueryNumber > listRO.Count - 1)
                    {  MessageBox.Show(string.Format("The number must be less than {0}", listRO.Count)); }
                    if (endQueryNumber < 0)
                    {  MessageBox.Show(string.Format("The number must be larger than {0}", startQueryNumber)); }


                    if ((endQueryNumber > startQueryNumber) && (endQueryNumber < listRO.Count - 1))
                    { break; }
                }
            }

            Cursor.Current = Cursors.WaitCursor;
            DateTime startRangeSearches = System.DateTime.Now;
            for (int i = startQueryNumber; i < endQueryNumber + 1; i++)
            {
                trecResultsArray = myLuceneApp.GenerateTrecResult(listRO, myLuceneApp, trecResultsArray, i, "Simple", wordNet);
            }
            DateTime endRangeSearches = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to perform {0} searches: {1}", endQueryNumber - startQueryNumber + 1, endRangeSearches - startRangeSearches);

            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time for automatic trec_eval results file generation: {0}", end - start);
            Cursor.Current = Cursors.Default;
            myLuceneApp.SaveTrecResults(trecResultsArray, dataLoc, defaultTrecResultsFileName);
        }



        /// <summary>
        /// Generates Qrel file
        /// </summary>
        /// <remarks>
        /// The correct answers for the trec evaluation
        /// </remarks>
        public void GenerateQrel(List<RootObject> oldListRO, string dataLoc)
        {
            start = System.DateTime.Now;
            List<string> qrel = new List<string>();
            for (int i = 0; i < oldListRO.Count; i++)
            {
                string line = "";
                for (int j = 0; j < oldListRO[i].passages.Count; j++)
                {
                    if (oldListRO[i].passages[j].is_selected.ToString() == "1")
                    {
                        line = oldListRO[i].query_id.ToString();
                        line += " 0 " + oldListRO[i].passages[j].passage_ID.ToString();
                        line += " " + oldListRO[i].passages[j].is_selected.ToString();
                        qrel.Add(line);
                    }
                }
            }
            string fileTWName = dataLoc + "qrel.txt";
            using (TextWriter fileTW = new StreamWriter(fileTWName))
            {
                fileTW.NewLine = "\n";
                foreach (string line in qrel)
                {
                    fileTW.WriteLine(line);
                }
            }
            end = System.DateTime.Now;
            Console.WriteLine("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to generate qrel file: {0}", end - start);
        }




        /// <summary>
        /// User input, natural language queries
        /// </summary>
        /// <remarks>
        /// Asks for original questions
        /// Asks if wanting query expansion
        /// Offers user to look at full documents from list
        /// </remarks>
        public void ManualNaturalLanguageQuery(NewSystem myLuceneApp, int numDocsToDisplay, WordNetEngine wordNet)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
    "You have chosen to enter original, natural language queries to the index~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            string parserVersion = AskIfQueryExpanding();
            while (true)
            {
                string Instruction = string.Format("Enter a query or enter <exit> to finish:");

                string question = "";
                string value = "";

                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    question = value;
                }

                if (question == "exit") break;
                

                Tuple<TopDocs, Query> searchAnswers = myLuceneApp.SearchIndex(question, parserVersion, myLuceneApp, wordNet);
                TopDocs nlpQueryResult = searchAnswers.Item1;
                Query query = searchAnswers.Item2;

                DisplayResults(nlpQueryResult, numDocsToDisplay, query);

                while (true)
                {
                    Instruction = string.Format("Which document do you want to see displayed in full?Enter the rank number, or enter <exit> to search for a new query:(the rank must be between 1 and {0} inclusive)", nlpQueryResult.TotalHits);

                    question = "";
                    int docToViewRank = "";
                    string inValue = "";

                    if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            docToViewRank = value;
                        }

                    if (!int.TryParse(inValue, out docToViewRank))
                        {
                            MessageBox.Show("Not an integer!");
                        }
                    if ((docToViewRank > nlpQueryResult.TotalHits) || (docToViewRank < 1))
                        { 
                            MessageBox.Show(string.Format("The number must be between 1 and {0} inclusive!!", nlpQueryResult.TotalHits)); 
                        }
                    if ((docToViewRank <= nlpQueryResult.TotalHits) && (docToViewRank > 0))
                        {
                            ScoreDoc scoreDoc = nlpQueryResult.ScoreDocs[docToViewRank - 1];
                            Lucene.Net.Documents.Document doc = searcher.Doc(scoreDoc.Doc);
                            string textAndUrlValue = doc.Get(passageFieldName).ToString();
                            string title = doc.Get(titleFieldName).ToString();
                            Console.WriteLine("Title of result at rank {0}: {1}Passage:{2}", docToViewRank, title, textAndUrlValue);
                        }
            } // end outer while loop
        } // end ManualNaturalLanguageQuery


        /// <summary>
        /// Asks user if this query is to be expanded
        /// </summary>
        /// <returns>
        /// "Simple" if no expansion, otherwise "Complex"
        /// </returns>
        public string AskIfQueryExpanding()
        {
            while (true)
            {
                string Instruction = string.Format("Do you want to use query expansion for your search? Enter yes or no:");

                string parserVersion = "";
                string value = "";

                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        parserVersion = value;
                    }
                if (parserVersion == "yes")
                    {
                        parserVersion = "Complex";
                        break;
                    }
                if (parserVersion == "no")
                    {
                        parserVersion = "Simple";
                        break;
                    }
                MessageBox.Show("You must enter yes or no!");   
            } // end while
            return parserVersion;   
        }

        /// <summary>
        /// Semi-automated demonstration of search function
        /// </summary>
        /// <remarks>
        /// Offers both a non expanded and expanded result
        /// Expanded result does require user input to select POS tags to use
        /// </remarks>
        public void Demonstration(NewSystem myLuceneApp, int numDocsToDisplay, WordNetEngine wordNet)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
    "You have chosen a demonstration of the search capacity with one of the supplied queries, but with both an 'as-is' search and a search with query expansion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            Console.WriteLine("The first query will use a simple parser");
            Tuple<TopDocs, Query> searchAnswers = myLuceneApp.SearchIndex("are polar bears black", "Simple", myLuceneApp, wordNet);
            TopDocs results = searchAnswers.Item1;
            Query query = searchAnswers.Item2;
            myLuceneApp.DisplayResults(results, numDocsToDisplay, query);


            Console.WriteLine("The next query will use a query expansion parser");
            searchAnswers = myLuceneApp.SearchIndex("are polar bears black", "Complex", myLuceneApp, wordNet);
            results = searchAnswers.Item1;
            query = searchAnswers.Item2;
            myLuceneApp.DisplayResults(results, numDocsToDisplay, query);

        }



        /// <summary>
        /// Asks for hyper-parameters 
        /// </summary>
        /// <remarks>
        /// Before data loading and index generation
        /// </remarks>
        public Tuple<string, string, string, string, int> SetupQuestions(string dataLoc, string indexLoc, string defaultManualTrecResultsFileName, string defaultAutoTrecResultsFileName)
        {
            int numDocsToDisplay;
            Console.WriteLine("Welcome to the upgraded KUT EduQuiz Question Answering System");
            Console.WriteLine("First, some questions to set things up:");
            while (true)
            {
                string Instruction = "How many results do you want to see displayed on the screen?";

                string inValue = "";
                string value = "";
                if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    inValue = value;
                }

                if (!int.TryParse(inValue, out numDocsToDisplay))
                {
                    MessageBox.Show("Not an integer!");

                }
                if (int.TryParse(inValue, out numDocsToDisplay))
                {
                    break;
                }
            } // end while

            string Instruction = "Enter a directory path containing the collection.json file:";
            string value = dataLoc;
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    dataLoc = value;
                }

            Instruction = "Enter a directory path containing the index folder location:";
            value = indexLoc;
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    indexLoc = value;
                }

            Instruction = "Enter a filename for the individually selected trec_eval results:";
            value = defaultManualTrecResultsFileName + ".txt";
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    defaultManualTrecResultsFileName = value;
                }
           
            Instruction = "Enter a filename for the generation of a range of trec_eval results:";
            value = defaultAutoTrecResultsFileName + ".txt";
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    defaultAutoTrecResultsFileName = value;
                }

            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Begin processing of source file");
            Cursor.Current = Cursors.WaitCursor;
                return Tuple.Create(dataLoc, indexLoc, defaultManualTrecResultsFileName, defaultAutoTrecResultsFileName, numDocsToDisplay);
        } //  end SetupQuestions


            /// <summary>
            /// Which search or trec results options
            /// </summary>
            /// <remarks>
            /// Home screen
            /// </remarks>
            public void UseQASystem(NewSystem myLuceneApp, int numDocsToDisplay, List<RootObject> listRO, List<string> trecResultsArray, string dataLoc, string defaultManualTrecResultsFileName, string defaultAutoTrecResultsFileName, WordNetEngine wordNet)
        {
            while (true)
            {
                    string inValue = "";
                Console.WriteLine("Enter 1 to see a demonstration of the Answering system using supplied queries.\nEnter 2 to manually enter queries.\nEnter 3 to select a range of supplied queries to generate the input to Information Retrieval evaluation program trec_eval.\nEnter 4 to select individual supplied queries as input to Information Retrieval evaluation program trec_eval.\nOr, enter <exit> to finish.");

                    string Instruction = "Enter 1, 2, 3, or 4:";
                    string value = "";
                    if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        inValue = value;
                    }
                                    
                if (inValue == "exit")
                {
                    Console.WriteLine("You are now shutting down the KUT EduQuiz Question Answering System");
                    break;
                }


                int taskSelection;
                if (!int.TryParse(inValue, out taskSelection))
                {
                     MessageBox.Show("Not an integer!");

                }
                if (int.TryParse(inValue, out taskSelection))
                {
                    if ((taskSelection < 1) || (taskSelection > 4))
                    {
                        Console.WriteLine("You must enter a number between 1 and 4 inclusive");
                    }

                    if ((taskSelection < 5) && (taskSelection > 0))
                    {
                        if (taskSelection == 1)
                        {
                            myLuceneApp.Demonstration(myLuceneApp, numDocsToDisplay, wordNet);
                        }
                        if (taskSelection == 2)
                        {
                            myLuceneApp.ManualNaturalLanguageQuery(myLuceneApp, numDocsToDisplay, wordNet);
                        }
                        if (taskSelection == 3)
                        {
                            myLuceneApp.SelectRangeTrecResults(myLuceneApp, listRO, trecResultsArray, dataLoc, defaultAutoTrecResultsFileName, wordNet);
                        }
                        if (taskSelection == 4)
                        {
                            myLuceneApp.SelectIndividualTrecResults(myLuceneApp, listRO, trecResultsArray, dataLoc, defaultManualTrecResultsFileName, numDocsToDisplay, wordNet);
                        }

                    }
                }
            } // end while loop
        }

       

        /// <summary>
        /// Creates wordnet libraries
        /// </summary>
        /// <remarks>
        /// No user input
        /// </remarks>
        public void LoadWordnet(string directory, WordNetEngine wordNet)
        {
            wordNet.AddDataSource(new StreamReader(Path.Combine(directory, "data.adj")), PartOfSpeech.Adjective);
            wordNet.AddDataSource(new StreamReader(Path.Combine(directory, "data.adv")), PartOfSpeech.Adverb);
            wordNet.AddDataSource(new StreamReader(Path.Combine(directory, "data.noun")), PartOfSpeech.Noun);
            wordNet.AddDataSource(new StreamReader(Path.Combine(directory, "data.verb")), PartOfSpeech.Verb);

            wordNet.AddIndexSource(new StreamReader(Path.Combine(directory, "index.adj")), PartOfSpeech.Adjective);
            wordNet.AddIndexSource(new StreamReader(Path.Combine(directory, "index.adv")), PartOfSpeech.Adverb);
            wordNet.AddIndexSource(new StreamReader(Path.Combine(directory, "index.noun")), PartOfSpeech.Noun);
            wordNet.AddIndexSource(new StreamReader(Path.Combine(directory, "index.verb")), PartOfSpeech.Verb);

            Cursor.Current = Cursors.WaitCursor;
            Console.WriteLine("Loading WordNet database...");
            start = System.DateTime.Now;
            wordNet.Load();
            end = System.DateTime.Now;
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Time to produce WordNet database: {0}", end - start);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("Load completed.");
            Cursor.Current = Cursors.Default;



        }

        /// <summary>
        /// Query expansion
        /// </summary>
        /// <remarks>
        /// Cycles through each token
        /// If it has synonyms, asks which POS kinds to expand the query with
        /// Asks for boost for original query terms
        /// Asks if any queries must be in results
        /// Asks if any words must not be in results
        /// </remarks>
        /// /// <returns>
        /// Expanded query as a single string, space delimited
        /// </returns>
        public string ExpandQuery(NewSystem myLuceneApp, WordNetEngine wordNet, string querytext)
        {

            string[] querytextArrayIn = querytext.Split(myLuceneApp.splitters, StringSplitOptions.RemoveEmptyEntries);
            List<string> querytextListOut = new List<string>();

            foreach (string word in querytextArrayIn)
            {
                string line = "no";
                var synSetList = wordNet.GetSynSets(word);
                if (synSetList.Count() > 0) // if there are no synonyms don't ask
                {
                    while (true)
                    {
                    string Instruction = string.Format("Do you want to expand the queries for the word {0}? Enter yes or no:", word);
                    string value = "";
                        if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            line = value;
                        }
                        if ((line == "yes") || (line == "no")) { break; }
                        MessageBox.Show("You must enter yes or no!!");
                    } //  end while
                }


                string wordBoostString = "1";
                float wordBoost = AskForBoost(("word " + word));

                string wordOriginal = word;
                while (true)
                {
                    Instruction = string.Format("Do you want to receive only those results containing the word {0}? Enter yes or no:", wordOriginal);
                    string mustUse = "";
                    value = "";
                    if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        mustUse = value;
                    }

                    if (mustUse == "yes")
                    {
                        wordOriginal = "+" + wordOriginal;
                        break;
                    }
                    if (mustUse == "no")
                    {
                        break;
                    }
                    MessageBox.Show("You must enter yes or no!!");
                } //  end while

                wordBoostString = System.Convert.ToString(wordBoost);
                querytextListOut.Add(wordOriginal + "^" + wordBoostString);

                if (line == "yes") // if user wants to expand queries
                {
                    List<string> posTypes = new List<string>();
                    while (true) // Nouns
                    {
                        Instruction = string.Format("Do you want to expand {0} with nouns? Enter yes or no:", word);
                        value = "";
                        if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            line = value;
                        }
                        if (line == "yes")
                        {
                            posTypes.Add("Noun");
                            break;
                        }
                        if (line == "no") break;
                        Console.WriteLine("You must enter yes or no!!");
                    } // end while nouns
                    while (true) // Verbs
                    {
                        Instruction = string.Format("Do you want to expand {0} with verbs? Enter yes or no:", word);
                        value = "";
                        if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            line = value;
                        }
                        if (line == "yes")
                        {
                            posTypes.Add("Verb");
                            break;
                        }
                        if (line == "no") break;
                        Console.WriteLine("You must enter yes or no!!");
                    } // end while verbs
                    while (true) // Adjectives
                    {
                        Instruction = string.Format("Do you want to expand {0} with adjectives? Enter yes or no:", word);
                        value = "";
                        if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            line = value;
                        }
                        if (line == "yes")
                        {
                            posTypes.Add("Adjective");
                            break;
                        }
                        if (line == "no") break;
                        Console.WriteLine("You must enter yes or no!!");
                    } // end while adjectives
                    while (true) // Adverbs
                    {
                        Instruction = string.Format("Do you want to expand {0} with advderbs? Enter yes or no:", word);
                        value = "";
                        if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                        {
                            line = value;
                        }
                        if (line == "yes")
                        {
                            posTypes.Add("Adverb");
                            break;
                        }
                        if (line == "no") break;
                        Console.WriteLine("You must enter yes or no!!");
                    } // end while adverbs

                    foreach (var synSet in synSetList)
                    {
                        string partOfSpeech = Convert.ToString(synSet.PartOfSpeech);
                        if (posTypes.Any(partOfSpeech.Contains))
                        { querytextListOut.AddRange(synSet.Words); }
                    } // end foreach (var synSet in synSetList)
                } // end of query expansion POS type
            } // end foreach (string word in querytextArrayIn)
            querytextListOut = querytextListOut.Distinct().ToList();

            Instruction = "Do you want to exclude any terms from your results? Enter one word, or enter <exit>:";
            value = "";
            if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                {
                    line = value;
                }

            while (true)
                {   
                if (line == "exit") break;
                    MessageBox.Show("All results containing {0} will be excluded.", line);
                    querytextListOut.Add("-" + line);
                    string Instruction = "Enter another term to exclude, or enter <exit>:";
                    string value = "";
                    if (InputBox("User Input Required", Instruction, ref value) == DialogResult.OK)
                    {
                        line = value;
                    }
                } // end while

            string querytextOut = String.Join(" ", querytextListOut.ToArray()); // convert expanded list of words into a single string
            return querytextOut;
        } // end ExpandQuery


        public void Run()
        {
            string dataLoc = @"H:\647C#\TheApplicationGUI";
            string indexLoc = @"H:\647C#\TheApplicationGUI";
            string fullFileName = "collection.json";
            string reducedFileName = "reducedCollection.json";
            string defaultManualTrecResultsFileName = "NewSystemResultsSelectedQueries";
            string defaultAutoTrecResultsFileName = "NewSystemResultsRangeQueries";
            NewSystem myLuceneApp = new NewSystem();
            List<string> trecResultsArray = new List<string>();
            //List<RootObject> listRO = new List<RootObject>();

            myLuceneApp.passageFieldName = "PassageContent";
            myLuceneApp.titleFieldName = "Title";
            myLuceneApp.urlFieldName = "URL";
            myLuceneApp.passageIdFieldName = "QueryId";

            var directory = System.IO.Directory.GetCurrentDirectory();
            var wordNet = new WordNetEngine();
            myLuceneApp.LoadWordnet(directory, wordNet);

            Tuple<string, string, string, string, int> setupAnswers = myLuceneApp.SetupQuestions(dataLoc, indexLoc, defaultManualTrecResultsFileName, defaultAutoTrecResultsFileName);

            dataLoc = setupAnswers.Item1;
            indexLoc = setupAnswers.Item2;
            defaultManualTrecResultsFileName = setupAnswers.Item3;
            defaultAutoTrecResultsFileName = setupAnswers.Item4;
            int numDocsToDisplay = setupAnswers.Item5;

            List<RootObject> oldListRO = myLuceneApp.LoadJson(dataLoc, fullFileName);
            myLuceneApp.GenerateQrel(oldListRO, dataLoc);
            List<RootObject> listRO = myLuceneApp.RemoveBadPassages(oldListRO);

            myLuceneApp.OpenIndex(indexLoc);
            myLuceneApp.CreateAnalyser();
            myLuceneApp.CreateWriter();
            myLuceneApp.IndexCycle(listRO, myLuceneApp);
            myLuceneApp.CleanUpWriter();
            myLuceneApp.CreateSearcher();
            myLuceneApp.CreateParser();
            Cursor.Current = Cursors.Default;
            myLuceneApp.UseQASystem(myLuceneApp, numDocsToDisplay, listRO, trecResultsArray, dataLoc, defaultManualTrecResultsFileName, defaultAutoTrecResultsFileName, wordNet);
        }

        // I copied this from: http://www.csharp-examples.net/inputbox/
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
