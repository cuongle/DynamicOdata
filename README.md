# DynamicOdata
Dynamic Odata version 3

Library gives You posibility to query database over OData. Dynamic Odata is simple to use, you don't need to create all Clr classes if you want to support ODATA over new database. 

You have two options to use this:

1. Simple data query to any database. One thing you need to do is add connectionstring to configuration
2. Advanced data query with hierarchical data models read from views.

## **Option 1 -  Simple data access to any database**

All what you need to do is simply add the new connectionstring. Example: if you want to get data from [Adventure](http://msftdbprodsamples.codeplex.com/releases), just provide and add the connection string into web.config:

```
    <add name="adventure" connectionString="data source=.\SQLEXPRESS;initial catalog=AdventureWorks2014;User ID={username};Password={password};"/>
```

Then ODATA service will be available at: 

    localhost:{port}/odata/adventure

### **Architecture**

Under the hood, this library is built based on two points:

1. Build EDM model dynamically for per request. This step might be heavy if you have database which has lots of tables, in that case you can consider to use cache for EDM model per database.
sddsfdwefew
2. Convert ODATA query to SQL directly. This implementation just support some basic ODATA queries: `$top`,`$select`, `$order`, `$filter`, `$skip`.

## **Option 2 -  Advanced data query with hierarchical data models read from views**

This option is made for OWIN servers. Functionality of this module is to read views in specified database schema. When views are created with some convention it returns hierarchical models. 

### Simple scenario

To start using You need to call extension method `UseDynamicOData` on `HttpConfiguration` from namepsace `DynamicOdata.Service.Owin`. Method exposes `Action<ODataServiceSettings>`  to setup and extend functionality. You need to set required things as:

* `ConnectionString` - database on which perform queries
* `RoutePrefix` - prefix url on which odata controllers will be deployed
* `Schema` - database schema where DynamicOdata have to search for views

Simple start up Your project and call `{server}:{port}/{RoutePrefix}/$metadata` to see models read up from database.

### Advanced scenario with hierarchical data model 

If You want get hierarchical data model views need to be created with specific convention. This convention is simple as follows `.` is separator for nested object. Here is example:

```sql
CREATE VIEW [dbo].[CarView]
	AS
  SELECT
    C.Model,
    C.Producer,
    E.Cylinders as [Engine.Cylinders],
    E.FuelType as [Engine.FuelType],
    FE.InCity as [Engine.FuelConsumption.InCity],     
    FE.OutsideCity as [Engine.FuelConsumption.OutsideCity],
    FE.MixedMode as [Engine.FuelConsumption.MixedMode]
  FROM [Car] C
  join [Engine] E on E.CarId = C.Id
  join [FuelConsumption] FE on FE.Id = E.FuelConsumptionId
```

data returned will be as below:

```json
{
  "odata.metadata": "http://localhost:9000/odata/$metadata#dbo.container.CarViews",
  "value": [
    {
      "Engine": {
        "Cylinders": 4,
        "FuelType": "On",
        "FuelConsumption": {
          "InCity": "7.70",
          "OutsideCity": "5.10",
          "MixedMode": "6.20"
        }
      },
      "Model": "308",
      "Producer": "Peugeot"
    }
  ]
}
```

### Supported Odata queries
* `$top`
* `$skip`
* `$select`
* `$filter`
* `$order`
* `$inlinecount`
* functions: `startswith`, `endswith`, `substring`

### Extensibility

In `ODataServiceSettings` there is a extensibility point at which You can replace some services. Below is list of replacable services:

* `IDataService` - service which is called by deployed controller endpoints which will translate queries to database query and results to Edm entities.
* `IEdmModelBuilder` - builder for OData metadata model.
* `ISchemaReader` - service which reads database schema. Read schema is used by `IEdmModelBuilder`.

Possible usage:

* Limiting data by principal context in which call is made
* Changing $metadata model by own conventions
* Reading additional options from schema

For more examples look into samples folder in solution (*DynamicOdata.SelfHost* and *DynamicOdata.WebViews*).
    
### **How it works?**

At startup schema of views is read from specified database. After that *$metadata* is generated from that. At the end we build up `DataServiceV2` with injected ``ISqlQueryBuilder`` and ``IResultTransformer``. By default in this implementation query builder knows how to map OData queries to hierarchical database queries. Same with sql data result transformer. It produces hierarchical data results.

### *NOTES:*
If You add new view now it is required to restart Your OWIN server. It will *NOT* dynamically update the *$metadata*.