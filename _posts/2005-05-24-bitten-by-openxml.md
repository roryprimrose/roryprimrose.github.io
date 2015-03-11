---
title: Bitten by OPENXML
date: 2005-05-24 02:02:38 +10:00
---

I have used XML queries in SQL Server for several years now and I am very impressed with the changes to FOR XML in the latest SQL Server 2005 beta. Unfortunately, there doesn't seem to be much changed in the OPENXML arena.

I was working with an OPENXML statement the other week and I was sick of writing WITH clauses to define how to interpret the XML data. I looked at the documentation for the WITH statement and discovered that you can specify an existing table and use its schema to interpret the XML.

This was great news as I shape my XML data in a way that is consistent with the table schema that stores the data in the database. I wanted to just specify the table that the data was related to. This prevents having to manually type the schema in the WITH clause and also means that if the schema of the real table changes, namely field size or type changes, the procedures don't need to be updated unless fields are removed or added.

<!--more-->

Here is where I hit the snag. My tables typically have a primary key that is an INT IDENTITY and WITH clauses will not work with these. What is happening here is that when with WITH clause in OPENXML uses a table scheme, it skips IDENTITY fields when it interprets the XML.

I guess the argument is that INT IDENTITY fields are not modifiable (without intentionally disabling the constraint with SET IDENTITY_INSERT), therefore why allow it to be seen in interpreted XML. The designers must have thought that people might try to insert these values into an IDENTITY field if they allowed us to see the values in the XML. I think they really should have given the developer the choice though. Besides, OPENXML is about reading data, not writing it.

Here is a test script to illustrate what I mean.

{% highlight sql %}
DECLARE @sXML NVARCHAR(4000)
DECLARE @hDoc INT

SET @sXML = '
<Root>
 <Item>
  <Key>1</Key>
  <Test1>A</Test1>
  <Test2>A2</Test2>
 </Item>
 <Item>
  <Key>2</Key>
  <Test1>B</Test1>
  <Test2>B3</Test2>
 </Item>
 <Item>
  <Key>3</Key>
  <Test1>C</Test1>
  <Test2>C4</Test2>
 </Item>
 <Item>
  <Key>4</Key>
  <Test1>D</Test1>
  <Test2>D5</Test2>
 </Item>
</Root>
'

CREATE TABLE #tabletest
(
 [Key] INT IDENTITY(1, 1),
 Test1 NVARCHAR(50),
 Test2 NVARCHAR(50)
)

EXEC sp_xml_preparedocument @hDoc OUTPUT, @sXML

SELECT *
FROM OPENXML (@hDoc, '/Root/Item', 2)
WITH #tabletest

EXEC sp_xml_removedocument @hDoc

DROP TABLE #tabletest

{% endhighlight %}

In this example, only the Test and Test fields are returned. Key, being the INT IDENTITY, is ignored.

You may notice that I used a temporary table in the WITH statement. WITH statements won't work with table variables. My original intention here was to use my permanent table for the WITH clause, but then there is this IDENTITY problem. It is unfortunate that this situation forces more redundant code to be written and code that is not flexible to changing schema's.

The way I write these procedures is to define a temporary table (like the above example) that has the same schema as the permanent table but without the IDENTITY declaration. This means that I can use the temporary table to not only provide a schema for the XML, but also insert the records from OPENXML into that table. This then allows me release the XML document handle and work with normal TSQL statements.&#160;

The way I would use the above code would be something like this:

{% highlight sql %}
DECLARE @sXML NVARCHAR(4000)
DECLARE @hDoc INT

SET @sXML = '
<Root>
 <Item>
  <Key>1</Key>
  <Test1>A</Test1>
  <Test2>A2</Test2>
 </Item>
 <Item>
  <Key>2</Key>
  <Test1>B</Test1>
  <Test2>B3</Test2>
 </Item>
 <Item>
  <Key>3</Key>
  <Test1>C</Test1>
  <Test2>C4</Test2>
 </Item>
 <Item>
  <Key>4</Key>
  <Test1>D</Test1>
  <Test2>D5</Test2>
 </Item>
</Root>
'

CREATE TABLE #tabletest
(
 [Key] INT,
 Test1 NVARCHAR(50),
 Test2 NVARCHAR(50)
)

EXEC sp_xml_preparedocument @hDoc OUTPUT, @sXML

INSERT INTO #tabletest
SELECT *
FROM OPENXML (@hDoc, '/Root/Item', 2)
WITH #tabletest

EXEC sp_xml_removedocument @hDoc

-- Use the records in #tablets
SELECT *
FROM #tabletest

DROP TABLE #tabletest
{% endhighlight %}


