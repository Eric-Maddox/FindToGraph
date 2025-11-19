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

Using CoPilot, it didn't take log to create 3 page models with controllers and views. There is a *HomeType* page with a link to a *SearchPage* which searches for *ArticlePage* type pages. Article Pages have a *Body* property which is rich text. The search controller didn't search at this point. 

From the admin interface, I created instances of these pages and 3 Article Pages with content in the Body property. I assigned the HomeType page as the site's start page.

> **Note:** Optimizely used to call Search & Navigation "FIND". All code references still use the word "Find".

[commit](commit/33cf131957f987d787dfe28581119408e4e14776)

## Add Search & Navigation (FIND)
Next, I added the NuGet packages for Search & Navigation. 
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

**Note**  that the Single Key is not secret. It can be used in client side code. It gives read-only access to published content. 

I do not want to check in the appsettings.Development.json file. It's specific to each developer's environment. On test servers and production, the host will provide the keys in environment variables.

The ContentGraph package will synchronize the content to the Graph service when content is published. It adds some scheduled jobs that administrators can use to trigger a sync as well. It also adds the link to the GraphiQL playground. 

At this point we have both FIND and Graph installed. 

[commit](commit/193f69aa9b7ca79e2cb57106bac845acec47ad04)

## Add StrawberryShake GraphQL Client
It does not give us a way to query the graph from code. 
For that we need another package. Optimizely once offered a package called Optimizely.Graph.Client but it is now deprecated. You may see references to it in documentation and blog posts, but don't use it.
Instead we use a standard GraphQL client. StrawberryShake is a good choice.

The documentation is https://chillicream.com/docs/strawberryshake/v15/get-started/console

You need the server package and the tools package. The tools package is a command line tool that downloads the Graph schema.
```bash
dotnet add StrawberryShake.Server
dotnet new tool-manifest
dotnet tool install StrawberryShake.Tools
```

You need to give the tool the URL for the GraphQL endpoint and the single key.
https://cg.optimizely.com/content/v2?auth=  your single key

I named my client GraphQLClient. That's the name of the class that will contain all the queries. It's specific to the project. 

The command to download the schema is:
```bash
dotnet graphql init https://cg.optimizely.com/content/v2?auth=  your single key  -n  GraphQLClient
```

It stores the parameters in .graphqlrc.json. When the models change, the new models get synchronized to the Graph index. Then you can update the schema in your development environment with 
```bash
dotnet graphql update
```
You don't need any parameters. It uses the .graphqlrc.json file.

In startup.cs I added the GraphQL client to the services. It requires setting the BaseAddress for the HttpClient.
```csharp
services.AddGraphQLClient().ConfigureHttpClient(c => c.BaseAddress = new Uri("https://cg.optimizely.com/content/v2?auth=
```

## Convert the code from FIND to Graph

To write the query in GraphQL I used the GraphiQL playground to build and test the query. Then I copied the code to 
a new file with a .graphql extension in Visual Studio. 

StrawberryShake allows type validation and autocompletion in .graphql files. The file gets compiled into the GraphQLClient class.

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
- Call the GraphQL
- Map the data items to a viewModel. When you use "Expanded" in the query, you often don't need to call the database. 
- Cache. 
- Make the controller Async. 
- Use the results in the controller. 

[commit](commit/13b7c87197a13ce5c0560d786a8ea4c54847d4af)

## Bug fixing

I didn't see the results I was expecting. 

If a property is an XHtmlString, it is normally not part of the `_fulltext` and it can't be used in a where clause. When you select it's response you have to select html or json formatted data. 
If you mark the property as `[searchable]` then it's serialized as a String. It can be used in the where clause and it appears in the `_fulltext`. When you select it's response you get a string of html. You can't specify Html or Json. 

I had to modify the `ArticlePage` model. Then I ran the project. That caused the new model type to be synchronized to the Graph service. Then I updated the schema in my development environment. Then I modified the query to match the new model.
```bash
dotnet graphql update
```
I also had to restart Visual Studio when it didn't recognize the changes.

At that point, the .graphql file wouldn't compile. My reference to Body {{ html }} was no longer valid with the new schema. It's nice to have compile time checking.

## Summary
This simple web application demonstrated the steps required in converting from Search & Navigation (FIND) to Optimizely Graph. Each commit shows exact file changes including all my mistakes. It shows which packages to add and the modifications required in the startup.cs. 
