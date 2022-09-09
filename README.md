# CSV to Content Channel Block

This block is designed for simple use to populate content channels from an external data source. This tool is not designed to clean your data or compensate for garbage data, it is assumed you have been thoughtful in preparing a CSV file with appropriate data. Data that can be saved in attribute fields will be, however there will not be any validation, so it is a good idea to go through the Rock UI for each Content Channel Item and verify the data imported is valid.

## Block Configuration

**Structured Content Template**: Content Channels configured as Structured Content store JSON data as opposed to raw HTML for their content field. The default configuration is for v13 or less of Rock, it is possible if you are using this on a higher version of Rock it needs to be updated. Take a look at the raw value of the content of another structured content item to verify the template is correct. Be careful when modifying, make sure your JSON is valid. The double curly braces contain content that is being searched for and replaced in the code.

`{{RockDateTime.Now.Ticks}}`: will be replaced in the code with the appropriate time information and format.

`{{Content}}`: will be replaced with the value of the content field configured column in your CSV.

## Reminders About Attributes

- Person attributes expect the Primary Alias Guid as their value
- Entity attributes will be expecting a Guid not an Id
- Spaces matter in multi-select fields. i.e. A,B,C != A, B, C

## Useful Queries

### Get Primary Alias Guid

Person Ids can be pulled from the URL of their profile, additionally to using SQL you could also make a person attribute that uses lava to get the primary alias guid.

```sql
SELECT TOP 1 Guid FROM PersonAlias WHERE PersonId = CHANGEME
```

```lava
{{Entity.PrimaryAlias.Guid}}
```
