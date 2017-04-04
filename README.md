New Classrooms .NET Trial Project
=================

Instructions
-----------------
Open the solution with Visual Studio and run (may need administrator privilege because of HttpListener and port 4333 shouldn't be in use otherwise you can change it). To test the console app, enter the path of the file you want to convert. If successful, the converted file will be in /ConsoleApp1/bin/debug/ as output.json or output.xml. To test the HTTP API, you need to send a request with the file or data you want to test. I used Postman for testing. 

Design Choices
-----------------
The HTTP listener is in a separate thread because it needs to listen for connections and we also need the console to run the console app. 
The check to predetermine if a file is XML or Json or neither is by checking the first character. XML should start with "<" while Json should start with "{" or "[". It does not check for the validity of the file since that would require to read the entire file. Instead, the app will raise an exception during the conversion if the file is not valid (using Newtonsoft's Json.NET).
The HTTP API returns the conversion as text. 