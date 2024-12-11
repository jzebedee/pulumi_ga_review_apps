using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(async () =>
{
    var config = new Pulumi.Config();
    var stackName = config.Require("stackName");

    var baseRef = new StackReference(stackName);
    var rgName = (string)await baseRef.RequireValueAsync("rg-review-app:name");
    var rgId = (string)await baseRef.RequireValueAsync("rg-review-app:id");

    var resourceGroup = new ResourceGroup(rgName, new()
    {
        ResourceGroupName = rgName
    }, new()
    {
        ImportId = rgId,
        RetainOnDelete = true
    });

    // Create an Azure resource (Storage Account)
    var storageAccount = new StorageAccount("sa", new StorageAccountArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuArgs
        {
            Name = SkuName.Standard_LRS
        },
        Kind = Kind.StorageV2,
        EnableHttpsTrafficOnly = true
    });

    // Enable static website hosting on the storage account
    var staticWebsite = new StorageAccountStaticWebsite("staticWebsite", new StorageAccountStaticWebsiteArgs
    {
        AccountName = storageAccount.Name,
        ResourceGroupName = resourceGroup.Name,
        IndexDocument = "index.html",
    });

    var (project, stack) = (Pulumi.Deployment.Instance.ProjectName, Pulumi.Deployment.Instance.StackName);

    // Upload the `index.html` file to the Blob Container
    var indexBlob = new Blob("index.html", new()
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name,
        ContainerName = staticWebsite.ContainerName,
        ContentType = "text/html",
        Source = new StringAsset($"<h1>Hello from project {project} on stack {stack}</h1>")
    });

    return new Dictionary<string, object?>() {
        { "static-site-url", storageAccount.PrimaryEndpoints.Apply(e => $"{e.Web}/index.html") }
    };
});