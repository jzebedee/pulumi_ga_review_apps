using Pulumi;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Pulumi.Config();
    var stackName = config.Require("stackName");

    var baseRef = new StackReference(stackName);
    var rgName = baseRef.RequireOutput("rg-review-app:name").Apply<string>(o => o.ToString());

    // Create an Azure resource (Storage Account)
    var storageAccount = new StorageAccount("sa", new StorageAccountArgs
    {
        ResourceGroupName = rgName,
        Sku = new SkuArgs
        {
            Name = SkuName.Standard_LRS
        },
        Kind = Kind.StorageV2
    });

    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = rgName,
        AccountName = storageAccount.Name
    });

    var primaryStorageKey = storageAccountKeys.Apply(accountKeys =>
    {
        var firstKey = accountKeys.Keys[0].Value;
        return Output.CreateSecret(firstKey);
    });

    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});