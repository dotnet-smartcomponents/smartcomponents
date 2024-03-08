# Configure the OpenAI backend

Do the following in your **server** project:

* Install the package `SmartComponents.Inference.OpenAI`
* In `Program.cs`, update your call to `AddSmartComponents` as follows:

    ```cs
    builder.Services.AddSmartComponents()
        .WithInferenceBackend<OpenAIInferenceBackend>();
    ```

* Configure API keys by adding a block similar to the following at the top level inside `appsettings.Development.json`:

    ```json
    "SmartComponents": {
      "ApiKey": "your key here",
      "DeploymentName": "gpt-3.5-turbo",

      // Required for Azure OpenAI only. If you're using OpenAI, remove the following line.
      "Endpoint": "https://YOUR_ACCOUNT.openai.azure.com/"
    }
    ```

    * To use Azure OpenAI, first [deploy an Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource), then values for `ApiKey`, `DeploymentName`, and `Endpoint` will all be provided to you.

    * Or, to use OpenAI, [create an API key](https://platform.openai.com/api-keys). The value for `DeploymentName` is the model you wish to use (e.g., `gpt-3.5-turbo`, `gpt-4`, etc.). Remove the `Endpoint` line from configuration entirely.