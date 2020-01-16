# question-answering-index
C#

Creates a searchable index from the Microsoft Machine Reading Comprehension dataset (msmarco.org).The data consists of 100 000 human generated queries and 1 000 000 results passages from real Bing search engine usage. The correct result(s) for each query are available for system testing, and each  query also has a human generated response.

• processes the json source files   
• develops part-of-speech tags for words using the WordNet library   
• attempts to generate titles from the URL of each passage   
• generates searchable Lucene fields for the results passages and title   
• allows boosting of query words and search fields   
• allows part-of-speech query term expansion   
• allows user to enter a new, natural language query or to insert the supplied queries in the Bing data   
• allows user the choice of whether to pre-process the query   
• allows detailed display of document score calculation   
• highlights matching text between query and document   
• implements alternative document score function    
• generates bulk answers in the TREC query relevance judgments format for system comparison   
