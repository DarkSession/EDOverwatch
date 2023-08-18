# Elite: Dangerous Overwatch (ED: Overwatch / DCoH: Overwatch)

## Purpose
ED: Overwatch is an application to track the progress of Aftermath War in the second Thargoid war in Elite Dangerous.  
The application collects data from various sources and makes it available to users and application through the web app or API respectively.

The web app is accessible using the following link:  
[DCoH.Watch](https://dcoh.watch/)

## Key Dependencies
- Framework: ASP.NET 7 runtime
- ActiveMQ Artemis
- MariaDB
- Tesseract OCR Engine

## Components

### DCoH Tracker Discord Bot
Directory: [src/DCoHTrackerDiscordBot](src/DCoHTrackerDiscordBot)  
Built with .NET 7 and Discord.Net it provides the functionality for the DCoH Tracker Discord Bot.

### CApi
Directory: [src/EDCApi](src/EDCApi)  
Small library component for the Frontier CApi functionality in the other projects.

### Database
Directory: [src/EDDatabase](src/EDDatabase)  
Entity Framework Core structure of the database.

### Data Processor
Directory: [src/EDDataProcessor](src/EDDataProcessor)  
Main data processor which receives and processes the data from other sources.

### EDDN Client
Directory: [src/EDDNClient](src/EDDNClient)  
Client application to receive and pre-filter messages from the [EDDN](https://github.com/EDCD/EDDN).

### Overwatch
Directory: [src/EDOverwatch](src/EDOverwatch)  
Processes the events from other applications to detect and update Thargoid activity.

### Overwatch Web
Directory: [src/EDOverwatch Web](src/EDOverwatch+Web)  
The ED: Overwatch web application, built with ASP.Net 7 and Angular.

### Overwatch Weekly Reset
Directory: [src/EDOverwatchWeeklyReset](src/EDOverwatchWeeklyReset)  
Updates the systems to reflect the new in-game states after the weekly server tick.

### System Progress
Directory: [src/EDSystemProgress](src/EDSystemProgress)  
Library to recognize the progress screenshots posted and extract the relevant information from them.

