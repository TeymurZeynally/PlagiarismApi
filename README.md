This project is intended to be used in the [GitHub Classroom](https://classroom.github.com/) to detect plagiarism in student works. [Moss](http://theory.stanford.edu/~aiken/moss/) is used to analyze works for plagiarism. This project is can be used in Autogading pipelines: after a student uploads code, plagiarism report is shown in pipeline logs.

## Project features

### Checking the student's code for similarity with the code of other students in the assignment

You need to call the `​/api​/github` method and pass the following parameters:

+ `organization` - the organization to which the GitHub Classroom is attached.
+ `repositoryPrefix` - prefix of the repositories in the organization. The prefix must be specified in such a way that the repositories belonging to a specific task are selected.
+ `reportFilterRegex` - Regex for filtering the Moss report. For example, to display only the lines that relate to a particular student.
+ `fileRegex` - Regex for files to look for in repositories. For example `.*.cpp` will only send files with .cpp extension to Moss.
+ `language` - Programming language from the list available for Moss
+ `templateRepository` - a repository whose files will be considered base for Moss. Code from base files is not parsed by Moss. For example `TeymurZeynally/GHC-Template-CPP-Plagiarism`.

### Checking zip files
Sometimes you just need to download a zip file with all the students' work and get a report.

You need to call the `/api/moss` method and pass the following parameters:
+ `language` - Programming language from the list available for Moss.
+ `fileRegex` - Regex for files to look for in zip archives. For example `.*.cpp` will only send files with .cpp extension to Moss.
+ `zip` - Zip archive with student work.
+ `baseZip` - Zip archive with base files.

## Dependencies
+ [TeymurZeynally/MossClient](https://github.com/TeymurZeynally/MossClient)
+ [octokit/octokit.net](https://github.com/octokit/octokit.net)

## Swagger
https://codeplagiarismapi.azurewebsites.net/swagger/index.html

