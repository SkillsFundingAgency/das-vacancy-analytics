# Vacancy Analytics

This repository represents the Vacancy Analytics subsystem code base being used as part of the ESFA Vacancy Services related government services.

## Contents

* [Developer setup](#devsetup)
	* [Environment setup](#envsetup)
		* [Using docker hosted containers](#usingDocker)
		* [Database setup](#dbSetup)
		* [Functions project setup](#funcSetup)
* [Running the functions/jobs](#running)
* [Function and webjob logs](#logs)
* [Development](#development)
* [License](#license)

&nbsp;

## Developer setup <a name="devsetup"></a>

### Requirements

In order to run this solution locally you will need the following:

* An EventHub namespace with a single `vacancy` EventHub in Azure. - create using [Azure portal](https://portal.azure.com) or [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* A storage account in Azure (for EventHubs local storage emulators will not work) - create using [Azure portal](https://portal.azure.com) or [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* [.NET Core SDK >= 2.1](https://www.microsoft.com/net/download/)
* (VS Code Only) [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
* (Full Visual Studio Only) [Azure Function and Web Jobs Tools Extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools)
* [Docker for X](https://docs.docker.com/install/#supported-platforms)
* [SSDT (SQL Server Data Tools)](https://msdn.microsoft.com/en-us/library/hh272686(v=vs.103).aspx "SQL Server Data Tools") - for scaffolding/publishing a SQL Database
* [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) :zap: - installation instructions via [homebrew](https://brew.sh/)/[chocolatey](https://chocolatey.org/)/[npm](https://www.npmjs.com/) described in [link](https://github.com/Azure/azure-functions-core-tools#installing) (needs to be a version compatible with v2 functions)

&nbsp;

### Environment setup (after cloning repository locally) <a name="envsetup"></a>

> Ignore the following `Using docker hosted containers` section if not using an installed SQL Server instance and/or Azure storage emulator and ELK logging.

#### Using docker hosted containers <a href="usingDocker"></a>
The default development environment uses docker containers to host it's dependencies (outside of Azure EventHub).

* SqlDb (hosted in a Linux :penguin: container if an existing installed SQL Server/LocalDB is not being used
* [Azurite](https://github.com/Azure/Azurite) (Cross platform Azure Storage Emulator) if an existing installed Azure Storage Emulator is not being used to emulate external storage queue.
* Redis and ELK containers - These are created purely for logging from the app into a logstash pipeline for viewing/monitoring in Kibana. (Not essential)

To start the containers copy the `docker-compose.yml` and `.docker` file from [das-recruit](https://github.com/SkillsFundingAgency/das-recruit/tree/master/setup/containers) to a directory of your choice and then run the following from the directory:

>`docker-compose up -d`

You can view the state of the running containers using:

>`docker ps -a`

&nbsp;

Check you can connect to the container hosted SQL server using the following details or building a [SQL Server connection string](https://www.connectionstrings.com/sql-server/):

|						|																							|
|-----------------------|-------------------------------------------------------------------------------------------|
|Server name/address 	| `127.0.0.1,1401` (Use a `,` to separate the server name address from port number :wink:)  |
|Database name       	| `master` (for now `das-vacancy-analytic-events` later)                                    |
|SQL login user      	| `sa`                                                                                      |
|Password            	| `Ozzyscottypassword1`                                                                     |
|						|																							|

Alternatively the following SQL connection string:
>Data Source=127.0.0.1,1401;Initial Catalog=das-vacancy-analytic-events;User Id=sa;Password=Ozzyscottypassword1

&nbsp;

#### Database setup <a href="dbSetup"></a>
##### Preparing the Vacancy Event Store SQL Database (Windows Only)
* Build the `Analytics.sln` solution using Visual Studio. Alternatively in the directory of the `.sln` file run `dotnet build`/`msbuild`.
* Within Visual Studio right-click the VacancyAnalyticsEvents sql proj and select `Publish...`, in the popup window select the target SQL server and the name of the database as `das-vacancy-analytic-events` and select `Publish` action and wait for the database to be created and setup. Alternatively save/load and previously created/used profile. If using the SQL server docker container in the popup window load the `VacancyAnalyticEvents-docker-publish.xml` profile and select `Publish`.

##### Preparing the Vacancy Event Store SQL Database (Linux/Mac Only)
* Using SQL CMD or SQL Operations Studio, connect to the docker conatiner SQL database server and create a database called `das-vacancy-analytic-events`.
* Run the following scripts in the following order from the _**src/VacancyAnalyticEvents/**_ directory:
	* */Security/VACANCY.sql*
	* */VACANCY/Tables/Event.sql*
	* */VACANCY/User-Defined Table Types/VacancyEventsTableType.sql*
	* */VACANCY/Stored Procedures/Event_GET_EventsSummaryForVacancy.sql*
	* */VACANCY/Stored Procedures/Event_GET_RecentlyAffectedVacancies.sql*
	* */VACANCY/Stored Procedures/Event_INSERT_BatchEvents.sql*

#### Functions Project Setup <a href="funcSetup"></a>

Azure functions expect configuration settings to be specified within the Azure portal hosting the function. For running locally, the functions app expects a `local.settings.json` file to hold configuration settings which will need to be created manually in the directory of the `Functions.csproj` as this file will not be committed to the repository.

Typical configuration settings with placeholders describing values that will need providing:

```json
{
    "IsEncrypted": false,
    "Values": {
        "APPINSIGHTS_INSTRUMENTATIONKEY": "{Generate a new AppInsights instrumentation key in Azure portal for this functions app}",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "AzureWebJobsStorage": "{This should be a real Azure storage account}",
        "VacancyEventHub": "{EventHub namespace not including the individual EventHub EntityPath}"
    },
    "ConnectionStrings": {
        "VacancyAnalyticEventsSqlDbConnectionString": "{Connection string with the user that has read/write/execute priveleges}",
        "QueueStorage": "{das-recruit storage account}"
    }
}
```

There exists two functions within the functions app:

|Name | Trigger | Purpose |
|----------------------------------------|---------------------|--------|
|EnqueueVacancyAnalyticsSummaryGenerator | Timer (Runs Hourly) | Uses the event store (SQL) to determine which vacancies have had new events in the last hour and quques each vacancy to a queue named `generate-vacancy-analytics-summary` for a das-recruit job to process and generate a Vacancy Analytics Summary query store document for. |
|PublishVacancyEvent | Http Trigger | Is a temporary function used to send an event to the `vacancy` EventHub. Will be used typically from the Find an apprenticeship service. |



#### WebJobs Project Setup

There is exists a solitary webjob which runs as a regular console app. Unlike Azure functions apps, the webjobs project can make use of [*user secrets*]() to store sensitive configuration settings that will not be committed to the repository. 

Purpose: To read the events from the Azure `vacancy` event hub (generally as a stream in batches) and save those events to a SQL store for use by other ESFA Vacancy services.

```json
{
    "ConnectionStrings": {
        "AzureWebJobsStorage": "{Must be a real Azure storage account connection string}",
        "VacancyEventHub": "{Must be a real Azure EventHub account connection string with read priveleges}"
    }
}
```

&nbsp;

### Running the functions/jobs :running: <a name="running"></a>

* Open command prompt and change directory to _**/src/Functions/**_
* Start the **Functions** :zap::

MacOS :apple: / Linux :penguin:
```bash
sudo func host start --build
```
Windows cmd
```cmd
func host start --build
```

* Open a second command prompt and change directory to _**/src/Jobs/Esfa.VacancyAnalytics.Jobs/**_
* Start the **Webjob** :cyclone::

MacOS :apple: / Linux :penguin:
```bash
ASPNETCORE_ENVIRONMENT=Development
dotnet run
```
Windows cmd
```cmd
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```
&nbsp;

### Function and webjob logs <a href="logs"></a>
There is minimal logging for the functions and webjob because of how busy the proccesses will get and to avoid noisy logs. For running locally logging will be written to the console. In addition, telemetry will be sent to App Insights if configured correctly which can highlight issues and help with monitoring performance.

If using the docker containers described near the top of this document, then logging for the webjob will also write to a containerised ELK stack which can be viewed in Kibana hosted at https//localhost:5601

&nbsp;

### Development <a href="development"></a>

* Open the solution _**/src/Analytics.sln**_ in either Visual Studio or the _**src**_ directory in VS Code.

#### Functions :zap:

* Use `func new` for a wizard to help create a new function within the functions app. The new functions have to be C#.

#### Webjobs :cyclone:

* There should exist only one webjob and that is the main console app and `Main` entry point. The project isn't for creating trigger event webjobs hosted inside a webjobs host.

#### Database

* Inside the solution, or folder structure for `VacancyAnalyticEvents`, store SQL definition scripts for SQL database objects.

&nbsp;

## License <a href="license"></a>

Licensed under the [MIT license](LICENSE)