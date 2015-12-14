# DynamicOdata
Dynamic Odata version 3


Dynamic ODATA is ODATA service which dynamically get your data in any database throught out ODATA.

The purpose is to make ODATA API is simple to use, if you want to get data from [Adventure](http://msftdbprodsamples.codeplex.com/releases), just provide and add the connection string into web.config:

     <add name="adventure" connectionString="data source=.\SQLEXPRESS;initial catalog=AdventureWorks2014;User ID={username};Password={password};"/>

Then ODATA service will be available at: 

    localhost:{port}/odata/adventure
    
Under the hood, this library is built based on two points:

1. Build EDM model dynamically for per request.
2. Convert ODATA query to SQL directly. This implementation just support some basic ODATA query $top, $select, $order, $filter, $skip.
