﻿using Pulumi;
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
        Kind = Kind.StorageV2
    });

    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = resourceGroup.Name,
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