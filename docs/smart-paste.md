# Smart Paste

Smart Paste is an intelligent app feature that fills out forms automatically using data from the user's clipboard. You can use this with any existing form in your web app.

### Example use cases

 * **Mailing address form**
 
   A user could copy a whole mailing address from an email or Word document, and then click "Smart Paste" in your application to populate all the address-related fields in a form (name, line 1, line 2, city, state, etc.).
 
   This reduces the workload on your user, because they don't have to type out each field manually, or separately copy-and-paste each field.

 * **Bug tracking form**
 
   A user could copy a short natural-language description of a problem (perhaps sent to them via IM/Teams), and then click "Smart Paste" inside your "Create New Issue" page. This would populate fields like "Title", "Severity", "Repro steps", etc., based on the clipboard text.

   The language model will automatically rephrase the source text as needed. For example, it would convert phrases like "I just clicked X on screen Y" to repro steps like "1. Go to screen Y, 2. Click X.".

Smart Paste is designed to work with any form. You don't have to configure or annotate your forms, since the system will infer the meanings of the fields from your HTML. You can optionally provide annotations if it helps to produce better results.

## Adding Smart Paste in Blazor

First, make sure you've followed the [Smart Component installation steps for Blazor](getting-started-blazor.md). This includes [configuring an OpenAI backend](configure-openai-backend.md), which is a prerequisite for Smart Paste.

Then, in a `.razor` file, inside any `<form>` or `<EditForm>`, add the `<SmartPasteButton>` component. Example:

```razor
@page "/"
@using SmartComponents

<form>
    <p>Name: <InputText @bind-Value="@name" /></p>
    <p>Address line 1: <InputText @bind-Value="@addr1" /></p>
    <p>City: <InputText @bind-Value="@city" /></p>
    <p>Zip/postal code: <InputText @bind-Value="@zip" /></p>

    <button type="submit">Submit</button>
    <SmartPasteButton DefaultIcon />
</form>

@code {
    string? name, addr1, city, zip;
}
```

Now when this app is run, you can copy a mailing address to your clipboard from some other application, and then click the "Smart Paste" button to fill out all the corresponding form fields.

Note: this example is only intended to show `SmartPasteButton`. This form won't do anything useful if submitted - see [Blazor docs for more about form submissions](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms).

## Adding Smart Paste in MVC / Razor Pages

First, make sure you've followed the [Smart Component installation steps for MVC/Razor Pages](getting-started-mvc-razor-pages.md). This includes [configuring an OpenAI backend](configure-openai-backend.md), which is a prerequisite for Smart Paste.

Then, in a page/view `.cshtml` file, inside any `<form>`, add a `<smart-paste-button>` tag. Example:

```cshtml
<form>
    <p>Name: <input name="name" /></p>
    <p>Address line 1: <input name="addr1" /></p>
    <p>City: <input name="city" /></p>
    <p>Zip/postal code: <input name="zip" /></p>

    <button type="submit">Submit</button>
    <smart-paste-button default-icon />
</form>
```

Now when this app is run, you can copy a mailing address to your clipboard from some other application, and then click the "Smart Paste" button to fill out all the corresponding form fields.

## Customizing the text label

To customize the text label, pass arbitrary child content, e.g.:

 * Blazor:
   ```razor
   <SmartPasteButton>Smart paste, baby!</SmartPasteButton>
   ```
 * MVC/Razor Pages:
   ```cshtml
   <smart-paste-button>Smart paste, baby!</smart-paste-button>
   ```

You can use this to specify localized text, or any other button content such as a custom icon.

## Styling the button

The smart paste button renders as a simple HTML `<button>` element. You can style it by adding any CSS class names or other HTML attributes that apply to `<button>`.

By default, the smart paste button gives itself the CSS class name `smart-paste-button`. During the period when smart paste is in progress (waiting for a server response), the button will apply the `disabled` attribute to itself. So, you can apply styles in CSS as follows:

```css
/* Applies to all smart paste buttons all the time */
.smart-paste-button { /* your styles here */ }

/* Applies to smart paste buttons while they are waiting for a server response */
.smart-paste-button:disabled { /* your styles here */ }
```

You can override the default CSS class name `smart-paste-button` by setting an explicit `class` attribute on the `<SmartPasteButton>` or `<smart-paste-button>` tag.

### Using scoped CSS

If you are using scoped CSS (i.e., a `.razor.css` or `.cshtml.css` file), remember to use the `::deep` pseudoselector to match the button, since it is being rendered in a child context. For example:

```css
::deep .smart-paste-button { /* ... */ }
::deep .smart-paste-button:disabled { /* ... */ }
```

## Rendering an icon

You can optionally have the button render a default "smart paste" icon inside itself:

 * Blazor: `<SmartPasteButton DefaultIcon />`
 * MVC/Razor Pages: `<smart-paste-button default-icon />`

This will render as an inline `<svg class='smart-paste-icon'>` element within the button. You can use the `smart-paste-icon` CSS class name to apply additional styling rules to the icon.

If you want to use a custom icon, then instead of setting `DefaultIcon` or `default-icon`, simply supply your icon as child content:

 * Blazor:
   ```razor
   <SmartPasteButton>
       <svg>...</svg> <!-- Can be inline or use a 'src' attribute -->
       Click me now!
   </SmartPasteButton>
   ```
 * MVC/Razor Pages:
   ```html
   <smart-paste-button>
       <svg>...</svg> <!-- Can be inline or use a 'src' attribute -->
       Click me now!
   </smart-paste-button>
   ```

You can supply any child content that can be rendered inside a `<button>`, as well as any other attributes that make sense on a `<button>`.

## Customizing the AI inference

By default, Smart Paste infers the meanings of your form fields automatically, and has a built-in prompt that instructs the language model. You can customize either of these.

### Annotating your form fields

By default, Smart Paste finds all the fields in your `<form>` (i.e., `<input>`, `<select>`, and `<textarea>` elements), and generates a description for them based on their associated `<label>`, or their `name` attribute, or nearby text content. This whole form descriptor is then supplied to the backend language model and is used to build a prompt.

You can optionally override this on specific form fields. To do so, add a `data-smartpaste-description` attribute that sets a field description to supply to the language model. Examples:

```html
<input data-smartpaste-description="The user's vehicle registration number which must be in the form XYZ-123" />

<textarea data-smartpaste-description="The job description which must start with JOB TITLE in all caps, and then contain one paragraph"></textarea>

<input type="checkbox" data-smartpaste-description="True if the product description indicates this is for children, otherwise False" />
```

Language models outputs vary, and prompt engineering is a skill in its own right. You may have to experiment to get the best results in your scenario.

### Customizing the prompt

You can customize the prompt by registering your own subclass of `SmartPasteInference` as a DI service. For example, define this class:

```cs
class MySmartPasteInference : SmartPasteInference
{
    public override ChatParameters BuildPrompt(SmartPasteRequestData data)
    {
        var prompt = base.BuildPrompt(data);
        prompt.Messages!.Add(new ChatMessage(ChatMessageRole.System,
            "All form field values must be in ALL CAPS"));

        prompt.Temperature = 0.5f; // Less deterministic, more creative
        return prompt;
    }
}
```

... and then in `Program.cs`, *before* the call to `builder.Services.AddSmartComponents`, register it:

```cs
builder.Services.AddSingleton<SmartPasteInference, MySmartPasteInference>();
```

Using the debugger and a breakpoint inside your `BuildPrompt` method, you can inspect the default prompt and parameters, and write code that modifies it in any way.

### Customizing the language model backend

If you want to use a backend other than OpenAI or Azure OpenAI, you can implement `IInferenceBackend` and use your custom type in `Program.cs`:

```cs
builder.Services.AddSmartComponents()
    .WithInferenceBackend<MyCustomInferenceBackend>();

class MyCustomInferenceBackend : IInferenceBackend { ... }
```
