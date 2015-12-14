# DynamicOdata
Dynamic Odata version 3


Dynamic ODATA is ODATA service which dynamically get your data in any database throught out ODATA. 

The purpose is to make ODATA API is simple to use, you don't need to create all Clr classes if you want to support ODATA over new database. All what you need to do is simply add the new connectionstring. Example: if you want to get data from [Adventure](http://msftdbprodsamples.codeplex.com/releases), just provide and add the connection string into web.config:

    <add name="adventure" connectionString="data source=.\SQLEXPRESS;initial catalog=AdventureWorks2014;User ID={username};Password={password};"/>

Then ODATA service will be available at: 

    localhost:{port}/odata/adventure
    
Under the hood, this library is built based on two points:

1. Build EDM model dynamically for per request. This step might be heavy if you have database which has lots of tables, in that case you can consider to use cache for EDM model per database.

2. Convert ODATA query to SQL directly. This implementation just support some basic ODATA queries: `$top`,`$select`, `$order`, `$filter`, `$skip`.
