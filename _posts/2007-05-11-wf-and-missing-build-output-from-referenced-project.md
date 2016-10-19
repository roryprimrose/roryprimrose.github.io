---
title: WF and missing build output from referenced project
categories: .Net
tags: WF
date: 2007-05-11 10:08:38 +10:00
---

Ever come across a WorkflowValidationFailedException at runtime even though the project containing the workflow was successfully validated and compiled without any errors? The reason for this occurring is how the compiler manages references. 

Lets look at a simple solution that contains ProjectA, ProjectB and ProjectC. ProjectA references ProjectB which then references ProjectC. 

ProjectA contains a form that references ClassB in ProjectB like this:

<!--more-->

```csharp
using System;
using System.Diagnostics;
using System.Windows.Forms;
using ProjectB;
     
namespace ProjectA
{
    public partial class FormA : Form
    {
        public FormA()
        {
            InitializeComponent();
        }
     
        private void button1_Click(object sender, EventArgs e)
        {
            ClassB TestB = new ClassB();
     
            Debug.WriteLine(TestB.CreateValue());
        }
    }
}
    
```

ProjectB contains ClassB that references ClassC in ProjectC like this:

```csharp
using System;
using ProjectC;
     
namespace ProjectB
{
    public class ClassB
    {
        public String CreateValue()
        {
            ClassC TestC = new ClassC();
     
            return TestC.RunTest();
        }
    }
}
    
```

ClassC in ProjectC looks like this:

```csharp
using System;
     
namespace ProjectC
{
    public class ClassC
    {
        public String RunTest()
        {
            return Guid.NewGuid() + " - " + DateTime.Now;
        }
    }
}
    
```

When ProjectA compiles, the build output in bin\Debug is the following:

ProjectA.exe         
ProjectB.dll         
ProjectC.dll

Everything is good here and ProjectA will execute successfully.

Now lets simulate a scenario that WF can bring into the mix. Lets say that ProjectB contains a workflow. This workflow ends up executing a rule set, probably through a PolicyActivity. A rule in the rule set makes a reference to ClassC in ProjectC. Nowhere else in ProjectB references any type defined in ProjectC. What happens? The result can be simulated by making ClassB in ProjectB look like this:

```csharp
using System;
using ProjectC;
     
namespace ProjectB
{
    public class ClassB
    {
        public String CreateValue()
        {
            //ClassC TestC = new ClassC();
     
            //return TestC.RunTest();
     
            return "Break me";
        }
    }
}
    
```

When ProjectA compiles, the build output in bin\Debug is the following:

ProjectA.exe         
ProjectB.dll

Traditionally, this is really great. The compiler is smart enough to know that even though ProjectB references ProjectC, it doesn't actually reference any types defined in ProjectC, so why pull across its build output. That would just be unnecessary bloat. 

How does this relate to WF? Well, rules are defined in XML and CodeDOM is used to interpret that XML into code that will be executed. This means that at compile time, the type references don't exist as far as the compiler is concerned. The more WF is used, the more likely it will become that types and their projects are only referenced through rules therefore their build output will not be available at runtime. This results in the WorkflowValidationFailedException at runtime.

The solutions to this problem with VS2005 are to either make a reference of ProjectC in ProjectA (first level references are always pulled along), or to make a reference to a ProjectC type in code in somewhere in ProjectB. Hopefully Orcas will address this issue (I haven't checked yet).


