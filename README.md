Snout
=====
- the fluent interface generator for .NET

**NOTE** Snout is very much in an alpha state, and if you intend to use it, it's pretty much at your own risk. It's running code right now, but 
it generates unweildy code and I'm experimenting to see if this stuff can be made context free, because that would be awesome. At least the way
you define your grammar will change from what there is now.

What is Snout?
--------------

Snout is a tool for making it easier to write and maintain fluent interfaces to your code. A fluent interface is a variety of domain specific
language which makes your code read in the manner similar to the english language. Here's an example

```csharp
var pen = new ShapeBuilder();

pen.AddCircle().WithRadius(10)
   .AddRectangle().Width(10).Height(10)
   .AddPolygon().Point(1,2).Point(2,9).Point(4,8);

```

These sorts of interfaces are easy to use, but they are very hard to write and a pain to maintain, since introducing new options will invariably result
in having to rewrite a lot of interfaces to accomodate a new option. Snout can help you with this!

The Builder pattern
-------------------

Snout is based on the concept of builders. A builder is a class which is responsible for configuring an object by calling methods in the correct sequence.
For instance, in the above example a builder might look like this:

```csharp
class ShapeBuilder
{
	void AddCircle() { ... }
	void SetCircleRadius(int radius) { ... }

	void AddRectangle() { ... }
	void SetRectangleWidth(int width) { ... }
	void SetRectangleHeight(int height) { ... }

	void AddPolygon() { ... }
	void AddPolygonPoint(int x, int y) { ... }
}
```

Obviously, this interface is pretty unsafe to use. What if you call AddCircle and then AddPolygonPoint? Bad things. Snout helps you by constructing a layer on
top of this to provide a safe and easy syntax.

Using snout
-----------

Snout works by reflection and using the generated XML documentations for your assemblies. To generate a fluent interface for the builder that matches the first
example, you add documentation to your methods and class. The extra annotations are added in addition to your normal documentation. Your documentation will be
transferred to the generated builder code (except for the extra snout specific tags).

An example first, explanations below:

```csharp
/// <summary>...</summary>
/// <builderclass name="ShapeSyntax" namespace="Snout.Example">
/// (AddCircle WithRadius | AddRectangle Width Height | AddPolygon Point+ ) +
/// </builderclass>
class ShapeBuilder
{
	/// <summary>Add a circle</summary>
	/// <buildermethod/>
	void AddCircle() { ... }

    /// <summary>Set the circle radius</summary>
	/// <param name="radius">Circle radius</param>
	/// <buildermethod name="WithRadius"/>
	void SetCircleRadius(int radius) { ... }

	// Omitted summary and params for brevity on the rest of the 
	// methods.

	/// <buildermethod/>
	void AddRectangle() { ... }

	/// <buildermethod name="Width"/>
	void SetRectangleWidth(int width) { ... }

	/// <buildermethod name="Height"/>
	void SetRectangleHeight(int height) { ... }

	/// <buildermethod/>
	void AddPolygon() { ... }

	/// <buildermethod name="Point"/>
	void AddPolygonPoint(int x, int y) { ... }
}
```

&lt;builderclass&gt;
--------------------

builderclass is used to specify a class to generate a DSL from. Apply this to your class.

## Attributes 
### name
Name specifies the name of the generated classes of your fluent interface. The initial class will always be named the 
exact value of name. Further classes will have derivative names. This name will also be used to name the output file
when executing Snout.

### namespace
Namespace of the generated classes.

## Content

Content of this node is a regular grammar, defined as a regular expression of sorts. Instead of letters you use the
symbols that determined by the dslname of the buildermethod tags. If dslname is not specified, use the name attribute.
If name isn't specified use the name of the commented methods.

### Operators ###

Similar to regular expressions you can use operators. They act the same way as you'd expect and the priorities are
the same as using regular expressions. Supported operators are:

- + Repeat once or more time
- ? Repeat zero or once
- * Repeat zero or more
- | Alternation, use either side of the operator.
- () Parenthesis are used just as with regular expressions. There is no concept of capture groups, just use normal undecorated parenthesis.

Whitespace is ignored. You can use c-style comments.

&lt;buildermethod&gt;
---------------------

Buildermethod is used to specify that a method is to be available to the generated fluent interface. Apply to method
documentation.

## Attributes
### name
This is the name that your buildermethod will be exposed as in the fluent configuration. This enables you to use
different naming terminologies in your builder than in your fluent interface. If this attribute is not specified
the name of the method will be used. This needs to be a valid C# method name, and if dslname isn't specified
it must be unique within the class scope regardless of parameters.

### dslname
Dslname is used to specify a different name for use in the fluent configuration content in the builderclass 
tag. This becomes neccessary if you intend to overload some of your methods, since you cannot specify parameters
while building your fluent grammar. If not set, this will default to the value of the name parameter if specified
or, if name is not specified, the name of the method.

### useproperty
Normally Snout will generate properties for all methods which have no parameters and have no generic parameters instead
of method calls. Set this attribute to false if you do not desire this behaviour.

Running Snout
=============

Snout is a console application. Invoke normally using the command prompt of your choice.

<pre>
Usage
  -a, --assembly=VALUE       Path to assembly to use
  -d, --doc=VALUE            Path to documentation to use
  -o, --output=VALUE         Output path
</pre>

Only assembly is required. If doc is not specified it will search in the same folder as the assembly. If output isn't specified
Snout will output files in the current directory.

More examples
=============
Snout has been developed primarily as a method of maintaining and developing the fluent interfaces for Piglet, the fluent parser
generator. In this function, it has seen a bit of use and viewing the sources of Piglet (more specifically, the RuleBuilder class)
is helpful. In fact, it is Piglet which powers Snout, making a fun little circle of life :)

Bug tracker
-----------

Please create an issue here on GitHub for any issue you might have. Or just fork it and fix stuff as you wish, that's what open 
source is for.

https://github.com/Dervall/Snout/issues

Authors
-------

**Per Dervall**
+ http://twitter.com/dervall
+ http://binarysculpting.com

Copyright and license
---------------------

Snout is licenced under the MIT license. Refer to LICENSE.txt for more information.