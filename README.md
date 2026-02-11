# Find to Graph
An example Optimizely PaaS CMS 12 site showing the steps to convert from Search & Navigation to Optimizely Graph. 

## Build a sample site. 
I started with an empty PaaS site. I created it with: 

```bash
$ dotnet new epi-cms-empty
````

My database was on SQL server running in a Docker container. It's not the simplest system. Any database can work. Start with an empty database. Put the ConnectionString in appsettings.Development.json
That file should be in the .gitignore file so it doesn't get checked in.

The site should run at this point. It prompts for an admin user to be created. Since there is no home page, it will show a 404 error.

The admin path is `/episerver/cms/`. Some CMS 12 sites use `/ui/cms/`

Using CoPilot, it didn't take long to create 3 page models with controllers and views. There is a *HomeType* page with a link to a *SearchPage* which searches for *ArticlePage* type pages. Article Pages have a *Body* property which is rich text. The search controller didn't search at this point. 

From the admin interface, I created instances of these pages and 3 Article Pages with content in the Body property. I assigned the HomeType page as the site's start page.

> **Note:** Optimizely used to call Search & Navigation "FIND". All code references still use the word "Find".

[commit](commit/33cf131957f987d787dfe28581119408e4e14776)

## Add Search & Navigation (FIND)
Next, I added the NuGet package for Search & Navigation. 
```bash
$ dotnet add EPiServer.Find.Cms
```

I needed to get a demo index for Find. I signed up at https://find.optimizely.com/. I copied the settings from that web site into appsettings.Development.json.

In Startup.cs I needed to call `.AddFind()` in the `ConfigureServices` method.

The search controller calls FIND and passes the query. Once it gets the responses it calls `GetContentResult()` to get the pages from the database for each result.

When I ran the site, I noticed \<P\> elements from my rich text were included in the search results. I added code to strip HTML tags from the Body property.

In general, a site using FIND will follow this pattern. It will get the results from FIND and then get the content from the database. Then it will map the results to a ViewModel. 

[commit](commit/62bbd8a8b321cb933f2506253577d01a072ee010)

## Add Graph
Now for the Graph part. I added the NuGet package for Graph.
```bash	
dotnet add Optimizely.ContentGraph.Cms
```

In Startup.cs I added `.AddContentGraph()` in the `ConfigureServices` method.
Graph depends on the ContentDeliveryApi, so I also added `services.AddContentDeliveryApi();`

I got the Graph keys and added them to appsettings.Development.json. 
The keys for Optimizely Graph come from the Development Portal (formerly called PaaS Portal). If you haven't started with Graph yet, there is a button on the Development portal to start using Optimizely Graph. There is a scary message about not being able to undo it. You do need to press that button when you are ready to start developing for Graph. It doesn't break anything. 

**Note:**  that the Single Key is not secret. It can be used in client side code. It gives read-only access to published content. 

I do not want to check in the appsettings.Development.json file. It's in the .gitignore. It's specific to each developer's environment. On test servers and production, the settings and keys are automatically in the configuration.
Developers don't have to get a development index for Optimizely Graph like they did for FIND. Instead, developers use the keys and Graph instance from the **integration** environment. 

The ContentGraph package will synchronize the content to the Graph service when content is published. It adds some scheduled jobs that administrators can use to trigger a sync as well. It also adds the link to the GraphiQL playground. 

At this point we have both FIND and Graph installed. 


[commit](commit/193f69aa9b7ca79e2cb57106bac845acec47ad04)

## Add StrawberryShake GraphQL Client

GraphQL queries will return JSON text. It would be unnecessary work to write code to parse the JSON and check types. 

GraphQL provides a schema describing the types of all the data.

Optimizely once offered a package called Optimizely.Graph.Client but it is now deprecated. You may see references to it in documentation and blog posts, but don't use it.

Instead we use a standard GraphQL client. StrawberryShake is a good choice.

StrawberryShake knows what fields you asked for in your query. The GraphQL schema tells it what type those fields are. It defines a strongly typed class for the results. 

The documentation is https://chillicream.com/docs/strawberryshake/v15/get-started/console

You need the server package and the tools package. The tools package is a command line tool that downloads the Graph schema.
```bash
dotnet add StrawberryShake.Server
dotnet new tool-manifest
dotnet tool install StrawberryShake.Tools
```

You need to give the tool the URL for the GraphQL endpoint and the single key.
https://cg.optimizely.com/content/v2?auth=  your single key

The command to download the schema is:
```bash
dotnet graphql init https://cg.optimizely.com/content/v2?auth=  your single key  -n  GraphQLClient
```

It stores the parameters in .graphqlrc.json. The schema is downloaded as schema.graphql and schema.extensions.graphql.
Those files should be added to source control because they are needed by the build. You should not call init or update during the build because you want to build with the schema.graphql that was used during development. 

When the models change, the new models get synchronized to the Graph index. You can download the schema.graphql in your development environment with 
```bash
dotnet graphql update
```
You don't need any parameters. It uses the URL in the .graphqlrc.json file.

In the .graphqlrc.json file, I named my client "GraphQLClient". That's the name of the class that will contain all the queries. It's specific to the project. You should choose a different name for your client. 

In startup.cs I added my GraphQLClient to the services. It requires setting the BaseAddress for the HttpClient.
You would read the auth key from the configuration, but for this demo I hard-coded it. 
```csharp
services.AddGraphQLClient().ConfigureHttpClient(c => c.BaseAddress = new Uri("https://cg.optimizely.com/content/v2?auth=
```

## Convert the code from FIND to Graph

To write the query in GraphQL, I used the GraphiQL playground to build and test the query. Then I copied the code to 
a new file with a .graphql extension in Visual Studio. 

StrawberryShake provides type validation and autocompletion in .graphql files. The file gets compiled into the GraphQLClient class.

Here is the query I used:
```graphql
query articles($query: String) {
  ArticlePage(where: { _fulltext: { match: $query } }) {
    items {
      Body {         
        html
      }
    }
  }
}
```

In the search Controller, I replaced the IClient from FIND with the IGraphQLClient. Note that interface name is the name I gave the client and will be different for different projects. 
The code has to be converted and it is different. StrawberryShake is strongly typed and matches the response that you saw from the GraphiQL playground.

The biggest difficulty is that StrawberryShake does not provide a non async version of the ExecuteQuery method. You could use `.GetResult()` to call it synchronously, but that has a danger of creating thread deadlocks. It's better to make the controller action async. If your FIND queries were deep in a service, every method up the call stack has to be made async. 

At this point I was able to remove all references to FIND including the NuGet package.

A common pattern for FIND is: 

- Configure the search parameters. 
- Search
- Get content items from the database for each search result. 
- Map content to a viewModel. This may require fetching dependencies from the database. 
- Cache. 
- Use the results in a controller. 

When converting to Graph: 
- configure the search parameters as variables. 
- Make the controller Async. 
- Call the GraphQL ExecuteAsync
- Map the data items to a viewModel. When you use "Expanded" in the query, you often don't need to call the database. 
- Cache. 
- Use the results in the controller. 

[commit](commit/13b7c87197a13ce5c0560d786a8ea4c54847d4af)

## Bug fixing

I didn't see the results I was expecting. 

If a property is an XHtmlString, it is normally not part of the `_fulltext` and it can't be used in a where clause. When you select its response you have to select html or json formatted data. 
If you mark the property as `[searchable]` then it's serialized as a String. It can be used in the where clause and it appears in the `_fulltext`. When you select its response, you get a string of HTML. You can't specify html or json. 

I had to modify the `ArticlePage` model. Then I ran the project. That caused the new model type to be synchronized to the Graph service. Then I updated the schema in my development environment. Then I modified the query to match the new model.
```bash
dotnet graphql update
```
I also had to restart Visual Studio when it didn't recognize the changes.

At that point, the .graphql file wouldn't compile. My reference to `Body { html }` was no longer valid with the new schema. It's nice to have compile time checking.

## Summary
This simple web application demonstrated the steps required in converting from Search & Navigation (FIND) to Optimizely Graph. Each commit shows exact file changes including all my mistakes. It shows which packages to add and the modifications required in the startup.cs. 
