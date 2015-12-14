# DynamicOdata
Dynamic Odata version 3


Dynamic ODATA is ODATA service which dynamically get your data in any database throught out ODATA.

If you want to get data from Adventure, just add the connection string into web.config:

     <add name="adventure" connectionString="data source=.\SQLEXPRESS;initial catalog=AdventureWorks2014;User ID={username};Password={password};"/>

Then ODATA service will be available at: 

    localhost:{port}/odata/adventure
    

