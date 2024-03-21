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

## Using Ollama or another self-hosted endpoint

If you prefer, you can use [Ollama](https://ollama.com/) or any other endpoint that is compatible with the OpenAI API schema.

To do this, add a config value `SelfHosted` with value `true`, and give the URL to your endpoint. For example, to use Ollama locally:

```json
"SmartComponents": {
  "SelfHosted": true,
  "DeploymentName": "mistral:7b", // Or "llama2:13b" etc.
  "Endpoint": "http://localhost:11434/"
}
```

For Ollama, the `DeploymentName` specifies which model to invoke. See [Ollama's list of available models](https://ollama.com/library), and install them by running `ollama pull <modelname>`.

### Model quality

Quality and speed varies dramaticaly from one model to another. **For best results, use GPT 3.5 Turbo or better** - this will respond quickly and at good quality.

Based on experimentation:

 * **GPT 3.5 Turbo** (hosted service) produces good quality results and is fast.
 * **Mistral 7B** produces good output for Smart TextArea, but is inconsistent for Smart Paste (filling some forms, but leaving others blank or introducing strange characters). Output speed is good for a local model, but on most workstations is still way slower than a hosted service.
 * **Llama2 7B** output quality is insufficient (with Smart Paste, it puts strange or hallucinated text into form fields, and with Smart TextArea, it writes generic text that doesn't well account for the configured phrases).
 * **Mixtral 47B** produced good output for Smart TextArea, but wouldn't follow the instructions properly for Smart Paste and hence left forms blank. Additionally it's too big to run on most workstation GPUs so can be impractically slow.

It's possible that specific models or customized prompts (see docs) may behave better, so please let us know if you find ways to improve compatibility or performance with local models!
