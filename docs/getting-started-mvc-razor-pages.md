# Getting started with Smart Components in MVC / Razor Pages

1. **Create a new ASP.NET Core Web App project (.NET 8) or use an existing one.**

   * Command line: Run `dotnet new mvc` or `dotnet new razor`
   * Visual Studio: Select *File*->*New*->*Project...* then choose *ASP.NET Core Web App (Model-View-Controller or Razor pages)*

1. **Install packages**

   * Install the NuGet package `SmartComponents.AspNetCore`.

     * Command line: `dotnet add package SmartComponents.AspNetCore`
     * Visual Studio: Right-click your project name, choose *Manage NuGet packages...*, and then search for and install `SmartComponents.AspNetCore`.

1. **Register SmartComponents in your application**

   a. In `Program.cs`, under the comment `// Add services to the container`, add:

   ```cs
   builder.Services.AddSmartComponents();
   ```

   b. In your `_ViewImports.cshtml` file (in the `Pages` or `Views` folder), reference the tag helpers:

   ```cshtml
   @addTagHelper *, SmartComponents.AspNetCore
   ```

   c. In your layout file (by default, `_Layout.cshtml` in `Views/Shared` or `Pages/Shared`), just before the closing `</body>` tag, add the following which will load the required JavaScript code:

   ```html
   <smart-components-script />
   ```

1. **Configure the OpenAI backend** (if needed)

   If you will be using either `SmartPaste` or `SmartTextArea`, you need to provide access to a language model backend. See: [Configure the OpenAI backend](configure-openai-backend.md).
   
   If you will only use `SmartComboBox`, you don't need any language model backend and can skip this step.

1. **Add components to your pages**

   You can now add the following inside your Blazor pages/components:

   * [SmartPaste](smart-paste.md)
   * SmartTextArea
   * SmartComboBox
