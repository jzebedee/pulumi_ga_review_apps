using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.WebPubSub.Inputs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

return await Pulumi.Deployment.RunAsync(async () =>
{
    var az = new Pulumi.Config("azure-native");

    var subscriptionId = az.Require("subscriptionId");
    var location = az.Require("location");

    // base
    var base_mi = await BuildBaseManagedIdentity(subscriptionId, location);

    return new Dictionary<string, object?>
    {
        ["mi-shared-review:principalId"] = base_mi.PrincipalId
    };
});

static async Task<UserAssignedIdentity> BuildBaseManagedIdentity(string subscriptionId, string location)
{
    string identityName = "mi-shared-review";

    var managedIdentity = new UserAssignedIdentity(identityName, new()
    {
        Location = location,
        ResourceGroupName = "rg-shared-review",
        ResourceName = "mi-shared-review",
    });

    const string roleDefinitionId_managedIdentityContributor = "e40ec5ca-96e0-45a2-b4ff-59039f2c2b59";

    string ra_name = $"ra-managedIdentityContributor_{identityName}";
    string ra_roleDefId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId_managedIdentityContributor}";

    var roleAssignment = new RoleAssignment(ra_name, new()
    {
        PrincipalId = managedIdentity.PrincipalId,
        PrincipalType = PrincipalType.ServicePrincipal,
        Scope = $"/subscriptions/{subscriptionId}",
        RoleDefinitionId = ra_roleDefId,
    });

    return managedIdentity;
}