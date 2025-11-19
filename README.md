# Find to Graph
An example Optimizely PaaS CMS 12 site showing the steps to convert from Search & Navigation to Optimizely Graph. 

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

Optimizely used to call Search & Navigation "FIND". All code references still use the word "Find".

Next, I added the NuGet packages for Search & Navigation. 
```bash
$ dotnet add EPiServer.Find.Cms
```

I needed to get a demo index for Find. I signed up at https://find.optimizely.com/. I copied the settings from that web site into appsettings.Development.json.

In Startup.cs I needed to call `.AddFind()` in the `ConfigureServices` method.

The search controller calls FIND and passes the query. Once it gets the responses it calls `GetContentResult()` to get the pages from the database for each result.

When I ran the site, I noticed \<P\> elements from my rich text were included in the search results. I added code to strip HTML tags from the Body property.
In general, a site using FIND will follow this pattern. It will get the results from FIND and then get the content from the database. Then it will 