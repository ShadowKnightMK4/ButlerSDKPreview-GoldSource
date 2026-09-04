
TheWorks is meant for integration and some end to end tests.




Arguments:
-olama			::	this means run the olama providere test
-all			::	shortand for running *all* tests in this app
-gemini			::	Run the gemini integration provider test
-openai			::  Run the openai integration provider test.
-crossref_tests ::  Run the test that brings a chat collection between all 3 providers.


While Olama doesn't need an api key - Gemini and OpenAI do.
you have two ways to do that.

-envsource_openai     x      :: This means read env 'x' as the openai key to use
-envsource_gemini	  y      :: This means read env 'y' as the gemini variable.


-filesource_openai   x		:: Load this file for openai
-filesource_gemini	 y		:: Load this file for gmeini.



Important on file loaders. If you specify a fully qualified file as it C:\Keys\what.key - it'll work as expected.
If you specify realative file (lack of the ':') it assumes its pulling out of the current directory.
Why?  Aiming to provide someone cloning this and accidently dropping keys into a public repo.


Consider the below:
TheWorks.exe  -crossref_tests -filesource_openai T:\OPENAI.key.txt -filesource_gemini T:\GEMINI.key.txt

This tells the works to run the crossref_test, and load two keys OUTSIDE the repo for use.



TheWorks.exe -gemini -filesource_gemini  "KEY_NAME"
This has the gemini test run and the env variable "KEY_NAME" is read for the api key.


TheWorks.exe -openai -filesource_openai  "\system\keys.dat"
Because this is not a full path in windows - it's gonna be read relative from the current directory.
It runs the openai test with the key stored in the pased file.

TheWorks.exe -openai -filesource_openai  "F:\data\keys.dat" -gemini -envsource_gemini "OTHER"
This command would run the openai test, load its key from F:\Data\keys.dat AND run the gemini test
while loading gemini's key from the "OTHER" env variable.


After the works:
--

Each test asks the LLM something and outputs it to see if ButlerSDK's path works.
CrossRef test in particual is making a random poem between Ollama (quen 0.5 on my pc), Gemini gemini-3.7-flash, and "gpt-4o".
While each butler instance (chat session object) is isolated, the chat collection isn't.

So Ollama starts off.
We make a Gemini butler with the chat collection reference we passed to Ollama. It adds on.
Finally we make an OpenAI butler with the same chat colleciton reference as the other 2.

We're testing if the data makes the jump and getting a silly poam as a bonus.






