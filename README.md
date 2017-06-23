ASP.NET Core MVC Precompilation
===

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/jx955ph2045dw1w0/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/mvcprecompilation/branch/dev)
Travis:   [![Travis](https://travis-ci.org/aspnet/MvcPrecompilation.svg?branch=dev)](https://travis-ci.org/aspnet/mvcprecompilation)

The Razor syntax provides a fast, terse, clean, and lightweight way to combine server code with HTML to create dynamic web content. This repo contains tooling that allows compilation of MVC Razor views as part of build and publish.

## Installation and usage
Add a package reference to `Microsoft.AspNetCore.Mvc.Razor.ViewCompilation` in your project:

```xml
<ItemGroup>
  <Microsoft.AspNetCore.Mvc.Razor.ViewCompilation Version="1.0.0" />
</ItemGroup>
```

To enable compilation as part of publishing, add the `MvcRazorCompileOnPublish` property:
```xml
<PropertyGroup>
  <MvcRazorCompileOnPublish>true</MvcRazorCompileOnPublish>
</PropertyGroup>
```

Alternatively, you may wire up the `MvcRazorPrecompile` target to a build event:
```xml
 <Target 
    Name="PrecompileRazorViews" 
    AfterTargets="Build"
    DependsOnTargets="MvcRazorPrecompile" />
```

## Options

* `MvcRazorExcludeViewFilesFromPublish `: Enabling `MvcRazorCompileOnPublish` prevents .cshtml files from being published. This option disables this behavior. 
Note: Mvc Core does not support updateable precompiled views. Any modifications to published cshtml files will be ignored if a precompiled view is discovered for that path.

* `MvcRazorExcludeRefAssembliesFromPublish`: Enabling `MvcRazorCompileOnPublish` causes the target to prevent the `refs` directory from being published. This option disables this behavior.
Note: Setting this option is useful if your application is using a mix of precompiled and runtime compiled views.
    
* `MvcRazorFilesToCompile`: An item group that specifies view files to compile. By default this includes all .cshtml files marked as content.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
