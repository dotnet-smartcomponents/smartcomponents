# Smart TextArea

Smart TextArea is an AI upgrade to the traditional textarea. It provides suggested autocompletions to whole sentences based on its configuration and what the user is currently typing.

### Example use cases

 * **Customer service**

   Your app might allow agents to respond to customer/staff/user messages by typing free text, e.g., in a live chat system, support ticket system, CRM, bug tracker, etc. You can use Smart TextArea to help those agents be more productive, writing better-phrased responses with fewer keystrokes.

   The key benefit of Smart TextArea (compared with an OS's built-in autocomplete) is that you can configure a house style and a set of predefined phrases, rules, policies, URLs, etc. This sets the tone, level of formality, greetings, etc. that you prefer agents to use, while not forcing them to do so.
   
   For example if an HR agent types "**Your vacation allowance is**", the system might suggest a completion like "**28 days as detailed in our policy at https://.../policies/vacation**". Or, if configured for a Wild West-themed restaurant's booking enquiries chat, a staff member typing "**Thanks for**" might be offered the completion "**chattin' to us, Sherrif!**"

 * **Business process tracking**

   Many processes require tracking events and status using free text. Smart TextArea can reduce the number of keystrokes needed to refer to industry or business-specific terms and events.

   For example, a car servicing log system might offer to complete "**Tyre pre**" as "**Tyre pressure checked and is**", or to complete "**Next s**" as "**Next service due at**", if any variation of these is configured as a preferred phrase.

## Adding Smart TextArea in Blazor

First, make sure you've followed the [Smart Component installation steps for Blazor](getting-started-blazor.md). This includes [configuring an OpenAI backend](configure-openai-backend.md), which is a prerequisite for Smart TextArea.

Then, in a `.razor` file, add the `<SmartTextArea>` component. Example:

```razor
@page "/"
@using SmartComponents

<SmartTextArea @bind-Value="@text" UserRole="Generic professional" />

@code {
    string? text; // Optionally, set an initial value here
}
```

The `UserRole` and `@bind-Value` attributes are both required. See below for how to customize the suggestions.

`SmartTextArea` integrates with Blazor's binding, forms, and validation systems in exactly the same way as `InputTextArea`.

## Adding Smart TextArea in MVC / Razor Pages

First, make sure you've followed the [Smart Component installation steps for MVC/Razor Pages](getting-started-mvc-razor-pages.md). This includes [configuring an OpenAI backend](configure-openai-backend.md), which is a prerequisite for Smart TextArea.

Then, in a `.cshtml` file, add the `<smart-text-area>` tag. Example:

```cshtml
<smart-textarea user-role="Generic professional" />
```

The `user-role` attribute is required. See below for how to customize the suggestions.

## Styling the textarea

Smart Textarea renders as a simple HTML `<textarea>` element. You can style it by adding any CSS class names or other HTML attributes that apply to `<textarea>`.

Examples:

 * Blazor:
   ```razor
   <SmartTextArea class="my-textarea" placeholder="Type here..."
                  rows="10" cols="80" ... />
   ```
 * MVC/Razor Pages:
   ```cshtml
   <smart-textarea class="my-textarea" placeholder="Type here..."
                   rows="10" cols="80" ... />
   ```

### Using scoped CSS

If you are using scoped CSS (i.e., a `.razor.css` or `.cshtml.css` file), remember to use the `::deep` pseudoselector to match the textarea, since it is being rendered in a child context. For example:

```css
::deep .my-textarea { /* ... */ }
```

## Customizing the suggestions

Under the default prompt, the language model will suggest completions based on:

 * The existing text before and after the cursor
 * The `UserRole`/`user-role` value
 * The `UserPhrases`/`user-phrases` value

You should set a **user role** to be a `string` that describes who is typing and for what reason, optionally giving other contextual information. Examples:

 * `"An HR agent replying to enquiries from staff"`
 * `"An open-source project owner replying to a GitHub issue"`
 * `"An open-source project owner replying to a GitHub issue posted by @SomeUser with the title 'Build fails after upgrading to v3'"`

You should set **user phrases** to be a `string[]` (array) that helps the language model reply using your preferred tone/voice, common phrases, and give any information you wish about policies, URLs, or anything else that may be relevant to incorporate into the suggested completions.

#### Reducing invented information

Don't configure user phrases like `"Tyre pressure is 35psi"`, because then the system will suggest 35psi for all tyre pressure-related completions. Instead, use the special token `NEED_INFO`, e.g., `"Tyre pressure is NEED_INFO"`. The language model will terminate the completion whenever it hits that special token, so the completion will appear as `"Tyre pressure is "`, allowing the user to fill in the value.

Language models are innately unpredictable, so you may have to experiment to find the most useful phrases. Even when you use `NEED_INFO`, it may still sometimes invent unwanted information. If needed, you can customize the underlying prompt to tune it more to your scenarios - see below.

#### Blazor example

```razor
<SmartTextArea @bind-Value="@text" UserRole="@userRole" UserPhrases="@userPhrases" />

@code {
    string? text;

    string userRole = "Staff at a wild-west themed restaurant called 'The Wild Brunch', replying to a booking enquiry";

    string[] userPhrases = [
        "Yee-haw!",
        "So you wanna mosey on down for a bite?",
        "Why sure, we can cook you up somethin' special",
        "We're open everyday except Tuesdays (that's when we're at the rodeo)",
        "Sorry pardner, even if you're the sherrif, we gotta stick to our policy!",
        "Can I book you in for NEED_INFO",
        "How can I help you, kind stranger?",
        "Our website is at https://wildbrunch.example.com/"
    ];
}
```

#### MVC/Razor Pages example

```cshtml
@{
    ViewData["Title"] = "Home Page";

    string userRole = "Maintainer of this open-source project replying to GitHub issues";
    string[] userPhrases = [
        "Thankyou for contacting us.",
        "To investigate, we'll need a repro as a public Git repo.",
        "Could you please post a screenshot of NEED_INFO",
        "This sounds like a usage question. This issue tracker is intended for bugs and feature proposals. Unfortunately we don't have capacity to answer general usage questions and would recommend StackOverflow for a faster response.",
        "We don't accept ZIP files as repros.",
    ];
}

<smart-textarea user-role="@userRole" user-phrases="@userPhrases" />
```

## Controlling the suggestion UX

Suggestions appear when the textarea is focused, after the user has paused typing.

 * **On non-touch devices (desktop)**, by default the suggestion appears inline inside the textarea, in grey text ahead of the cursor. The user may accept the suggestion by pressing "tab".
 * **On touch devices (mobile)**, by default the suggestion appears in a floating overlay below the cursor. The user may accept the suggestion by tapping the suggestion overlay (or by pressing "tab" if they have such a key, though most mobile users do not).

If you want to override the default UI style, you can pass the attribute `data-inline-suggestions`.

 * Blazor: `<SmartTextArea data-inline-suggestions="true" ... />`
 * MVC/Razor Pages: `<smart-text-area data-inline-suggestions="true" ... />`

| Attribute value | Behavior |
| --- | --- |
| `true` | Suggestions are always shown inline, and can be accepted by pressing "tab". |
| `false` | Suggestions are always shown as a floating overlay that can be tapped or clicked. |
| Not set | Touch devices use overlay; non-touch devices use inline. |

## Customizing the AI inference

Smart Textarea contains logic that generates a prompt for the language model, but you can customize it.

### Customizing the prompt

You can customize the prompt by registering your own subclass of `SmartTextAreaInference` as a DI service. For example, define this class:

```cs
class MySmartTextAreaInference : SmartTextAreaInference
{
    public override ChatParameters BuildPrompt(
        SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var prompt = base.BuildPrompt(config, textBefore, textAfter);

        prompt.Messages!.Add(new(ChatMessageRole.System,
            "The suggestions must ALWAYS BE IN ALL CAPS."));

        prompt.Temperature = 0.5f; // Less deterministic, more creative (default = 0)

        return prompt;
    }
}
```

... and then in `Program.cs`, *before* the call to `builder.Services.AddSmartComponents`, register it:

```cs
builder.Services.AddSingleton<SmartTextAreaInference, MySmartTextAreaInference>();
```

Using the debugger and a breakpoint inside your `BuildPrompt` method, you can inspect the default prompt and parameters, and write code that modifies it in any way.

### Customizing the language model backend

If you want to use a backend other than OpenAI or Azure OpenAI, you can implement `IInferenceBackend` and use your custom type in `Program.cs`:

```cs
builder.Services.AddSmartComponents()
    .WithInferenceBackend<MyCustomInferenceBackend>();

class MyCustomInferenceBackend : IInferenceBackend { ... }
```
